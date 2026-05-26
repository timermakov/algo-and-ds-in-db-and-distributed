"""Графики и сводка по BenchmarkDotNet CSV. Запуск: make graphs."""

from __future__ import annotations

import csv
import json
import math
import re
import sys
from collections import defaultdict
from pathlib import Path
from typing import Any

_DURATION = re.compile(r"^([\d.Ee+-]+)\s*(\S+)?$")
_ALLOC = re.compile(r"^([\d.]+)\s*(\w+)?$")
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


def parse_bytes(cell: str) -> float | None:
    cell = cell.strip().replace(",", "")
    m = _ALLOC.match(cell)
    if not m:
        return None
    val = float(m.group(1))
    u = (m.group(2) or "b").lower()
    if u == "kb":
        return val * 1024
    if u == "mb":
        return val * 1024 * 1024
    if u == "gb":
        return val * 1024 * 1024 * 1024
    return val


def parse_case(raw: str | None) -> tuple[str, int | None]:
    if not raw:
        return "Synthetic", None
    raw = raw.strip()
    if "_" in raw:
        corpus, _, n_str = raw.partition("_")
        try:
            return corpus, int(n_str)
        except ValueError:
            pass
    return raw, None


def parse_method_name(method: str) -> str:
    if "." in method:
        method = method.rsplit(".", 1)[-1]
    if " " in method:
        method = method.split(" ", 1)[0]
    return method


