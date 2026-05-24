from __future__ import annotations

from dataclasses import dataclass
from statistics import mean, pstdev
from time import perf_counter
from typing import Iterable

import faiss
import numpy as np


@dataclass(frozen=True)
class RunMetrics:
    recall_at_100: float
    qps: float
    latency_ms: float


@dataclass(frozen=True)
class SummaryMetrics:
    mean_recall: float
    std_recall: float
    cv_recall: float
    mean_qps: float
    std_qps: float
    cv_qps: float
    mean_latency_ms: float
    std_latency_ms: float
    cv_latency_ms: float


def evaluate_once(
    index: faiss.Index,
    query_vectors: np.ndarray,
    query_indices: np.ndarray,
    ground_truth: np.ndarray,
    top_k: int,
) -> RunMetrics:
    t0 = perf_counter()
    _, predicted = index.search(query_vectors, top_k + 1)
    elapsed = perf_counter() - t0
    neighbors = _drop_self(predicted, query_indices, top_k)
    recall = recall_at_k(neighbors, ground_truth, top_k)
    qps = float(query_vectors.shape[0] / elapsed) if elapsed > 0 else 0.0
    latency_ms = float((elapsed * 1000.0) / query_vectors.shape[0]) if query_vectors.shape[0] else 0.0
    return RunMetrics(recall_at_100=recall, qps=qps, latency_ms=latency_ms)


def summarize_runs(runs: Iterable[RunMetrics]) -> SummaryMetrics:
    rows = list(runs)
    recalls = [x.recall_at_100 for x in rows]
    qps_values = [x.qps for x in rows]
    latency_values = [x.latency_ms for x in rows]
    return SummaryMetrics(
        mean_recall=mean(recalls),
        std_recall=_safe_std(recalls),
        cv_recall=_cv(recalls),
        mean_qps=mean(qps_values),
        std_qps=_safe_std(qps_values),
        cv_qps=_cv(qps_values),
        mean_latency_ms=mean(latency_values),
        std_latency_ms=_safe_std(latency_values),
        cv_latency_ms=_cv(latency_values),
    )


def recall_at_k(predicted: np.ndarray, ground_truth: np.ndarray, k: int) -> float:
    if predicted.shape[0] != ground_truth.shape[0]:
        raise ValueError("predicted and ground_truth row counts must match")
    total = 0.0
    for p_row, gt_row in zip(predicted, ground_truth, strict=True):
        p = set(int(x) for x in p_row[:k] if int(x) >= 0)
        g = set(int(x) for x in gt_row[:k] if int(x) >= 0)
        total += len(p.intersection(g)) / max(k, 1)
    return total / predicted.shape[0]


def _drop_self(predicted: np.ndarray, query_ids: np.ndarray, top_k: int) -> np.ndarray:
    output = np.empty((predicted.shape[0], top_k), dtype=np.int64)
    for i, row in enumerate(predicted):
        qid = int(query_ids[i])
        filtered = [int(x) for x in row if int(x) != qid and int(x) >= 0][:top_k]
        if len(filtered) < top_k:
            filtered.extend([-1] * (top_k - len(filtered)))
        output[i] = np.asarray(filtered, dtype=np.int64)
    return output


def _safe_std(values: list[float]) -> float:
    if len(values) <= 1:
        return 0.0
    return float(pstdev(values))


def _cv(values: list[float]) -> float:
    avg = mean(values)
    if avg == 0:
        return 0.0
    return _safe_std(values) / avg
