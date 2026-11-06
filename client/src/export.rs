//! Export helpers for validation reports (JSON and CSV).

use std::fs;
use std::io::Write;
use std::path::Path;

use anyhow::{Context as _, Result};

use crate::inspector::build_rows;
use crate::model::ValidationReport;

/// Write the full engine report as pretty-printed JSON (includes the header line entry).
pub fn write_json(report: &ValidationReport, path: &Path) -> Result<()> {
    let json = serde_json::to_string_pretty(report)?;
    fs::write(path, json).with_context(|| format!("Unable to write {}", path.display()))
}

/// Write failing records as CSV: `Line,FieldIndex,FieldName,Value,Error`.
///
/// Only invalid lines are exported, which keeps the output focused on what needs fixing.
pub fn write_errors_csv(report: &ValidationReport, path: &Path) -> Result<()> {
    let mut file =
        fs::File::create(path).with_context(|| format!("Unable to create {}", path.display()))?;
    file.write_all(errors_csv(report).as_bytes())?;
    Ok(())
}

/// Render the failing records as a CSV string.
pub fn errors_csv(report: &ValidationReport) -> String {
    let mut out = String::from("Line,FieldIndex,FieldName,Value,Error\n");
    for row in build_rows(report, true) {
        let fields = [
            row.line.to_string(),
            row.field_index.map(|i| i.to_string()).unwrap_or_default(),
            row.field_name,
            row.value,
            row.message,
        ];
        let line: Vec<String> = fields.iter().map(|f| escape(f)).collect();
        out.push_str(&line.join(","));
        out.push('\n');
    }
    out
}

fn escape(field: &str) -> String {
    if field.contains([',', '"', '\n', '\r']) {
        format!("\"{}\"", field.replace('"', "\"\""))
    } else {
        field.to_string()
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::model::{LineResult, RecordResult};

    fn report() -> ValidationReport {
        ValidationReport {
            success: true,
            schema_version: 1,
            lines: vec![
                LineResult {
                    line_count: 1,
                    valid: true,
                    message: None,
                    record_results: None,
                },
                LineResult {
                    line_count: 2,
                    valid: false,
                    message: None,
                    record_results: Some(vec![RecordResult {
                        line_count: 2,
                        field_index: Some(3),
                        field_name: Some("Narrative".into()),
                        key: "a, \"quoted\" value".into(),
                        valid: false,
                        error_message: Some("Too long".into()),
                    }]),
                },
            ],
            ..Default::default()
        }
    }

    #[test]
    fn csv_escapes_and_skips_valid_lines() {
        let csv = errors_csv(&report());
        assert_eq!(
            csv,
            "Line,FieldIndex,FieldName,Value,Error\n2,3,Narrative,\"a, \"\"quoted\"\" value\",Too long\n"
        );
    }

    #[test]
    fn writes_json_and_csv_files() {
        let dir = tempfile::tempdir().unwrap();
        let json_path = dir.path().join("r.json");
        let csv_path = dir.path().join("r.csv");
        write_json(&report(), &json_path).unwrap();
        write_errors_csv(&report(), &csv_path).unwrap();

        let round_trip: ValidationReport =
            serde_json::from_str(&fs::read_to_string(&json_path).unwrap()).unwrap();
        assert_eq!(round_trip, report());
        assert!(
            fs::read_to_string(&csv_path)
                .unwrap()
                .starts_with("Line,FieldIndex")
        );
    }
}
