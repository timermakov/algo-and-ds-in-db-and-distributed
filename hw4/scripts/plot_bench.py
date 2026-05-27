"""Графики по Hw4.Benchmarks.Operation*Benchmarks-report.csv. Запуск: make graphs."""

from __future__ import annotations

import csv
import json
import os
import re
import sys
from pathlib import Path
from typing import Any

API_OPS = ["Put", "Get", "Size", "Clear", "Merge", "Enumerate"]
IMPLS = ["Custom", "ConcurrentDictionary"]
IMPL_LABELS = {
    "Custom": "Hw4.ConcurrentMap",
    "ConcurrentDictionary": "ConcurrentDict",
}
COLORS = {"Custom": "#1b5e20", "ConcurrentDictionary": "#1565c0"}

_DURATION = re.compile(r"^([\d.Ee+-]+)\s*(\S+)?$")


def _field_map(fieldnames: list[str] | None) -> dict[str, str]:
    if not fieldnames:
        return {}
    return {k.strip().casefold(): k.strip() for k in fieldnames if k}


def _cell(row: dict[str, Any], canon: dict[str, str], *names: str) -> str | None:
    for n in names:
        key = canon.get(n.casefold())
        if not key:
            continue
        v = row.get(key)
        if v is None:
            continue
        s = str(v).strip()
        if s and s.upper() != "NA":
            return s
    return None


def parse_ms(cell: str) -> float | None:
    cell = cell.strip().replace(",", "")
    m = _DURATION.match(cell)
    if not m or m.group(1).upper() in ("?", "NA"):
        return None
    val = float(m.group(1))
    u = (m.group(2) or "").strip().lower()
    if not u or "ms" in u:
        return val
    if u in ("s", "sec"):
        return val * 1000.0
    if "ns" in u:
        return val * 1e-6
    if "us" in u or u.endswith("\u03bcs"):
        return val * 1e-3
    return val


def load_bench_settings(root: Path) -> dict[str, Any]:
    for rel in (
        "benchmarks/Hw4.Benchmarks/bench.settings.json",
        "config/bench.settings.example.json",
    ):
        p = root / rel
        if p.is_file():
            try:
                return json.loads(p.read_text(encoding="utf-8"))
            except json.JSONDecodeError:
                pass
    return {}


def ops_per_invocation(root: Path) -> int:
    env = os.environ.get("HW4_OPS_PER_INVOCATION")
    if env:
        return int(env)
    cfg = load_bench_settings(root)
    return int(cfg.get("opsPerInvocation", 4096))


def operation_csv_paths(results_dir: Path) -> list[Path]:
    return sorted(
        p
        for p in results_dir.glob("*-report.csv")
        if p.stem.split(".")[-1].startswith("Operation")
        and p.stem.endswith("Benchmarks-report")
    )


def operation_from_path(path: Path) -> str:
    tail = path.stem.split(".")[-1].replace("-report", "")
    return tail[len("Operation") : -len("Benchmarks")]


def parse_impl(method: str) -> str | None:
    if "_Custom" in method:
        return "Custom"
    if "ConcurrentDictionary" in method:
        return "ConcurrentDictionary"
    return None


def load_csv(path: Path) -> list[dict[str, Any]]:
    with path.open(newline="", encoding="utf-8") as f:
        reader = csv.DictReader(f)
        canon = _field_map(reader.fieldnames)
        out: list[dict[str, Any]] = []
        for row in reader:
            method = _cell(row, canon, "method")
            mean_raw = (_cell(row, canon, "mean") or "").strip('"')
            if not method or not mean_raw:
                continue
            ms = parse_ms(mean_raw)
            if ms is None:
                continue
            thr = _cell(row, canon, "threads")
            threads = int(float(thr)) if thr is not None else 1
            err_raw = _cell(row, canon, "error", "stdErr", "stderr")
            std_raw = _cell(row, canon, "stdDev", "stddev")
            err_ms = parse_ms(err_raw.replace(",", "")) if err_raw else None
            std_ms = parse_ms(std_raw.replace(",", "")) if std_raw else None
            spread = err_ms if err_ms is not None else std_ms
            out.append(
                {
                    "method": method,
                    "mean_ms": ms,
                    "threads": threads,
                    "error_ms": spread,
                }
            )
        return out


def usable_error(mean_ms: float, error_ms: float | None) -> float | None:
    if error_ms is None or error_ms <= 0 or mean_ms <= 0:
        return None
    if error_ms > mean_ms * 0.75:
        return None
    return error_ms


def pick_row(rows: list[dict[str, Any]], impl: str, threads: int | None) -> dict[str, Any] | None:
    matched = [r for r in rows if parse_impl(str(r["method"])) == impl]
    if threads is not None:
        matched = [r for r in matched if r["threads"] == threads]
    if not matched:
        return None
    return min(matched, key=lambda r: r["mean_ms"])


