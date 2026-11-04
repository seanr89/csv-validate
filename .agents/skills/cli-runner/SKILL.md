---
name: cli-runner
description: >-
  Procedures for running, testing, and debugging the interactive Spectre.Console validator application, managing runtime configuration in appsettings.json, and inspecting generated output files. Use this skill when launching the CLI app, testing interactive prompts, or validating generated output CSV/JSON files.
---

# CLI Runner Skill

This skill provides operational procedures for executing and testing the interactive `csv-validate` console application.

---

## 1. Prerequisites & Working Directory

> [!IMPORTANT]
> The console application must be executed from the `validator/` directory so that relative paths for `appsettings.json`, `Specifications/`, and `../files/` resolve accurately:

```bash
export PATH="$PATH:/usr/local/share/dotnet"
cd validator
dotnet run
```

---

## 2. Configuration Settings (`validator/appsettings.json`)

The application binds the `RunSettings` section at startup:

```json
{
  "RunSettings": {
    "InboxPrefix": "../files/",
    "OutboxPrefix": "../files/"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

- **`InboxPrefix`**: Directory where input delimited files are discovered. Defaults to `../files/`.
- **`OutboxPrefix`**: Directory where validation output files (`{type}_results.csv` or `{input}.json`) are written.

---

## 3. Interactive Execution Walkthrough

When `dotnet run` executes:

1. **Specification Discovery:**
   `SpecificationSelector` enumerates specifications in `Specifications/`. The user is presented with a Spectre.Console prompt:
   ```text
   Please choose a file type:
   > Account
     Card
     Customer
     Transaction
   ```
2. **File Processing:**
   The application looks for matching files in `InboxPrefix` (e.g. `../files/account.csv`).
3. **Validation Summary:**
   The terminal displays a formatted summary table:
   - Total records evaluated
   - Valid records count
   - Invalid records count
   - List of failing line numbers and specific error messages
4. **Output Generation Prompt:**
   The user is prompted whether to save results:
   - If `CSV`: Appends or writes to `{OutboxPrefix}/{type}_results.csv` (skips the header validation row).
   - If `JSON`: Serializes all line results (including header validation) to `{OutboxPrefix}/{filename}.json`.

---

## 4. Verifying Generated Output Files

After running a validation run, inspect the output in `files/`:

```bash
# Verify CSV results file
cat files/account_results.csv

# Verify JSON results file
cat files/account.csv.json
```

---

## 5. Non-Interactive / Batch Testing

To test the application flow programmatically without interactive Spectre prompts, reference `validator.Tests/AppFlowTests.cs`. It mocks the user input streams and verifies complete end-to-end execution.
