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


BASE_DATA_CONFIG = DataConfig(
    random_seed=42,
    corpus_size=50_000,
    query_size=10_000,
    sample_scan_limit=50_000,
)


def get_preset(name: PresetName) -> BenchmarkPreset:
    if name == "smoke":
        return BenchmarkPreset(
            name=name,
            data=BASE_DATA_CONFIG,
            eval=EvalConfig(warmup_runs=1, repeat_runs=3),
            lsh_grid=[LshConfig(nbits=512)],
            hnsw_grid=[HnswConfig(m=16, ef_construction=100, ef_search=128)],
            ivfpq_grid=[IvfPqConfig(nlist=512, nprobe=16, m_pq=48)],
        )

    if name == "coarse":
        return BenchmarkPreset(
            name=name,
            data=BASE_DATA_CONFIG,
            eval=EvalConfig(warmup_runs=1, repeat_runs=3),
            lsh_grid=[LshConfig(nbits=v) for v in [512, 1024, 2048]],
            hnsw_grid=[
                HnswConfig(m=m, ef_construction=100, ef_search=ef)
                for m in [8, 16]
                for ef in [64, 128]
            ],
            ivfpq_grid=[
                IvfPqConfig(nlist=nlist, nprobe=nprobe, m_pq=48)
                for nlist in [512, 1024]
                for nprobe in [8, 32]
            ],
        )

    if name == "fine":
        return BenchmarkPreset(
            name=name,
            data=BASE_DATA_CONFIG,
            eval=EvalConfig(warmup_runs=1, repeat_runs=3),
            lsh_grid=[LshConfig(nbits=v) for v in [1024, 2048]],
            hnsw_grid=[
                HnswConfig(m=16, ef_construction=100, ef_search=ef)
                for ef in [96, 128, 160]
            ],
            ivfpq_grid=[
                IvfPqConfig(nlist=1024, nprobe=nprobe, m_pq=48)
                for nprobe in [16, 24, 32]
            ],
        )

    if name == "final":
        return BenchmarkPreset(
            name=name,
            data=BASE_DATA_CONFIG,
            eval=EvalConfig(warmup_runs=1, repeat_runs=3),
            lsh_grid=[LshConfig(nbits=1024)],
            hnsw_grid=[HnswConfig(m=16, ef_construction=100, ef_search=128)],
            ivfpq_grid=[IvfPqConfig(nlist=1024, nprobe=24, m_pq=48)],
        )

    raise ValueError(f"Unsupported preset: {name}")


def ensure_dirs(data_dir: Path, artifacts_dir: Path) -> None:
    data_dir.mkdir(parents=True, exist_ok=True)
    artifacts_dir.mkdir(parents=True, exist_ok=True)
