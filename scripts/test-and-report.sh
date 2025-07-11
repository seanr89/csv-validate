#!/bin/bash
set -euo pipefail

cd ../validator.Tests

# Clean previous results
rm -rf TestResults
rm -rf coveragereport

# Run tests and collect coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate the HTML report
reportgenerator -reports:"**/TestResults/**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html

echo "Code coverage report generated in the 'coveragereport' directory."