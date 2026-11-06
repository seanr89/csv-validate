//! Error inspector data model: flattens a [`ValidationReport`] into table rows and applies filters.

use crate::model::{LineResult, ValidationReport};

/// Label used for line-level issues (no specific field).
pub const LINE_LEVEL: &str = "(line)";

/// A single row in the error inspector table.
#[derive(Debug, Clone, PartialEq)]
pub struct ErrorRow {
    /// Index into [`ValidationReport::lines`] for drill-down.
    pub line_idx: usize,
    pub line: u64,
    pub field_index: Option<i32>,
    pub field_name: String,
    pub value: String,
    pub valid: bool,
    pub message: String,
}

/// Build the inspector rows. When `invalid_only` is set, only failing lines/records are included;
/// otherwise every line is represented (valid lines as a single row).
pub fn build_rows(report: &ValidationReport, invalid_only: bool) -> Vec<ErrorRow> {
    let mut rows = Vec::new();
    for (idx, line) in report.lines.iter().enumerate() {
        if invalid_only && line.valid {
            continue;
        }
        let failing: Vec<_> = line.records().iter().filter(|r| !r.valid).collect();
        if failing.is_empty() {
            rows.push(line_row(idx, line));
            continue;
        }
        for record in failing {
            rows.push(ErrorRow {
                line_idx: idx,
                line: line.line_count,
                field_index: record.field_index,
                field_name: record
                    .field_name
                    .clone()
                    .filter(|n| !n.trim().is_empty())
                    .unwrap_or_else(|| LINE_LEVEL.to_string()),
                value: record.key.clone(),
                valid: false,
                message: record
                    .error_message
                    .clone()
                    .or_else(|| line.message.clone())
                    .unwrap_or_default(),
            });
        }
    }
    rows
}

fn line_row(idx: usize, line: &LineResult) -> ErrorRow {
    ErrorRow {
        line_idx: idx,
        line: line.line_count,
        field_index: None,
        field_name: LINE_LEVEL.to_string(),
        value: String::new(),
        valid: line.valid,
        message: line
            .message
            .clone()
            .filter(|m| !m.is_empty())
            .unwrap_or_else(|| {
                if line.valid {
                    "Line is valid".into()
                } else {
                    "Line is invalid".into()
                }
            }),
    }
}

/// User-controlled filters for the inspector.
#[derive(Debug, Clone, Default, PartialEq)]
pub struct ErrorFilter {
    /// Case-insensitive free-text search over field name, value and message.
    pub text: String,
    /// Restrict to a single field name (`None` = all fields).
    pub field: Option<String>,
    /// Inclusive line range bounds (raw user input; blank/invalid = unbounded).
    pub line_from: String,
    pub line_to: String,
}

impl ErrorFilter {
    pub fn is_active(&self) -> bool {
        !self.text.trim().is_empty()
            || self.field.is_some()
            || !self.line_from.trim().is_empty()
            || !self.line_to.trim().is_empty()
    }

    pub fn matches(&self, row: &ErrorRow) -> bool {
        if let Some(field) = &self.field
            && &row.field_name != field
        {
            return false;
        }
        if let Ok(from) = self.line_from.trim().parse::<u64>()
            && row.line < from
        {
            return false;
        }
        if let Ok(to) = self.line_to.trim().parse::<u64>()
            && row.line > to
        {
            return false;
        }
        let needle = self.text.trim().to_lowercase();
        if needle.is_empty() {
            return true;
        }
        row.field_name.to_lowercase().contains(&needle)
            || row.value.to_lowercase().contains(&needle)
            || row.message.to_lowercase().contains(&needle)
            || row.line.to_string() == needle
    }

    /// Indices into `rows` that match the filter.
    pub fn apply(&self, rows: &[ErrorRow]) -> Vec<usize> {
        rows.iter()
            .enumerate()
            .filter(|(_, r)| self.matches(r))
            .map(|(i, _)| i)
            .collect()
    }
}

/// Distinct field names present in `rows`, sorted, for the field filter dropdown.
pub fn distinct_fields(rows: &[ErrorRow]) -> Vec<String> {
    let mut fields: Vec<String> = rows.iter().map(|r| r.field_name.clone()).collect();
    fields.sort();
    fields.dedup();
    fields
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::model::RecordResult;

    fn record(line: u64, idx: i32, name: &str, key: &str, valid: bool, msg: &str) -> RecordResult {
        RecordResult {
            line_count: line,
            field_index: Some(idx),
            field_name: Some(name.into()),
            key: key.into(),
            valid,
            error_message: Some(msg.into()),
        }
    }

    fn sample() -> ValidationReport {
        ValidationReport {
            success: true,
            lines: vec![
                LineResult {
                    line_count: 1,
                    valid: true,
                    message: Some(String::new()),
                    record_results: Some(vec![]),
                },
                LineResult {
                    line_count: 2,
                    valid: false,
                    message: None,
                    record_results: Some(vec![
                        record(2, 0, "Id", "", false, "Field 'Id' is required"),
                        record(2, 1, "Code", "ok", true, ""),
                        record(
                            2,
                            2,
                            "Amount",
                            "abc",
                            false,
                            "Field 'Amount' invalid decimal",
                        ),
                    ]),
                },
                LineResult {
                    line_count: 3,
                    valid: false,
                    message: Some("Line has 9 fields but 3 expected".into()),
                    record_results: Some(vec![]),
                },
                LineResult {
                    line_count: 4,
                    valid: true,
                    message: Some("Line is valid".into()),
                    record_results: None,
                },
            ],
            ..Default::default()
        }
    }

    #[test]
    fn invalid_only_rows_flatten_failing_records_and_line_errors() {
        let rows = build_rows(&sample(), true);
        assert_eq!(rows.len(), 3);
        assert_eq!((rows[0].line, rows[0].field_name.as_str()), (2, "Id"));
        assert_eq!(
            (
                rows[1].line,
                rows[1].field_name.as_str(),
                rows[1].value.as_str()
            ),
            (2, "Amount", "abc")
        );
        assert_eq!(rows[2].field_name, LINE_LEVEL);
        assert_eq!(rows[2].message, "Line has 9 fields but 3 expected");
        assert!(rows.iter().all(|r| !r.valid));
    }

    #[test]
    fn all_rows_include_valid_lines() {
        let rows = build_rows(&sample(), false);
        assert_eq!(rows.len(), 5);
        assert!(rows[0].valid);
        assert_eq!(rows[0].message, "Line is valid");
        assert!(rows[4].valid);
    }

    #[test]
    fn filter_by_field_text_and_range() {
        let rows = build_rows(&sample(), true);
        let mut filter = ErrorFilter {
            field: Some("Amount".into()),
            ..Default::default()
        };
        assert_eq!(filter.apply(&rows), vec![1]);

        filter = ErrorFilter {
            text: "REQUIRED".into(),
            ..Default::default()
        };
        assert_eq!(filter.apply(&rows), vec![0]);

        filter = ErrorFilter {
            line_from: "3".into(),
            line_to: " ".into(),
            ..Default::default()
        };
        assert_eq!(filter.apply(&rows), vec![2]);

        filter = ErrorFilter {
            line_from: "x".into(),
            line_to: "2".into(),
            ..Default::default()
        };
        assert_eq!(filter.apply(&rows), vec![0, 1]);
        assert!(filter.is_active());
        assert!(!ErrorFilter::default().is_active());
    }

    #[test]
    fn distinct_fields_sorted() {
        let rows = build_rows(&sample(), true);
        assert_eq!(distinct_fields(&rows), vec!["(line)", "Amount", "Id"]);
    }
}
