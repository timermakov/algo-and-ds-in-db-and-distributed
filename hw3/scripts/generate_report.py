from __future__ import annotations

import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "src"))

from hw3.reporting import generate_report_artifacts, update_report_markdown


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Generate charts and markdown report from benchmark csv.")
    parser.add_argument("--artifacts-dir", type=Path, default=Path("report/artifacts"))
    parser.add_argument("--report-path", type=Path, default=Path("report/REPORT.md"))
    parser.add_argument("--preset", type=str, default="coarse")
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    args.artifacts_dir.mkdir(parents=True, exist_ok=True)
    args.report_path.parent.mkdir(parents=True, exist_ok=True)
    summary_path, plot_path = generate_report_artifacts(args.artifacts_dir, args.preset)
    update_report_markdown(args.report_path, args.preset, args.artifacts_dir)
    print(f"Summary markdown: {summary_path}")
    print(f"Tradeoff plot: {plot_path}")
    print(f"Report: {args.report_path}")


if __name__ == "__main__":
    main()
