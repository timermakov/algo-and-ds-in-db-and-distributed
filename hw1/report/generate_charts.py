from __future__ import annotations

import csv
import math
from pathlib import Path

import matplotlib.pyplot as plt


ARTIFACTS_DIR = Path(__file__).parent / "artifacts"


def parse_microseconds(value: str) -> float:
    raw = value.replace(" ", "").replace(",", "")
    if raw in {"NA", "-", "?"}:
        return math.nan
    if raw.endswith("ns"):
        return float(raw[:-2]) / 1000.0
    if raw.endswith("μs"):
        return float(raw[:-2])
    if raw.endswith("us"):
        return float(raw[:-2])
    if raw.endswith("ms"):
        return float(raw[:-2]) * 1000.0
    return float(raw)


def parse_bytes(value: str) -> float:
    cleaned = value.replace(",", "").strip()
    if cleaned.endswith("KB"):
        return float(cleaned.replace("KB", "").strip()) * 1024.0
    if cleaned.endswith("MB"):
        return float(cleaned.replace("MB", "").strip()) * 1024.0 * 1024.0
    if cleaned.endswith("B"):
        return float(cleaned.replace("B", "").strip())
    if cleaned == "NA":
        return 0.0
    return float(cleaned)


def read_rows(csv_path: Path) -> list[dict[str, str]]:
    with csv_path.open("r", encoding="utf-8", newline="") as file:
        reader = csv.DictReader(file)
        return list(reader)


def get_row(rows: list[dict[str, str]], method: str, n: int) -> dict[str, str]:
    return next(r for r in rows if int(r["N"]) == n and r["Method"] == method)


def plot_latency_with_stddev(
    csv_name: str,
    methods: list[str],
    labels: list[str],
    output_name: str,
    title: str,
) -> None:
    rows = read_rows(ARTIFACTS_DIR / csv_name)
    n_values = sorted({int(row["N"]) for row in rows})

    plt.figure(figsize=(8, 5))
    for method, label in zip(methods, labels):
        values = []
        stddevs = []
        for n in n_values:
            row = get_row(rows, method, n)
            values.append(parse_microseconds(row["Mean"]))
            stddevs.append(parse_microseconds(row["StdDev"]))
        plt.errorbar(
            n_values,
            values,
            yerr=stddevs,
            marker="o",
            linewidth=2,
            capsize=4,
            elinewidth=1.2,
            label=label,
        )

    plt.title(title)
    plt.xlabel("N")
    plt.ylabel("Mean ± StdDev, us")
    plt.grid(alpha=0.3)
    plt.legend()
    plt.tight_layout()
    plt.savefig(ARTIFACTS_DIR / output_name, dpi=160)
    plt.close()


def plot_cv_percent(
    csv_name: str,
    methods: list[str],
    labels: list[str],
    output_name: str,
    title: str,
) -> None:
    rows = read_rows(ARTIFACTS_DIR / csv_name)
    n_values = sorted({int(row["N"]) for row in rows})

    plt.figure(figsize=(8, 5))
    for method, label in zip(methods, labels):
        cv_values = []
        for n in n_values:
            row = get_row(rows, method, n)
            mean = parse_microseconds(row["Mean"])
            stddev = parse_microseconds(row["StdDev"])
            if math.isnan(mean) or math.isnan(stddev):
                cv_values.append(math.nan)
                continue
            cv_values.append(0.0 if mean == 0.0 else (stddev / mean) * 100.0)
        plt.plot(n_values, cv_values, marker="o", linewidth=2, label=label)

    plt.title(title)
    plt.xlabel("N")
    plt.ylabel("CV, % (StdDev / Mean)")
    plt.grid(alpha=0.3)
    plt.legend()
    plt.tight_layout()
    plt.savefig(ARTIFACTS_DIR / output_name, dpi=160)
    plt.close()


def plot_speedup_ratio(
    csv_name: str,
    baseline_method: str,
    compared_method: str,
    output_name: str,
    title: str,
) -> None:
    rows = read_rows(ARTIFACTS_DIR / csv_name)
    n_values = sorted({int(row["N"]) for row in rows})
    speedups = []
    for n in n_values:
        baseline = parse_microseconds(get_row(rows, baseline_method, n)["Mean"])
        compared = parse_microseconds(get_row(rows, compared_method, n)["Mean"])
        if math.isnan(baseline) or math.isnan(compared):
            speedups.append(math.nan)
            continue
        speedups.append(0.0 if compared == 0.0 else baseline / compared)

    plt.figure(figsize=(8, 5))
    plt.plot(n_values, speedups, marker="o", linewidth=2, color="#8b0000")
    plt.axhline(1.0, linestyle="--", color="#444", linewidth=1)
    plt.title(title)
    plt.xlabel("N")
    plt.ylabel("Speedup (baseline / compared)")
    plt.grid(alpha=0.3)
    plt.tight_layout()
    plt.savefig(ARTIFACTS_DIR / output_name, dpi=160)
    plt.close()


