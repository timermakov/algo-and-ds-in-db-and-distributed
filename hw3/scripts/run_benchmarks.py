from __future__ import annotations

import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "src"))

from hw3.benchmark_runner import run_benchmark
from hw3.config import ensure_dirs, get_preset


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Run ANN benchmark sweeps.")
    parser.add_argument("--data-dir", type=Path, default=Path("data"))
    parser.add_argument("--artifacts-dir", type=Path, default=Path("report/artifacts"))
    parser.add_argument("--preset", type=str, default="coarse", choices=["smoke", "coarse", "fine", "final"])
    parser.add_argument("--ground-truth", type=Path, default=None)
    parser.add_argument(
        "--algorithms",
        type=str,
        default="lsh,hnsw,ivfpq,ivf_flat",
        help="Comma-separated list: lsh,hnsw,ivfpq,ivf_flat",
    )
    parser.add_argument("--no-resume", action="store_true", help="Ignore existing CSV and recalculate everything")
    parser.add_argument(
        "--max-single-search-s",
        type=float,
        default=120.0,
        help="Skip config if first measured search run exceeds this threshold",
    )
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    preset = get_preset(args.preset)  # type: ignore[arg-type]
    ensure_dirs(args.data_dir, args.artifacts_dir)
    raw_algorithms = {item.strip().lower() for item in args.algorithms.split(",") if item.strip()}
    supported = {"lsh", "hnsw", "ivfpq", "ivf_flat"}
    unknown = raw_algorithms - supported
    if unknown:
        raise ValueError(f"Unsupported algorithms: {sorted(unknown)}")

    csv_path, best_path = run_benchmark(
        data_dir=args.data_dir,
        artifacts_dir=args.artifacts_dir,
        preset=preset,
        ground_truth_path=args.ground_truth,
        algorithms=raw_algorithms,
        resume=not args.no_resume,
        max_single_search_s=args.max_single_search_s,
    )
    print(f"Benchmark CSV: {csv_path}")
    print(f"Best configs JSON: {best_path}")


if __name__ == "__main__":
    main()
