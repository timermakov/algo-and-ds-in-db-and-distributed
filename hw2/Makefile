SOLUTION=Hw2.sln
DOTNET=dotnet

ifeq ($(wildcard .dotnet/dotnet.exe),.dotnet/dotnet.exe)
DOTNET=.\\.dotnet\\dotnet.exe
endif

.PHONY: check-sdk restore build test bench bench-collect profile-cpu profile-memory profile-async profile-flamegraph profile-cpu-bench profile-memory-bench profile-async-bench profile-flamegraph-bench profile-all profile-all-bench report clean

check-sdk:
	@powershell -NoProfile -Command "$$v = & '$(DOTNET)' --version; if ($$v -notmatch '^10\.') { Write-Host '.NET 10 SDK required'; exit 1 }"

restore: check-sdk
	@$(DOTNET) restore $(SOLUTION)

build: check-sdk
	@$(DOTNET) build $(SOLUTION) -c Release

test: check-sdk
	@$(DOTNET) test $(SOLUTION) -c Release

bench: check-sdk
	@$(DOTNET) run --project benchmarks/Hw2.Benchmarks/Hw2.Benchmarks.csproj -c Release --

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

profile-cpu-bench:
	@cmd /C "tools\\profile-cpu-bench.cmd %FILTER% %DURATION%"

profile-memory-bench:
	@cmd /C "tools\\profile-memory-bench.cmd %FILTER%"

profile-async-bench:
	@cmd /C "tools\\profile-async-bench.cmd %FILTER%"

profile-flamegraph-bench:
	@cmd /C "tools\\profile-flamegraph-bench.cmd %FILTER% %DURATION%"

profile-all:
	@cmd /C "tools\\profile-cpu.cmd %PID%"
	@cmd /C "tools\\profile-memory.cmd %PID%"
	@cmd /C "tools\\profile-async.cmd %PID%"
	@cmd /C "tools\\profile-flamegraph.cmd %PID%"

profile-all-bench:
	@cmd /C "tools\\profile-all-bench.cmd %FILTER% %DURATION%"

report:
	@cmd /C "if not exist report\\artifacts mkdir report\\artifacts"
	@python report\\generate_charts.py
	@cmd /C "echo Report artifacts dir: report\\artifacts"

clean:
	@$(DOTNET) clean $(SOLUTION)
