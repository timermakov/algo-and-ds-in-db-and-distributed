from __future__ import annotations

import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "src"))

from hw3.data_pipeline import load_prepared_data
from hw3.ground_truth import build_ground_truth, save_ground_truth


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Build exact top-k ground truth with FAISS FlatL2.")
    parser.add_argument("--data-dir", type=Path, default=Path("data"))
    parser.add_argument("--top-k", type=int, default=100)
    parser.add_argument("--batch-size", type=int, default=256)
    parser.add_argument("--output", type=Path, default=None)
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    vectors, query_indices = load_prepared_data(args.data_dir)
    gt = build_ground_truth(vectors, query_indices, top_k=args.top_k, batch_size=args.batch_size)
    output_path = args.output or (args.data_dir / f"ground_truth_top{args.top_k}.npy")
    save_ground_truth(output_path, gt)
    print(f"Ground truth saved: {output_path}")
    print(f"Shape: {gt.shape}")


if __name__ == "__main__":
    main()
