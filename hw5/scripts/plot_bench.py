"""Графики и сводка по Hw5 IndexQueryBenchmarks-report.csv. Запуск: make graphs."""

from __future__ import annotations

import csv
import json
import math
import re
import sys
from pathlib import Path
from typing import Any

_DURATION = re.compile(r"^([\d.Ee+-]+)\s*(\S+)?$")
METHODS = [
    "Memory_AndQuery",
    "DiskMmap_AndQuery",
    "Memory_NearAdjQuery",
    "Memory_Bm25Top10",
]
METHOD_LABELS = {
    "Memory_AndQuery": "RAM (AND)",
    "DiskMmap_AndQuery": "mmap сегмент (AND)",
    "Memory_NearAdjQuery": "RAM (NEAR/ADJ)",
    "Memory_Bm25Top10": "RAM (BM25 Top10)",
}
COLORS = {
    "Memory_AndQuery": "#1b5e20",
    "DiskMmap_AndQuery": "#1565c0",
    "Memory_NearAdjQuery": "#6a1b9a",
    "Memory_Bm25Top10": "#e65100",
}
JOBS = ("Warm", "Cold")


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


def parse_ns(cell: str) -> float | None:
    cell = cell.strip().replace(",", "")
    m = _DURATION.match(cell)
    if not m or m.group(1).upper() in ("?", "NA"):
        return None
    val = float(m.group(1))
    u = (m.group(2) or "").strip().lower()
    if not u or "ns" in u:
        return val
    if "us" in u or u.endswith("\u03bcs"):
        return val * 1_000.0
    if "ms" in u:
        return val * 1_000_000.0
    if u in ("s", "sec"):
        return val * 1_000_000_000.0
    return val


def parse_method_name(method: str) -> str | None:
    for name in METHODS:
        if name in method:
            return name
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
            mean_ns = parse_ns(mean_raw)
            if mean_ns is None:
                continue
            mname = parse_method_name(method)
            if mname is None:
                continue
            job = (_cell(row, canon, "job") or "Warm").strip()
            if job not in JOBS:
                continue
            std_raw = _cell(row, canon, "stddev", "stdDev")
            err_raw = _cell(row, canon, "error", "stdErr", "stderr")
            ratio_raw = _cell(row, canon, "ratio")
            std_ns = parse_ns(std_raw.replace(",", "")) if std_raw else None
            err_ns = parse_ns(err_raw.replace(",", "")) if err_raw else None
            ratio = float(ratio_raw) if ratio_raw and ratio_raw != "?" else None
            out.append(
                {
                    "method": mname,
                    "job": job,
                    "mean_ns": mean_ns,
                    "stddev_ns": std_ns,
                    "error_ns": err_ns,
                    "ratio": ratio,
                }
            )
        return out


def t_crit_95(n: int) -> float:
    table = {1: 12.706, 2: 4.303, 3: 3.182, 4: 2.776, 5: 2.571, 6: 2.447, 7: 2.365, 8: 2.306}
    return table.get(max(1, n), 2.0)


def enrich_stats(rows: list[dict[str, Any]], n: int = 8) -> list[dict[str, Any]]:
    enriched = []
    for r in rows:
        mean = r["mean_ns"]
        std = r.get("stddev_ns") or r.get("error_ns") or 0.0
        stderr = std / math.sqrt(n) if n > 0 else 0.0
        ci = t_crit_95(n) * stderr
        cv = (std / mean * 100.0) if mean > 0 else 0.0
        qps = 1_000_000_000.0 / mean if mean > 0 else 0.0
        enriched.append(
            {
                **r,
                "stderr_ns": stderr,
                "ci95_lo_ns": mean - ci,
                "ci95_hi_ns": mean + ci,
                "cv_percent": cv,
                "queries_per_sec": qps,
            }
        )
    return enriched


def fmt_us(ns: float) -> str:
    return f"{ns / 1_000.0:.2f}"


def fmt_ms(ns: float) -> str:
    return f"{ns / 1_000_000.0:.3f}"


def find_report_csv(artifacts: Path) -> Path | None:
    candidates = sorted(artifacts.glob("*IndexQueryBenchmarks*-report.csv"))
    if candidates:
        return candidates[-1]
    candidates = sorted(artifacts.glob("*-report.csv"))
    return candidates[-1] if candidates else None


def matrix(rows: list[dict[str, Any]], job: str) -> dict[str, dict[str, Any]]:
    out: dict[str, dict[str, Any]] = {}
    for r in rows:
        if r["job"] != job:
            continue
        out[r["method"]] = r
    return out