def matrix_at_threads(files: list[Path], threads: int) -> dict[str, dict[str, float]]:
    matrix: dict[str, dict[str, float]] = {}
    for path in files:
        op = operation_from_path(path)
        records = load_csv(path)
        row: dict[str, float] = {}
        for impl in IMPLS:
            rec = pick_row(records, impl, threads)
            if rec is not None:
                row[impl] = rec["mean_ms"]
        if row:
            matrix[op] = row
    return matrix


def series_by_threads(records: list[dict[str, Any]], impl: str) -> dict[int, dict[str, Any]]:
    out: dict[int, dict[str, Any]] = {}
    for r in records:
        if parse_impl(str(r["method"])) != impl:
            continue
        t = int(r["threads"])
        prev = out.get(t)
        if prev is None or r["mean_ms"] < prev["mean_ms"]:
            out[t] = r
    return out


def thread_counts_for_op(all_by_op: dict[str, list[dict[str, Any]]], op: str) -> list[int]:
    ts = {int(r["threads"]) for r in all_by_op.get(op, [])}
    return sorted(ts)


def plot_impl_bar(
    op: str,
    means: dict[str, float],
    errors: dict[str, float | None],
    out: Path,
    threads: int,
) -> None:
    import matplotlib.pyplot as plt

    impls = [i for i in IMPLS if i in means]
    xs = list(range(len(impls)))
    yerr = [errors.get(i) for i in impls]
    use_err = any(e is not None and e > 0 for e in yerr)
    fig, ax = plt.subplots(figsize=(max(5.5, len(impls) * 2.4), 4.8))
    ax.bar(
        xs,
        [means[i] for i in impls],
        yerr=yerr if use_err else None,
        capsize=4 if use_err else 0,
        color=[COLORS[i] for i in impls],
        edgecolor="#222",
        linewidth=0.5,
    )
    ax.set_xticks(xs)
    ax.set_xticklabels([IMPL_LABELS[i] for i in impls], rotation=10, ha="right")
    ax.set_ylabel("Mean, ms")
    ax.set_title(f"{op}: Custom vs ConcurrentDictionary (Threads={threads})")
    ax.grid(axis="y", linestyle="--", alpha=0.35)
    fig.savefig(out, dpi=140, bbox_inches="tight")
    plt.close(fig)


def plot_threads_scaling(op: str, records: list[dict[str, Any]], out: Path) -> None:
    import matplotlib.pyplot as plt

    thread_list = thread_counts_for_op({op: records}, op)
    if len(thread_list) < 2:
        return

    fig, ax = plt.subplots(figsize=(6.5, 4.5))
    for impl in IMPLS:
        series = series_by_threads(records, impl)
        xs = [t for t in thread_list if t in series]
        if not xs:
            continue
        ys = [series[t]["mean_ms"] for t in xs]
        ax.plot(xs, ys, marker="o", linewidth=2, label=IMPL_LABELS[impl], color=COLORS[impl])

    ax.set_xlabel("Threads")
    ax.set_ylabel("Mean, ms")
    ax.set_title(f"{op}: latency vs thread count")
    ax.set_xticks(thread_list)
    ax.grid(True, linestyle="--", alpha=0.35)
    ax.legend(fontsize=8)
    fig.savefig(out, dpi=140, bbox_inches="tight")
    plt.close(fig)


