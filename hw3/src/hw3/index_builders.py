from __future__ import annotations

from dataclasses import asdict
from time import perf_counter
from typing import Any

import faiss
import numpy as np

from .config import HnswConfig, IvfPqConfig, LshConfig


def build_lsh(vectors: np.ndarray, cfg: LshConfig) -> tuple[faiss.IndexLSH, float]:
    t0 = perf_counter()
    index = faiss.IndexLSH(vectors.shape[1], cfg.nbits)
    index.add(vectors)
    return index, perf_counter() - t0


def build_hnsw(vectors: np.ndarray, cfg: HnswConfig) -> tuple[faiss.IndexHNSWFlat, float]:
    t0 = perf_counter()
    index = faiss.IndexHNSWFlat(vectors.shape[1], cfg.m)
    index.hnsw.efConstruction = cfg.ef_construction
    index.hnsw.efSearch = cfg.ef_search
    index.add(vectors)
    return index, perf_counter() - t0


def build_ivfpq(vectors: np.ndarray, cfg: IvfPqConfig) -> tuple[faiss.IndexIVFPQ, float]:
    dim = vectors.shape[1]
    if dim % cfg.m_pq != 0:
        raise ValueError(f"Invalid m_pq={cfg.m_pq} for dim={dim}; dim must be divisible by m_pq.")

    t0 = perf_counter()
    quantizer = faiss.IndexFlatL2(dim)
    index = faiss.IndexIVFPQ(quantizer, dim, cfg.nlist, cfg.m_pq, cfg.pq_bits)
    index.train(vectors)
    index.add(vectors)
    index.nprobe = cfg.nprobe
    return index, perf_counter() - t0


def build_ivf_flat(vectors: np.ndarray, nlist: int, nprobe: int) -> tuple[faiss.IndexIVFFlat, float]:
    """Build IVF without PQ (IndexIVFFlat) - exact L2 within clusters."""
    dim = vectors.shape[1]
    t0 = perf_counter()
    quantizer = faiss.IndexFlatL2(dim)
    index = faiss.IndexIVFFlat(quantizer, dim, nlist)
    index.train(vectors)
    index.add(vectors)
    index.nprobe = nprobe
    return index, perf_counter() - t0


def measure_index_size_mb(index: faiss.Index) -> float:
    return float(faiss.serialize_index(index).nbytes / (1024 * 1024))


def config_to_dict(cfg: Any) -> dict[str, Any]:
    return asdict(cfg)
