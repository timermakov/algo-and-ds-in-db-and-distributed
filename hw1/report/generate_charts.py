from __future__ import annotations

import csv
from pathlib import Path

import matplotlib.pyplot as plt


ARTIFACTS_DIR = Path(__file__).parent / "artifacts"


def parse_microseconds(value: str) -> float:
    cleaned = value.replace("μs", "").replace(" ", "").replace(",", "")
    return float(cleaned)


def read_rows(csv_path: Path) -> list[dict[str, str]]:
    with csv_path.open("r", encoding="utf-8", newline="") as file:
        reader = csv.DictReader(file)
        return list(reader)


def plot_latency(
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
            row = next(r for r in rows if int(r["N"]) == n and r["Method"] == method)
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


def main() -> None:
    plot_latency(
        csv_name="Hw1.Benchmarks.FileBucketHashBenchmarks-report.csv",
        methods=["InsertFileHash", "InsertDictionary"],
        labels=["FileBucketHash", "Dictionary"],
        output_name="filebucket_latency.png",
        title="FileBucketHash benchmarks: mean latency",
    )
    plot_latency(
        csv_name="Hw1.Benchmarks.StaticPerfectHashBenchmarks-report.csv",
        methods=["LookupPerfectHash", "LookupDictionary"],
        labels=["StaticPerfectHash", "Dictionary"],
        output_name="perfecthash_latency.png",
        title="StaticPerfectHash benchmarks: mean latency",
    )
    plot_latency(
        csv_name="Hw1.Benchmarks.TextLshBenchmarks-report.csv",
        methods=["QueryLsh", "QueryFullScan"],
        labels=["LSH query", "Full scan query"],
        output_name="textlsh_latency.png",
        title="TextLSH benchmarks: mean latency",
    )


if __name__ == "__main__":
    main()