def plot_warm_bar(warm: dict[str, dict[str, Any]], out: Path) -> None:
    import matplotlib.pyplot as plt

    methods = [m for m in METHODS if m in warm]
    xs = list(range(len(methods)))
    means = [warm[m]["mean_ns"] / 1_000.0 for m in methods]
    errs = [warm[m].get("stderr_ns", 0) / 1_000.0 for m in methods]
    fig, ax = plt.subplots(figsize=(9, 5))
    ax.bar(xs, means, yerr=errs, capsize=4, color=[COLORS[m] for m in methods], edgecolor="#222", linewidth=0.5)
    ax.set_xticks(xs)
    ax.set_xticklabels([METHOD_LABELS[m] for m in methods], rotation=15, ha="right")
    ax.set_ylabel("Mean, µs")
    ax.set_title("IndexQuery (Warm): RAM vs mmap, позиционные запросы, BM25")
    ax.grid(axis="y", linestyle="--", alpha=0.35)
    fig.savefig(out, dpi=140, bbox_inches="tight")
    plt.close(fig)


def plot_ratio_disk_vs_memory(warm: dict[str, dict[str, Any]], out: Path) -> None:
    import matplotlib.pyplot as plt

    base = warm.get("Memory_AndQuery")
    disk = warm.get("DiskMmap_AndQuery")
    if not base or not disk:
        return
    ratio = disk["mean_ns"] / base["mean_ns"]
    fig, ax = plt.subplots(figsize=(5, 4))
    ax.bar(["DiskMmap / RAM\n(AND)"], [ratio], color="#1565c0")
    ax.axhline(1.0, color="#bf360c", linestyle="--", label="parity")
    ax.set_ylabel("Относительная задержка (>1 = медленнее)")
    ax.set_title(f"Накладные расходы mmap+декод: {ratio:.2f}×")
    ax.grid(axis="y", linestyle="--", alpha=0.35)
    fig.savefig(out, dpi=140, bbox_inches="tight")
    plt.close(fig)


def plot_warm_cold(warm: dict[str, dict[str, Any]], cold: dict[str, dict[str, Any]], out: Path) -> None:
    import matplotlib.pyplot as plt
    import numpy as np

    methods = [m for m in METHODS if m in warm and m in cold]
    if not methods:
        return
    x = np.arange(len(methods))
    w = 0.35
    fig, ax = plt.subplots(figsize=(10, 5))
    ax.bar(x - w / 2, [warm[m]["mean_ns"] / 1_000.0 for m in methods], w, label="Warm", color="#2e7d32")
    ax.bar(x + w / 2, [cold[m]["mean_ns"] / 1_000.0 for m in methods], w, label="Cold", color="#c62828")
    ax.set_xticks(x)
    ax.set_xticklabels([METHOD_LABELS[m] for m in methods], rotation=12, ha="right")
    ax.set_ylabel("Mean, µs")
    ax.set_title("Warm vs Cold (первый запуск без прогрева)")
    ax.legend()
    ax.grid(axis="y", linestyle="--", alpha=0.35)
    fig.savefig(out, dpi=140, bbox_inches="tight")
    plt.close(fig)


def plot_throughput(warm: dict[str, dict[str, Any]], out: Path) -> None:
    import matplotlib.pyplot as plt

    methods = [m for m in METHODS if m in warm]
    xs = list(range(len(methods)))
    qps = [warm[m]["queries_per_sec"] for m in methods]
    fig, ax = plt.subplots(figsize=(9, 5))
    ax.bar(xs, qps, color=[COLORS[m] for m in methods], edgecolor="#222", linewidth=0.5)
    ax.set_xticks(xs)
    ax.set_xticklabels([METHOD_LABELS[m] for m in methods], rotation=15, ha="right")
    ax.set_ylabel("Throughput (запросов/с)")
    ax.set_title("Пропускная способность (1 / mean latency)")
    ax.grid(axis="y", linestyle="--", alpha=0.35)
    fig.savefig(out, dpi=140, bbox_inches="tight")
    plt.close(fig)


def markdown_table(headers: list[str], cells: list[list[str]]) -> str:
    w = [max(len(headers[i]), max((len(str(r[i])) for r in cells), default=0)) for i in range(len(headers))]
    head = "| " + " | ".join(h.ljust(w[i]) for i, h in enumerate(headers)) + " |"
    sep = "|" + "|".join("-" * (w[i] + 2) for i in range(len(headers))) + "|"
    body = ["| " + " | ".join(str(r[i]).ljust(w[i]) for i in range(len(headers))) + " |" for r in cells]
    return "\n".join([head, sep, *body])


