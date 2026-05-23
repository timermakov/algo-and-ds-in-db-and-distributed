from __future__ import annotations

import json
import random
from pathlib import Path
from time import perf_counter
from typing import Any

import faiss
import numpy as np
import pandas as pd

from .config import BenchmarkPreset, HnswConfig, IvfFlatConfig, IvfPqConfig, LshConfig
from .data_pipeline import load_prepared_data
from .evaluator import RunMetrics, evaluate_once, summarize_runs
from .ground_truth import load_ground_truth
from .index_builders import (
    build_hnsw,
    build_ivf_flat,
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
    algorithms: set[str] | None = None,
    resume: bool = True,
    max_single_search_s: float = 120.0,
) -> tuple[Path, Path]:
    _setup_reproducibility(seed=preset.data.random_seed)
    vectors, query_indices = load_prepared_data(data_dir)
    gt_path = ground_truth_path or (data_dir / "ground_truth_top100.npy")
    ground_truth = load_ground_truth(gt_path)
    query_vectors = vectors[query_indices]

    out_csv = artifacts_dir / f"benchmark_{preset.name}.csv"
    enabled_algorithms = ["lsh", "hnsw", "ivfpq"]
    if preset.ivf_flat_grid:
        enabled_algorithms.append("ivf_flat")
    if algorithms:
        enabled_algorithms = [name for name in enabled_algorithms if name in algorithms]
        if not enabled_algorithms:
            raise ValueError("No enabled algorithms left after filtering")

    existing_keys: set[str] = set()
    if out_csv.exists() and resume:
        existing_df = pd.read_csv(out_csv)
        for _, row in existing_df.iterrows():
            existing_keys.add(_config_key(str(row["algorithm"]), str(row["config_json"])))
    elif out_csv.exists() and not resume:
        out_csv.unlink()

    rows: list[dict[str, Any]] = []
    if "lsh" in enabled_algorithms:
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
                out_csv=out_csv,
                existing_keys=existing_keys,
                max_single_search_s=max_single_search_s,
            )
        )
    if "hnsw" in enabled_algorithms:
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
                out_csv=out_csv,
                existing_keys=existing_keys,
                max_single_search_s=max_single_search_s,
            )
        )
    if "ivfpq" in enabled_algorithms:
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
                out_csv=out_csv,
                existing_keys=existing_keys,
                max_single_search_s=max_single_search_s,
            )
        )
    if "ivf_flat" in enabled_algorithms and preset.ivf_flat_grid:
        rows.extend(
            _run_grid(
                algorithm="ivf_flat",
                grid=preset.ivf_flat_grid,
                vectors=vectors,
                query_vectors=query_vectors,
                query_indices=query_indices,
                ground_truth=ground_truth,
                warmups=preset.eval.warmup_runs,
                repeats=preset.eval.repeat_runs,
                top_k=preset.data.top_k,
                out_csv=out_csv,
                existing_keys=existing_keys,
                max_single_search_s=max_single_search_s,
            )
        )

    if out_csv.exists():
        detail_df = pd.read_csv(out_csv)
    else:
        detail_df = pd.DataFrame(rows)
        if not detail_df.empty:
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
    grid: list[LshConfig] | list[HnswConfig] | list[IvfPqConfig] | list[IvfFlatConfig],
    vectors,
    query_vectors,
    query_indices,
    ground_truth,
    warmups: int,
    repeats: int,
    top_k: int,
    out_csv: Path,
    existing_keys: set[str],
    max_single_search_s: float,
) -> list[dict[str, Any]]:
    output: list[dict[str, Any]] = []
    for idx, cfg in enumerate(grid, start=1):
        cfg_json = json.dumps(config_to_dict(cfg), sort_keys=True)
        key = _config_key(algorithm, cfg_json)
        if key in existing_keys:
            print(f"[resume] skip {algorithm} {cfg_json}")
            continue
        print(f"[{algorithm}] {idx}/{len(grid)} {cfg_json}")

        if algorithm == "lsh":
            index, build_s = build_lsh(vectors, cfg)  # type: ignore[arg-type]
        elif algorithm == "hnsw":
            index, build_s = build_hnsw(vectors, cfg)  # type: ignore[arg-type]
        elif algorithm == "ivfpq":
            index, build_s = build_ivfpq(vectors, cfg)  # type: ignore[arg-type]
        elif algorithm == "ivf_flat":
            index, build_s = build_ivf_flat(vectors, cfg.nlist, cfg.nprobe)  # type: ignore[arg-type]
        else:
            raise ValueError(f"Unknown algorithm: {algorithm}")

        for _ in range(warmups):
            evaluate_once(index, query_vectors, query_indices, ground_truth, top_k)

        runs: list[RunMetrics] = []
        skip_cfg = False
        for run_idx in range(repeats):
            t0 = perf_counter()
            run = evaluate_once(index, query_vectors, query_indices, ground_truth, top_k)
            elapsed_s = perf_counter() - t0
            if run_idx == 0 and elapsed_s > max_single_search_s:
                print(
                    f"[skip] {algorithm} {cfg_json} first run took "
                    f"{elapsed_s:.1f}s > limit {max_single_search_s:.1f}s"
                )
                skip_cfg = True
                break
            runs.append(run)
        if skip_cfg:
            continue

        summary = summarize_runs(runs)
        row = {
            "algorithm": algorithm,
            "config_json": cfg_json,
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
        _append_row_to_csv(out_csv, row)
        existing_keys.add(key)
    return output


def _append_row_to_csv(path: Path, row: dict[str, Any]) -> None:
    frame = pd.DataFrame([row])
    frame.to_csv(path, mode="a", header=not path.exists(), index=False)


def _config_key(algorithm: str, cfg_json: str) -> str:
    return f"{algorithm}::{cfg_json}"


def _write_best_configs(df: pd.DataFrame, preset: BenchmarkPreset, path: Path) -> None:
    best: dict[str, Any] = {"preset": preset.name, "rules": []}
    recall_target = 0.80
    algorithms = ["lsh", "hnsw", "ivfpq"]
    if preset.ivf_flat_grid:
        algorithms.append("ivf_flat")
    for algorithm in algorithms:
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
