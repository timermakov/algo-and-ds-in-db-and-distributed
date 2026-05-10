from __future__ import annotations

import json
from pathlib import Path

import matplotlib.pyplot as plt
import pandas as pd


def generate_report_artifacts(artifacts_dir: Path, preset: str) -> tuple[Path, Path]:
    csv_path = artifacts_dir / f"benchmark_{preset}.csv"
    if not csv_path.exists():
        raise FileNotFoundError(f"Benchmark csv not found: {csv_path}")

    df = pd.read_csv(csv_path)
    summary_md_path = artifacts_dir / f"summary_{preset}.md"
    _write_summary_md(df, artifacts_dir / f"best_configs_{preset}.json", summary_md_path)
    _plot_tradeoffs(df, artifacts_dir, preset)
    _plot_parameter_sweeps(artifacts_dir)
    return summary_md_path, artifacts_dir / f"tradeoff_{preset}.png"


def update_report_markdown(report_path: Path, preset: str, artifacts_dir: Path) -> None:
    summary_file = artifacts_dir / f"summary_{preset}.md"
    best_file = artifacts_dir / f"best_configs_{preset}.json"
    if not summary_file.exists():
        raise FileNotFoundError(f"Missing summary file: {summary_file}")
    best_notes = _render_best_notes(best_file)
    report_body = (
        "# Отчёт по HW3: ANN-бенчмарк\n\n"
        "## Пресет\n"
        f"- `{preset}`\n\n"
        "## Гипотезы\n"
        "- HNSW даст максимальный Recall при большем размере индекса.\n"
        "- IVF+PQ даст лучшую компактность и высокий QPS, но с потерей Recall.\n"
        "- LSH даст быстрый build и хороший компромисс по скорости/памяти.\n\n"
        "## Критерии сравнения (явно)\n"
        "- Основные метрики: `Recall@100`, `QPS`, `latency_ms`, `build_s`, `size_mb`, `CV(QPS)`.\n"
        "- Правило выбора: сначала ограничение `Recall@100 >= 0.8` (если достижимо), затем максимум `QPS`, далее минимум `size_mb`, затем минимум `build_s`.\n\n"
        "## Результаты\n\n"
        + summary_file.read_text(encoding="utf-8")
        + "\n\n## Интерпретация\n\n"
        + best_notes
        + "\n\n## Пояснение по этапам coarse/fine/final\n\n"
        + "Этапы `coarse -> fine -> final` нужны для сужения области параметров и выбора рабочей конфигурации, а не для обязательного монотонного улучшения всех метрик на каждом шаге.\n"
        + "\n\n## Графики влияния параметров\n\n"
        + "### HNSW: влияние параметра m\n"
        + "![HNSW m sweep](artifacts/hnsw_m_sweep.png)\n\n"
        + "### HNSW: влияние параметра efSearch\n"
        + "![HNSW ef sweep](artifacts/hnsw_ef_sweep.png)\n\n"
        + "### LSH: влияние параметра nbits\n"
        + "![LSH nbits sweep](artifacts/lsh_nbits_sweep.png)\n\n"
        + "### IVF+PQ: влияние параметра nlist\n"
        + "![IVFPQ nlist sweep](artifacts/ivfpq_nlist_sweep.png)\n\n"
        + "### IVF+PQ: влияние параметра nprobe\n"
        + "![IVFPQ nprobe sweep](artifacts/ivfpq_nprobe_sweep.png)\n"
        + "\n\n"
        f"![График trade-off](artifacts/tradeoff_{preset}.png)\n"
    )
    report_path.write_text(report_body, encoding="utf-8")


