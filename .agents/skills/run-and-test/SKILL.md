---
name: run-and-test
description: >-
  Procedures for compiling the .NET solution, running xUnit test suites, executing targeted tests via filter expressions, generating code coverage reports, and verifying build integrity in csv-validate. Use this skill whenever building code, running tests, checking code coverage, or investigating test failures.
---

# Run and Test Skill

This skill provides step-by-step procedures for building `csv-validate`, executing tests, running filtered test cases, collecting code coverage, and troubleshooting environment configurations.

---

## 1. Environment & Tooling Prerequisites

On macOS, the .NET SDK (`net10.0`) is commonly located in `/usr/local/share/dotnet`. Always ensure the path is exported in test execution subshells:

```bash
export PATH="$PATH:/usr/local/share/dotnet"
dotnet --version # Should return 10.0.x
```

---

## 2. Compiling the Project

### Fast Incremental Build
```bash
export PATH="$PATH:/usr/local/share/dotnet"
make build
# Or directly:
dotnet build ./validator/validator.csproj
```

### Release Build
```bash
export PATH="$PATH:/usr/local/share/dotnet"
make release
# Or directly:
dotnet publish ./validator/validator.csproj -c Release -o ./bin
```

---

## 3. Running the Test Suite

### Run All 103+ Unit Tests
```bash
export PATH="$PATH:/usr/local/share/dotnet"
make run-tests
# Or directly:
dotnet test ./validator.Tests/validator.Tests.csproj --verbosity normal
```

### Run a Single Test Class
```bash
export PATH="$PATH:/usr/local/share/dotnet"
dotnet test ./validator.Tests/validator.Tests.csproj --no-restore --filter "FullyQualifiedName~ValidatorServiceTests"
```

### Run a Single Specific Test Method
```bash
export PATH="$PATH:/usr/local/share/dotnet"
dotnet test ./validator.Tests/validator.Tests.csproj --no-restore --filter "FullyQualifiedName~HeaderValidatorTests.Validate_ValidHeader_ReturnsNoErrors"
```

### Run Tests Matching a Pattern
```bash
export PATH="$PATH:/usr/local/share/dotnet"
dotnet test ./validator.Tests/validator.Tests.csproj --no-restore --filter "FullyQualifiedName~AllowedValues"
```

---

## 4. Code Coverage Generation

To collect Cobertura code coverage and generate an HTML report:

```bash
export PATH="$PATH:/usr/local/share/dotnet"
cd scripts && ./test-and-report.sh
```

This runs:
1. `dotnet test --collect:"XPlat Code Coverage"` inside `validator.Tests/`.
2. Compiles an HTML report in `validator.Tests/coveragereport/` via `reportgenerator`.

To view the generated report in Google Chrome:
```bash
cd scripts && ./openreport.sh
```

---

## 5. Troubleshooting Common Failures

### `make: dotnet: No such file or directory`
- **Cause:** The shell running `make` does not have `/usr/local/share/dotnet` in its `PATH`.
- **Fix:** Prefix the command with `export PATH="$PATH:/usr/local/share/dotnet" && make ...`.

### Test Cannot Find Specifications
- **Cause:** Tests execute from `validator.Tests/bin/Debug/net10.0/`. Specifications must be copied to the output directory.
- **Verification:** Ensure `validator.csproj` contains `<CopyToOutputDirectory>Always</CopyToOutputDirectory>` for `Specifications\**\*.json`.
