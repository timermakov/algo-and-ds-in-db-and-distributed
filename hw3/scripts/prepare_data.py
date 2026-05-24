from __future__ import annotations

import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "src"))

from hw3.config import DataConfig
from hw3.data_pipeline import prepare_dataset


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Prepare embedding corpus and query subset.")
    parser.add_argument("--data-dir", type=Path, default=Path("data"))
    parser.add_argument(
        "--dataset-name",
        type=str,
        default="Qdrant/arxiv-abstracts-instructorxl-embeddings",
    )
    parser.add_argument("--split", type=str, default="train")
    parser.add_argument("--embedding-column", type=str, default=None)
    parser.add_argument("--corpus-size", type=int, default=100_000)
    parser.add_argument("--query-size", type=int, default=10_000)
    parser.add_argument("--scan-limit", type=int, default=100_000)
    parser.add_argument("--seed", type=int, default=42)
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    args.data_dir.mkdir(parents=True, exist_ok=True)
    cfg = DataConfig(
        dataset_name=args.dataset_name,
        split=args.split,
        embedding_column=args.embedding_column,
        corpus_size=args.corpus_size,
        query_size=args.query_size,
        sample_scan_limit=args.scan_limit,
        random_seed=args.seed,
    )
    paths = prepare_dataset(args.data_dir, cfg)
    print(f"Prepared vectors: {paths.corpus_vectors}")
    print(f"Prepared query ids: {paths.query_indices}")


if __name__ == "__main__":
    main()