def _write_summary_md(df: pd.DataFrame, best_json_path: Path, out_path: Path) -> None:
    lines: list[str] = []
    lines.append("### Агрегированные метрики\n")
    lines.append("| Алгоритм | Конфигурация | Recall@100 | QPS | Latency ms | Build s | Size MB | CV(QPS) |")
    lines.append("|---|---|---:|---:|---:|---:|---:|---:|")
    for _, row in df.sort_values(by=["algorithm", "mean_recall"], ascending=[True, False]).iterrows():
        lines.append(
            "| {alg} | `{cfg}` | {recall:.4f} | {qps:.0f} | {lat:.4f} | {build:.2f} | {size:.2f} | {cv:.4f} |".format(
                alg=row["algorithm"],
                cfg=row["config_json"],
                recall=row["mean_recall"],
                qps=row["mean_qps"],
                lat=row["mean_latency_ms"],
                build=row["build_s"],
                size=row["size_mb"],
                cv=row["cv_qps"],
            )
        )

    if best_json_path.exists():
        payload = json.loads(best_json_path.read_text(encoding="utf-8"))
        lines.append("\n### Лучшие конфигурации\n")
        for item in payload.get("rules", []):
            r = item["row"]
            lines.append(
                "- **{alg}**: `{cfg}` | recall={rec:.4f}, qps={qps:.0f}, size={size:.2f}MB, build={build:.2f}s ({rule})".format(
                    alg=item["algorithm"],
                    cfg=json.dumps(r["config"], ensure_ascii=False),
                    rec=r["mean_recall"],
                    qps=r["mean_qps"],
                    size=r["size_mb"],
                    build=r["build_s"],
                    rule=item["selection_rule"],
                )
            )
    out_path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def _plot_tradeoffs(df: pd.DataFrame, artifacts_dir: Path, preset: str) -> None:
    fig, axes = plt.subplots(1, 2, figsize=(12, 5))
    for alg, sub in df.groupby("algorithm"):
        axes[0].errorbar(
            sub["mean_recall"],
            sub["mean_qps"],
            xerr=sub["std_recall"] if "std_recall" in sub else None,
            yerr=sub["std_qps"] if "std_qps" in sub else None,
            fmt="o",
            capsize=3,
            label=alg,
        )
        axes[1].errorbar(
            sub["mean_recall"],
            sub["size_mb"],
            xerr=sub["std_recall"] if "std_recall" in sub else None,
            fmt="o",
            capsize=3,
            label=alg,
        )

    axes[0].set_title("Recall vs QPS")
    axes[0].set_xlabel("Recall@100")
    axes[0].set_ylabel("QPS")
    axes[0].set_xlim(left=0.0, right=1.05)
    axes[0].set_ylim(bottom=0.0)
    axes[0].grid(True, alpha=0.3)
    axes[0].legend()

    axes[1].set_title("Recall vs Size")
    axes[1].set_xlabel("Recall@100")
    axes[1].set_ylabel("Size MB")
    axes[1].set_xlim(left=0.0, right=1.05)
    axes[1].set_ylim(bottom=0.0)
    axes[1].grid(True, alpha=0.3)
    axes[1].legend()

    fig.suptitle(f"ANN trade-off ({preset})")
    fig.tight_layout()
    out_path = artifacts_dir / f"tradeoff_{preset}.png"
    fig.savefig(out_path, dpi=150, bbox_inches="tight")
    plt.close(fig)


def _plot_parameter_sweeps(artifacts_dir: Path) -> None:
    coarse_path = artifacts_dir / "benchmark_coarse.csv"
    fine_path = artifacts_dir / "benchmark_fine.csv"
    if not coarse_path.exists() or not fine_path.exists():
        return

    coarse = pd.read_csv(coarse_path)
    fine = pd.read_csv(fine_path)
    coarse = _with_config_columns(coarse)
    fine = _with_config_columns(fine)

    _plot_hnsw_m_sweep(coarse, artifacts_dir / "hnsw_m_sweep.png")
    _plot_hnsw_ef_sweep(fine, artifacts_dir / "hnsw_ef_sweep.png")
    _plot_lsh_nbits_sweep(coarse, artifacts_dir / "lsh_nbits_sweep.png")
    _plot_ivfpq_nlist_sweep(coarse, artifacts_dir / "ivfpq_nlist_sweep.png")
    _plot_ivfpq_nprobe_sweep(fine, artifacts_dir / "ivfpq_nprobe_sweep.png")


def _with_config_columns(df: pd.DataFrame) -> pd.DataFrame:
    out = df.copy()
    cfg = out["config_json"].apply(json.loads)
    out["nbits"] = cfg.apply(lambda x: x.get("nbits"))
    out["m"] = cfg.apply(lambda x: x.get("m"))
    out["ef_search"] = cfg.apply(lambda x: x.get("ef_search"))
    out["nlist"] = cfg.apply(lambda x: x.get("nlist"))
    out["nprobe"] = cfg.apply(lambda x: x.get("nprobe"))
    return out


