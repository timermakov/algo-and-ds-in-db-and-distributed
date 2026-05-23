from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
from typing import Literal


PresetName = Literal["smoke", "coarse", "fine", "final"]
AlgorithmName = Literal["lsh", "hnsw", "ivfpq"]


@dataclass(frozen=True)
class DataConfig:
    dataset_name: str = "Qdrant/arxiv-abstracts-instructorxl-embeddings"
    split: str = "train"
    embedding_column: str | None = None
    random_seed: int = 42
    corpus_size: int = 200_000
    query_size: int = 10_000
    top_k: int = 100
    sample_scan_limit: int = 2_000_000


@dataclass(frozen=True)
class EvalConfig:
    warmup_runs: int
    repeat_runs: int


@dataclass(frozen=True)
class HnswConfig:
    m: int
    ef_construction: int
    ef_search: int


@dataclass(frozen=True)
class IvfPqConfig:
    nlist: int
    nprobe: int
    m_pq: int
    pq_bits: int = 8


@dataclass(frozen=True)
class IvfFlatConfig:
    """IVF without PQ - exact L2 within clusters."""
    nlist: int
    nprobe: int


@dataclass(frozen=True)
class LshConfig:
    nbits: int


@dataclass(frozen=True)
class BenchmarkPreset:
    name: PresetName
    data: DataConfig
    eval: EvalConfig
    lsh_grid: list[LshConfig]
    hnsw_grid: list[HnswConfig]
    ivfpq_grid: list[IvfPqConfig]
    ivf_flat_grid: list[IvfFlatConfig] | None = None


# База под целевой режим: 100k векторов, 10k запросов.
BASE_DATA_CONFIG = DataConfig(
    random_seed=42,
    corpus_size=100_000,
    query_size=10_000,
    sample_scan_limit=100_000,
)


def get_preset(name: PresetName) -> BenchmarkPreset:
    if name == "smoke":
        # Быстрый sanity-check на одной конфигурации алгоритма.
        return BenchmarkPreset(
            name=name,
            data=BASE_DATA_CONFIG,
            eval=EvalConfig(warmup_runs=1, repeat_runs=3),
            lsh_grid=[LshConfig(nbits=512)],
            hnsw_grid=[HnswConfig(m=16, ef_construction=100, ef_search=128)],
            ivfpq_grid=[IvfPqConfig(nlist=1024, nprobe=64, m_pq=16, pq_bits=8)],
            ivf_flat_grid=[IvfFlatConfig(nlist=1024, nprobe=64)],
        )

    if name == "coarse":
        # Умеренно широкая сетка: много точек без взрывного времени.
        return BenchmarkPreset(
            name=name,
            data=BASE_DATA_CONFIG,
            eval=EvalConfig(warmup_runs=1, repeat_runs=3),
            lsh_grid=[LshConfig(nbits=v) for v in [256, 512, 1024, 2048, 4096]],
            hnsw_grid=[
                HnswConfig(m=m, ef_construction=150, ef_search=efs)
                for m in [8, 16, 32]
                for efs in [32, 64, 128]
            ],
            ivfpq_grid=[
                IvfPqConfig(nlist=1024, nprobe=nprobe, m_pq=mpq, pq_bits=pq_bits)
                for nprobe in [16, 32, 64, 128]
                for mpq in [8, 16, 24]
                for pq_bits in [8, 10]
            ],
            ivf_flat_grid=[
                IvfFlatConfig(nlist=1024, nprobe=nprobe)
                for nprobe in [16, 32, 64, 128, 256, 512]
            ],
        )

    if name == "fine":
        # Локальное уточнение вокруг кандидатов с лучшим quality/speed.
        return BenchmarkPreset(
            name=name,
            data=BASE_DATA_CONFIG,
            eval=EvalConfig(warmup_runs=1, repeat_runs=3),
            lsh_grid=[LshConfig(nbits=v) for v in [1024, 1536, 2048]],
            hnsw_grid=[
                HnswConfig(m=m, ef_construction=200, ef_search=efs)
                for m in [16, 32]
                for efs in [96, 128, 192, 256]
            ],
            ivfpq_grid=[
                IvfPqConfig(nlist=1024, nprobe=nprobe, m_pq=mpq, pq_bits=pq_bits)
                for nprobe in [64, 96, 128, 192]
                for mpq in [12, 16, 24]
                for pq_bits in [8, 10]
            ],
            ivf_flat_grid=[
                IvfFlatConfig(nlist=1024, nprobe=nprobe)
                for nprobe in [64, 96, 128, 192, 256]
            ],
        )

    if name == "final":
        # Финальный профиль для защиты: 1-3 лучших на алгоритм.
        return BenchmarkPreset(
            name=name,
            data=BASE_DATA_CONFIG,
            eval=EvalConfig(warmup_runs=2, repeat_runs=5),
            lsh_grid=[LshConfig(nbits=v) for v in [1024, 1536, 2048]],
            hnsw_grid=[
                HnswConfig(m=16, ef_construction=200, ef_search=128),
                HnswConfig(m=16, ef_construction=200, ef_search=192),
                HnswConfig(m=32, ef_construction=200, ef_search=128),
            ],
            ivfpq_grid=[
                IvfPqConfig(nlist=1024, nprobe=96, m_pq=16, pq_bits=8),
                IvfPqConfig(nlist=1024, nprobe=128, m_pq=16, pq_bits=8),
                IvfPqConfig(nlist=1024, nprobe=128, m_pq=12, pq_bits=10),
                IvfPqConfig(nlist=1024, nprobe=128, m_pq=48, pq_bits=10),
                IvfPqConfig(nlist=1024, nprobe=256, m_pq=48, pq_bits=10),
                IvfPqConfig(nlist=1024, nprobe=128, m_pq=96, pq_bits=8),
                IvfPqConfig(nlist=1024, nprobe=256, m_pq=96, pq_bits=8),
                IvfPqConfig(nlist=512, nprobe=256, m_pq=96, pq_bits=8),
                IvfPqConfig(nlist=512, nprobe=512, m_pq=192, pq_bits=8),
            ],
            ivf_flat_grid=[
                IvfFlatConfig(nlist=1024, nprobe=64),
                IvfFlatConfig(nlist=1024, nprobe=128),
                IvfFlatConfig(nlist=1024, nprobe=256),
            ],
        )

    raise ValueError(f"Unsupported preset: {name}")


def ensure_dirs(data_dir: Path, artifacts_dir: Path) -> None:
    data_dir.mkdir(parents=True, exist_ok=True)
    artifacts_dir.mkdir(parents=True, exist_ok=True)
