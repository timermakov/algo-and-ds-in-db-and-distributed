from __future__ import annotations

from pathlib import Path

import faiss
import numpy as np


def build_ground_truth(
    vectors: np.ndarray,
    query_indices: np.ndarray,
    top_k: int,
    batch_size: int = 256,
) -> np.ndarray:
    dim = vectors.shape[1]
    index = faiss.IndexFlatL2(dim)
    index.add(vectors)
    queries = vectors[query_indices]

    all_neighbors: list[np.ndarray] = []
    for start in range(0, queries.shape[0], batch_size):
        batch = queries[start : start + batch_size]
        _, indices = index.search(batch, top_k + 1)
        cleaned = _drop_self(indices, query_indices[start : start + batch_size], top_k)
        all_neighbors.append(cleaned)
    return np.vstack(all_neighbors).astype(np.int64, copy=False)


def save_ground_truth(path: Path, neighbors: np.ndarray) -> None:
    np.save(path, neighbors)


def load_ground_truth(path: Path) -> np.ndarray:
    if not path.exists():
        raise FileNotFoundError(f"Ground truth not found: {path}")
    return np.load(path).astype(np.int64, copy=False)


def _drop_self(indices: np.ndarray, query_ids: np.ndarray, top_k: int) -> np.ndarray:
    output = np.empty((indices.shape[0], top_k), dtype=np.int64)
    for i, row in enumerate(indices):
        qid = int(query_ids[i])
        filtered = row[row != qid][:top_k]
        if filtered.shape[0] < top_k:
            padded = np.full((top_k,), -1, dtype=np.int64)
            padded[: filtered.shape[0]] = filtered
            output[i] = padded
        else:
            output[i] = filtered
    return output
