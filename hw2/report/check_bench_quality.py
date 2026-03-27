from __future__ import annotations

import csv
from dataclasses import dataclass
from pathlib import Path


@dataclass(frozen=True)
class CvRow:
    file_name: str
    method: str
    n: int
    mean_us: float
    stddev_us: float
    cv_percent: float


def parse_microseconds(value: str) -> float:
    raw = value.replace(" ", "").replace(",", "")
    if raw in {"NA", "-", "?"}:
        return float("nan")
    if raw.endswith("ns"):
        return float(raw[:-2]) / 1000.0
    if raw.endswith("μs"):
        return float(raw[:-2])
    if raw.endswith("us"):
        return float(raw[:-2])
    if raw.endswith("ms"):
        return float(raw[:-2]) * 1000.0
    return float(raw)


def load_rows(csv_path: Path) -> list[CvRow]:
    rows: list[CvRow] = []
    with csv_path.open("r", encoding="utf-8", newline="") as file:
        for row in csv.DictReader(file):
            mean = parse_microseconds(row["Mean"])
            stddev = parse_microseconds(row["StdDev"])
            if mean != mean or stddev != stddev:
                rows.append(
                    CvRow(
                        file_name=csv_path.name,
                        method=row["Method"],
                        n=int(row["N"]),
                        mean_us=float("nan"),
                        stddev_us=float("nan"),
                        cv_percent=float("nan"),
                    )
                )
                continue

            cv = 0.0 if mean == 0.0 else (stddev / mean) * 100.0
            rows.append(
                CvRow(
                    file_name=csv_path.name,
                    method=row["Method"],
                    n=int(row["N"]),
                    mean_us=mean,
                    stddev_us=stddev,
                    cv_percent=cv,
                )
            )
    return rows


def write_summary(summary_path: Path, rows: list[CvRow]) -> None:
    lines = [
        "# Benchmark CV quality check",
        "",
        "Summary is informational only, no fixed CV threshold is enforced.",
        "",
        "| File | Method | N | Mean (us) | StdDev (us) | CV (%) |",
        "|---|---|---:|---:|---:|---:|",
    ]

    for item in rows:
        lines.append(
            f"| {item.file_name} | {item.method} | {item.n} | "
            f"{item.mean_us:.3f} | {item.stddev_us:.3f} | {item.cv_percent:.2f} |"
        )

    summary_path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    artifacts_dir = Path(__file__).parent / "artifacts"
    csv_files = sorted(artifacts_dir.glob("*.csv"))
    if not csv_files:
        print("No CSV files found in report/artifacts")
        return 2

    rows: list[CvRow] = []
    for csv_file in csv_files:
        rows.extend(load_rows(csv_file))

    write_summary(artifacts_dir / "benchmark_quality.md", rows)
    print("CV quality report generated.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
