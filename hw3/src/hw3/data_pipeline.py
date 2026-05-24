from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path

import numpy as np
from datasets import load_dataset
from tqdm import tqdm

from .config import DataConfig


@dataclass(frozen=True)
class PreparedDataPaths:
    corpus_vectors: Path
    query_indices: Path
    metadata: Path


def default_paths(data_dir: Path) -> PreparedDataPaths:
    return PreparedDataPaths(
        corpus_vectors=data_dir / "corpus_vectors.npy",
        query_indices=data_dir / "query_indices.npy",
        metadata=data_dir / "dataset_meta.npz",
    )


def prepare_dataset(data_dir: Path, cfg: DataConfig) -> PreparedDataPaths:
    paths = default_paths(data_dir)
    if paths.corpus_vectors.exists() and paths.query_indices.exists():
        if _has_sufficient_cached_data(paths, cfg):
            return paths
        # Rebuild cache when existing arrays are smaller than required preset.
        paths.corpus_vectors.unlink(missing_ok=True)
        paths.query_indices.unlink(missing_ok=True)
        paths.metadata.unlink(missing_ok=True)

    ds = load_dataset(cfg.dataset_name, split=cfg.split, streaming=True)
    embedding_column = cfg.embedding_column or _detect_embedding_column(ds)
    vectors = _sample_vectors(
        dataset=ds,
        embedding_column=embedding_column,
        target_size=cfg.corpus_size,
        scan_limit=cfg.sample_scan_limit,
        seed=cfg.random_seed,
    )

    if vectors.shape[0] < cfg.query_size:
        raise ValueError(
            f"Not enough vectors sampled: {vectors.shape[0]} < query_size={cfg.query_size}"
        )

    rng = np.random.default_rng(cfg.random_seed)
    query_indices = np.sort(
        rng.choice(vectors.shape[0], size=cfg.query_size, replace=False)
    ).astype(np.int64)

    np.save(paths.corpus_vectors, vectors)
    np.save(paths.query_indices, query_indices)
    np.savez(
        paths.metadata,
        dataset_name=cfg.dataset_name,
        split=cfg.split,
        embedding_column=embedding_column,
        corpus_size=vectors.shape[0],
        dim=vectors.shape[1],
        query_size=cfg.query_size,
        random_seed=cfg.random_seed,
    )
    return paths


def load_prepared_data(data_dir: Path) -> tuple[np.ndarray, np.ndarray]:
    paths = default_paths(data_dir)
    for required in [paths.corpus_vectors, paths.query_indices]:
        if not required.exists():
            raise FileNotFoundError(f"Missing prepared data file: {required}")
    vectors = np.load(paths.corpus_vectors)
    query_indices = np.load(paths.query_indices)
    return vectors.astype(np.float32, copy=False), query_indices.astype(np.int64, copy=False)


def _detect_embedding_column(dataset_stream) -> str:
    for row in dataset_stream.take(20):
        for key, value in row.items():
            if _is_vector(value):
                return key
    raise ValueError("Could not auto-detect embedding column. Pass --embedding-column explicitly.")


def _sample_vectors(
    dataset,
    embedding_column: str,
    target_size: int,
    scan_limit: int,
    seed: int,
) -> np.ndarray:
    rng = np.random.default_rng(seed)
    reservoir: list[np.ndarray] = []
    scanned = 0
    progress = tqdm(total=target_size, desc="Sampling embeddings", unit="vec")

    for row in dataset:
        if scanned >= scan_limit:
            break
        scanned += 1
        value = row.get(embedding_column)
        if not _is_vector(value):
            continue
        vec = np.asarray(value, dtype=np.float32)
        if len(reservoir) < target_size:
            reservoir.append(vec)
            progress.update(1)
            continue

        # Reservoir sampling keeps a nearly uniform sample from large streams.
        j = int(rng.integers(0, scanned))
        if j < target_size:
            reservoir[j] = vec

    progress.close()
    if not reservoir:
        raise ValueError(f"No vectors extracted from column '{embedding_column}'.")

    min_dim = min(v.shape[0] for v in reservoir)
    matrix = np.stack([v[:min_dim] for v in reservoir], axis=0).astype(np.float32, copy=False)
    return matrix


def _is_vector(value: object) -> bool:
    if isinstance(value, np.ndarray):
        return value.ndim == 1 and value.size > 0
    if isinstance(value, list):
        return len(value) > 0 and all(isinstance(x, (int, float)) for x in value)
    return False


def _has_sufficient_cached_data(paths: PreparedDataPaths, cfg: DataConfig) -> bool:
    vectors = np.load(paths.corpus_vectors, mmap_mode="r")
    query_indices = np.load(paths.query_indices, mmap_mode="r")
    if vectors.shape[0] < cfg.corpus_size:
        return False
    if query_indices.shape[0] < cfg.query_size:
        return False
    if np.max(query_indices) >= vectors.shape[0]:
        return False
    return True
