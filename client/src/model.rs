//! Serde models mirroring the headless JSON contract emitted by the .NET engine
//! (`validator --list-specs` and `validator --spec <type> --file <path>`).
//!
//! See `validator/Headless/ValidationReport.cs` for the authoritative C# definitions.

use std::collections::BTreeMap;

use serde::{Deserialize, Serialize};

/// The engine JSON schema version this client understands.
pub const SUPPORTED_SCHEMA_VERSION: u32 = 1;

#[derive(Debug, Clone, Default, PartialEq, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct SpecListReport {
    #[serde(default)]
    pub schema_version: u32,
    #[serde(default)]
    pub spec_directory: Option<String>,
    #[serde(default)]
    pub specs: Vec<SpecDescriptor>,
}

#[derive(Debug, Clone, Default, PartialEq, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct SpecDescriptor {
    pub file_type: String,
    pub delimiter: String,
    #[serde(default)]
    pub header_line: bool,
    #[serde(default)]
    pub columns: Vec<ColumnSpec>,
}

#[derive(Debug, Clone, Default, PartialEq, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct ColumnSpec {
    pub name: String,
    #[serde(rename = "type")]
    pub kind: String,
    #[serde(default)]
    pub index: i32,
    #[serde(default)]
    pub is_nullable: bool,
    #[serde(default)]
    pub min_length: Option<i32>,
    #[serde(default)]
    pub max_length: Option<i32>,
    #[serde(default)]
    pub allowed_values: Option<Vec<String>>,
    #[serde(default)]
    pub formats: Option<Vec<String>>,
    #[serde(default)]
    pub error_message: Option<String>,
}

impl ColumnSpec {
    /// Human readable one-line description of the column rules, used in the error inspector.
    pub fn rule_summary(&self) -> String {
        let mut parts = vec![format!("type: {}", self.kind)];
        parts.push(if self.is_nullable {
            "nullable".into()
        } else {
            "required".into()
        });
        match (self.min_length, self.max_length) {
            (Some(min), Some(max)) => parts.push(format!("length {min}..={max}")),
            (Some(min), None) => parts.push(format!("length >= {min}")),
            (None, Some(max)) => parts.push(format!("length <= {max}")),
            (None, None) => {}
        }
        if let Some(values) = self.allowed_values.as_ref().filter(|v| !v.is_empty()) {
            parts.push(format!("allowed: {}", values.join(", ")));
        }
        if let Some(formats) = self.formats.as_ref().filter(|f| !f.is_empty()) {
            parts.push(format!("formats: {}", formats.join(", ")));
        }
        parts.join(" · ")
    }
}

#[derive(Debug, Clone, Default, PartialEq, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct ValidationReport {
    #[serde(default)]
    pub schema_version: u32,
    #[serde(default)]
    pub success: bool,
    #[serde(default)]
    pub error: Option<String>,
    #[serde(default)]
    pub spec: Option<String>,
    #[serde(default)]
    pub file_path: Option<String>,
    #[serde(default)]
    pub file_size_bytes: u64,
    #[serde(default)]
    pub duration_ms: u64,
    #[serde(default)]
    pub stats: Option<ReportStats>,
    #[serde(default)]
    pub summary: Option<Summary>,
    #[serde(default)]
    pub lines: Vec<LineResult>,
}

impl ValidationReport {
    /// Rows processed per second, based on the engine-reported duration.
    pub fn throughput(&self) -> Option<f64> {
        let stats = self.stats.as_ref()?;
        if self.duration_ms == 0 {
            return None;
        }
        Some(stats.data_lines as f64 / (self.duration_ms as f64 / 1000.0))
    }
}

#[derive(Debug, Clone, Default, PartialEq, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct ReportStats {
    pub total_lines: u64,
    pub data_lines: u64,
    pub valid_lines: u64,
    pub invalid_lines: u64,
    pub field_errors: u64,
    pub header_valid: bool,
    pub pass_rate: f64,
    #[serde(default)]
    pub errors_by_field: BTreeMap<String, u64>,
}

impl ReportStats {
    /// Field error counts sorted by count (descending) then name.
    pub fn errors_by_field_sorted(&self) -> Vec<(&str, u64)> {
        let mut entries: Vec<(&str, u64)> = self
            .errors_by_field
            .iter()
            .map(|(k, v)| (k.as_str(), *v))
            .collect();
        entries.sort_by(|a, b| b.1.cmp(&a.1).then_with(|| a.0.cmp(b.0)));
        entries
    }
}

