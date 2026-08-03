#!/usr/bin/env python3
"""
Deep speedscope call-tree analyzer for PlaxionMediator round-2 profiling.

Handles modern dotnet-trace Speedscope *evented* profiles (O/C events).
Optimized for large traces (100MB+): analyzes the busiest thread only and
uses frame-index stack keys.
"""
from __future__ import annotations

import argparse
import json
import re
import statistics
import sys
from collections import Counter, defaultdict
from pathlib import Path
from typing import Any


FRAMEWORKS = ("Plaxion", "Mediator", "MediatR")
SCENARIOS = ("Send5", "Send20", "TypeVariety50")

DI_RE = re.compile(
    r"GetService|GetRequiredService|GetServices|ServiceProvider|ResolveService|"
    r"CallSite|ServiceLookup|DependencyInjection|PipelineBehaviorResolver|"
    r"RequestHandlerResolver|Materialize|IServiceProvider",
    re.I,
)
PIPE_RE = re.compile(
    r"PipelineComposer|PipelineRunner|ExecuteCore|ExecuteAsync|\.Next\(|"
    r"Compose\(|AwaitWithExceptionWrapping",
    re.I,
)
IFACE_RE = re.compile(
    r"IPipelineBehavior|IRequestHandler|ISender|IMediator|RequestHandlerDelegate|"
    r"MessageHandlerDelegate|\.Handle\(",
    re.I,
)
INLINE_RE = re.compile(r"ExecuteCore|CastOrAdapt|GetBehaviors|CanCacheHandlersPerScope", re.I)
SMALL_RE = re.compile(
    r"ExecuteAsync|PipelineComposer|SendCore_|CastOrAdapt|GetBehaviors|"
    r"CanCacheHandlersPerScope|Materialize",
    re.I,
)
NOISE_RE = re.compile(
    r"WaitHandle|Monitor\.Wait|CPU_TIME|Non-Activities|^Threads$|Process64|"
    r"Thread \(|GarbageCollect|gc_heap|ntdll|KernelBase|EventPipe|Profiler|"
    r"JIT_|TieredCompilation",
    re.I,
)
GCDUMP_RE = re.compile(
    r"PipelineRunner|RequestHandlerDelegate|DisplayClass|Func`|Action`|"
    r"AsyncStateMachine|d__\d+|IPipelineBehavior|Plaxion|Mediator|MediatR|"
    r"Task`|ValueTask|Delegate|Behavior",
    re.I,
)
INTEREST_RE = re.compile(
    r"plaxion|mediatr|mediator|pipeline|dependencyinjection|serviceprovider|"
    r"getservice|getrequired|requesthandler|behavior|sendcore|compose|"
    r"executeasync|executecore|pipelinerunner|castoradapt|handler|"
    r"send\(|handle\(|next\(|comparison\.|benchmark|simulated",
    re.I,
)


def norm(name: str | None) -> str:
    if not name:
        return "<unknown>"
    return re.sub(r"`\d+", "", name.strip())


def short(name: str, limit: int = 140) -> str:
    n = norm(name)
    return n if len(n) <= limit else n[: limit - 3] + "..."


def load_json(path: Path) -> Any:
    with path.open("r", encoding="utf-8-sig") as f:
        return json.load(f)


def classify_frames(frame_names: list[str]) -> dict[str, Any]:
    n = len(frame_names)
    is_int = [False] * n
    is_di = [False] * n
    is_pipe = [False] * n
    is_iface = [False] * n
    is_inline = [False] * n
    is_small = [False] * n
    is_noise = [False] * n
    root_kind = [None] * n  # type: ignore[var-annotated]
    for i, name in enumerate(frame_names):
        is_noise[i] = bool(NOISE_RE.search(name))
        is_int[i] = (not is_noise[i]) and bool(INTEREST_RE.search(name))
        is_di[i] = bool(DI_RE.search(name))
        is_pipe[i] = bool(PIPE_RE.search(name))
        is_iface[i] = bool(IFACE_RE.search(name))
        is_inline[i] = bool(INLINE_RE.search(name))
        is_small[i] = bool(SMALL_RE.search(name))
        low = name.lower()
        if "plaxionmediatorsender" in low:
            root_kind[i] = "plaxion"
        elif "mediatr" in low:
            root_kind[i] = "mediatr"
        elif "mediator.send" in low or "mediator.internals" in low or (
            "mediator." in low and "send" in low
        ):
            root_kind[i] = "mediator"
    return {
        "is_int": is_int,
        "is_di": is_di,
        "is_pipe": is_pipe,
        "is_iface": is_iface,
        "is_inline": is_inline,
        "is_small": is_small,
        "is_noise": is_noise,
        "root_kind": root_kind,
    }


