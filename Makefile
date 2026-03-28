.PHONY: restore build test coverage coverage-report format format-check clean

SOLUTION := InertiaCore.slnx
COVERAGE_DIR := coverage
SANDBOX := samples/Sandbox

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
	rm -rf $(COVERAGE_DIR) dist

check: build test format-check

# ============================================================
# Sandbox: Development — CSR (no SSR)
# ============================================================

sandbox-dev:
	@echo "=== Dev: CSR only (no SSR) ==="
	cd $(SANDBOX) && SSR_MODE=http SSR_ENABLED=false npm run dev

# ============================================================
# Sandbox: Development — SSR modes
# ============================================================

# Http SSR + Nuxt-style dev server (zero rebuild)
sandbox-dev-http-ssr:
	@echo "=== Dev: Http SSR + Nuxt-style dev server ==="
	cd $(SANDBOX) && SSR_MODE=http SSR_ENABLED=true npm run dev

# Http SSR + Async Page Data
sandbox-dev-http-ssr-async:
	@echo "=== Dev: Http SSR + Async Page Data ==="
	cd $(SANDBOX) && SSR_MODE=http SSR_ENABLED=true ASYNC_PAGE_DATA=true npm run dev

# MessagePack SSR (binary IPC)
sandbox-dev-msgpack-ssr:
	@echo "=== Dev: MessagePack SSR ==="
	cd $(SANDBOX) && SSR_MODE=msgpack SSR_ENABLED=true npm run dev

# MessagePack SSR + Async Page Data
sandbox-dev-msgpack-ssr-async:
	@echo "=== Dev: MessagePack SSR + Async Page Data ==="
	cd $(SANDBOX) && SSR_MODE=msgpack SSR_ENABLED=true ASYNC_PAGE_DATA=true npm run dev

# EmbeddedV8 SSR (in-process, no Node.js)
sandbox-dev-v8-ssr:
	@echo "=== Dev: EmbeddedV8 SSR (in-process) ==="
	cd $(SANDBOX) && SSR_MODE=v8 SSR_ENABLED=true npm run dev

# EmbeddedV8 SSR + Async Page Data
sandbox-dev-v8-ssr-async:
	@echo "=== Dev: EmbeddedV8 SSR + Async Page Data ==="
	cd $(SANDBOX) && SSR_MODE=v8 SSR_ENABLED=true ASYNC_PAGE_DATA=true npm run dev

# ============================================================
# Sandbox: Production
# ============================================================

sandbox-build:
	@echo "Building Sandbox for production..."
	cd $(SANDBOX) && npm run build
	dotnet publish $(SANDBOX)/Sandbox.csproj -c Release -o dist/sandbox

# CSR only
sandbox-prod:
	@echo "=== Prod: CSR only ==="
	cd dist/sandbox && SSR_ENABLED=false ./Sandbox --urls "http://localhost:5000"

# Http SSR (needs Node.js sidecar)
sandbox-prod-http-ssr: sandbox-build
	@echo "=== Prod: Http SSR ==="
	@echo "Start sidecar: node dist/sandbox/dist/ssr/ssr.js"
	cd dist/sandbox && SSR_MODE=http SSR_ENABLED=true ./Sandbox --urls "http://localhost:5000"

# Http SSR + Async Page Data
sandbox-prod-http-ssr-async: sandbox-build
	@echo "=== Prod: Http SSR + Async Page Data ==="
	@echo "Start sidecar: node dist/sandbox/dist/ssr/ssr.js"
	cd dist/sandbox && SSR_MODE=http SSR_ENABLED=true ASYNC_PAGE_DATA=true ./Sandbox --urls "http://localhost:5000"

# MessagePack SSR
sandbox-prod-msgpack-ssr: sandbox-build
	@echo "=== Prod: MessagePack SSR ==="
	@echo "Start sidecar: node dist/sandbox/dist/ssr/ssr.js"
	cd dist/sandbox && SSR_MODE=msgpack SSR_ENABLED=true ./Sandbox --urls "http://localhost:5000"

# MessagePack SSR + Async Page Data
sandbox-prod-msgpack-ssr-async: sandbox-build
	@echo "=== Prod: MessagePack SSR + Async Page Data ==="
	@echo "Start sidecar: node dist/sandbox/dist/ssr/ssr.js"
	cd dist/sandbox && SSR_MODE=msgpack SSR_ENABLED=true ASYNC_PAGE_DATA=true ./Sandbox --urls "http://localhost:5000"

# EmbeddedV8 SSR (single process, no Node.js)
sandbox-prod-v8-ssr: sandbox-build
	@echo "=== Prod: EmbeddedV8 SSR (no Node.js) ==="
	cd dist/sandbox && SSR_MODE=v8 SSR_ENABLED=true ./Sandbox --urls "http://localhost:5000"

# EmbeddedV8 SSR + Async Page Data (the dream)
sandbox-prod-v8-ssr-async: sandbox-build
	@echo "=== Prod: EmbeddedV8 SSR + Async Page Data ==="
	cd dist/sandbox && SSR_MODE=v8 SSR_ENABLED=true ASYNC_PAGE_DATA=true ./Sandbox --urls "http://localhost:5000"

# ============================================================
# npm packages
# ============================================================

npm-build:
	cd packages/ssr && npm run build
	cd packages/vite-plugin && npm run build

npm-test:
	cd packages/ssr && npx vitest run
	cd packages/vite-plugin && npm test

# ============================================================
# Benchmarks (app must be running on the target port)
# ============================================================

BENCH_HOST ?= http://localhost:5274
BENCH_N ?= 2000
BENCH_C ?= 50

bench-sm:
	@echo "=== Bench: ~100KB payload ($(BENCH_HOST)/bench/sm) ==="
	@ab -n $(BENCH_N) -c $(BENCH_C) -s 10 $(BENCH_HOST)/bench/sm 2>&1 | grep -E "Requests per second|Time per request|Failed|Document Length|Transfer rate"

bench-md:
	@echo "=== Bench: ~250KB payload ($(BENCH_HOST)/bench/md) ==="
	@ab -n $(BENCH_N) -c $(BENCH_C) -s 10 $(BENCH_HOST)/bench/md 2>&1 | grep -E "Requests per second|Time per request|Failed|Document Length|Transfer rate"

bench-lg:
	@echo "=== Bench: ~500KB payload ($(BENCH_HOST)/bench/lg) ==="
	@ab -n $(BENCH_N) -c $(BENCH_C) -s 10 $(BENCH_HOST)/bench/lg 2>&1 | grep -E "Requests per second|Time per request|Failed|Document Length|Transfer rate"

bench-xl:
	@echo "=== Bench: ~1MB payload ($(BENCH_HOST)/bench/xl) ==="
	@ab -n $(BENCH_N) -c $(BENCH_C) -s 10 $(BENCH_HOST)/bench/xl 2>&1 | grep -E "Requests per second|Time per request|Failed|Document Length|Transfer rate"

bench-all: bench-sm bench-md bench-lg bench-xl

# Convenience: benchmark prod server
bench-prod-all:
	$(MAKE) bench-all BENCH_HOST=http://localhost:5000

# Full automated server benchmark: all variants × all sizes
bench-suite:
	@./scripts/benchmark.sh

# Full automated browser benchmark: all variants × all sizes × all networks
bench-browser:
	@./scripts/browser-benchmark.sh