#[derive(Debug, Clone, Default, PartialEq, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct Summary {
    pub total_records: u64,
    pub valid_records: u64,
    pub invalid_records: u64,
    pub file_name: String,
    #[serde(default)]
    pub processed_at: Option<String>,
}

#[derive(Debug, Clone, Default, PartialEq, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct LineResult {
    pub line_count: u64,
    pub valid: bool,
    #[serde(default)]
    pub message: Option<String>,
    #[serde(default)]
    pub record_results: Option<Vec<RecordResult>>,
}

impl LineResult {
    pub fn records(&self) -> &[RecordResult] {
        self.record_results.as_deref().unwrap_or_default()
    }
}

#[derive(Debug, Clone, Default, PartialEq, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct RecordResult {
    pub line_count: u64,
    #[serde(default)]
    pub field_index: Option<i32>,
    #[serde(default)]
    pub field_name: Option<String>,
    #[serde(default)]
    pub key: String,
    pub valid: bool,
    #[serde(default)]
    pub error_message: Option<String>,
}

#[cfg(test)]
mod tests {
    use super::*;

    const REPORT: &str = r#"{
      "schemaVersion": 1,
      "success": true,
      "error": null,
      "spec": "account",
      "filePath": "/tmp/account.csv",
      "fileSizeBytes": 630,
      "durationMs": 4,
      "stats": {
        "totalLines": 3, "dataLines": 2, "validLines": 2, "invalidLines": 1,
        "fieldErrors": 1, "headerValid": true, "passRate": 50.0,
        "errorsByField": { "Balance": 1 }
      },
      "summary": {
        "totalRecords": 3, "validRecords": 2, "invalidRecords": 1,
        "fileName": "account.csv", "processedAt": "2026-11-06T19:04:54.746921+00:00"
      },
      "lines": [
        { "lineCount": 1, "valid": true, "message": "", "recordResults": [] },
        { "lineCount": 2, "valid": true, "message": "Line is valid", "recordResults": [] },
        { "lineCount": 3, "valid": false, "message": null, "recordResults": [
          { "lineCount": 3, "fieldIndex": 6, "fieldName": "Balance", "key": "abc", "valid": false,
            "errorMessage": "Field 'Balance' at index 6: invalid decimal." }
        ] }
      ]
    }"#;

    #[test]
    fn parses_validation_report() {
        let report: ValidationReport = serde_json::from_str(REPORT).unwrap();
        assert!(report.success);
        assert_eq!(report.spec.as_deref(), Some("account"));
        let stats = report.stats.as_ref().unwrap();
        assert_eq!(stats.invalid_lines, 1);
        assert_eq!(stats.errors_by_field.get("Balance"), Some(&1));
        assert_eq!(report.lines.len(), 3);
        let bad = &report.lines[2];
        assert!(!bad.valid);
        assert_eq!(bad.records()[0].field_name.as_deref(), Some("Balance"));
        assert_eq!(report.throughput(), Some(500.0));
    }

    #[test]
    fn parses_error_payload() {
        let json = r#"{"schemaVersion":1,"success":false,"error":"File not found: x","spec":"account",
            "filePath":"x","fileSizeBytes":0,"durationMs":0,"stats":null,"summary":null,"lines":[]}"#;
        let report: ValidationReport = serde_json::from_str(json).unwrap();
        assert!(!report.success);
        assert_eq!(report.error.as_deref(), Some("File not found: x"));
        assert!(report.throughput().is_none());
    }

    #[test]
    fn parses_spec_list_and_summarises_rules() {
        let json = r#"{"schemaVersion":1,"specDirectory":"/s","specs":[{"fileType":"account","delimiter":"|",
            "headerLine":true,"columns":[{"name":"Currency","type":"string","index":5,"isNullable":false,
            "minLength":3,"maxLength":3,"allowedValues":["GBP","EUR"],"formats":null,"errorMessage":null}]}]}"#;
        let list: SpecListReport = serde_json::from_str(json).unwrap();
        assert_eq!(list.specs.len(), 1);
        let col = &list.specs[0].columns[0];
        assert_eq!(
            col.rule_summary(),
            "type: string · required · length 3..=3 · allowed: GBP, EUR"
        );
    }

    #[test]
    fn sorts_errors_by_field() {
        let mut stats = ReportStats::default();
        stats.errors_by_field.insert("A".into(), 1);
        stats.errors_by_field.insert("B".into(), 5);
        stats.errors_by_field.insert("C".into(), 5);
        assert_eq!(
            stats.errors_by_field_sorted(),
            vec![("B", 5), ("C", 5), ("A", 1)]
        );
    }
}
