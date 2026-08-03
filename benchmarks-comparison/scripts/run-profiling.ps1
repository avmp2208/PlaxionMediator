<#
.SYNOPSIS
    Collects CPU (.nettrace + speedscope) and memory (.gcdump) snapshots for the
    Plaxion / MediatR / Mediator profiling comparison harness.

.DESCRIPTION
    Builds the profiling console app, ensures dotnet-trace and dotnet-gcdump are
    installed, then loops over framework x scenario combinations. Each combo is
    launched under `dotnet-trace collect` (cpu-sampling + GC events). A mid-run
    `dotnet-gcdump collect` captures a heap snapshot against the same process.

.PARAMETER DurationSeconds
    How long each scenario loop runs (default 4).

.PARAMETER Frameworks
    Subset of frameworks to profile. Default: all three.

.PARAMETER Scenarios
    Subset of scenarios to profile. Default: all five.

.PARAMETER SkipBuild
    Skip the Release build step.

.PARAMETER SkipGcDump
    Skip gcdump collection (CPU-only).

.PARAMETER ResultsRoot
    Output root. Default: benchmarks-comparison/profiling-results
#>
[CmdletBinding()]
param(
    [double]$DurationSeconds = 4,
    [string[]]$Frameworks = @('Plaxion', 'MediatR', 'Mediator'),
    [string[]]$Scenarios = @('Send0', 'Send5', 'Send10', 'Send20', 'TypeVariety50'),
    [switch]$SkipBuild,
    [switch]$SkipGcDump,
    [string]$ResultsRoot = ''
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ComparisonRoot = Split-Path -Parent $ScriptDir
$ProjectPath = Join-Path $ComparisonRoot 'src\Plaxion.BenchMarks.Comparison.Profiling\Plaxion.BenchMarks.Comparison.Profiling.csproj'

if ([string]::IsNullOrWhiteSpace($ResultsRoot)) {
    $ResultsRoot = Join-Path $ComparisonRoot 'profiling-results'
}

function Write-Step([string]$Message) {
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Ensure-GlobalTool([string]$ToolName, [string]$PackageId) {
    $existing = & dotnet tool list -g 2>$null | Select-String -SimpleMatch $PackageId
    if ($existing) {
        Write-Host "  $ToolName already installed"
        return
    }

    Write-Host "  Installing $PackageId ..."
    & dotnet tool install --global $PackageId
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to install global tool $PackageId"
    }
}

function Resolve-DllPath {
    $candidates = @(
        (Join-Path $ComparisonRoot 'src\Plaxion.BenchMarks.Comparison.Profiling\bin\Release\net9.0\Plaxion.BenchMarks.Comparison.Profiling.dll'),
        (Join-Path $ComparisonRoot 'src\Plaxion.BenchMarks.Comparison.Profiling\bin\Release\net9.0\win-x64\Plaxion.BenchMarks.Comparison.Profiling.dll')
    )
    foreach ($c in $candidates) {
        if (Test-Path $c) { return $c }
    }
    throw "Profiling DLL not found after build. Looked in: $($candidates -join '; ')"
}

Write-Step "Ensuring diagnostics global tools"
Ensure-GlobalTool -ToolName 'dotnet-trace' -PackageId 'dotnet-trace'
Ensure-GlobalTool -ToolName 'dotnet-gcdump' -PackageId 'dotnet-gcdump'

if (-not $SkipBuild) {
    Write-Step "Building profiling project (Release)"
    & dotnet build $ProjectPath -c Release --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }
}

$dll = Resolve-DllPath
Write-Host "  DLL: $dll"

New-Item -ItemType Directory -Force -Path $ResultsRoot | Out-Null
$manifestPath = Join-Path $ResultsRoot 'manifest.json'
$manifest = [ordered]@{
    generatedUtc = (Get-Date).ToUniversalTime().ToString('o')
    durationSeconds = $DurationSeconds
    machine = $env:COMPUTERNAME
    combos = @()
}

$total = $Frameworks.Count * $Scenarios.Count
$index = 0

