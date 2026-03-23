.PHONY: restore build test coverage coverage-report format format-check clean

SOLUTION := InertiaCore.slnx
COVERAGE_DIR := coverage

restore:
	dotnet restore $(SOLUTION)
	dotnet tool restore

build: restore
	dotnet build $(SOLUTION) --no-restore

test: build
	dotnet test $(SOLUTION) --no-build

coverage: build
	rm -rf $(COVERAGE_DIR)
	dotnet test $(SOLUTION) --no-build /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=../../$(COVERAGE_DIR)/
	dotnet reportgenerator -reports:$(COVERAGE_DIR)/coverage.cobertura.xml -targetdir:$(COVERAGE_DIR) -reporttypes:TextSummary
	@cat $(COVERAGE_DIR)/Summary.txt

coverage-report: coverage
	dotnet reportgenerator -reports:$(COVERAGE_DIR)/coverage.cobertura.xml -targetdir:$(COVERAGE_DIR)/html -reporttypes:Html
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
