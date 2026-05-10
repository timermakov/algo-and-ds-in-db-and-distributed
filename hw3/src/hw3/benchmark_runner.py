from __future__ import annotations

import json
import random
from pathlib import Path
from typing import Any

import faiss
import numpy as np
import pandas as pd

from .config import BenchmarkPreset, HnswConfig, IvfPqConfig, LshConfig
from .data_pipeline import load_prepared_data
from .evaluator import RunMetrics, evaluate_once, summarize_runs
from .ground_truth import load_ground_truth
from .index_builders import (
    build_hnsw,
    build_ivfpq,
    build_lsh,
    config_to_dict,
    measure_index_size_mb,
)


def run_benchmark(
    data_dir: Path,
    artifacts_dir: Path,
    preset: BenchmarkPreset,
    ground_truth_path: Path | None = None,
) -> tuple[Path, Path]:
    _setup_reproducibility(seed=preset.data.random_seed)
    vectors, query_indices = load_prepared_data(data_dir)
    gt_path = ground_truth_path or (data_dir / "ground_truth_top100.npy")
    ground_truth = load_ground_truth(gt_path)
    query_vectors = vectors[query_indices]

    rows: list[dict[str, Any]] = []
    rows.extend(
        _run_grid(
            algorithm="lsh",
            grid=preset.lsh_grid,
            vectors=vectors,
            query_vectors=query_vectors,
            query_indices=query_indices,
            ground_truth=ground_truth,
            warmups=preset.eval.warmup_runs,
            repeats=preset.eval.repeat_runs,
            top_k=preset.data.top_k,
        )
    )
    rows.extend(
        _run_grid(
            algorithm="hnsw",
            grid=preset.hnsw_grid,
            vectors=vectors,
            query_vectors=query_vectors,
            query_indices=query_indices,
            ground_truth=ground_truth,
            warmups=preset.eval.warmup_runs,
            repeats=preset.eval.repeat_runs,
            top_k=preset.data.top_k,
        )
    )
    rows.extend(
        _run_grid(
            algorithm="ivfpq",
            grid=preset.ivfpq_grid,
            vectors=vectors,
            query_vectors=query_vectors,
            query_indices=query_indices,
            ground_truth=ground_truth,
            warmups=preset.eval.warmup_runs,
            repeats=preset.eval.repeat_runs,
            top_k=preset.data.top_k,
        )
    )

    detail_df = pd.DataFrame(rows)
    out_csv = artifacts_dir / f"benchmark_{preset.name}.csv"
    detail_df.to_csv(out_csv, index=False)

    best_path = artifacts_dir / f"best_configs_{preset.name}.json"
    _write_best_configs(detail_df, preset, best_path)
    return out_csv, best_path


def _setup_reproducibility(seed: int) -> None:
    random.seed(seed)
    np.random.seed(seed)
    # Keep deterministic behavior across repeated runs.
    faiss.omp_set_num_threads(1)


def _run_grid(
    algorithm: str,
    grid: list[LshConfig] | list[HnswConfig] | list[IvfPqConfig],
    vectors,
    query_vectors,
    query_indices,
    ground_truth,
    warmups: int,
    repeats: int,
    top_k: int,
) -> list[dict[str, Any]]:
    output: list[dict[str, Any]] = []
    for cfg in grid:
        if algorithm == "lsh":
            index, build_s = build_lsh(vectors, cfg)  # type: ignore[arg-type]
        elif algorithm == "hnsw":
            index, build_s = build_hnsw(vectors, cfg)  # type: ignore[arg-type]
        elif algorithm == "ivfpq":
            index, build_s = build_ivfpq(vectors, cfg)  # type: ignore[arg-type]
        else:
            raise ValueError(f"Unknown algorithm: {algorithm}")

        for _ in range(warmups):
            evaluate_once(index, query_vectors, query_indices, ground_truth, top_k)

        runs: list[RunMetrics] = []
        for _ in range(repeats):
            runs.append(evaluate_once(index, query_vectors, query_indices, ground_truth, top_k))

        summary = summarize_runs(runs)
        row = {
            "algorithm": algorithm,
            "config_json": json.dumps(config_to_dict(cfg), sort_keys=True),
            "build_s": build_s,
            "size_mb": measure_index_size_mb(index),
            "mean_recall": summary.mean_recall,
            "std_recall": summary.std_recall,
            "cv_recall": summary.cv_recall,
            "mean_qps": summary.mean_qps,
            "std_qps": summary.std_qps,
            "cv_qps": summary.cv_qps,
            "mean_latency_ms": summary.mean_latency_ms,
            "std_latency_ms": summary.std_latency_ms,
            "cv_latency_ms": summary.cv_latency_ms,
            "repeats": repeats,
            "warmups": warmups,
        }
        output.append(row)
    return output


def _write_best_configs(df: pd.DataFrame, preset: BenchmarkPreset, path: Path) -> None:
    best: dict[str, Any] = {"preset": preset.name, "rules": []}
    recall_target = 0.80
    for algorithm in ["lsh", "hnsw", "ivfpq"]:
        sub = df[df["algorithm"] == algorithm].copy()
        if sub.empty:
            continue

        feasible = sub[sub["mean_recall"] >= recall_target]
        if feasible.empty:
            best_row = sub.sort_values(
                by=["mean_recall", "mean_qps", "size_mb"],
                ascending=[False, False, True],
            ).iloc[0]
            rule = "fallback: maximize recall then qps"
        else:
            best_row = feasible.sort_values(
                by=["mean_qps", "size_mb", "build_s"],
                ascending=[False, True, True],
            ).iloc[0]
            rule = f"recall>={recall_target}, then max qps"

        best["rules"].append(
            {
                "algorithm": algorithm,
                "selection_rule": rule,
                "row": {
                    "config": json.loads(best_row["config_json"]),
                    "mean_recall": float(best_row["mean_recall"]),
                    "mean_qps": float(best_row["mean_qps"]),
                    "mean_latency_ms": float(best_row["mean_latency_ms"]),
                    "size_mb": float(best_row["size_mb"]),
                    "build_s": float(best_row["build_s"]),
                    "cv_qps": float(best_row["cv_qps"]),
                },
            }
        )

    path.write_text(json.dumps(best, indent=2), encoding="utf-8")
