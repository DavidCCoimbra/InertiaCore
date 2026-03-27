.PHONY: restore build test coverage coverage-report format format-check clean

SOLUTION := InertiaCore.slnx
COVERAGE_DIR := coverage

# Test projects
TEST_CORE := tests/InertiaCore.Tests/InertiaCore.Tests.csproj
TEST_MSGPACK := tests/InertiaCore.MessagePack.Tests/InertiaCore.MessagePack.Tests.csproj
TEST_SIGNALR := tests/InertiaCore.SignalR.Tests/InertiaCore.SignalR.Tests.csproj
TEST_VITE := tests/InertiaCore.Vite.Tests/InertiaCore.Vite.Tests.csproj

restore:
	dotnet restore $(SOLUTION)
	dotnet tool restore

build: restore
	dotnet build $(SOLUTION) --no-restore

test: build
	dotnet test $(SOLUTION) --no-build

coverage: build
	rm -rf $(COVERAGE_DIR)
	dotnet test $(TEST_CORE) --no-build /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=../../$(COVERAGE_DIR)/core.cobertura.xml /p:Include="[InertiaCore]*"
	dotnet test $(TEST_MSGPACK) --no-build /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=../../$(COVERAGE_DIR)/msgpack.cobertura.xml /p:Include="[InertiaCore.MessagePack]*"
	dotnet test $(TEST_SIGNALR) --no-build /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=../../$(COVERAGE_DIR)/signalr.cobertura.xml /p:Include="[InertiaCore.SignalR]*"
	dotnet test $(TEST_VITE) --no-build /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=../../$(COVERAGE_DIR)/vite.cobertura.xml /p:Include="[InertiaCore.Vite]*"
	dotnet reportgenerator -reports:"$(COVERAGE_DIR)/*.cobertura.xml" -targetdir:$(COVERAGE_DIR) -reporttypes:TextSummary
	@cat $(COVERAGE_DIR)/Summary.txt

coverage-report: coverage
	dotnet reportgenerator -reports:"$(COVERAGE_DIR)/*.cobertura.xml" -targetdir:$(COVERAGE_DIR)/html -reporttypes:Html
	@echo "Report: $(COVERAGE_DIR)/html/index.html"
	@open $(COVERAGE_DIR)/html/index.html 2>/dev/null || true

format:
	dotnet format $(SOLUTION)

format-check:
	dotnet format $(SOLUTION) --verify-no-changes

clean:
	dotnet clean $(SOLUTION)
	rm -rf $(COVERAGE_DIR)

check: build test format-check
