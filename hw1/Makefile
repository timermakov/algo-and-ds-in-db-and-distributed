SOLUTION=Hw1.sln
DOTNET=dotnet

ifeq ($(wildcard .dotnet/dotnet.exe),.dotnet/dotnet.exe)
DOTNET=.\\.dotnet\\dotnet.exe
endif

.PHONY: check-sdk restore build test bench bench-collect profile-cpu profile-memory profile-async profile-flamegraph profile-cpu-hotpath profile-memory-hotpath profile-async-hotpath profile-flamegraph-hotpath profile-all profile-all-hotpath report clean

check-sdk:
	@powershell -NoProfile -Command "$$v = & '$(DOTNET)' --version; if ($$v -notmatch '^10\.') { Write-Host '.NET 10 SDK required'; exit 1 }"

restore: check-sdk
	@$(DOTNET) restore $(SOLUTION)

build: check-sdk
	@$(DOTNET) build $(SOLUTION) -c Release

test: check-sdk
	@$(DOTNET) test $(SOLUTION) -c Release

bench: check-sdk
	@$(DOTNET) run --project benchmarks/Hw1.Benchmarks/Hw1.Benchmarks.csproj -c Release --

bench-collect: check-sdk
	@cmd /C "tools\\collect-bench.cmd"

profile-cpu:
	@cmd /C "tools\\profile-cpu.cmd %PID%"

profile-memory:
	@cmd /C "tools\\profile-memory.cmd %PID%"

profile-async:
	@cmd /C "tools\\profile-async.cmd %PID%"

profile-flamegraph:
	@cmd /C "tools\\profile-flamegraph.cmd %PID%"

profile-cpu-hotpath:
	@cmd /C "tools\\profile-cpu-hotpath.cmd %MODE% %DURATION% %N%"

profile-memory-hotpath:
	@cmd /C "tools\\profile-memory-hotpath.cmd %MODE% %N%"

profile-async-hotpath:
	@cmd /C "tools\\profile-async-hotpath.cmd %MODE% %DURATION% %N%"

profile-flamegraph-hotpath:
	@cmd /C "tools\\profile-flamegraph-hotpath.cmd %MODE% %DURATION% %N%"

profile-all:
	@cmd /C "tools\\profile-cpu.cmd %PID%"
	@cmd /C "tools\\profile-memory.cmd %PID%"
	@cmd /C "tools\\profile-async.cmd %PID%"
	@cmd /C "tools\\profile-flamegraph.cmd %PID%"

profile-all-hotpath:
	@cmd /C "tools\\profile-all-hotpath.cmd %MODE% %DURATION% %N%"

report:
	@cmd /C "if not exist report\\artifacts mkdir report\\artifacts"
	@python report\\generate_charts.py
	@cmd /C "echo Report artifacts dir: report\\artifacts"

clean:
	@$(DOTNET) clean $(SOLUTION)