def analyze_evented(path: Path) -> dict[str, Any]:
    data = load_json(path)
    frames_raw = data.get("shared", {}).get("frames") or []
    frame_names = [
        norm(f.get("name") if isinstance(f, dict) else str(f)) for f in frames_raw
    ]
    cls = classify_frames(frame_names)
    is_int = cls["is_int"]
    is_di = cls["is_di"]
    is_pipe = cls["is_pipe"]
    is_iface = cls["is_iface"]
    is_inline = cls["is_inline"]
    is_small = cls["is_small"]
    root_kind = cls["root_kind"]

    profiles = [p for p in (data.get("profiles") or []) if p.get("type") == "evented"]
    if not profiles:
        return {"error": "no-evented-profile", "path": str(path)}

    # Busiest thread only (by event count).
    pr = max(profiles, key=lambda p: len(p.get("events") or []))
    events = pr.get("events") or []
    profile_name = pr.get("name")

    self_time: Counter[int] = Counter()
    total_time: Counter[int] = Counter()
    # stack key = tuple of interesting frame indices only
    stack_weight: Counter[tuple[int, ...]] = Counter()
    stack_count: Counter[tuple[int, ...]] = Counter()
    di_counts: list[int] = []
    iface_counts: list[int] = []
    pipe_counts: list[int] = []
    depths_i: list[int] = []
    depths_f: list[int] = []
    best_stack: tuple[int, ...] | None = None
    best_w = 0.0
    total_weight = 0.0
    intervals = 0
    flame_under: dict[str, dict[int, Counter[int]]] = defaultdict(
        lambda: defaultdict(Counter)
    )
    # edge among interesting frames
    edge_w: Counter[tuple[int, int]] = Counter()

    stack: list[int] = []
    prev_at = float(events[0].get("at") or 0) if events else 0.0
    # Only record full interesting stacks when dt exceeds this (ms) OR stack changed
    # after accumulating; still attribute all time to self/total.
    MIN_STACK_RECORD_MS = 0.0  # record all non-zero; keys are int tuples (cheap)

    for e in events:
        at = float(e.get("at") or prev_at)
        dt = at - prev_at
        if dt < 0:
            dt = 0.0

        if dt > 0 and stack:
            total_weight += dt
            intervals += 1
            leaf = stack[-1]
            self_time[leaf] += dt
            # unique frames on stack for total
            seen: set[int] = set()
            for fi in stack:
                if fi not in seen:
                    total_time[fi] += dt
                    seen.add(fi)
            depths_f.append(len(stack))

            inter_idx = [fi for fi in stack if 0 <= fi < len(is_int) and is_int[fi]]
            depths_i.append(len(inter_idx))
            if inter_idx and dt >= MIN_STACK_RECORD_MS:
                key = tuple(inter_idx)
                stack_weight[key] += dt
                stack_count[key] += 1
                di_n = sum(1 for fi in stack if 0 <= fi < len(is_di) and is_di[fi])
                iface_n = sum(1 for fi in stack if 0 <= fi < len(is_iface) and is_iface[fi])
                pipe_n = sum(1 for fi in stack if 0 <= fi < len(is_pipe) and is_pipe[fi])
                di_counts.append(di_n)
                iface_counts.append(iface_n)
                pipe_counts.append(pipe_n)
                score = dt
                # boost stacks containing pipeline/send
                if any(
                    is_pipe[fi] or ("send" in frame_names[fi].lower())
                    for fi in inter_idx
                    if 0 <= fi < len(frame_names)
                ):
                    score *= 1.5
                if score > best_w and len(inter_idx) >= 3:
                    best_w = score
                    best_stack = key
                # edges
                for a, b in zip(inter_idx, inter_idx[1:]):
                    edge_w[(a, b)] += dt
                # flame under first root
                start = None
                rk = None
                for i, fi in enumerate(inter_idx):
                    rk = root_kind[fi] if 0 <= fi < len(root_kind) else None
                    if rk:
                        start = i
                        break
                if start is not None and rk is not None:
                    sub = inter_idx[start : start + 18]
                    bucket = flame_under[rk]
                    for off, fi in enumerate(sub):
                        bucket[off][fi] += dt

        t = e.get("type")
        fi = e.get("frame")
        try:
            fi_i = int(fi) if fi is not None else None
        except (TypeError, ValueError):
            fi_i = None
        if t == "O" and fi_i is not None:
            stack.append(fi_i)
        elif t == "C" and fi_i is not None:
            if stack and stack[-1] == fi_i:
                stack.pop()
            elif fi_i in stack:
                while stack and stack[-1] != fi_i:
                    stack.pop()
                if stack:
                    stack.pop()
        prev_at = at

    def pct(v: float) -> float:
        return round(100.0 * v / total_weight, 3) if total_weight > 0 else 0.0

    def name_of(i: int) -> str:
        if 0 <= i < len(frame_names):
            return frame_names[i]
        return "<oob>"

    def top_by_index(c: Counter[int], n: int = 25, only_int: bool = False) -> list[dict[str, Any]]:
        items = c.most_common()
        out = []
        for idx, val in items:
            nm = name_of(idx)
            if only_int and not (0 <= idx < len(is_int) and is_int[idx]):
                continue
            out.append({"name": short(nm), "value": round(val, 3), "pct": pct(val), "idx": idx})
            if len(out) >= n:
                break
        return out

    def stats(vals: list[int]) -> dict[str, Any]:
        if not vals:
            return {"n": 0}
        s = sorted(vals)
        return {
            "n": len(vals),
            "mean": round(statistics.mean(vals), 3),
            "median": statistics.median(vals),
            "p90": s[int(0.9 * (len(s) - 1))],
            "max": max(vals),
            "min": min(vals),
            "hist": dict(sorted(Counter(vals).items())[:40]),
        }

    top_stacks = []
    for key, w in stack_weight.most_common(10):
        top_stacks.append(
            {
                "weight": round(w, 3),
                "pct": pct(w),
                "count": stack_count[key],
                "depth": len(key),
                "frames": [short(name_of(i)) for i in key],
            }
        )

    non_inlined = []
    for idx, hits in total_time.most_common():
        nm = name_of(idx)
        if (0 <= idx < len(is_inline) and is_inline[idx]) or (
            0 <= idx < len(is_small) and is_small[idx] and is_int[idx]
        ):
            non_inlined.append(
                {
                    "name": short(nm),
                    "hit_pct": pct(hits),
                    "self_pct": pct(self_time.get(idx, 0)),
                    "total_pct": pct(hits),
                    "aggressive_inline_attr": bool(
                        0 <= idx < len(is_inline) and is_inline[idx]
                    ),
                }
            )
        if len(non_inlined) >= 20:
            break
    non_inlined.sort(key=lambda x: -x["hit_pct"])

    def match_frames(pred, n=15):
        out = []
        for idx, tot in total_time.most_common():
            if 0 <= idx < len(pred) and pred[idx]:
                out.append(
                    {
                        "name": short(name_of(idx)),
                        "total_pct": pct(tot),
                        "self_pct": pct(self_time.get(idx, 0)),
                        "hit_pct": pct(tot),
                    }
                )
            if len(out) >= n:
                break
        return out

    flames: dict[str, list[dict[str, Any]]] = {}
    for rk, levels in flame_under.items():
        arr = []
        hitw = sum(levels[0].values()) if 0 in levels else 1.0
        for off in range(0, 18):
            if off not in levels:
                break
            top = levels[off].most_common(6)
            arr.append(
                {
                    "offset": off,
                    "top": [
                        {
                            "name": short(name_of(i)),
                            "pct_of_root": round(100 * v / hitw, 2),
                            "value": round(v, 3),
                        }
                        for i, v in top
                    ],
                }
            )
        flames[rk] = arr

    rep = (
        [short(name_of(i)) for i in best_stack]
        if best_stack
        else (top_stacks[0]["frames"] if top_stacks else [])
    )

    return {
        "path": str(path),
        "profileName": profile_name,
        "eventCount": len(events),
        "sampleLikeIntervals": intervals,
        "totalWeightMs": round(total_weight, 3),
        "topSelfInteresting": top_by_index(self_time, 20, True),
        "topTotalInteresting": top_by_index(total_time, 25, True),
        "topSelf": top_by_index(self_time, 15, False),
        "topStacks": top_stacks,
        "representativeDispatchChain": rep,
        "representativeWeight": round(best_w, 3),
        "diPerInterval": stats(di_counts),
        "interfacePerInterval": stats(iface_counts),
        "pipelinePerInterval": stats(pipe_counts),
        "interestingDepth": stats(depths_i),
        "fullStackDepth": stats(depths_f),
        "nonInlinedCandidates": non_inlined[:15],
        "pipelineFrames": match_frames(is_pipe),
        "diFrames": match_frames(is_di),
        "interfaceFrames": match_frames(is_iface),
        "flames": flames,
        "edgeTop": [
            {
                "from": short(name_of(a), 90),
                "to": short(name_of(b), 90),
                "pct": pct(w),
            }
            for (a, b), w in edge_w.most_common(25)
        ],
    }