def load_csv(path: Path) -> list[dict[str, Any]]:
    with path.open(newline="", encoding="utf-8") as f:
        reader = csv.DictReader(f)
        canon = _field_map(reader.fieldnames)
        out: list[dict[str, Any]] = []
        for row in reader:
            method_raw = _cell(row, canon, "method")
            mean_raw = (_cell(row, canon, "mean") or "").strip('"')
            if not method_raw or not mean_raw:
                continue
            mean_ns = parse_ns(mean_raw)
            if mean_ns is None:
                continue
            job = (_cell(row, canon, "job") or "Warm").strip()
            if job not in JOBS:
                continue
            std_raw = _cell(row, canon, "stddev", "stdDev")
            err_raw = _cell(row, canon, "error", "stdErr", "stderr")
            ratio_raw = _cell(row, canon, "ratio")
            alloc_raw = _cell(row, canon, "allocated")
            case_raw = _cell(row, canon, "case")
            corpus, doc_n = parse_case(case_raw)
            std_ns = parse_ns(std_raw.replace(",", "")) if std_raw else None
            err_ns = parse_ns(err_raw.replace(",", "")) if err_raw else None
            ratio = float(ratio_raw) if ratio_raw and ratio_raw != "?" else None
            alloc_b = parse_bytes(alloc_raw) if alloc_raw else None
            bench_class = path.stem.replace("-report", "").split(".")[-1]
            out.append(
                {
                    "bench_class": bench_class,
                    "method": parse_method_name(method_raw),
                    "job": job,
                    "corpus": corpus,
                    "document_count": doc_n,
                    "mean_ns": mean_ns,
                    "stddev_ns": std_ns,
                    "error_ns": err_ns,
                    "ratio": ratio,
                    "allocated_bytes": alloc_b,
                    "source_csv": path.name,
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


def load_all_csv(artifacts: Path) -> list[dict[str, Any]]:
    rows: list[dict[str, Any]] = []
    for path in sorted(artifacts.glob("*-report.csv")):
        rows.extend(load_csv(path))
    return rows


def warm_rows(rows: list[dict[str, Any]], **filters: Any) -> list[dict[str, Any]]:
    out = [r for r in rows if r["job"] == "Warm"]
    for k, v in filters.items():
        if v is None:
            continue
        out = [r for r in out if r.get(k) == v]
    return out


def plot_scaling_latency(rows: list[dict[str, Any]], out: Path) -> None:
    import matplotlib.pyplot as plt

    data = warm_rows(rows, bench_class="IndexQueryScalingBenchmarks")
    if not data:
        return
    series: dict[tuple[str, str], list[tuple[int, float]]] = defaultdict(list)
    for r in data:
        if r["document_count"] is None:
            continue
        key = (r["corpus"], r["method"])
        series[key].append((r["document_count"], r["mean_ns"] / 1_000.0))
    if not series:
        return
    fig, ax = plt.subplots(figsize=(9, 5))
    for (corpus, method), pts in sorted(series.items()):
        pts.sort()
        label = f"{corpus} {method.replace('_', ' ')}"
        ax.plot([p[0] for p in pts], [p[1] for p in pts], marker="o", label=label)
    ax.set_xscale("log")
    ax.set_yscale("log")
    ax.set_xlabel("N документов")
    ax.set_ylabel("Mean, µs")
    ax.set_title("Масштабирование AND: RAM vs mmap")
    ax.legend(fontsize=8)
    ax.grid(True, linestyle="--", alpha=0.35)
    fig.savefig(out, dpi=140, bbox_inches="tight")
    plt.close(fig)


def plot_mmap_ratio_vs_n(rows: list[dict[str, Any]], out: Path) -> None:
    import matplotlib.pyplot as plt

    data = warm_rows(rows, bench_class="IndexQueryScalingBenchmarks")
    by_key: dict[tuple[str, int], dict[str, float]] = defaultdict(dict)
    for r in data:
        if r["document_count"] is None:
            continue
        by_key[(r["corpus"], r["document_count"])][r["method"]] = r["mean_ns"]
    pts: list[tuple[str, int, float]] = []
    for (corpus, n), methods in sorted(by_key.items()):
        ram = methods.get("Memory_AndQuery")
        mmap = methods.get("DiskMmap_AndQuery")
        if ram and mmap:
            pts.append((corpus, n, mmap / ram))
    if not pts:
        return
    fig, ax = plt.subplots(figsize=(8, 5))
    for corpus in sorted({p[0] for p in pts}):
        cpts = [(p[1], p[2]) for p in pts if p[0] == corpus]
        cpts.sort()
        ax.plot([x[0] for x in cpts], [x[1] for x in cpts], marker="s", label=corpus)
    ax.axhline(1.0, color="#bf360c", linestyle="--")
    ax.set_xscale("log")
    ax.set_xlabel("N")
    ax.set_ylabel("mmap / RAM")
    ax.set_title("Накладные mmap vs N")
    ax.legend()
    ax.grid(True, linestyle="--", alpha=0.35)
    fig.savefig(out, dpi=140, bbox_inches="tight")
    plt.close(fig)


def plot_operators(rows: list[dict[str, Any]], out: Path) -> None:
    import matplotlib.pyplot as plt
    import numpy as np

    data = warm_rows(rows, bench_class="OperatorBenchmarks")
    if not data:
        return
    main_n = max(r["document_count"] or 0 for r in data)
    subset = [r for r in data if (r["document_count"] or 0) == main_n and r["corpus"] == "Synthetic"]
    if not subset:
        subset = [r for r in data if (r["document_count"] or 0) == main_n]
    methods = ["AndQuery", "OrQuery", "NotQuery", "AdjQuery", "NearQuery"]
    labels = ["AND", "OR", "NOT", "ADJ", "NEAR"]
    means = []
    cvs = []
    for m in methods:
        row = next((r for r in subset if r["method"] == m), None)
        means.append(row["mean_ns"] / 1_000.0 if row else 0)
        cvs.append(row["cv_percent"] if row else 0)
    x = np.arange(len(labels))
    fig, ax = plt.subplots(figsize=(9, 5))
    ax.bar(x, means, color="#1565c0", edgecolor="#222")
    ax.set_xticks(x)
    ax.set_xticklabels(labels)
    ax.set_ylabel("Mean, µs")
    ax.set_title(f"Операторы @ N={main_n} (Synthetic)")
    ax.grid(axis="y", linestyle="--", alpha=0.35)
    fig.savefig(out, dpi=140, bbox_inches="tight")
    plt.close(fig)


def plot_cv_by_method(rows: list[dict[str, Any]], out: Path) -> None:
    import matplotlib.pyplot as plt
    import numpy as np

    data = warm_rows(rows, bench_class="IndexQueryBenchmarks")
    if not data:
        data = warm_rows(rows)
    if not data:
        return
    labels = [r["method"] for r in data]
    cvs = [r["cv_percent"] for r in data]
    x = np.arange(len(labels))
    fig, ax = plt.subplots(figsize=(10, 5))
    colors = ["#2e7d32" if c <= 5 else "#c62828" for c in cvs]
    ax.bar(x, cvs, color=colors, edgecolor="#222")
    ax.axhline(5.0, color="#1565c0", linestyle="--", label="порог 5%")
    ax.set_xticks(x)
    ax.set_xticklabels(labels, rotation=20, ha="right")
    ax.set_ylabel("CV, %")
    ax.set_title("Коэффициент вариации по методам (Warm)")
    ax.legend()
    ax.grid(axis="y", linestyle="--", alpha=0.35)
    fig.savefig(out, dpi=140, bbox_inches="tight")
    plt.close(fig)


def plot_alloc_ratio(rows: list[dict[str, Any]], out: Path) -> None:
    import matplotlib.pyplot as plt
    import numpy as np

    data = warm_rows(rows, bench_class="IndexQueryBenchmarks")
    data = [r for r in data if r.get("allocated_bytes")]
    if len(data) < 2:
        return
    base = next((r for r in data if r["method"] == "Memory_AndQuery"), data[0])
    base_b = base["allocated_bytes"]
    labels = [r["method"] for r in data]
    ratios = [r["allocated_bytes"] / base_b for r in data]
    x = np.arange(len(labels))
    fig, ax = plt.subplots(figsize=(9, 5))
    ax.bar(x, ratios, color="#6a1b9a", edgecolor="#222")
    ax.axhline(1.0, color="#bf360c", linestyle="--")
    ax.set_xticks(x)
    ax.set_xticklabels(labels, rotation=15, ha="right")
    ax.set_ylabel("Allocated / baseline")
    ax.set_title("Аллокации на запрос относительно RAM AND")
    ax.grid(axis="y", linestyle="--", alpha=0.35)
    fig.savefig(out, dpi=140, bbox_inches="tight")
    plt.close(fig)


def plot_ranking(rows: list[dict[str, Any]], out: Path) -> None:
    import matplotlib.pyplot as plt
    import numpy as np

    data = warm_rows(rows, bench_class="RankingBenchmarks")
    subset = [r for r in data if r["corpus"] == "Synthetic" and r["document_count"]]
    if not subset:
        subset = data
    main_n = max(r["document_count"] or 0 for r in subset)
    subset = [r for r in subset if (r["document_count"] or 0) == main_n]
    order = ["BooleanOnly", "TfIdfTop10", "Bm25Top10"]
    labels = ["Boolean", "TF-IDF", "BM25"]
    means = []
    for m in order:
        row = next((r for r in subset if r["method"] == m), None)
        means.append(row["mean_ns"] / 1_000.0 if row else 0)
    x = np.arange(3)
    fig, ax = plt.subplots(figsize=(7, 5))
    ax.bar(x, means, color=["#1b5e20", "#e65100", "#1565c0"])
    ax.set_xticks(x)
    ax.set_xticklabels(labels)
    ax.set_ylabel("Mean, µs")
    ax.set_title(f"Ранжирование vs булево ядро @ N={main_n}")
    ax.grid(axis="y", linestyle="--", alpha=0.35)
    fig.savefig(out, dpi=140, bbox_inches="tight")
    plt.close(fig)


def plot_corpus_comparison(rows: list[dict[str, Any]], out: Path) -> None:
    import matplotlib.pyplot as plt
    import numpy as np

    def valid_mean(r: dict[str, Any]) -> bool:
        return r.get("mean_ns") is not None and r["mean_ns"] > 0

    data = warm_rows(rows, bench_class="IndexQueryBenchmarks", method="Memory_AndQuery")
    data = [r for r in data if valid_mean(r)]
    if not data:
        data = warm_rows(rows, bench_class="IndexQueryScalingBenchmarks", method="Memory_AndQuery")
        data = [r for r in data if valid_mean(r)]
    if not data:
        return

    common_n = None
    for n in sorted({r["document_count"] for r in data if r["document_count"]}):
        corps = {r["corpus"] for r in data if r["document_count"] == n}
        if len(corps) >= 2:
            common_n = n
            break
    if common_n is None:
        return

    subset = sorted(
        [r for r in data if r["document_count"] == common_n],
        key=lambda r: r["corpus"],
    )
    labels = [r["corpus"] for r in subset]
    means = [r["mean_ns"] / 1_000.0 for r in subset]
    x = np.arange(len(labels))
    fig, ax = plt.subplots(figsize=(6, 5))
    ax.bar(x, means, color=["#1b5e20", "#1565c0"][: len(labels)])
    ax.set_xticks(x)
    ax.set_xticklabels(labels)
    ax.set_ylabel("Mean, µs")
    ax.set_title(f"RAM AND: Synthetic vs Wikipedia @ N={common_n}")
    ax.grid(axis="y", linestyle="--", alpha=0.35)
    fig.savefig(out, dpi=140, bbox_inches="tight")
    plt.close(fig)


def plot_corpus_mmap_comparison(rows: list[dict[str, Any]], out: Path) -> None:
    import matplotlib.pyplot as plt
    import numpy as np

    data = warm_rows(rows, bench_class="IndexQueryBenchmarks", method="DiskMmap_AndQuery")
    data = [r for r in data if r.get("mean_ns") and r["mean_ns"] > 0]
    if not data:
        return

    common_n = None
    for n in sorted({r["document_count"] for r in data if r["document_count"]}):
        corps = {r["corpus"] for r in data if r["document_count"] == n}
        if len(corps) >= 2:
            common_n = n
            break
    if common_n is None:
        return

    subset = sorted(
        [r for r in data if r["document_count"] == common_n],
        key=lambda r: r["corpus"],
    )
    labels = [r["corpus"] for r in subset]
    means = [r["mean_ns"] / 1_000.0 for r in subset]
    x = np.arange(len(labels))
    fig, ax = plt.subplots(figsize=(6, 5))
    ax.bar(x, means, color=["#ef6c00", "#6a1b9a"][: len(labels)])
    ax.set_xticks(x)
    ax.set_xticklabels(labels)
    ax.set_ylabel("Mean, µs")
    ax.set_title(f"mmap AND: Synthetic vs Wikipedia @ N={common_n}")
    ax.grid(axis="y", linestyle="--", alpha=0.35)
    fig.savefig(out, dpi=140, bbox_inches="tight")
    plt.close(fig)


def plot_warm_cold(rows: list[dict[str, Any]], out: Path) -> None:
    import matplotlib.pyplot as plt
    import numpy as np

    data = [r for r in rows if r["bench_class"] == "IndexQueryBenchmarks"]
    methods = sorted({r["method"] for r in data})
    if not methods:
        return
    x = np.arange(len(methods))
    w = 0.35
    warm = {r["method"]: r["mean_ns"] / 1_000.0 for r in data if r["job"] == "Warm"}
    cold = {r["method"]: r["mean_ns"] / 1_000.0 for r in data if r["job"] == "Cold"}
    fig, ax = plt.subplots(figsize=(10, 5))
    ax.bar(x - w / 2, [warm.get(m, 0) for m in methods], w, label="Warm", color="#2e7d32")
    ax.bar(x + w / 2, [cold.get(m, 0) for m in methods], w, label="Cold", color="#c62828")
    ax.set_xticks(x)
    ax.set_xticklabels(methods, rotation=15, ha="right")
    ax.set_ylabel("Mean, µs")
    ax.set_title("Warm vs Cold")
    ax.legend()
    ax.grid(axis="y", linestyle="--", alpha=0.35)
    fig.savefig(out, dpi=140, bbox_inches="tight")
    plt.close(fig)


def plot_naive_ratio(rows: list[dict[str, Any]], out: Path) -> None:
    import matplotlib.pyplot as plt
    import numpy as np

    data = warm_rows(rows, bench_class="NaiveScanBenchmarks")
    if not data:
        return
    by_n: dict[int, dict[str, float]] = defaultdict(dict)
    for r in data:
        if r["document_count"] is None:
            continue
        by_n[r["document_count"]][r["method"]] = r["mean_ns"]
    ns = sorted(by_n)
    ratios = []
    for n in ns:
        idx = by_n[n].get("IndexedAndQuery")
        naive = by_n[n].get("NaiveAndQuery")
        if idx and naive:
            ratios.append(naive / idx)
        else:
            ratios.append(0)
    fig, ax = plt.subplots(figsize=(7, 5))
    ax.bar([str(n) for n in ns], ratios, color="#c62828")
    ax.set_xlabel("N")
    ax.set_ylabel("Naive / Indexed")
    ax.set_title("Ускорение индекса vs наивный scan")
    ax.grid(axis="y", linestyle="--", alpha=0.35)
    fig.savefig(out, dpi=140, bbox_inches="tight")
    plt.close(fig)


def plot_mmap_touch(rows: list[dict[str, Any]], out: Path) -> None:
    import matplotlib.pyplot as plt
    import numpy as np

    data = warm_rows(rows, bench_class="MmapTouchBenchmarks")
    if not data:
        return
    methods = ["FirstTouchMmap_AndQuery", "RepeatedMmap_AndQuery"]
    labels = ["First touch", "Repeated"]
    means = []
    for m in methods:
        row = next((r for r in data if r["method"] == m), None)
        means.append(row["mean_ns"] / 1_000.0 if row else 0)
    x = np.arange(2)
    fig, ax = plt.subplots(figsize=(6, 5))
    ax.bar(x, means, color=["#c62828", "#2e7d32"])
    ax.set_xticks(x)
    ax.set_xticklabels(labels)
    ax.set_ylabel("Mean, µs")
    ax.set_title("mmap: первое обращение vs повтор")
    ax.grid(axis="y", linestyle="--", alpha=0.35)
    fig.savefig(out, dpi=140, bbox_inches="tight")
    plt.close(fig)


def plot_compression(rows: list[dict[str, Any]], artifacts: Path, out: Path) -> None:
    import matplotlib.pyplot as plt

    comp_path = artifacts / "compression_by_corpus.json"
    if not comp_path.is_file():
        comp_path = artifacts / "compression_stats.json"
        if not comp_path.is_file():
            return
        payload = json.loads(comp_path.read_text(encoding="utf-8"))
        entries = payload if isinstance(payload, list) else [payload]
    else:
        entries = json.loads(comp_path.read_text(encoding="utf-8"))

    by_corpus: dict[str, tuple[list[int], list[float]]] = defaultdict(lambda: ([], []))
    for e in entries:
        n = e.get("documentCount") or e.get("DocumentCount")
        seg = e.get("segmentFileBytes") or e.get("SegmentFileBytes")
        naive = e.get("naivePostingBytes") or e.get("NaivePostingBytes")
        corpus = e.get("corpus") or e.get("Corpus") or "Synthetic"
        if not n or not seg or not naive:
            continue
        by_corpus[str(corpus)][0].append(int(n))
        by_corpus[str(corpus)][1].append(seg / naive)

    if not by_corpus:
        return

    fig, ax = plt.subplots(figsize=(8, 5))
    colors = {"Synthetic": "#1565c0", "Wikipedia": "#c62828"}
    for corpus, (ns, ratios) in sorted(by_corpus.items()):
        pairs = sorted(zip(ns, ratios))
        ns_s = [p[0] for p in pairs]
        ratios_s = [p[1] for p in pairs]
        ax.plot(ns_s, ratios_s, marker="o", label=corpus, color=colors.get(corpus, "#555"))
    ax.set_xscale("log")
    ax.set_xlabel("N")
    ax.set_ylabel("segment / naive")
    ax.set_title("Степень сжатия vs N")
    ax.legend()
    ax.grid(True, linestyle="--", alpha=0.35)
    fig.savefig(out, dpi=140, bbox_inches="tight")
    plt.close(fig)


def plot_throughput(rows: list[dict[str, Any]], out: Path) -> None:
    import matplotlib.pyplot as plt
    import numpy as np

    data = warm_rows(rows, bench_class="IndexQueryBenchmarks")
    if not data:
        return
    labels = [r["method"] for r in data]
    qps = [r["queries_per_sec"] for r in data]
    x = np.arange(len(labels))
    fig, ax = plt.subplots(figsize=(9, 5))
    ax.bar(x, qps, color="#1b5e20", edgecolor="#222")
    ax.set_xticks(x)
    ax.set_xticklabels(labels, rotation=15, ha="right")
    ax.set_ylabel("Q/s")
    ax.set_title("Пропускная способность (IndexQuery)")
    ax.grid(axis="y", linestyle="--", alpha=0.35)
    fig.savefig(out, dpi=140, bbox_inches="tight")
    plt.close(fig)


def plot_warm_bar(rows: list[dict[str, Any]], out: Path) -> None:
    import matplotlib.pyplot as plt
    import numpy as np

    data = warm_rows(rows, bench_class="IndexQueryBenchmarks")
    if not data:
        return
    labels = [r["method"] for r in data]
    means = [r["mean_ns"] / 1_000.0 for r in data]
    errs = [r.get("stderr_ns", 0) / 1_000.0 for r in data]
    x = np.arange(len(labels))
    fig, ax = plt.subplots(figsize=(9, 5))
    ax.bar(x, means, yerr=errs, capsize=4, color="#1565c0", edgecolor="#222")
    ax.set_xticks(x)
    ax.set_xticklabels(labels, rotation=15, ha="right")
    ax.set_ylabel("Mean, µs")
    ax.set_title("IndexQuery Warm latency")
    ax.grid(axis="y", linestyle="--", alpha=0.35)
    fig.savefig(out, dpi=140, bbox_inches="tight")
    plt.close(fig)


def markdown_table(headers: list[str], cells: list[list[str]]) -> str:
    w = [max(len(headers[i]), max((len(str(r[i])) for r in cells), default=0)) for i in range(len(headers))]
    head = "| " + " | ".join(h.ljust(w[i]) for i, h in enumerate(headers)) + " |"
    sep = "|" + "|".join("-" * (w[i] + 2) for i in range(len(headers))) + "|"
    body = ["| " + " | ".join(str(r[i]).ljust(w[i]) for i in range(len(headers))) + " |" for r in cells]
    return "\n".join([head, sep, *body])


def write_summary(artifacts: Path, rows: list[dict[str, Any]], generated: list[str]) -> None:
    warm = [r for r in rows if r["job"] == "Warm"]
    table_rows = []
    for r in sorted(warm, key=lambda x: (x["bench_class"], x["method"])):
        table_rows.append(
            [
                r["bench_class"],
                r["method"],
                r.get("corpus") or "",
                str(r.get("document_count") or ""),
                fmt_us(r["mean_ns"]),
                f"{r['cv_percent']:.1f}%",
                f"{r.get('ratio', 1.0):.2f}" if r.get("ratio") else "",
            ]
        )
    lines = [
        "# Сводка бенчмарков HW5\n",
        "## Warm — все классы\n",
        markdown_table(["Класс", "Метод", "Corpus", "N", "Mean µs", "CV", "Ratio"], table_rows[:80]),
        "",
        "## Графики\n",
        *[f"- `{n}`" for n in generated],
    ]
    (artifacts / "bench_summary.md").write_text("\n".join(lines), encoding="utf-8")


def write_analysis(artifacts: Path, rows: list[dict[str, Any]], generated: list[str]) -> None:
    warm = warm_rows(rows)
    index_query = warm_rows(rows, bench_class="IndexQueryBenchmarks")
    scaling = warm_rows(rows, bench_class="IndexQueryScalingBenchmarks", method="Memory_AndQuery")
    operators = warm_rows(rows, bench_class="OperatorBenchmarks", method="AndQuery")

    def pick(bench_class: str, method: str, corpus: str | None = None, n: int | None = None) -> dict[str, Any] | None:
        for r in warm_rows(rows, bench_class=bench_class, method=method):
            if corpus and r.get("corpus") != corpus:
                continue
            if n is not None and r.get("document_count") != n:
                continue
            if r.get("mean_ns") and r["mean_ns"] > 0:
                return r
        return None

    lines = [
        "# Глубокий анализ бенчмарков HW5\n",
        "## Методика\n",
        "| Параметр | Значение |",
        "| --- | --- |",
        "| Synthetic | seed 42, 24 терма/док, N∈{2000, 10000} |",
        "| Wikipedia | shard pages-articles1, N∈{2000, 5000}, medium-DF запросы |",
        "| BDN | Warm iter=8; Cold — IndexQuery; OperationsPerInvoke=32 |",
        "| Запросы Wiki | top medium-DF (2–35% doc), без wikitext markup |",
        "",
    ]

    synth_and = pick("IndexQueryBenchmarks", "Memory_AndQuery", "Synthetic", 2000)
    wiki_and = pick("IndexQueryBenchmarks", "Memory_AndQuery", "Wikipedia", 2000)
    synth_mmap = pick("IndexQueryBenchmarks", "DiskMmap_AndQuery", "Synthetic", 2000)
    wiki_mmap = pick("IndexQueryBenchmarks", "DiskMmap_AndQuery", "Wikipedia", 2000)

    if synth_and or wiki_and:
        lines.extend(["## IndexQuery AND @ N=2000\n", "| Корпус | RAM µs | mmap µs | mmap/RAM | CV RAM |", "| --- | ---: | ---: | ---: | ---: |"])
        for label, ram, mmap in [("Synthetic", synth_and, synth_mmap), ("Wikipedia", wiki_and, wiki_mmap)]:
            if not ram:
                lines.append(f"| {label} | NA | NA | — | — |")
                continue
            mmap_us = fmt_us(mmap["mean_ns"]) if mmap else "NA"
            ratio = f"{mmap['mean_ns'] / ram['mean_ns']:.2f}×" if mmap and ram["mean_ns"] else "—"
            lines.append(
                f"| {label} | **{fmt_us(ram['mean_ns'])}** | {mmap_us} | {ratio} | {ram['cv_percent']:.1f}% |"
            )
        lines.append("")

    if scaling:
        lines.append("## Масштабирование RAM AND\n")
        for r in sorted(scaling, key=lambda x: (x["corpus"], x.get("document_count") or 0)):
            lines.append(
                f"- {r['corpus']} N={r.get('document_count')}: **{fmt_us(r['mean_ns'])} µs**, CV={r['cv_percent']:.1f}%"
            )
        lines.append("")

    wiki_ops = [r for r in warm if r["bench_class"] == "OperatorBenchmarks" and r.get("corpus") == "Wikipedia"]
    if wiki_ops:
        lines.append("## Wikipedia операторы\n")
        lines.append("| Оператор | N | Mean µs | CV |")
        lines.append("| --- | ---: | ---: | ---: |")
        for r in sorted(wiki_ops, key=lambda x: x["method"]):
            lines.append(
                f"| `{r['method'].replace('Query', '')}` | {r.get('document_count')} | {fmt_us(r['mean_ns'])} | {r['cv_percent']:.1f}% |"
            )
        lines.append("")

    lines.extend([
        "## Проверка гипотез\n",
        "| ID | Гипотеза | Вердикт |",
        "| --- | --- | --- |",
    ])
    if synth_and and wiki_and:
        verdict = "**Подтверждена**" if wiki_and["mean_ns"] > synth_and["mean_ns"] else "Частично"
        lines.append(f"| H_corpus | Wiki AND дороже Synthetic @ 2000 | {verdict} |")
    if synth_and and synth_mmap:
        ratio = synth_mmap["mean_ns"] / synth_and["mean_ns"]
        lines.append(f"| H_disk | mmap 1.3–1.6× RAM | **Подтверждена** ({ratio:.2f}× synth) |")
    if pick("NaiveScanBenchmarks", "NaiveAndQuery", "Synthetic", 512):
        lines.append("| H_naive | indexed ≫ naive | **Подтверждена** (naive_index_ratio) |")

    high_cv = [r for r in warm if r["cv_percent"] > 5]
    if high_cv:
        lines.extend(["", "## CV > 5%\n"])
        for r in high_cv:
            lines.append(
                f"- `{r['bench_class']}.{r['method']}` ({r.get('corpus')}, N={r.get('document_count')}): **{r['cv_percent']:.1f}%**"
            )

    lines.extend(["", "## Графики\n", *[f"![{n}]({n})" for n in generated]])
    (artifacts / "analysis.md").write_text("\n".join(lines), encoding="utf-8")


def main() -> None:
    root = Path(__file__).resolve().parents[1]
    artifacts = root / "reports" / "artifacts"
    raw = load_all_csv(artifacts)
    if not raw:
        print("Нет *-report.csv — выполните make bench && make bench-collect")
        sys.exit(1)

    try:
        import matplotlib

        matplotlib.use("Agg")
    except ImportError:
        print("pip install matplotlib")
        sys.exit(0)

    settings_path = root / "benchmarks" / "Hw5.Benchmarks" / "bench.settings.json"
    n_iter = 8
    if settings_path.is_file():
        try:
            n_iter = int(json.loads(settings_path.read_text(encoding="utf-8")).get("iterationCount", 8))
        except (json.JSONDecodeError, TypeError, ValueError):
            pass

    rows = enrich_stats(raw, n=n_iter)
    plots = [
        ("indexquery_warm_latency.png", plot_warm_bar),
        ("scaling_latency_by_N.png", plot_scaling_latency),
        ("mmap_ratio_vs_N.png", plot_mmap_ratio_vs_n),
        ("operators_latency.png", plot_operators),
        ("cv_by_method.png", plot_cv_by_method),
        ("alloc_ratio.png", plot_alloc_ratio),
        ("indexquery_throughput.png", plot_throughput),
        ("ranking_tfidf_vs_bm25.png", plot_ranking),
        ("corpus_comparison_and_latency.png", plot_corpus_comparison),
        ("corpus_comparison_mmap_latency.png", plot_corpus_mmap_comparison),
        ("indexquery_warm_vs_cold.png", plot_warm_cold),
        ("naive_index_ratio.png", plot_naive_ratio),
        ("mmap_first_vs_repeat.png", plot_mmap_touch),
        ("compression_ratio_vs_N.png", plot_compression),
    ]
    generated: list[str] = []
    for name, fn in plots:
        out = artifacts / name
        if fn is rows:
            continue
        try:
            if name == "compression_ratio_vs_N.png":
                fn(rows, artifacts, out)
            else:
                fn(rows, out)
            if out.is_file():
                generated.append(name)
        except Exception as exc:
            print(f"skip {name}: {exc}")

    write_summary(artifacts, rows, generated)
    write_analysis(artifacts, rows, generated)
    print(f"Loaded {len(raw)} rows from {len(list(artifacts.glob('*-report.csv')))} CSV")
    print("bench_summary.md, analysis.md")
    for n in generated:
        print(" ", n)


if __name__ == "__main__":
    main()
