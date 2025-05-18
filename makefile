
.DEFAULT_GOAL := help


help:
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "\033[36m%-30s\033[0m   %s\n", $$1, $$2}'

build: ## Build the validator
	@echo "Building validator"
	@dotnet build ./validator/validator.csproj

release: ## Build the release version
	@echo "Building validator"
	@dotnet publish ./validator/validator.csproj -c Release -o ./bin

run-tests: ## Run the tests
	@echo "Running tests"
	@dotnet test ./validator.Tests/validator.Tests.csproj --no-restore --verbosity normal