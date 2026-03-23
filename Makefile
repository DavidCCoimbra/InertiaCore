.PHONY: restore build test format format-check clean

SOLUTION := InertiaCore.slnx

restore:
	dotnet restore $(SOLUTION)

build: restore
	dotnet build $(SOLUTION) --no-restore

test: build
	dotnet test $(SOLUTION) --no-build

format:
	dotnet format $(SOLUTION)

format-check:
	dotnet format $(SOLUTION) --verify-no-changes

clean:
	dotnet clean $(SOLUTION)

check: build test format-check