def parse_gcdump(path: Path) -> dict[str, Any] | None:
    if not path.is_file():
        return None
    lines = path.read_text(encoding="utf-8-sig", errors="replace").splitlines()
    interesting_lines = [ln.strip() for ln in lines if GCDUMP_RE.search(ln)]
    return {"interesting": interesting_lines[:100], "header": lines[:5]}


def write_markdown(summary: dict[str, Any], out_md: Path) -> None:
    md: list[str] = []
    md.append("# Call-tree analysis (round 2, evented speedscope)")
    md.append("")
    md.append(f"Results root: `{summary['resultsRoot']}`")
    md.append(f"Captured combos: **{summary['captured']}** / {len(summary['combos'])}")
    md.append("")
    for c in summary["combos"]:
        a = c.get("analysis") or {}
        md.append(f"## {c['framework']} / {c['scenario']}")
        md.append("")
        meta = c.get("meta") or {}
        if meta.get("opsPerSec") is not None:
            md.append(
                f"- Harness: **{meta['opsPerSec']:,.0f} ops/s**, "
                f"iterations={meta.get('iterations')}"
            )
        if not c.get("hasSpeedscope"):
            md.append("_No speedscope artifact._")
            md.append("")
            continue
        if a.get("error"):
            md.append(f"ERROR: {a['error']}")
            md.append("")
            continue
        md.append(
            f"- Profile: `{a.get('profileName')}`, events={a.get('eventCount')}, "
            f"intervals={a.get('sampleLikeIntervals')}, "
            f"totalWeightMs={a.get('totalWeightMs')}"
        )
        md.append(f"- Interesting depth: `{a.get('interestingDepth')}`")
        md.append(f"- Full depth: `{a.get('fullStackDepth')}`")
        md.append(f"- DI frames/interval: `{a.get('diPerInterval')}`")
        md.append(f"- Interface frames/interval: `{a.get('interfacePerInterval')}`")
        md.append(f"- Pipeline frames/interval: `{a.get('pipelinePerInterval')}`")
        md.append("")
        md.append("### Representative dispatch chain")
        md.append("")
        chain = a.get("representativeDispatchChain") or []
        if chain:
            for i, fr in enumerate(chain):
                md.append(f"{i}. `{fr}`")
        else:
            md.append("_none_")
        md.append("")
        md.append("### Top interesting self %")
        md.append("")
        md.append("| Frame | Self % |")
        md.append("|-------|-------:|")
        for fr in (a.get("topSelfInteresting") or [])[:12]:
            md.append(f"| `{fr['name']}` | {fr['pct']} |")
        md.append("")
        md.append("### Top interesting total %")
        md.append("")
        md.append("| Frame | Total % |")
        md.append("|-------|--------:|")
        for fr in (a.get("topTotalInteresting") or [])[:15]:
            md.append(f"| `{fr['name']}` | {fr['pct']} |")
        md.append("")
        md.append("### Non-inlined candidates")
        md.append("")
        md.append("| Frame | Hit%/Total% | Self% | AggressiveInlining? |")
        md.append("|-------|------------:|------:|:-------------------:|")
        for fr in (a.get("nonInlinedCandidates") or [])[:12]:
            md.append(
                f"| `{fr['name']}` | {fr['hit_pct']} | {fr['self_pct']} | "
                f"{fr['aggressive_inline_attr']} |"
            )
        md.append("")
        md.append("### Pipeline frames")
        md.append("")
        md.append("| Frame | Total% | Self% |")
        md.append("|-------|-------:|------:|")
        for fr in (a.get("pipelineFrames") or [])[:12]:
            md.append(f"| `{fr['name']}` | {fr['total_pct']} | {fr['self_pct']} |")
        md.append("")
        md.append("### DI frames")
        md.append("")
        md.append("| Frame | Total% | Self% |")
        md.append("|-------|-------:|------:|")
        for fr in (a.get("diFrames") or [])[:12]:
            md.append(f"| `{fr['name']}` | {fr['total_pct']} | {fr['self_pct']} |")
        md.append("")
        md.append("### Top stacks")
        md.append("")
        for si, st in enumerate((a.get("topStacks") or [])[:4], 1):
            md.append(
                f"**Stack {si}** pct={st['pct']} depth={st['depth']} n={st['count']}"
            )
            md.append("")
            for fr in st["frames"]:
                md.append(f"- `{fr}`")
            md.append("")
        flames = a.get("flames") or {}
        key = (
            "plaxion"
            if c["framework"] == "Plaxion"
            else ("mediatr" if c["framework"] == "MediatR" else "mediator")
        )
        flame = flames.get(key) or (next(iter(flames.values())) if flames else None)
        if flame:
            md.append("### Flame under sender")
            md.append("")
            for lvl in flame[:14]:
                tops = ", ".join(
                    f"`{t['name']}` ({t['pct_of_root']}%)" for t in lvl["top"][:4]
                )
                md.append(f"- +{lvl['offset']}: {tops}")
            md.append("")
        g = c.get("gcdump") or {}
        if g.get("interesting"):
            md.append("### gcdump interesting retained types")
            md.append("")
            for ln in g["interesting"][:30]:
                md.append(f"- `{ln}`")
            md.append("")
    out_md.write_text("\n".join(md), encoding="utf-8")


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--results-root", default="")
    ap.add_argument("--out-dir", default="")
    ap.add_argument("--only", default="", help="Comma list like Plaxion/Send5,Mediator/Send5")
    args = ap.parse_args()

    script_dir = Path(__file__).resolve().parent
    comparison_root = script_dir.parent
    results_root = (
        Path(args.results_root)
        if args.results_root
        else comparison_root / "profiling-results" / "round2"
    )
    out_dir = Path(args.out_dir) if args.out_dir else results_root / "analysis"
    out_dir.mkdir(parents=True, exist_ok=True)

    only: set[tuple[str, str]] | None = None
    if args.only.strip():
        only = set()
        for part in args.only.split(","):
            part = part.strip()
            if not part:
                continue
            fw, sc = part.split("/", 1)
            only.add((fw, sc))

    combos: list[dict[str, Any]] = []
    for fw in FRAMEWORKS:
        for sc in SCENARIOS:
            if only is not None and (fw, sc) not in only:
                continue
            label = f"{fw}_{sc}"
            d = results_root / fw / sc
            sp = d / f"{label}_speedscope.json"
            if not sp.is_file() and d.is_dir():
                alts = list(d.glob("*speedscope*.json"))
                if alts:
                    sp = alts[0]
            meta_p = d / f"{label}_meta.json"
            meta = load_json(meta_p) if meta_p.is_file() else None
            entry: dict[str, Any] = {
                "framework": fw,
                "scenario": sc,
                "label": label,
                "meta": meta,
                "hasSpeedscope": sp.is_file(),
                "speedscopePath": str(sp) if sp.is_file() else None,
            }
            if sp.is_file():
                print(
                    f"Analyzing {label} ({sp.stat().st_size / 1e6:.1f} MB)...",
                    flush=True,
                )
                try:
                    entry["analysis"] = analyze_evented(sp)
                    a = entry["analysis"]
                    print(
                        f"  done intervals={a.get('sampleLikeIntervals')} "
                        f"weightMs={a.get('totalWeightMs')}",
                        flush=True,
                    )
                except Exception as ex:  # noqa: BLE001
                    entry["analysis"] = {"error": str(ex)}
                    print(f"  ERROR: {ex}", flush=True)
            entry["gcdump"] = parse_gcdump(d / f"{label}_gcdump_report.txt")
            combos.append(entry)

    summary = {
        "resultsRoot": str(results_root),
        "captured": sum(1 for c in combos if c.get("hasSpeedscope")),
        "combos": combos,
    }
    out_json = out_dir / "calltree-analysis.json"
    out_json.write_text(json.dumps(summary, indent=2), encoding="utf-8")
    print(f"Wrote {out_json}", flush=True)
    out_md = out_dir / "calltree-analysis.md"
    write_markdown(summary, out_md)
    print(f"Wrote {out_md}", flush=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())
