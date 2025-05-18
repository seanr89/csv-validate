# TODO & Task List

## Features
- [ ] Refactor `ValidatorService` to use dependency injection for validators (e.g., `ITypeValidator`)
- [ ] Add support for custom field validation rules (e.g., regex, custom delegates)
- [ ] Support for optional/nullable fields in validation
- [ ] Add configuration for default date/datetime formats in appsettings.json
- [ ] Implement async file processing in `ValidatorService`
- [ ] Add summary statistics to validation results (e.g., number of valid/invalid lines)
- [ ] CLI: Add command-line options for output format (e.g., JSON, CSV, plain text)
- [ ] Add logging configuration for log level and output file
- [ ] Add support for validating files with variable column order (header mapping)
- [ ] Add unit tests for `ValidatorService` and header validation logic
- [ ] Add integration tests for end-to-end file validation

## Bugs
- [ ] `ValidatorService.ProcessFileWithConfig` does not handle empty files gracefully
- [ ] `ValidatorService` header validation does not check for missing/extra columns
- [ ] `TypeValidator.ValidateType` returns `true` for unknown types (should be `false` or configurable)
- [ ] `TypeValidator.ValidateDateTime` and `ValidateDate` log misleading error messages when all formats fail
- [ ] Error messages in validation results are not always descriptive (e.g., missing field index or value)
- [ ] `ProcessFieldByType` in `ValidatorService` does not handle null/empty fields consistently

---

_This list is auto-generated based on the current codebase and may need to be updated as features are added or bugs are fixed._
