PYTHON=python
ROOT=.
DATA_DIR=$(ROOT)/data
ARTIFACTS_DIR=$(ROOT)/report/artifacts

.PHONY: install prepare ground-truth bench-smoke bench-coarse bench-fine bench bench-final bench-ivfpq bench-ivf-flat report-smoke report-coarse report-fine report-final report all clean

install:
	@$(PYTHON) -m pip install -r requirements.txt

prepare:
	@$(PYTHON) scripts/prepare_data.py --data-dir "$(DATA_DIR)"

ground-truth:
	@$(PYTHON) scripts/build_ground_truth.py --data-dir "$(DATA_DIR)" --top-k 100

bench-smoke:
	@$(PYTHON) scripts/run_benchmarks.py --data-dir "$(DATA_DIR)" --artifacts-dir "$(ARTIFACTS_DIR)" --preset smoke

bench-coarse:
	@$(PYTHON) scripts/run_benchmarks.py --data-dir "$(DATA_DIR)" --artifacts-dir "$(ARTIFACTS_DIR)" --preset coarse

bench-fine:
	@$(PYTHON) scripts/run_benchmarks.py --data-dir "$(DATA_DIR)" --artifacts-dir "$(ARTIFACTS_DIR)" --preset fine

bench:
	@$(PYTHON) scripts/run_benchmarks.py --data-dir "$(DATA_DIR)" --artifacts-dir "$(ARTIFACTS_DIR)" --preset final

bench-final: bench

bench-ivfpq:
	@$(PYTHON) scripts/run_benchmarks.py --data-dir "$(DATA_DIR)" --artifacts-dir "$(ARTIFACTS_DIR)" --preset final --algorithms ivfpq --max-single-search-s 180

bench-ivf-flat:
	@$(PYTHON) scripts/run_benchmarks.py --data-dir "$(DATA_DIR)" --artifacts-dir "$(ARTIFACTS_DIR)" --preset final --algorithms ivf_flat --max-single-search-s 180

report-smoke:
	@$(PYTHON) scripts/generate_report.py --artifacts-dir "$(ARTIFACTS_DIR)" --report-path report/REPORT.md --preset smoke

report-coarse:
	@$(PYTHON) scripts/generate_report.py --artifacts-dir "$(ARTIFACTS_DIR)" --report-path report/REPORT.md --preset coarse

report-fine:
	@$(PYTHON) scripts/generate_report.py --artifacts-dir "$(ARTIFACTS_DIR)" --report-path report/REPORT.md --preset fine

report-final:
	@$(PYTHON) scripts/generate_report.py --artifacts-dir "$(ARTIFACTS_DIR)" --report-path report/REPORT.md --preset final

report: report-final

all: prepare ground-truth bench-coarse bench-fine bench-final report

clean:
	@$(PYTHON) -c "import pathlib; [p.unlink() for p in pathlib.Path('$(ARTIFACTS_DIR)').glob('*.csv')]; [p.unlink() for p in pathlib.Path('$(ARTIFACTS_DIR)').glob('*.json')]; [p.unlink() for p in pathlib.Path('$(ARTIFACTS_DIR)').glob('*.png')]; [p.unlink() for p in pathlib.Path('$(ARTIFACTS_DIR)').glob('*.md')]"