foreach ($framework in $Frameworks) {
    foreach ($scenario in $Scenarios) {
        $index++
        $label = "${framework}_${scenario}"
        $outDir = Join-Path $ResultsRoot "$framework\$scenario"
        New-Item -ItemType Directory -Force -Path $outDir | Out-Null

        $cpuTrace = Join-Path $outDir "${label}_cpu.nettrace"
        $speedscope = Join-Path $outDir "${label}_speedscope.json"
        $gcDump = Join-Path $outDir "${label}.gcdump"
        $stdoutLog = Join-Path $outDir "${label}_stdout.txt"
        $metaPath = Join-Path $outDir "${label}_meta.json"

        Write-Step "[$index/$total] Profiling $framework / $scenario"

        # Remove prior artifacts for this combo so re-runs are clean.
        foreach ($f in @($cpuTrace, $speedscope, $gcDump, $stdoutLog, $metaPath)) {
            if (Test-Path $f) { Remove-Item -Force $f }
        }

        $appArgs = @(
            'exec', $dll,
            '--framework', $framework,
            '--scenario', $scenario,
            '--duration-seconds', "$DurationSeconds",
            '--ready-signal'
        )

        # Windows: use dotnet-sampled-thread-time (cpu-sampling is Linux-only in newer dotnet-trace).
        # Combine with gc-verbose for allocation tick events in the same .nettrace.
        $traceArgs = @(
            'collect',
            '--profile', 'dotnet-sampled-thread-time,gc-verbose',
            '--format', 'Speedscope',
            '--show-child-io',
            '-o', $cpuTrace,
            '--',
            'dotnet'
        ) + $appArgs

        $comboStart = Get-Date
        $traceOk = $false
        $gcOk = $false
        $convertOk = $false
        $appExit = $null
        $iterations = $null
        $opsPerSec = $null

        # Start gcdump helper job that waits for READY line then dumps mid-run.
        $gcdumpJob = $null
        if (-not $SkipGcDump) {
            $gcdumpJob = Start-Job -ScriptBlock {
                param($OutDump, $DurationSeconds)
                $deadline = (Get-Date).AddSeconds([Math]::Max($DurationSeconds + 30, 45))
                $targetPid = $null

                while ((Get-Date) -lt $deadline -and -not $targetPid) {
                    try {
                        $procs = Get-CimInstance Win32_Process -Filter "Name = 'dotnet.exe'" -ErrorAction SilentlyContinue
                        foreach ($p in $procs) {
                            if ($p.CommandLine -and $p.CommandLine -match 'Plaxion\.BenchMarks\.Comparison\.Profiling') {
                                $targetPid = $p.ProcessId
                                break
                            }
                        }
                    } catch { }
                    Start-Sleep -Milliseconds 200
                }

                if (-not $targetPid) {
                    return @{ ok = $false; reason = 'process-not-found' }
                }

                # Wait until roughly mid-run so the heap reflects steady-state dispatch.
                Start-Sleep -Seconds ([Math]::Max([int]($DurationSeconds / 2), 1))

                try {
                    $null = & dotnet-gcdump collect -p $targetPid -o $OutDump 2>&1
                    $code = $LASTEXITCODE
                    return @{ ok = ($code -eq 0 -and (Test-Path $OutDump)); pid = $targetPid; exitCode = $code }
                } catch {
                    return @{ ok = $false; reason = $_.Exception.Message; pid = $targetPid }
                }
            } -ArgumentList $gcDump, $DurationSeconds
        }

        Write-Host "  Running dotnet-trace collect ..."
        $prevEap = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        try {
            $traceOutput = & dotnet-trace @traceArgs 2>&1 | Tee-Object -FilePath $stdoutLog
            $appExit = $LASTEXITCODE
        } catch {
            $traceOutput = @($_.Exception.Message)
            $appExit = 1
        } finally {
            $ErrorActionPreference = $prevEap
        }
        $traceOk = (Test-Path $cpuTrace)

        if ($traceOk) {
            # collect --format Speedscope emits a sibling *.speedscope.json next to the nettrace.
            $autoSpeedscope = [System.IO.Path]::ChangeExtension($cpuTrace, $null) + 'speedscope.json'
            if (-not (Test-Path $speedscope)) {
                if (Test-Path $autoSpeedscope) {
                    Move-Item -Force $autoSpeedscope $speedscope
                } else {
                    $alt = Get-ChildItem -Path $outDir -Filter '*speedscope*.json' -ErrorAction SilentlyContinue | Select-Object -First 1
                    if ($alt) {
                        Move-Item -Force $alt.FullName $speedscope
                    } else {
                        Write-Host "  Converting to speedscope via dotnet-trace convert ..."
                        & dotnet-trace convert $cpuTrace --format Speedscope -o $speedscope 2>&1 | Out-Null
                        if (-not (Test-Path $speedscope)) {
                            $alt2 = Get-ChildItem -Path $outDir -Filter '*speedscope*.json' -ErrorAction SilentlyContinue | Select-Object -First 1
                            if ($alt2) { Move-Item -Force $alt2.FullName $speedscope }
                        }
                    }
                }
            }
            $convertOk = Test-Path $speedscope
        } else {
            Write-Warning "  Trace collection failed for $label (exit=$appExit)"
            if ($traceOutput) {
                $traceOutput | Select-Object -Last 20 | ForEach-Object { Write-Host "    $_" }
            }
        }

        if ($gcdumpJob) {
            $gcResult = Receive-Job -Job $gcdumpJob -Wait -AutoRemoveJob
            if ($gcResult -and $gcResult.ok) {
                $gcOk = $true
                Write-Host "  gcdump captured (pid=$($gcResult.pid))"
            } else {
                $reason = if ($gcResult) { $gcResult.reason } else { 'unknown' }
                Write-Warning "  gcdump not captured for $label ($reason)"
            }
        }

        # Parse iteration stats from harness stdout (child IO and/or log file).
        $doneCandidates = @()
        if ($traceOutput) { $doneCandidates += @($traceOutput | ForEach-Object { "$_" }) }
        if (Test-Path $stdoutLog) { $doneCandidates += Get-Content -LiteralPath $stdoutLog -ErrorAction SilentlyContinue }
        $doneLine = $doneCandidates | Where-Object { $_ -match 'DONE iterations=' } | Select-Object -Last 1
        if ($doneLine -match 'iterations=(\d+).*ops_per_sec=([\d\.]+)') {
            $iterations = [long]$Matches[1]
            $opsPerSec = [double]$Matches[2]
        }

        $comboMeta = [ordered]@{
            framework = $framework
            scenario = $scenario
            label = $label
            durationSeconds = $DurationSeconds
            startedUtc = $comboStart.ToUniversalTime().ToString('o')
            finishedUtc = (Get-Date).ToUniversalTime().ToString('o')
            traceOk = $traceOk
            convertOk = $convertOk
            gcDumpOk = $gcOk
            appExitCode = $appExit
            iterations = $iterations
            opsPerSec = $opsPerSec
            artifacts = [ordered]@{
                cpuNettrace = if (Test-Path $cpuTrace) { $cpuTrace } else { $null }
                speedscopeJson = if (Test-Path $speedscope) { $speedscope } else { $null }
                gcdump = if (Test-Path $gcDump) { $gcDump } else { $null }
                stdout = if (Test-Path $stdoutLog) { $stdoutLog } else { $null }
            }
        }

        ($comboMeta | ConvertTo-Json -Depth 6) | Set-Content -Path $metaPath -Encoding UTF8
        $manifest.combos += $comboMeta

        Write-Host ("  Result: trace={0} speedscope={1} gcdump={2} iters={3} ops/s={4}" -f `
            $traceOk, $convertOk, $gcOk, $iterations, $opsPerSec)
    }
}

($manifest | ConvertTo-Json -Depth 8) | Set-Content -Path $manifestPath -Encoding UTF8
Write-Step "Done. Manifest: $manifestPath"
Write-Host "Artifacts under: $ResultsRoot"