def plot_threads_scaling_grid(all_by_op: dict[str, list[dict[str, Any]]], out: Path) -> None:
    import matplotlib.pyplot as plt

    ops = [o for o in API_OPS if len(thread_counts_for_op(all_by_op, o)) >= 2]
    if not ops:
        return

    cols, rows = 3, max(1, (len(ops) + 2) // 3)
    fig, axes = plt.subplots(rows, cols, figsize=(4.0 * cols, 3.6 * rows), squeeze=False)
    for i, op in enumerate(ops):
        ax = axes[i // cols][i % cols]
        records = all_by_op[op]
        thread_list = thread_counts_for_op(all_by_op, op)
        for impl in IMPLS:
            series = series_by_threads(records, impl)
            xs = [t for t in thread_list if t in series]
            if not xs:
                continue
            ys = [series[t]["mean_ms"] for t in xs]
            ax.plot(xs, ys, marker="o", linewidth=1.8, label=IMPL_LABELS[impl][:10], color=COLORS[impl])
        ax.set_title(op)
        ax.set_xlabel("Threads")
        ax.set_ylabel("Mean, ms")
        ax.grid(True, linestyle="--", alpha=0.3)
        if i == 0:
            ax.legend(fontsize=7)
    for j in range(len(ops), rows * cols):
        axes[j // cols][j % cols].set_visible(False)
    fig.suptitle("Scaling: Custom vs ConcurrentDictionary", fontsize=11)
    fig.tight_layout()
    fig.savefig(out, dpi=136, bbox_inches="tight")
    plt.close(fig)


def plot_dashboard(matrix: dict[str, dict[str, float]], out: Path, threads: int) -> None:
    import matplotlib.pyplot as plt

    ops = [o for o in API_OPS if o in matrix]
    cols, rows = 3, max(1, (len(ops) + 2) // 3)
    fig, axes = plt.subplots(rows, cols, figsize=(4.2 * cols, 4.0 * rows), squeeze=False)
    for i, op in enumerate(ops):
        ax = axes[i // cols][i % cols]
        impls = [x for x in IMPLS if x in matrix[op]]
        ys = [matrix[op][x] for x in impls]
        ax.bar(range(len(impls)), ys, color=[COLORS[x] for x in impls])
        ax.set_xticks(range(len(impls)))
        ax.set_xticklabels([IMPL_LABELS[x][:12] for x in impls], fontsize=8)
        ax.set_title(op)
        ax.set_ylabel("Mean, ms")
        if ys:
            ax.axhline(min(ys), color="#ffb300", linestyle="--", linewidth=0.9)
        ax.grid(axis="y", linestyle="--", alpha=0.3)
    for j in range(len(ops), rows * cols):
        axes[j // cols][j % cols].set_visible(False)
    fig.suptitle(f"Operation benchmarks (Mean, ms, Threads={threads})", fontsize=12)
    fig.tight_layout()
    fig.savefig(out, dpi=136, bbox_inches="tight")
    plt.close(fig)


def plot_custom_latency(matrix: dict[str, dict[str, float]], out: Path) -> None:
    import matplotlib.pyplot as plt

    ops = [o for o in API_OPS if o in matrix and "Custom" in matrix[o]]
    fig, ax = plt.subplots(figsize=(max(8, len(ops) * 1.2), 5.0))
    ax.bar(range(len(ops)), [matrix[o]["Custom"] for o in ops], color=COLORS["Custom"])
    ax.set_xticks(range(len(ops)))
    ax.set_xticklabels(ops)
    ax.set_ylabel("Mean, ms")
    ax.set_title("ConcurrentHashTable (Custom)")
    ax.grid(axis="y", linestyle="--", alpha=0.35)
    fig.savefig(out, dpi=140, bbox_inches="tight")
    plt.close(fig)


def plot_custom_vs_baseline(matrix: dict[str, dict[str, float]], out: Path) -> None:
    import matplotlib.pyplot as plt

    ops = [o for o in API_OPS if o in matrix and "Custom" in matrix[o] and "ConcurrentDictionary" in matrix[o]]
    ratios = [matrix[o]["Custom"] / matrix[o]["ConcurrentDictionary"] for o in ops]
    fig, ax = plt.subplots(figsize=(max(8, len(ops) * 1.2), 5.0))
    ax.bar(range(len(ops)), ratios, color="#1565c0")
    ax.axhline(1.0, color="#bf360c", linestyle="--", label="parity")
    ax.set_xticks(range(len(ops)))
    ax.set_xticklabels(ops)
    ax.set_ylabel("Custom / ConcurrentDict (>1 = slower)")
    ax.grid(axis="y", linestyle="--", alpha=0.35)
    fig.savefig(out, dpi=140, bbox_inches="tight")
    plt.close(fig)


def plot_throughput(matrix: dict[str, dict[str, float]], ops_per_inv: int, out: Path) -> None:
    import matplotlib.pyplot as plt

    ops = [o for o in API_OPS if o in matrix]
    fig, ax = plt.subplots(figsize=(max(9, len(ops) * 1.3), 5.2))
    w = 0.35
    for j, impl in enumerate(IMPLS):
        ys = [ops_per_inv / matrix[o][impl] * 1000.0 for o in ops if impl in matrix[o]]
        ax.bar([i + (j - 0.5) * w for i in range(len(ops))], ys, width=w * 0.9, label=IMPL_LABELS[impl], color=COLORS[impl])
    ax.set_xticks(range(len(ops)))
    ax.set_xticklabels(ops)
    ax.set_ylabel(f"Throughput (ops/s), {ops_per_inv} ops/invocation")
    ax.legend(fontsize=8)
    ax.grid(axis="y", linestyle="--", alpha=0.35)
    fig.savefig(out, dpi=140, bbox_inches="tight")
    plt.close(fig)


def markdown_table(headers: list[str], cells: list[list[str]]) -> str:
    w = [max(len(headers[i]), max((len(str(r[i])) for r in cells), default=0)) for i in range(len(headers))]
    head = "| " + " | ".join(h.ljust(w[i]) for i, h in enumerate(headers)) + " |"
    sep = "|" + "|".join("-" * (w[i] + 2) for i in range(len(headers))) + "|"
    body = ["| " + " | ".join(str(r[i]).ljust(w[i]) for i in range(len(headers))) + " |" for r in cells]
    return "\n".join([head, sep, *body])


def scaling_table(all_by_op: dict[str, list[dict[str, Any]]]) -> str:
    lines = ["## Mean (ms) по числу потоков\n"]
    for op in API_OPS:
        if op not in all_by_op:
            continue
        threads = thread_counts_for_op(all_by_op, op)
        if not threads:
            continue
        rows: list[list[str]] = []
        for impl in IMPLS:
            series = series_by_threads(all_by_op[op], impl)
            cells = [f"{series[t]['mean_ms']:.4f}" if t in series else "—" for t in threads]
            rows.append([IMPL_LABELS[impl], *cells])
        hdr = ["Реализация", *[f"T={t}" for t in threads]]
        lines.append(f"### {op}\n")
        lines.append(markdown_table(hdr, rows))
        lines.append("")
    return "\n".join(lines)


def main() -> None:
    root = Path(__file__).resolve().parents[1]
    results_dir = root / "BenchmarkDotNet.Artifacts" / "results"
    files = operation_csv_paths(results_dir)
    if not files:
        print("Нет Operation*-report.csv — выполните make bench или make bench-full.")
        sys.exit(1)

    try:
        import matplotlib

        matplotlib.use("Agg")
    except ImportError:
        print("pip install matplotlib")
        sys.exit(0)

    out_dir = root / "reports" / "figures"
    out_dir.mkdir(parents=True, exist_ok=True)
    for png in out_dir.glob("*.png"):
        png.unlink()

    all_by_op = {operation_from_path(p): load_csv(p) for p in files}
    ops_per_inv = ops_per_invocation(root)
    matrix_t1 = matrix_at_threads(files, 1)
    max_threads = max(
        (max(thread_counts_for_op(all_by_op, o), default=1) for o in all_by_op),
        default=1,
    )
    matrix_peak = matrix_at_threads(files, max_threads)

    table_rows = []
    for op in API_OPS:
        if op not in matrix_t1:
            continue
        c = matrix_t1[op].get("Custom")
        cd = matrix_t1[op].get("ConcurrentDictionary")
        table_rows.append(
            [
                op,
                f"{c:.6f}" if c else "—",
                f"{cd:.6f}" if cd else "—",
                f"{c / cd:.3f}x" if c and cd else "—",
            ]
        )

    generated: list[str] = []
    for op in API_OPS:
        if op not in matrix_t1:
            continue
        errs = {
            impl: usable_error(
                matrix_t1[op][impl],
                (pick_row(all_by_op[op], impl, 1) or {}).get("error_ms"),
            )
            for impl in IMPLS
            if impl in matrix_t1[op]
        }
        name = f"{op}_impl_comparison.png"
        plot_impl_bar(op, matrix_t1[op], errs, out_dir / name, threads=1)
        generated.append(name)

        scaling_name = f"{op}_threads_scaling.png"
        plot_threads_scaling(op, all_by_op[op], out_dir / scaling_name)
        if scaling_name not in generated and (out_dir / scaling_name).is_file():
            generated.append(scaling_name)

    for name, fn in (
        ("dashboard_operations.png", lambda p: plot_dashboard(matrix_t1, p, threads=1)),
        ("dashboard_operations_peak_threads.png", lambda p: plot_dashboard(matrix_peak, p, threads=max_threads)),
        ("operations_threads_scaling.png", lambda p: plot_threads_scaling_grid(all_by_op, p)),
        ("operations_custom_latency.png", lambda p: plot_custom_latency(matrix_t1, p)),
        ("operations_custom_vs_baseline.png", lambda p: plot_custom_vs_baseline(matrix_t1, p)),
        ("operations_throughput.png", lambda p: plot_throughput(matrix_t1, ops_per_inv, p)),
    ):
        fn(out_dir / name)
        if (out_dir / name).is_file():
            generated.append(name)

    md = [
        "# Сводка бенчмарков (Operation API)\n",
        f"- CSV: {', '.join(p.name for p in files)}",
        f"- Ops per invocation: **{ops_per_inv}** (из bench.settings.json или `HW4_OPS_PER_INVOCATION`)",
        "- Baseline: **ConcurrentDictionary** (.NET) — эталон промышленной concurrent-map.",
        "",
        "## Матрица Mean (ms), Threads=1\n",
        markdown_table(["Операция", "Custom", "ConcurrentDict", "Custom/CD"], table_rows),
        "",
        scaling_table(all_by_op),
        "## Графики\n",
        *[f"- `{n}`" for n in generated],
    ]
    (out_dir / "summary_tables.md").write_text("\n".join(md), encoding="utf-8")
    print("summary_tables.md")
    for n in generated:
        print(" ", n)


if __name__ == "__main__":
    main()
