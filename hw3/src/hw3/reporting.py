from __future__ import annotations

import json
import math
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
        "## Критерии сравнения\n"
        "- Основные метрики: `Recall@100`, `QPS`, `latency_ms`, `build_s`, `size_mb`, `CV(QPS)`.\n"
        "- Правило выбора: сначала ограничение `Recall@100 >= 0.8` (если достижимо), затем максимум `QPS`, далее минимум `size_mb`, затем минимум `build_s`.\n\n"
        "## Результаты\n\n"
        + summary_file.read_text(encoding="utf-8")
        + "\n\n## Интерпретация\n\n"
        + best_notes
        + "\n\n## Графики сравнения алгоритмов\n\n"
        + f"![Матрица trade-off](artifacts/tradeoff_{preset}.png)\n\n"
        + "![IVF Flat vs IVF+PQ](artifacts/comparison_ivf_flat_vs_ivfpq.png)\n\n"
        + "## Графики влияния параметров\n\n"
        + "### HNSW: влияние m\n"
        + "![HNSW m sweep](artifacts/hnsw_m_sweep.png)\n\n"
        + "### HNSW: влияние efSearch\n"
        + "![HNSW ef sweep](artifacts/hnsw_ef_sweep.png)\n\n"
        + "### LSH: влияние nbits\n"
        + "![LSH nbits sweep](artifacts/lsh_nbits_sweep.png)\n\n"
        + "### IVF+PQ: влияние nlist\n"
        + "![IVFPQ nlist sweep](artifacts/ivfpq_nlist_sweep.png)\n\n"
        + "### IVF+PQ: влияние nprobe\n"
        + "![IVFPQ nprobe sweep](artifacts/ivfpq_nprobe_sweep.png)\n\n"
        + "### IVF+PQ: влияние m_pq\n"
        + "![IVFPQ m_pq sweep](artifacts/ivfpq_mpq_sweep.png)\n\n"
        + "### IVF+PQ: влияние pq_bits\n"
        + "![IVFPQ pq_bits sweep](artifacts/ivfpq_pqbits_sweep.png)\n\n"
        + "### IVF+PQ: heatmap recall (m_pq x nprobe)\n"
        + "![IVFPQ heatmap](artifacts/ivfpq_heatmap_mpq_nprobe.png)\n\n"
        + "### IVF Flat: влияние nprobe\n"
        + "![IVF Flat nprobe sweep](artifacts/ivf_flat_nprobe_sweep.png)\n"
    )
    report_path.write_text(report_body, encoding="utf-8")