def _plot_hnsw_m_sweep(df: pd.DataFrame, out_path: Path) -> None:
    sub = df[(df["algorithm"] == "hnsw") & (df["ef_search"] == 64)].sort_values("m")
    if sub.empty:
        return
    _plot_four_metrics(sub, "m", "HNSW: влияние m (efSearch=64)", out_path)


def _plot_hnsw_ef_sweep(df: pd.DataFrame, out_path: Path) -> None:
    sub = df[(df["algorithm"] == "hnsw") & (df["m"] == 16)].sort_values("ef_search")
    if sub.empty:
        return
    _plot_four_metrics(sub, "ef_search", "HNSW: влияние efSearch (m=16)", out_path)


def _plot_lsh_nbits_sweep(df: pd.DataFrame, out_path: Path) -> None:
    sub = df[df["algorithm"] == "lsh"].sort_values("nbits")
    if sub.empty:
        return
    _plot_four_metrics(sub, "nbits", "LSH: влияние nbits", out_path)


def _plot_ivfpq_nlist_sweep(df: pd.DataFrame, out_path: Path) -> None:
    sub = df[(df["algorithm"] == "ivfpq") & (df["nprobe"] == 32)].sort_values("nlist")
    if sub.empty:
        return
    _plot_four_metrics(sub, "nlist", "IVF+PQ: влияние nlist (nprobe=32)", out_path)


def _plot_ivfpq_nprobe_sweep(df: pd.DataFrame, out_path: Path) -> None:
    sub = df[(df["algorithm"] == "ivfpq") & (df["nlist"] == 1024)].sort_values("nprobe")
    if sub.empty:
        return
    _plot_four_metrics(sub, "nprobe", "IVF+PQ: влияние nprobe (nlist=1024)", out_path)


def _plot_four_metrics(df: pd.DataFrame, x_col: str, title: str, out_path: Path) -> None:
    fig, axes = plt.subplots(2, 2, figsize=(10, 7))
    fig.suptitle(title, fontsize=13)
    metrics = [
        ("mean_recall", "Recall@100"),
        ("mean_qps", "QPS"),
        ("mean_latency_ms", "Latency ms"),
        ("size_mb", "Size MB"),
    ]
    std_by_metric = {
        "mean_recall": "std_recall",
        "mean_qps": "std_qps",
        "mean_latency_ms": "std_latency_ms",
    }
    x = df[x_col]
    for ax, (col, ylab) in zip(axes.flat, metrics):
        std_col = std_by_metric.get(col)
        if std_col and std_col in df.columns:
            ax.errorbar(
                x,
                df[col],
                yerr=df[std_col],
                fmt="o-",
                linewidth=2,
                capsize=3,
            )
        else:
            ax.plot(x, df[col], "o-", linewidth=2)
        ax.set_xlabel(x_col)
        ax.set_ylabel(ylab)
        # Use full axis scale from zero to avoid visual jumps from truncation.
        ax.set_ylim(bottom=0.0)
        if pd.api.types.is_numeric_dtype(x):
            ax.set_xlim(left=0.0)
        ax.grid(True, alpha=0.3)
    plt.tight_layout()
    fig.savefig(out_path, dpi=150, bbox_inches="tight")
    plt.close(fig)


def _render_best_notes(best_json_path: Path) -> str:
    if not best_json_path.exists():
        return "Сводка лучших конфигураций пока не сформирована."

    payload = json.loads(best_json_path.read_text(encoding="utf-8"))
    lines: list[str] = []
    for item in payload.get("rules", []):
        row = item["row"]
        lines.append(
            "- {alg}: выбрана `{cfg}`; recall={rec:.4f}, qps={qps:.0f}, size={size:.2f}MB, build={build:.2f}s.".format(
                alg=item["algorithm"].upper(),
                cfg=json.dumps(row["config"], ensure_ascii=False),
                rec=row["mean_recall"],
                qps=row["mean_qps"],
                size=row["size_mb"],
                build=row["build_s"],
            )
        )
    lines.append("- Итоговый победитель определяется по явному критерию: recall-ограничение -> максимум QPS -> минимум размера и времени сборки.")
    return "\n".join(lines)
