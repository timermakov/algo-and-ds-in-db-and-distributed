from __future__ import annotations

import csv
import math
from collections import defaultdict
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
        return list(csv.DictReader(file))


def group_by_param(rows: list[dict[str, str]], param_name: str) -> dict[int, list[dict[str, str]]]:
    groups: dict[int, list[dict[str, str]]] = defaultdict(list)
    for row in rows:
        groups[int(float(row[param_name]))].append(row)
    return groups


def get_row(rows: list[dict[str, str]], method: str, n: int) -> dict[str, str]:
    return next(r for r in rows if int(r["N"]) == n and r["Method"] == method)


def plot_latency_with_stddev(
    rows: list[dict[str, str]],
    methods: list[str],
    labels: list[str],
    output_name: str,
    title: str,
) -> None:
    n_values = sorted({int(row["N"]) for row in rows})
    plt.figure(figsize=(8, 5))
    for method, label in zip(methods, labels):
        values = []
        stddevs = []
        for n in n_values:
            row = get_row(rows, method, n)
            values.append(parse_microseconds(row["Mean"]))
            stddevs.append(parse_microseconds(row["StdDev"]))
        plt.errorbar(n_values, values, yerr=stddevs, marker="o", linewidth=2, capsize=4, elinewidth=1.2, label=label)

    plt.title(title)
    plt.xlabel("N")
    plt.ylabel("Mean ± StdDev, us")
    plt.grid(alpha=0.3)
    plt.legend()
    plt.tight_layout()
    plt.savefig(ARTIFACTS_DIR / output_name, dpi=160)
    plt.close()


def plot_cv_percent(
    rows: list[dict[str, str]],
    methods: list[str],
    labels: list[str],
    output_name: str,
    title: str,
) -> None:
    n_values = sorted({int(row["N"]) for row in rows})
    plt.figure(figsize=(8, 5))
    for method, label in zip(methods, labels):
        values = []
        for n in n_values:
            row = get_row(rows, method, n)
            mean = parse_microseconds(row["Mean"])
            stddev = parse_microseconds(row["StdDev"])
            values.append(0.0 if mean == 0.0 else (stddev / mean) * 100.0)
        plt.plot(n_values, values, marker="o", linewidth=2, label=label)

    plt.title(title)
    plt.xlabel("N")
    plt.ylabel("CV, % (StdDev / Mean)")
    plt.grid(alpha=0.3)
    plt.legend()
    plt.tight_layout()
    plt.savefig(ARTIFACTS_DIR / output_name, dpi=160)
    plt.close()


def plot_speedup_ratio(
    rows: list[dict[str, str]],
    baseline_method: str,
    compared_method: str,
    output_name: str,
    title: str,
) -> None:
    n_values = sorted({int(row["N"]) for row in rows})
    values = []
    for n in n_values:
        baseline = parse_microseconds(get_row(rows, baseline_method, n)["Mean"])
        compared = parse_microseconds(get_row(rows, compared_method, n)["Mean"])
        values.append(0.0 if compared == 0.0 else baseline / compared)

    plt.figure(figsize=(8, 5))
    plt.plot(n_values, values, marker="o", linewidth=2, color="#8b0000")
    plt.axhline(1.0, linestyle="--", color="#444", linewidth=1)
    plt.title(title)
    plt.xlabel("N")
    plt.ylabel("Speedup (baseline / compared)")
    plt.grid(alpha=0.3)
    plt.tight_layout()
    plt.savefig(ARTIFACTS_DIR / output_name, dpi=160)
    plt.close()


def plot_allocated_bytes(
    rows: list[dict[str, str]],
    methods: list[str],
    labels: list[str],
    output_name: str,
    title: str,
) -> None:
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


def generate_suite(
    csv_name: str,
    param_name: str,
    fixed_param_value: int,
    methods: list[str],
    labels: list[str],
    short_name: str,
) -> None:
    csv_path = ARTIFACTS_DIR / csv_name
    if not csv_path.exists():
        return

    rows = read_rows(csv_path)
    grouped = group_by_param(rows, param_name)
    if fixed_param_value not in grouped:
        fixed_param_value = sorted(grouped.keys())[0]
    subset = grouped[fixed_param_value]

    title_suffix = f"{param_name}={fixed_param_value}"
    plot_latency_with_stddev(subset, methods, labels, f"{short_name}_latency.png", f"{short_name}: mean latency ({title_suffix})")
    plot_cv_percent(subset, methods, labels, f"{short_name}_cv.png", f"{short_name}: variability ({title_suffix})")
    plot_speedup_ratio(subset, methods[1], methods[0], f"{short_name}_speedup.png", f"{short_name}: speedup vs baseline ({title_suffix})")
    plot_allocated_bytes(subset, methods, labels, f"{short_name}_allocated.png", f"{short_name}: allocated bytes ({title_suffix})")


def main() -> None:
    generate_suite(
        csv_name="Hw2.Benchmarks.GeoKdTreeRadiusBenchmarks-report.csv",
        param_name="RadiusMeters",
        fixed_param_value=1_000,
        methods=["QueryKdTreeRadius", "QueryFullScanRadius"],
        labels=["KD-tree radius", "Full scan radius"],
        short_name="geo_radius",
    )
    generate_suite(
        csv_name="Hw2.Benchmarks.GeoKdTreeKnnBenchmarks-report.csv",
        param_name="K",
        fixed_param_value=10,
        methods=["QueryKdTreeKnn", "QueryFullScanKnn"],
        labels=["KD-tree kNN", "Full scan kNN"],
        short_name="geo_knn",
    )


if __name__ == "__main__":
    main()
