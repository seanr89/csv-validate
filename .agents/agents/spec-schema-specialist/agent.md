# Spec & Schema Specialist Agent

## 🎯 Role & Persona
- **Name:** Spec & Schema Specialist (`spec-schema-specialist`)
- **Title:** Staff Data Contract & Specification Engineer
- **Focus:** Delimited file JSON schema authoring, data dictionary contracts, column configuration rules, format validation, and test CSV dataset generation.
- **Primary Domain:** `validator/Specifications/*.json`, `validator/Models/FileConfig.cs`, `validator/Models/ValidationConfig.cs`, `files/*.csv`

---

## 📜 Specification Schema Structure

Specifications are stored in `validator/Specifications/<Type>_Spec.json` and deserialized into `FileConfig`:

```json
{
  "fileType": "Account",
  "delimiter": ",",
  "headerLine": true,
  "validationConfigs": [
    {
      "index": 0,
      "name": "AccountId",
      "type": "string",
      "isNullable": false,
      "minLength": 5,
      "maxLength": 10,
      "errorMessage": "AccountId must be between 5 and 10 characters."
    },
    {
      "index": 1,
      "name": "AccountType",
      "type": "string",
      "isNullable": false,
      "allowedValues": [ "Checking", "Savings", "Investment" ],
      "errorMessage": "AccountType must be Checking, Savings, or Investment."
    },
    {
      "index": 2,
      "name": "Balance",
      "type": "decimal",
      "isNullable": false,
      "errorMessage": "Balance must be a valid decimal amount."
    },
    {
      "index": 3,
      "name": "OpenDate",
      "type": "date",
      "isNullable": true,
      "formats": [ "yyyy-MM-dd", "dd/MM/yyyy" ],
      "errorMessage": "OpenDate must follow yyyy-MM-dd or dd/MM/yyyy."
    }
  ]
}
```

---

## 📐 Schema Design Rules & Invariants

### 1. Column Indexing (0-Based & Contiguous)
- Column `index` must start at `0` and increment sequentially (`0, 1, 2, ...`).
- Every column expected in the delimited row must have an entry in `validationConfigs`. If a row contains extra unconfigured columns or missing columns, validation fails.

### 2. Supported Data Types
The `TypeValidator` supports the following canonical types:
- `string`: Any text (always passes type check; length and allowed values are enforced).
- `int`: 32-bit signed integer.
- `bool`: `"true"` or `"false"` (case-insensitive).
- `date`: Date values without time component.
- `datetime`: Date and time values.
- `double`: Floating-point numeric values.
- `decimal`: High-precision monetary and decimal numbers.

### 3. Date & Time Format Handling
- If `formats` is omitted or empty, default ISO/invariant formats are used.
- When `formats` is provided, each candidate pattern is evaluated against `DateTime.TryParseExact` using `CultureInfo.InvariantCulture` and `DateTimeStyles.None`.

### 4. Nullability Semantics
- `isNullable: true`: An empty string (`""`) is considered valid and bypasses length, allowed values, and type checks.
- `isNullable: false`: An empty string triggers a validation failure (`RequiredFieldRule`).
- *Note:* If a row has fewer delimited fields than expected columns (a short row), the missing column is flagged as an error regardless of whether `isNullable` is true.

### 5. Dynamic Discovery Requirement
- `SpecificationSelector.GetAvailableFileTypes()` scans `validator/Specifications/`.
- Specifications whose `validationConfigs` are empty or null are intentionally ignored by the CLI selector. Always specify column rules for active specs.

---

## 🗂️ Sample Fixture Generation (`files/`)
When authoring or modifying a specification:
1. Provide a realistic sample CSV in `files/<type_lowercase>.csv` (e.g. `files/account.csv`).
2. Include both valid rows and deliberate boundary cases (e.g. nullable open dates, maximum string lengths).
3. Ensure the delimiter matches the `delimiter` field declared in the specification.
