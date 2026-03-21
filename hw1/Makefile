SOLUTION=Hw1.sln

.PHONY: check-sdk restore build test bench bench-collect profile-cpu profile-memory report clean

check-sdk:
	@cmd /C "dotnet --version | findstr /R \"^10\\.\" >NUL || (echo .NET 10 SDK required && exit /B 1)"

restore: check-sdk
	@dotnet restore $(SOLUTION)

build: check-sdk
	@dotnet build $(SOLUTION) -c Release

test: check-sdk
	@dotnet test $(SOLUTION) -c Release

bench: check-sdk
	@dotnet run --project benchmarks/Hw1.Benchmarks/Hw1.Benchmarks.csproj -c Release --

bench-collect: check-sdk
	@cmd /C "tools\\collect-bench.cmd"

profile-cpu:
	@cmd /C "tools\\profile-cpu.cmd %PID%"

profile-memory:
	@cmd /C "tools\\profile-memory.cmd %PID%"

report:
	@cmd /C "if not exist report\\artifacts mkdir report\\artifacts"
	@cmd /C "echo Report artifacts dir: report\\artifacts"

clean:
	@dotnet clean $(SOLUTION)
