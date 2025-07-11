# csv-validate
tooling to test csv file parsing and validation via a .json specification format

## Status Badges (Actions)
[![.NET Build & Test](https://github.com/seanr89/csv-validate/actions/workflows/build.yml/badge.svg)](https://github.com/seanr89/csv-validate/actions/workflows/build.yml)



## About
This project is a .NET console application that validates CSV files against a JSON specification. It checks for header integrity, data type correctness, and other configurable rules.

## Build and Run Instructions
1. **Build the project:**
   ```bash
   dotnet build validator/validator.sln
   ```
2. **Run the project:**
   ```bash
   dotnet run --project validator/validator.csproj
   ```

## Specifications
This project uses JSON files to define the validation rules for different CSV file types. The following specification files are available:
- `validator/Specifications/Account_Spec.json`
- `validator/Specifications/Card_Spec.json`
- `validator/Specifications/Customer_Spec.json`
- `validator/Specifications/Transaction_Spec.json`

## Scripts
The `scripts` directory contains helper scripts for development and reporting:
- `test-and-report.sh`: This script runs the test suite, collects code coverage data, and generates an HTML report. It then automatically opens the report in Google Chrome.
- `openreport.sh`: A convenience script to quickly open the latest code coverage report in Google Chrome without re-running the tests.