def _write_summary_md(df: pd.DataFrame, best_json_path: Path, out_path: Path) -> None:
    lines: list[str] = []
    df = df[df["algorithm"] != "flat"].copy()
    df = _with_ci_columns(df)

    lines.append("### Агрегированные метрики (с 95% доверительными интервалами)\n")
    lines.append("| Алгоритм | Конфигурация | Recall@100 (CI) | QPS (CI) | Latency ms (CI) | Build s | Size MB |")
    lines.append("|---|---|---:|---:|---:|---:|---:|")
    for _, row in df.sort_values(by=["algorithm", "mean_recall"], ascending=[True, False]).iterrows():
        lines.append(
            "| {alg} | `{cfg}` | {rec} | {qps} | {lat} | {build:.2f} | {size:.2f} |".format(
                alg=row["algorithm"],
                cfg=row["config_json"],
                rec=_fmt_with_ci(row["mean_recall"], row["ci_recall"], 4),
                qps=_fmt_with_ci(row["mean_qps"], row["ci_qps"], 0),
                lat=_fmt_with_ci(row["mean_latency_ms"], row["ci_latency"], 4),
                build=row["build_s"],
                size=row["size_mb"],
            )
        )

    lines.append("\n### Топ-3 по Recall внутри каждого алгоритма\n")
    for alg, sub in df.groupby("algorithm"):
        lines.append(f"- **{alg}**")
        top = sub.sort_values(by=["mean_recall", "mean_qps"], ascending=[False, False]).head(3)
        for _, row in top.iterrows():
            lines.append(
                "  - `{cfg}` | recall={rec}, qps={qps}, size={size:.2f}MB".format(
                    cfg=row["config_json"],
                    rec=_fmt_with_ci(row["mean_recall"], row["ci_recall"], 4),
                    qps=_fmt_with_ci(row["mean_qps"], row["ci_qps"], 0),
                    size=row["size_mb"],
                )
            )

    lines.append("\n### Топ-3 по QPS внутри каждого алгоритма\n")
    for alg, sub in df.groupby("algorithm"):
        lines.append(f"- **{alg}**")
        top = sub.sort_values(by=["mean_qps", "mean_recall"], ascending=[False, False]).head(3)
        for _, row in top.iterrows():
            lines.append(
                "  - `{cfg}` | qps={qps}, recall={rec}, size={size:.2f}MB".format(
                    cfg=row["config_json"],
                    qps=_fmt_with_ci(row["mean_qps"], row["ci_qps"], 0),
                    rec=_fmt_with_ci(row["mean_recall"], row["ci_recall"], 4),
                    size=row["size_mb"],
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
    df = df[df["algorithm"] != "flat"].copy()
    fig, axes = plt.subplots(1, 3, figsize=(18, 5))

    for alg, sub in df.groupby("algorithm"):
        axes[0].errorbar(
            sub["mean_recall"], sub["mean_qps"],
            xerr=sub["std_recall"], yerr=sub["std_qps"],
            fmt="o", capsize=3, label=alg
        )
        axes[1].errorbar(
            sub["mean_recall"], sub["size_mb"],
            xerr=sub["std_recall"], fmt="o", capsize=3, label=alg
        )
        axes[2].errorbar(
            sub["mean_recall"], sub["build_s"],
            xerr=sub["std_recall"], fmt="o", capsize=3, label=alg
        )

    axes[0].set_title("Recall vs QPS")
    axes[0].set_xlabel("Recall@100")
    axes[0].set_ylabel("QPS")
    axes[0].set_xlim(left=0.0, right=1.05)
    axes[0].set_yscale("log")
    axes[0].grid(True, alpha=0.3)

    axes[1].set_title("Recall vs Index Size")
    axes[1].set_xlabel("Recall@100")
    axes[1].set_ylabel("Size MB")
    axes[1].set_xlim(left=0.0, right=1.05)
    axes[1].set_ylim(bottom=0.0)
    axes[1].grid(True, alpha=0.3)

    axes[2].set_title("Recall vs Build Time")
    axes[2].set_xlabel("Recall@100")
    axes[2].set_ylabel("Build s")
    axes[2].set_xlim(left=0.0, right=1.05)
    axes[2].set_yscale("log")
    axes[2].grid(True, alpha=0.3)
    axes[2].legend()

    fig.suptitle(f"Algorithm comparison ({preset})")
    fig.tight_layout()
    fig.savefig(artifacts_dir / f"tradeoff_{preset}.png", dpi=150, bbox_inches="tight")
    plt.close(fig)

    # Dedicated IVF Flat vs IVF+PQ chart for defense narrative.
    ivf_only = df[df["algorithm"].isin(["ivf_flat", "ivfpq"])].copy()
    if not ivf_only.empty:
        fig, axes = plt.subplots(1, 2, figsize=(12, 5))
        for alg, sub in ivf_only.groupby("algorithm"):
            axes[0].plot(sub["mean_recall"], sub["mean_qps"], "o-", linewidth=2, label=alg)
            axes[1].plot(sub["mean_recall"], sub["size_mb"], "o-", linewidth=2, label=alg)
        axes[0].set_title("IVF Flat vs IVF+PQ: Recall vs QPS")
        axes[0].set_xlabel("Recall@100")
        axes[0].set_ylabel("QPS")
        axes[0].set_xlim(left=0.0, right=1.05)
        axes[0].set_yscale("log")
        axes[0].grid(True, alpha=0.3)
        axes[1].set_title("IVF Flat vs IVF+PQ: Recall vs Size")
        axes[1].set_xlabel("Recall@100")
        axes[1].set_ylabel("Size MB")
        axes[1].set_xlim(left=0.0, right=1.05)
        axes[1].set_ylim(bottom=0.0)
        axes[1].grid(True, alpha=0.3)
        axes[1].legend()
        fig.tight_layout()
        fig.savefig(artifacts_dir / "comparison_ivf_flat_vs_ivfpq.png", dpi=150, bbox_inches="tight")
        plt.close(fig)


def _plot_parameter_sweeps(artifacts_dir: Path) -> None:
    dfs: list[pd.DataFrame] = []
    for name in ["coarse", "fine", "final", "smoke"]:
        path = artifacts_dir / f"benchmark_{name}.csv"
        if path.exists():
            frame = pd.read_csv(path)
            frame["preset_name"] = name
            dfs.append(frame)
    if not dfs:
        return

    data = pd.concat(dfs, ignore_index=True)
    data = data[data["algorithm"] != "flat"].copy()
    data = _with_config_columns(data)

    _plot_hnsw_m_sweep(data, artifacts_dir / "hnsw_m_sweep.png")
    _plot_hnsw_ef_sweep(data, artifacts_dir / "hnsw_ef_sweep.png")
    _plot_lsh_nbits_sweep(data, artifacts_dir / "lsh_nbits_sweep.png")
    _plot_ivfpq_nlist_sweep(data, artifacts_dir / "ivfpq_nlist_sweep.png")
    _plot_ivfpq_nprobe_sweep(data, artifacts_dir / "ivfpq_nprobe_sweep.png")
    _plot_ivfpq_mpq_sweep(data, artifacts_dir / "ivfpq_mpq_sweep.png")
    _plot_ivfpq_pqbits_sweep(data, artifacts_dir / "ivfpq_pqbits_sweep.png")
    _plot_ivfpq_heatmap(data, artifacts_dir / "ivfpq_heatmap_mpq_nprobe.png")
    _plot_ivf_flat_nprobe_sweep(data, artifacts_dir / "ivf_flat_nprobe_sweep.png")


def _with_config_columns(df: pd.DataFrame) -> pd.DataFrame:
    out = df.copy()
    cfg = out["config_json"].apply(json.loads)
    out["nbits"] = cfg.apply(lambda x: x.get("nbits"))
    out["m"] = cfg.apply(lambda x: x.get("m"))
    out["ef_search"] = cfg.apply(lambda x: x.get("ef_search"))
    out["ef_construction"] = cfg.apply(lambda x: x.get("ef_construction"))
    out["nlist"] = cfg.apply(lambda x: x.get("nlist"))
    out["nprobe"] = cfg.apply(lambda x: x.get("nprobe"))
    out["m_pq"] = cfg.apply(lambda x: x.get("m_pq"))
    out["pq_bits"] = cfg.apply(lambda x: x.get("pq_bits"))
    return out


def _plot_hnsw_m_sweep(df: pd.DataFrame, out_path: Path) -> None:
    sub = df[(df["algorithm"] == "hnsw") & (df["ef_search"] == 128)].sort_values("m")
    _plot_four_metrics(sub, "m", "HNSW: влияние m (efSearch=128)", out_path)


def _plot_hnsw_ef_sweep(df: pd.DataFrame, out_path: Path) -> None:
    sub = df[(df["algorithm"] == "hnsw") & (df["m"] == 16)].sort_values("ef_search")
    _plot_four_metrics(sub, "ef_search", "HNSW: влияние efSearch (m=16)", out_path)


def _plot_lsh_nbits_sweep(df: pd.DataFrame, out_path: Path) -> None:
    sub = df[df["algorithm"] == "lsh"].sort_values("nbits")
    _plot_four_metrics(sub, "nbits", "LSH: влияние nbits", out_path)


def _plot_ivfpq_nlist_sweep(df: pd.DataFrame, out_path: Path) -> None:
    sub = df[(df["algorithm"] == "ivfpq") & (df["nprobe"] == 64) & (df["m_pq"] == 16)].sort_values("nlist")
    _plot_four_metrics(sub, "nlist", "IVF+PQ: влияние nlist (nprobe=64, m_pq=16)", out_path)


def _plot_ivfpq_nprobe_sweep(df: pd.DataFrame, out_path: Path) -> None:
    sub = df[(df["algorithm"] == "ivfpq") & (df["nlist"] == 1024) & (df["m_pq"] == 16)].sort_values("nprobe")
    _plot_four_metrics(sub, "nprobe", "IVF+PQ: влияние nprobe (nlist=1024, m_pq=16)", out_path)


def _plot_ivfpq_mpq_sweep(df: pd.DataFrame, out_path: Path) -> None:
    sub = df[(df["algorithm"] == "ivfpq") & (df["nlist"] == 1024) & (df["nprobe"] == 128)].sort_values("m_pq")
    _plot_four_metrics(sub, "m_pq", "IVF+PQ: влияние m_pq (nlist=1024, nprobe=128)", out_path)


def _plot_ivfpq_pqbits_sweep(df: pd.DataFrame, out_path: Path) -> None:
    sub = df[(df["algorithm"] == "ivfpq") & (df["nlist"] == 1024) & (df["nprobe"] == 128) & (df["m_pq"] == 16)].sort_values("pq_bits")
    _plot_four_metrics(sub, "pq_bits", "IVF+PQ: влияние pq_bits (nlist=1024, nprobe=128, m_pq=16)", out_path)


def _plot_ivf_flat_nprobe_sweep(df: pd.DataFrame, out_path: Path) -> None:
    sub = df[(df["algorithm"] == "ivf_flat") & (df["nlist"] == 1024)].sort_values("nprobe")
    _plot_four_metrics(sub, "nprobe", "IVF Flat: влияние nprobe (nlist=1024)", out_path)


def _plot_ivfpq_heatmap(df: pd.DataFrame, out_path: Path) -> None:
    sub = df[(df["algorithm"] == "ivfpq") & (df["nlist"] == 1024)].copy()
    if sub.empty:
        return
    pivot = sub.pivot_table(index="m_pq", columns="nprobe", values="mean_recall", aggfunc="max")
    if pivot.empty:
        return
    fig, ax = plt.subplots(figsize=(8, 5))
    im = ax.imshow(pivot.values, aspect="auto")
    ax.set_title("IVF+PQ Recall heatmap (m_pq x nprobe)")
    ax.set_xlabel("nprobe")
    ax.set_ylabel("m_pq")
    ax.set_xticks(range(len(pivot.columns)))
    ax.set_xticklabels([str(x) for x in pivot.columns])
    ax.set_yticks(range(len(pivot.index)))
    ax.set_yticklabels([str(x) for x in pivot.index])
    fig.colorbar(im, ax=ax, label="Recall@100")
    fig.tight_layout()
    fig.savefig(out_path, dpi=150, bbox_inches="tight")
    plt.close(fig)


def _plot_four_metrics(df: pd.DataFrame, x_col: str, title: str, out_path: Path) -> None:
    if df.empty:
        return
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
            ax.errorbar(x, df[col], yerr=df[std_col], fmt="o-", linewidth=2, capsize=3)
        else:
            ax.plot(x, df[col], "o-", linewidth=2)
        ax.set_xlabel(x_col)
        ax.set_ylabel(ylab)
        ax.set_ylim(bottom=0.0)
        if pd.api.types.is_numeric_dtype(x):
            ax.set_xlim(left=0.0)
        if col in {"mean_qps", "mean_latency_ms"}:
            positive = (df[col] > 0).all()
            if positive:
                ax.set_yscale("log")
        ax.grid(True, alpha=0.3)
    plt.tight_layout()
    fig.savefig(out_path, dpi=150, bbox_inches="tight")
    plt.close(fig)


def _with_ci_columns(df: pd.DataFrame) -> pd.DataFrame:
    out = df.copy()
    t_values = out["repeats"].apply(_t_critical_95)
    denom = out["repeats"].apply(lambda n: math.sqrt(float(n)) if n and n > 0 else 1.0)
    ci_factor = t_values / denom
    out["ci_recall"] = ci_factor * out["std_recall"]
    out["ci_qps"] = ci_factor * out["std_qps"]
    out["ci_latency"] = ci_factor * out["std_latency_ms"]
    return out


def _fmt_with_ci(value: float, ci: float, digits: int) -> str:
    if ci <= 0:
        return f"{value:.{digits}f}"
    return f"{value:.{digits}f}±{ci:.{digits}f}"


def _t_critical_95(repeats: float | int) -> float:
    n = int(repeats)
    if n <= 1:
        return 0.0
    table = {
        2: 12.706, 3: 4.303, 4: 3.182, 5: 2.776, 6: 2.571, 7: 2.447, 8: 2.365, 9: 2.306,
        10: 2.262, 11: 2.228, 12: 2.201, 13: 2.179, 14: 2.160, 15: 2.145, 16: 2.131,
        17: 2.120, 18: 2.110, 19: 2.101, 20: 2.093, 21: 2.086, 22: 2.080, 23: 2.074,
        24: 2.069, 25: 2.064, 26: 2.060, 27: 2.056, 28: 2.052, 29: 2.048, 30: 2.045,
    }
    return table.get(n, 1.96)


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
    lines.append("- Доверительные интервалы (95% CI) считаются по фактическому числу повторов в строке.")
    return "\n".join(lines)
