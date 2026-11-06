//! Core (UI-independent) logic for the csv-validate desktop client.
//!
//! The GUI in `main.rs`/`app.rs` is a thin layer over these modules so that the
//! engine bridge, report model, inspector filtering and exports are unit-testable.

pub mod engine;
pub mod export;
pub mod inspector;
pub mod model;

/// Format a byte count for display (e.g. `1.5 MB`).
pub fn format_bytes(bytes: u64) -> String {
    const UNITS: [&str; 4] = ["B", "KB", "MB", "GB"];
    let mut value = bytes as f64;
    let mut unit = 0;
    while value >= 1024.0 && unit < UNITS.len() - 1 {
        value /= 1024.0;
        unit += 1;
    }
    if unit == 0 {
        format!("{bytes} B")
    } else {
        format!("{value:.1} {}", UNITS[unit])
    }
}

/// Format an integer with thousands separators (e.g. `12,345`).
pub fn format_count(value: u64) -> String {
    let digits = value.to_string();
    let mut out = String::with_capacity(digits.len() + digits.len() / 3);
    for (i, ch) in digits.chars().enumerate() {
        if i > 0 && (digits.len() - i).is_multiple_of(3) {
            out.push(',');
        }
        out.push(ch);
    }
    out
}

/// Guess the specification for a file by matching its stem against spec names
/// (e.g. `transactions_2025.csv` -> `transactions`). Longest match wins.
pub fn guess_spec<'a>(
    file_name: &str,
    specs: impl IntoIterator<Item = &'a str>,
) -> Option<&'a str> {
    let stem = std::path::Path::new(file_name)
        .file_stem()?
        .to_string_lossy()
        .to_lowercase();
    specs
        .into_iter()
        .filter(|spec| {
            let spec = spec.to_lowercase();
            !spec.is_empty() && (stem == spec || stem.starts_with(&spec) || stem.contains(&spec))
        })
        .max_by_key(|spec| spec.len())
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn formats_bytes() {
        assert_eq!(format_bytes(512), "512 B");
        assert_eq!(format_bytes(1536), "1.5 KB");
        assert_eq!(format_bytes(5 * 1024 * 1024), "5.0 MB");
    }

    #[test]
    fn formats_counts() {
        assert_eq!(format_count(0), "0");
        assert_eq!(format_count(999), "999");
        assert_eq!(format_count(1000), "1,000");
        assert_eq!(format_count(1234567), "1,234,567");
    }

    #[test]
    fn guesses_spec_from_file_name() {
        let specs = ["account", "transactions", "card", "customer"];
        assert_eq!(guess_spec("account.csv", specs), Some("account"));
        assert_eq!(
            guess_spec("Transactions_2025-01.csv", specs),
            Some("transactions")
        );
        assert_eq!(
            guess_spec("/x/y/customer_cards.txt", specs),
            Some("customer")
        );
        assert_eq!(guess_spec("random.csv", specs), None);
    }
}