def write_summary(
    out_dir: Path,
    rows: list[dict[str, Any]],
    compression: dict[str, Any] | None,
    generated: list[str],
) -> None:
    warm = matrix(rows, "Warm")
    cold = matrix(rows, "Cold")
    table_rows = []
    for m in METHODS:
        if m not in warm:
            continue
        r = warm[m]
        table_rows.append(
            [
                METHOD_LABELS[m],
                fmt_us(r["mean_ns"]),
                fmt_us(r.get("stddev_ns") or 0),
                fmt_us(r["stderr_ns"]),
                f"{r['ci95_lo_ns']/1000:.1f}–{r['ci95_hi_ns']/1000:.1f}",
                f"{r['cv_percent']:.1f}%",
                f"{r['queries_per_sec']:.0f}",
                f"{r.get('ratio', 1.0):.2f}" if r.get("ratio") else "1.00",
            ]
        )

    lines = [
        "# Сводка бенчмарков HW5 (IndexQuery)\n",
        "## Warm — статистика (n=8 итераций)\n",
        markdown_table(
            ["Метод", "Mean µs", "StdDev", "StdErr", "CI95 µs", "CV", "Q/s", "Ratio"],
            table_rows,
        ),
        "",
        "## Cold vs Warm (µs)\n",
    ]
    cold_rows = []
    for m in METHODS:
        if m in warm and m in cold:
            cold_rows.append(
                [
                    METHOD_LABELS[m],
                    fmt_us(warm[m]["mean_ns"]),
                    fmt_us(cold[m]["mean_ns"]),
                    f"{cold[m]['mean_ns'] / warm[m]['mean_ns']:.2f}×",
                ]
            )
    if cold_rows:
        lines.append(markdown_table(["Метод", "Warm", "Cold", "Cold/Warm"], cold_rows))
        lines.append("")

    if compression:
        lines.extend(
            [
                "## Сжатие сегмента (синтетика bench.settings)\n",
                f"- Документов: **{compression.get('documentCount')}**",
                f"- Наивный объём posting-list: **{compression.get('naivePostingBytes'):,}** байт",
                f"- Файл сегмента (delta+bitpack): **{compression.get('segmentFileBytes'):,}** байт",
                f"- Отношение segment/naive: **{compression.get('compressionRatio')}**",
                f"- Экономия места: **{compression.get('spaceSavingsPercent')}%**",
                "",
            ]
        )

    lines.append("## Графики\n")
    lines.extend(f"- `{n}`" for n in generated)
    (out_dir / "bench_summary.md").write_text("\n".join(lines), encoding="utf-8")


def main() -> None:
    root = Path(__file__).resolve().parents[1]
    artifacts = root / "reports" / "artifacts"
    csv_path = find_report_csv(artifacts)
    if not csv_path:
        print("Нет *-report.csv — выполните make bench && make bench-collect")
        sys.exit(1)

    try:
        import matplotlib

        matplotlib.use("Agg")
    except ImportError:
        print("pip install matplotlib")
        sys.exit(0)

    raw = load_csv(csv_path)
    if not raw:
        print(f"Пустой CSV: {csv_path}")
        sys.exit(1)

    settings_path = root / "benchmarks" / "Hw5.Benchmarks" / "bench.settings.json"
    n_iter = 8
    if settings_path.is_file():
        try:
            n_iter = int(json.loads(settings_path.read_text(encoding="utf-8")).get("iterationCount", 8))
        except (json.JSONDecodeError, TypeError, ValueError):
            pass

    rows = enrich_stats(raw, n=n_iter)
    warm = matrix(rows, "Warm")
    cold = matrix(rows, "Cold")

    compression = None
    comp_path = artifacts / "compression_stats.json"
    if comp_path.is_file():
        compression = json.loads(comp_path.read_text(encoding="utf-8"))

    generated: list[str] = []
    for name, fn in (
        ("indexquery_warm_latency.png", lambda p: plot_warm_bar(warm, p)),
        ("indexquery_disk_vs_memory_ratio.png", lambda p: plot_ratio_disk_vs_memory(warm, p)),
        ("indexquery_warm_vs_cold.png", lambda p: plot_warm_cold(warm, cold, p)),
        ("indexquery_throughput.png", lambda p: plot_throughput(warm, p)),
    ):
        fn(artifacts / name)
        if (artifacts / name).is_file():
            generated.append(name)

    write_summary(artifacts, rows, compression, generated)
    print(f"CSV: {csv_path.name}")
    print("bench_summary.md")
    for n in generated:
        print(" ", n)


if __name__ == "__main__":
    main()
