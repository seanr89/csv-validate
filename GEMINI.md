# GEMINI.md

This file provides system instructions, architectural context, and development guidelines for Gemini and AI pair programmers working in the `csv-validate` repository.

---

## 🎯 Project Overview & Purpose

`csv-validate` is a high-performance .NET 10 (C# 13) console application designed to validate delimited files (CSV, TSV, pipe-delimited) against declarative JSON specifications. It verifies header integrity, column counts, nullable vs required rules, string length bounds, allowed enum values, and culture-invariant data types.

### Key Technologies
- **Runtime:** .NET 10.0 (`net10.0`, C# 13)
- **Host & DI:** Microsoft Extensions (`Microsoft.Extensions.Hosting`, `Microsoft.Extensions.DependencyInjection`, `Microsoft.Extensions.Configuration`)
- **Console UI:** Spectre.Console (interactive terminal selection and styled logging)
- **JSON Serialization:** Newtonsoft.Json (specification deserialization and result export)
- **Testing:** xUnit (`validator.Tests/`), Coverlet / XPlat Code Coverage, ReportGenerator

---

## ⚙️ Environment & Tooling

> [!IMPORTANT]
> On macOS development environments, the .NET SDK may reside in `/usr/local/share/dotnet`. When running shell commands, ensure the path is exported:
> ```bash
> export PATH="$PATH:/usr/local/share/dotnet"
> ```

### Primary Commands

| Command | Action | Description |
| :--- | :--- | :--- |
| `make build` | Build | Compiles `validator/validator.csproj` |
| `make release` | Publish | Compiles release binaries to `./bin` |
| `make run-tests` | Test | Executes the complete xUnit test suite |
| `cd validator && dotnet run` | Run CLI | Launches interactive Spectre.Console menu |
| `cd scripts && ./test-and-report.sh` | Coverage | Generates Cobertura coverage & HTML report |
| `cd scripts && ./openreport.sh` | View Coverage | Opens the latest HTML coverage report in Chrome |

### Targeted Test Execution

Run a specific test fixture or single test method with xUnit `--filter`:

```bash
export PATH="$PATH:/usr/local/share/dotnet"
# Run single test
dotnet test validator.Tests/validator.Tests.csproj --no-restore --filter "FullyQualifiedName~HeaderValidatorTests.Validate_ValidHeader_ReturnsNoErrors"

# Run all tests in a class
dotnet test validator.Tests/validator.Tests.csproj --no-restore --filter "FullyQualifiedName~ValidatorServiceTests"
```

---

## 🏗️ Architecture & Component Map

The application follows clean dependency injection and modular pipeline patterns:

```mermaid
flowchart TD
    Program[Program.cs: Generic Host & DI] --> App[App.cs: Spectre.Console Workflow]
    App --> Selector[ISpecificationSelector: SpecificationSelector]
    App --> Service[IValidatorService: ValidatorService]
    App --> Writer[IFileWriter: FileWriter]

    Selector --> Specs[validator/Specifications/*.json]
    Service --> HeaderVal[IHeaderValidator: HeaderValidator]
    Service --> Pipeline[IFieldRule Pipeline]
    
    subgraph Rule Pipeline
        Pipeline --> Required[RequiredFieldRule]
        Pipeline --> Length[LengthFieldRule]
        Pipeline --> Allowed[AllowedValuesFieldRule]
        Pipeline --> TypeRule[TypeFieldRule]
        TypeRule --> TypeVal[ITypeValidator: TypeValidator]
    end

    Service --> Results[Models: LineResult & RecordResult]
    Writer --> OutputCSV[Outbox: {type}_results.csv]
    Writer --> OutputJSON[Outbox: {input}.json]
```

### Core Components

1. **Host Setup (`validator/Program.cs`)**
   - Configures `IConfiguration` via `appsettings.json` and environment variables.
   - Registers services by interface (`IHeaderValidator`, `ITypeValidator`, `IValidatorService`, `ISpecificationSelector`, `IFileWriter`) with Transient lifetime.
   - Registers `App` as a Singleton.
   - Binds `RunSettings` (`InboxPrefix` and `OutboxPrefix`).

2. **Execution Flow (`validator/App.cs`)**
   - Discovers available file types dynamically using `_specificationSelector.GetAvailableFileTypes()`. Specifications without column definitions are filtered out.
   - Prompts the user to pick a specification and locates matching files in `InboxPrefix` (defaults to `../files/`).
   - Executes validation through `_validatorService.ProcessFileWithConfig(filePath, config)`.
   - Optionally exports results to CSV or JSON using `_fileWriter`.

3. **Field Rule Pipeline (`validator/Services/Rules/`)**
   - `IFieldRule` interface returns `FieldRuleResult(RecordResult? RecordResult, bool ShouldContinue)`.
   - `RequiredFieldRule`: Fails if non-nullable field is empty. If nullable and empty, records passing result and halts subsequent rule checks.
   - `LengthFieldRule`: Enforces `MinLength` and `MaxLength` constraints.
   - `AllowedValuesFieldRule`: Validates that the raw string matches one of the allowed set.
   - `TypeFieldRule`: Delegates to `ITypeValidator`.
   - `RuleMessageFormatter`: Standardizes descriptive error messages (`Field '{Name}' at index {Index}: {Detail}. {ConfiguredMessage}`).

4. **Type Validation (`validator/Services/TypeValidator.cs`)**
   - Culture-invariant checks for `int`, `string` (always valid), `bool`, `date`, `datetime`, `double`, and `decimal`.
   - Handles custom date formats (`formats` array in config) falling back to default ISO/invariant formats.
   - Returns `false` for null, empty, whitespace, or unknown expected type names.

5. **Specifications (`validator/Specifications/*.json`)**
   - Serialized into `FileConfig` (`fileType`, `delimiter`, `headerLine`, `validationConfigs`).
   - Each column in `validationConfigs` has `index` (0-based), `name`, `type`, `isNullable`, optional `minLength`, `maxLength`, `allowedValues`, `formats`, and `errorMessage`.

---

## ⚠️ Gotchas & Non-Negotiable Directives

1. **Path Resolution & Working Directory:**
   - The CLI app is designed to run from `validator/` (`cd validator && dotnet run`). Input files are resolved relative to working directory or `RunSettings`.
   - In unit tests, `SpecificationSelector` resolves specifications against `AppContext.BaseDirectory` where JSON files are copied via `validator.csproj` (`<CopyToOutputDirectory>Always</CopyToOutputDirectory>`).
2. **CSV vs. JSON Header Results:**
   - In `FileWriter`, CSV export skips line 1 (the header result) using `results.Skip(1)` when `headerLine == true`.
   - JSON export serializes the entire `results` collection, including the header line validation entry. Keep this distinction intact when modifying output writers.
3. **Short Rows vs. Nullable Columns:**
   - If a delimited row has fewer fields than the specification columns, the missing column is flagged as an invalid `RecordResult` even if that column is marked `isNullable: true`. Nullability applies only when the field column exists but contains an empty value.
4. **Extra Columns:**
   - Rows with more fields than configured in the specification yield a line-level error.
5. **Surgical Changes & Comment Integrity:**
   - Always preserve existing comments, XML docstrings, and structure when editing C# code.
   - Ensure all 103+ unit tests continue to pass before concluding any task.

---

## 🤖 Agent Framework & Specialized Personas

This repository implements an autonomous multi-agent operational framework. For detailed orchestration workflows and specialist guidelines, refer to:

- [**`AGENTS.md`**](file:///Users/seanrafferty/Documents/development/repos/csv-validate/AGENTS.md): Master agent registry and collaboration protocol.
- [**`.agents/agents/dotnet-validator-specialist/agent.md`**](file:///Users/seanrafferty/Documents/development/repos/csv-validate/.agents/agents/dotnet-validator-specialist/agent.md): .NET 10 architecture & pipeline specialist.
- [**`.agents/agents/spec-schema-specialist/agent.md`**](file:///Users/seanrafferty/Documents/development/repos/csv-validate/.agents/agents/spec-schema-specialist/agent.md): Specification & CSV schema specialist.
- [**`.agents/agents/qa-test-specialist/agent.md`**](file:///Users/seanrafferty/Documents/development/repos/csv-validate/.agents/agents/qa-test-specialist/agent.md): xUnit testing & CI specialist.
- [**`.agents/agents/code-reviewer/agent.md`**](file:///Users/seanrafferty/Documents/development/repos/csv-validate/.agents/agents/code-reviewer/agent.md): Quality gatekeeper & PR auditor.

---

## 🛠️ Progressive-Disclosure Skills (`.agents/skills/`)

On-demand operational runbooks are located in `.agents/skills/`:
- `run-and-test`: Compiling, running tests with filters, and inspecting coverage reports.
- `spec-authoring`: Creating, registering, and testing new JSON file specifications.
- `field-rule-creation`: Implementing and testing new `IFieldRule` validation checkers.
- `cli-runner`: Running the interactive CLI and verifying generated output files.
