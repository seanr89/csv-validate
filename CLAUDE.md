# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

Prefer the makefile targets (.NET 10, `net10.0`):

- `make build` — build `validator/validator.csproj`
- `make release` — publish Release build to `./bin`
- `make run-tests` — run the xUnit suite in `validator.Tests/`

Single test (xUnit filter on the fully qualified name):

```bash
dotnet test validator.Tests/validator.Tests.csproj --no-restore --filter "FullyQualifiedName~HeaderValidatorTests.Validate_ValidHeader_ReturnsNoErrors"
```

Running the app: it is a Spectre.Console interactive menu, and the paths are relative to the working directory, so it must be run from `validator/`:

```bash
cd validator && dotnet run
```

Coverage: `scripts/test-and-report.sh` runs tests with coverage and builds an HTML report via `reportgenerator` (not installed by default). It `cd`s with a relative path, so run it from `scripts/`. `scripts/openreport.sh` opens the last report in Chrome.

CI (`.github/workflows/build.yml`) restores, builds Release, and runs tests on pushes and PRs to `main`/`master`.

## Architecture

The app is a single console process that validates delimited files against JSON specifications.

**Startup (`validator/Program.cs`)**: builds a Generic Host, registers services by interface (`IHeaderValidator`, `ITypeValidator`, `IValidatorService`, `ISpecificationSelector`) and the `App` singleton, and binds the `RunSettings` config section. `RunSettings` (inbox/outbox prefixes, storage type) is bound but not used by the current flow.

**Run flow (`validator/App.cs`)**:
1. The user picks a file type (`transactions`, `account`, `customer`, `card`).
2. `SpecificationSelector` (constructed with all `Specifications/*.json` already loaded) returns the matching `FileConfig`.
3. The input is `../files/{type}.csv` (exact name match, relative to `validator/`).
4. `ValidatorService.ProcessFileWithConfig` produces a `List<LineResult>`, each holding `RecordResult`s for the failing fields.
5. The user is optionally prompted to write results with `FileWriter` as CSV (`../files/outputs/{type}_results.csv`, append if it exists) or JSON (`../files/outputs/{input}.json`).

**Specifications (`validator/Specifications/*.json`)**: one file per type, deserialized with Newtonsoft into `FileConfig` (`fileType`, `delimiter`, `headerLine`, `validationConfigs`). Each `ValidationConfig` describes one column by `index`, with `type`, `isNullable`, `minLength`/`maxLength`, `allowedValues`/`hasExpected`, `formats` (for dates), and an `errorMessage` shown on failure. Adding a new file type means adding a JSON spec and a matching menu entry in `App.Run`, which is hard-coded.

**Per-line validation (`Services/ValidatorService.cs`)**: a line is split by the spec delimiter, and each field is checked in this order: null/empty (respecting `isNullable`), min/max length, allowed-values list, then type via `ITypeValidator`. A field index with no config yields a line-level error. When `headerLine` is true, line 1 goes through `HeaderValidator` and is skipped as data.

**Type and header checks**: `TypeValidator` handles `int`, `string` (always valid), `bool`, `date`/`datetime` (using `formats`), `double`, and `decimal`. Unknown types return `false`. `HeaderValidator` matches header names to config names case-insensitively, and column-count checks are shared in `Utils/ValidationUtils.cs`.

**Results model (`Models/`)**: `LineResult` holds the line number, validity, message, and record results. `RecordResult` holds the per-field failure. `Summary` is created in `App` but is not populated, so its output shows `N/A`.

## Gotchas

- The `Specifications` and `../files` paths are not resolved from the binary location. Running from anywhere other than `validator/` finds no specs or input files.
- The `RemoveAt(0)` on the header result happens only in the CSV write branch, so JSON output still includes the header entry.
- `TODO.md` is an auto-generated backlog; several listed bugs are already marked fixed.
