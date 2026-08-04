<#
.SYNOPSIS
    Parses speedscope JSON (and optional meta/gcdump presence) under profiling-results
    and emits a comparison summary used by PROFILING_REPORT.md generation.

.DESCRIPTION
    Speedscope JSON schema (simplified):
      {
        shared: { frames: [ { name, file, line, col }, ... ] },
        profiles: [ { type, name, unit, startValue, endValue, samples, weights }, ... ]
      }

    For sampled CPU profiles, each sample index points into frames; weights are sample
    counts (or time units). Self-time ~= weight of samples whose leaf frame is F.
    Total-time ~= weight of samples where F appears anywhere on the stack.

.PARAMETER ResultsRoot
    Root folder containing <Framework>/<Scenario>/ artifacts.

.PARAMETER TopN
    How many hot frames to keep per profile (default 20).

.PARAMETER OutputJson
    Path for machine-readable summary JSON.

.PARAMETER OutputMarkdown
    Path for human-readable markdown summary (partial report body).
#>
[CmdletBinding()]
param(
    [string]$ResultsRoot = '',
    [int]$TopN = 20,
    [string]$OutputJson = '',
    [string]$OutputMarkdown = ''
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ComparisonRoot = Split-Path -Parent $ScriptDir

if ([string]::IsNullOrWhiteSpace($ResultsRoot)) {
    $ResultsRoot = Join-Path $ComparisonRoot 'profiling-results'
}
if ([string]::IsNullOrWhiteSpace($OutputJson)) {
    $OutputJson = Join-Path $ResultsRoot 'analysis-summary.json'
}
if ([string]::IsNullOrWhiteSpace($OutputMarkdown)) {
    $OutputMarkdown = Join-Path $ResultsRoot 'analysis-summary.md'
}

function Get-InterestingName([string]$name) {
    if ([string]::IsNullOrWhiteSpace($name)) { return $false }
    # Keep mediator / DI / pipeline related frames; drop pure runtime noise later in ranking.
    return $true
}

function Normalize-FrameName([string]$name) {
    if ($null -eq $name) { return '<unknown>' }
    # Strip common noisy prefixes / generic arity markers for grouping.
    $n = $name.Trim()
    $n = $n -replace '`\d+', ''
    return $n
}

function Test-IsFrameworkFrame([string]$name) {
    $n = $name.ToLowerInvariant()
    return (
        $n -match 'plaxion' -or
        $n -match 'mediatr' -or
        $n -match 'mediator' -or
        $n -match 'pipeline' -or
        $n -match 'dependencyinjection' -or
        $n -match 'serviceprovider' -or
        $n -match 'getservice' -or
        $n -match 'getrequiredservice' -or
        $n -match 'requesthandler' -or
        $n -match 'behavior' -or
        $n -match 'sendcore' -or
        $n -match 'compose' -or
        $n -match 'executeasync'
    )
}

function Get-SpeedscopeHotFrames {
    param(
        [string]$Path,
        [int]$Top
    )

    if (-not (Test-Path $Path)) {
        return $null
    }

    # Speedscope files can be large; read as raw then ConvertFrom-Json.
    $raw = Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    $json = $raw | ConvertFrom-Json

    if (-not $json.shared -or -not $json.shared.frames) {
        return @{ error = 'no-frames'; path = $Path }
    }

    $frames = @($json.shared.frames)
    $profile = $null
    if ($json.profiles -and $json.profiles.Count -gt 0) {
        # Prefer sampled profiles.
        $profile = $json.profiles | Where-Object { $_.type -eq 'sampled' } | Select-Object -First 1
        if (-not $profile) { $profile = $json.profiles[0] }
    }
    if (-not $profile) {
        return @{ error = 'no-profile'; path = $Path }
    }

    $samples = @($profile.samples)
    $weights = @($profile.weights)
    if ($weights.Count -eq 0 -and $samples.Count -gt 0) {
        $weights = @(1) * $samples.Count
    }

    $self = @{}
    $total = @{}
    $totalWeight = 0.0

    for ($i = 0; $i -lt $samples.Count; $i++) {
        $stack = @($samples[$i])
        $w = if ($i -lt $weights.Count) { [double]$weights[$i] } else { 1.0 }
        $totalWeight += $w

        if ($stack.Count -eq 0) { continue }

        # Leaf = last frame in speedscope sample (top of stack).
        $leafIdx = [int]$stack[$stack.Count - 1]
        if ($leafIdx -ge 0 -and $leafIdx -lt $frames.Count) {
            $leafName = Normalize-FrameName ([string]$frames[$leafIdx].name)
            if (-not $self.ContainsKey($leafName)) { $self[$leafName] = 0.0 }
            $self[$leafName] += $w
        }

        $seen = @{}
        foreach ($idxObj in $stack) {
            $idx = [int]$idxObj
            if ($idx -lt 0 -or $idx -ge $frames.Count) { continue }
            $name = Normalize-FrameName ([string]$frames[$idx].name)
            if ($seen.ContainsKey($name)) { continue }
            $seen[$name] = $true
            if (-not $total.ContainsKey($name)) { $total[$name] = 0.0 }
            $total[$name] += $w
        }
    }

    function To-RankedList($map, $tw, $take) {
        $list = foreach ($k in $map.Keys) {
            [pscustomobject]@{
                name = $k
                value = [double]$map[$k]
                pct = if ($tw -gt 0) { [Math]::Round(100.0 * $map[$k] / $tw, 2) } else { 0 }
                interesting = Test-IsFrameworkFrame $k
            }
        }
        $list |
            Sort-Object -Property @{Expression = 'interesting'; Descending = $true }, @{Expression = 'value'; Descending = $true } |
            Select-Object -First $take
    }

    $unit = if ($profile.unit) { [string]$profile.unit } else { 'samples' }
    $startValue = if ($null -ne $profile.startValue) { [double]$profile.startValue } else { 0 }
    $endValue = if ($null -ne $profile.endValue) { [double]$profile.endValue } else { $totalWeight }

    return [ordered]@{
        path = $Path
        unit = $unit
        sampleCount = $samples.Count
        totalWeight = $totalWeight
        duration = $endValue - $startValue
        topSelf = @(To-RankedList $self $totalWeight $Top)
        topTotal = @(To-RankedList $total $totalWeight $Top)
        interestingSelf = @((To-RankedList $self $totalWeight ([Math]::Max($Top * 3, 40))) | Where-Object { $_.interesting } | Select-Object -First $Top)
        interestingTotal = @((To-RankedList $total $totalWeight ([Math]::Max($Top * 3, 40))) | Where-Object { $_.interesting } | Select-Object -First $Top)
    }
}

Write-Host "Scanning $ResultsRoot ..."

$combos = @()
$frameworkDirs = Get-ChildItem -Path $ResultsRoot -Directory -ErrorAction SilentlyContinue
foreach ($fwDir in $frameworkDirs) {
    $scenarioDirs = Get-ChildItem -Path $fwDir.FullName -Directory -ErrorAction SilentlyContinue
    foreach ($scDir in $scenarioDirs) {
        $framework = $fwDir.Name
        $scenario = $scDir.Name
        $label = "${framework}_${scenario}"

        $speedscope = Join-Path $scDir.FullName "${label}_speedscope.json"
        if (-not (Test-Path $speedscope)) {
            $alt = Get-ChildItem -Path $scDir.FullName -Filter '*speedscope*.json' -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($alt) { $speedscope = $alt.FullName }
        }

        $metaPath = Join-Path $scDir.FullName "${label}_meta.json"
        $gcDump = Join-Path $scDir.FullName "${label}.gcdump"
        $nettrace = Join-Path $scDir.FullName "${label}_cpu.nettrace"

        $meta = $null
        if (Test-Path $metaPath) {
            $meta = Get-Content -LiteralPath $metaPath -Raw | ConvertFrom-Json
        }

        $hot = $null
        if (Test-Path $speedscope) {
            Write-Host "  Parsing $framework/$scenario ..."
            try {
                $hot = Get-SpeedscopeHotFrames -Path $speedscope -Top $TopN
            } catch {
                $hot = @{ error = $_.Exception.Message; path = $speedscope }
            }
        }

        $combos += [ordered]@{
            framework = $framework
            scenario = $scenario
            label = $label
            hasSpeedscope = [bool](Test-Path $speedscope)
            hasNettrace = [bool](Test-Path $nettrace)
            hasGcDump = [bool](Test-Path $gcDump)
            iterations = if ($meta) { $meta.iterations } else { $null }
            opsPerSec = if ($meta) { $meta.opsPerSec } else { $null }
            speedscope = $hot
            paths = [ordered]@{
                speedscope = if (Test-Path $speedscope) { $speedscope } else { $null }
                nettrace = if (Test-Path $nettrace) { $nettrace } else { $null }
                gcdump = if (Test-Path $gcDump) { $gcDump } else { $null }
                meta = if (Test-Path $metaPath) { $metaPath } else { $null }
            }
        }
    }
}

# Cross-framework comparison per scenario
$scenarios = $combos | ForEach-Object { $_.scenario } | Sort-Object -Unique
$comparisons = @()
foreach ($scenario in $scenarios) {
    $byFw = @{}
    foreach ($c in $combos | Where-Object { $_.scenario -eq $scenario }) {
        $byFw[$c.framework] = $c
    }

    $comparisons += [ordered]@{
        scenario = $scenario
        frameworksPresent = @($byFw.Keys | Sort-Object)
        opsPerSec = $(
            $map = [ordered]@{}
            foreach ($k in $byFw.Keys) { $map[$k] = $byFw[$k].opsPerSec }
            $map
        )
        plaxionInterestingSelf = if ($byFw.Contains('Plaxion') -and $byFw['Plaxion'].speedscope) { $byFw['Plaxion'].speedscope.interestingSelf } else { @() }
        mediatRInterestingSelf = if ($byFw.Contains('MediatR') -and $byFw['MediatR'].speedscope) { $byFw['MediatR'].speedscope.interestingSelf } else { @() }
        mediatorInterestingSelf = if ($byFw.Contains('Mediator') -and $byFw['Mediator'].speedscope) { $byFw['Mediator'].speedscope.interestingSelf } else { @() }
    }
}

$summary = [ordered]@{
    generatedUtc = (Get-Date).ToUniversalTime().ToString('o')
    resultsRoot = $ResultsRoot
    comboCount = $combos.Count
    capturedCount = @($combos | Where-Object { $_.hasSpeedscope }).Count
    combos = $combos
    comparisons = $comparisons
}

($summary | ConvertTo-Json -Depth 12) | Set-Content -LiteralPath $OutputJson -Encoding UTF8

# Markdown summary
$md = New-Object System.Text.StringBuilder
[void]$md.AppendLine('# Profiling Analysis Summary')
[void]$md.AppendLine()
[void]$md.AppendLine("Generated (UTC): $($summary.generatedUtc)")
[void]$md.AppendLine()
[void]$md.AppendLine("Combos discovered: **$($summary.comboCount)**; with speedscope data: **$($summary.capturedCount)**")
[void]$md.AppendLine()
[void]$md.AppendLine('## Capture Matrix')
[void]$md.AppendLine()
[void]$md.AppendLine('| Framework | Scenario | Speedscope | Nettrace | GCDump | Iterations | Ops/sec |')
[void]$md.AppendLine('|-----------|----------|:----------:|:--------:|:------:|-----------:|--------:|')
foreach ($c in ($combos | Sort-Object framework, scenario)) {
    $ops = if ($null -ne $c.opsPerSec) { ('{0:N0}' -f $c.opsPerSec) } else { '-' }
    $iters = if ($null -ne $c.iterations) { $c.iterations } else { '-' }
    [void]$md.AppendLine("| $($c.framework) | $($c.scenario) | $(if($c.hasSpeedscope){'Y'}else{'N'}) | $(if($c.hasNettrace){'Y'}else{'N'}) | $(if($c.hasGcDump){'Y'}else{'N'}) | $iters | $ops |")
}
[void]$md.AppendLine()

foreach ($c in ($combos | Where-Object { $_.hasSpeedscope -and $_.speedscope -and -not $_.speedscope.error } | Sort-Object framework, scenario)) {
    [void]$md.AppendLine("## $($c.framework) / $($c.scenario)")
    [void]$md.AppendLine()
    if ($null -ne $c.opsPerSec) {
        [void]$md.AppendLine(("- Harness throughput (not a formal benchmark): **{0:N0} ops/sec** over {1} iterations" -f $c.opsPerSec, $c.iterations))
        [void]$md.AppendLine()
    }
    [void]$md.AppendLine('### Top interesting self-time frames')
    [void]$md.AppendLine()
    [void]$md.AppendLine('| Frame | Self % | Self value |')
    [void]$md.AppendLine('|-------|-------:|-----------:|')
    $selfFrames = @($c.speedscope.interestingSelf)
    if ($selfFrames.Count -eq 0) { $selfFrames = @($c.speedscope.topSelf | Select-Object -First 10) }
    foreach ($f in $selfFrames) {
        [void]$md.AppendLine(('| `{0}` | {1} | {2} |' -f $f.name, $f.pct, $f.value))
    }
    [void]$md.AppendLine()
    [void]$md.AppendLine('### Top interesting total-time frames')
    [void]$md.AppendLine()
    [void]$md.AppendLine('| Frame | Total % | Total value |')
    [void]$md.AppendLine('|-------|--------:|------------:|')
    $totFrames = @($c.speedscope.interestingTotal)
    if ($totFrames.Count -eq 0) { $totFrames = @($c.speedscope.topTotal | Select-Object -First 10) }
    foreach ($f in $totFrames) {
        [void]$md.AppendLine(('| `{0}` | {1} | {2} |' -f $f.name, $f.pct, $f.value))
    }
    [void]$md.AppendLine()
}

$md.ToString() | Set-Content -LiteralPath $OutputMarkdown -Encoding UTF8

Write-Host "Wrote $OutputJson"
Write-Host "Wrote $OutputMarkdown"
