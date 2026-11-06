
.DEFAULT_GOAL := help


help:
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "\033[36m%-30s\033[0m   %s\n", $$1, $$2}'

build: ## Build the validator
	@echo "Building validator"
	@dotnet build ./validator/validator.csproj

build-client: ## Build the Rust desktop client
	@echo "Building Rust client"
	@cargo build --manifest-path ./client/Cargo.toml

run-client: build ## Build validator and run the Rust desktop client
	@echo "Running Rust client"
	@cargo run --manifest-path ./client/Cargo.toml

test-client: build ## Run Rust client tests (including integration tests)
	@echo "Running Rust client tests"
	@cargo test --manifest-path ./client/Cargo.toml

bundle-macos: ## Create a standalone macOS .app bundle
	@echo "Bundling macOS application"
	@./client/scripts/bundle-macos.sh

release: ## Build the release version
	@echo "Building validator"
	@dotnet publish ./validator/validator.csproj -c Release -o ./bin

run-tests: ## Run the tests
	@echo "Running tests"
	@dotnet test ./validator.Tests/validator.Tests.csproj --verbosity normal

all-tests: run-tests test-client ## Run both .NET and Rust test suites