def plot_allocated_bytes(
    csv_name: str,
    methods: list[str],
    labels: list[str],
    output_name: str,
    title: str,
) -> None:
    rows = read_rows(ARTIFACTS_DIR / csv_name)
    n_values = sorted({int(row["N"]) for row in rows})

    plt.figure(figsize=(8, 5))
    for method, label in zip(methods, labels):
        values = []
        for n in n_values:
            row = get_row(rows, method, n)
            values.append(parse_bytes(row["Allocated"]))
        plt.plot(n_values, values, marker="o", linewidth=2, label=label)

    plt.title(title)
    plt.xlabel("N")
    plt.ylabel("Allocated, bytes")
    plt.grid(alpha=0.3)
    plt.legend()
    plt.tight_layout()
    plt.savefig(ARTIFACTS_DIR / output_name, dpi=160)
    plt.close()


def main() -> None:
    plot_latency_with_stddev(
        csv_name="Hw1.Benchmarks.FileBucketHashBenchmarks-report.csv",
        methods=["InsertFileHash", "InsertDictionary"],
        labels=["FileBucketHash", "Dictionary"],
        output_name="filebucket_latency.png",
        title="FileBucketHash benchmarks: mean latency",
    )
    plot_cv_percent(
        csv_name="Hw1.Benchmarks.FileBucketHashBenchmarks-report.csv",
        methods=["InsertFileHash", "InsertDictionary"],
        labels=["FileBucketHash", "Dictionary"],
        output_name="filebucket_cv.png",
        title="FileBucketHash benchmarks: variability (CV)",
    )
    plot_speedup_ratio(
        csv_name="Hw1.Benchmarks.FileBucketHashBenchmarks-report.csv",
        baseline_method="InsertDictionary",
        compared_method="InsertFileHash",
        output_name="filebucket_speedup.png",
        title="FileBucketHash benchmarks: speedup vs dictionary",
    )
    plot_allocated_bytes(
        csv_name="Hw1.Benchmarks.FileBucketHashBenchmarks-report.csv",
        methods=["InsertFileHash", "InsertDictionary"],
        labels=["FileBucketHash", "Dictionary"],
        output_name="filebucket_allocated.png",
        title="FileBucketHash benchmarks: allocated bytes",
    )

    plot_latency_with_stddev(
        csv_name="Hw1.Benchmarks.StaticPerfectHashBenchmarks-report.csv",
        methods=["LookupPerfectHash", "LookupDictionary"],
        labels=["StaticPerfectHash", "Dictionary"],
        output_name="perfecthash_latency.png",
        title="StaticPerfectHash benchmarks: mean latency",
    )
    plot_cv_percent(
        csv_name="Hw1.Benchmarks.StaticPerfectHashBenchmarks-report.csv",
        methods=["LookupPerfectHash", "LookupDictionary"],
        labels=["StaticPerfectHash", "Dictionary"],
        output_name="perfecthash_cv.png",
        title="StaticPerfectHash benchmarks: variability (CV)",
    )
    plot_speedup_ratio(
        csv_name="Hw1.Benchmarks.StaticPerfectHashBenchmarks-report.csv",
        baseline_method="LookupDictionary",
        compared_method="LookupPerfectHash",
        output_name="perfecthash_speedup.png",
        title="StaticPerfectHash benchmarks: speedup vs dictionary",
    )
    plot_allocated_bytes(
        csv_name="Hw1.Benchmarks.StaticPerfectHashBenchmarks-report.csv",
        methods=["LookupPerfectHash", "LookupDictionary"],
        labels=["StaticPerfectHash", "Dictionary"],
        output_name="perfecthash_allocated.png",
        title="StaticPerfectHash benchmarks: allocated bytes",
    )

    plot_latency_with_stddev(
        csv_name="Hw1.Benchmarks.TextLshBenchmarks-report.csv",
        methods=["QueryLsh", "QueryFullScan"],
        labels=["LSH query", "Full scan query"],
        output_name="textlsh_latency.png",
        title="TextLSH benchmarks: mean latency",
    )
    plot_cv_percent(
        csv_name="Hw1.Benchmarks.TextLshBenchmarks-report.csv",
        methods=["QueryLsh", "QueryFullScan"],
        labels=["LSH query", "Full scan query"],
        output_name="textlsh_cv.png",
        title="TextLSH benchmarks: variability (CV)",
    )
    plot_speedup_ratio(
        csv_name="Hw1.Benchmarks.TextLshBenchmarks-report.csv",
        baseline_method="QueryFullScan",
        compared_method="QueryLsh",
        output_name="textlsh_speedup.png",
        title="TextLSH benchmarks: speedup vs full scan",
    )
    plot_allocated_bytes(
        csv_name="Hw1.Benchmarks.TextLshBenchmarks-report.csv",
        methods=["QueryLsh", "QueryFullScan"],
        labels=["LSH query", "Full scan query"],
        output_name="textlsh_allocated.png",
        title="TextLSH benchmarks: allocated bytes",
    )


if __name__ == "__main__":
    main()
