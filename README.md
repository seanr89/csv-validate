# csv-validate
tooling to test csv file parsing and validation via a .json specification format

## Status Badges (Actions)
[![.NET Build & Test](https://github.com/seanr89/csv-validate/actions/workflows/build.yml/badge.svg)](https://github.com/seanr89/csv-validate/actions/workflows/build.yml)



## About
This project is a .NET console application that validates CSV files against a JSON specification. It checks for header integrity, data type correctness, and other configurable rules.

## Build and Run Instructions

### 1. Interactive Console Application (.NET 10)
```bash
make build
cd validator && dotnet run
```

### 2. Headless CLI Engine (.NET 10)
The validator can be invoked non-interactively to emit structured JSON reports:
```bash
# List available specifications
validator --list-specs

# Validate a file and emit JSON to stdout
validator --spec account --file files/account.csv

# Output to a specific JSON file
validator --spec account --file files/account.csv --output report.json
```

### 3. Desktop Frontend Client (Rust / egui)
A native, cross-platform GUI client featuring file drag-and-drop, interactive specification selection, summary statistics, line error drill-down, and CSV/JSON report exports:
```bash
# Run the client in development (auto-discovers validator engine)
make run-client

# Or run cargo directly
cargo run --manifest-path client/Cargo.toml
```

### 4. Standalone macOS Bundle (.app / DMG)
Packages a self-contained `.app` bundle embedding the .NET validation engine inside `Contents/Resources/engine` with ad-hoc signing (no external .NET runtime required on the user's Mac):
```bash
make bundle-macos
# Generated at client/dist/CSV Validate.app
```

## Testing & Quality Assurance
```bash
# Run all test suites (.NET 10 unit tests + Rust client unit and integration tests)
make all-tests

# Run only .NET unit tests
make run-tests

# Run only Rust client tests
make test-client
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

