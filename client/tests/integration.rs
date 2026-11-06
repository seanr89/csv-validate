use std::path::PathBuf;

use csv_validate_client::engine::Engine;

fn find_repo_root() -> PathBuf {
    let mut dir = std::env::current_dir().expect("cwd");
    while !dir.join("csv-validate.sln").is_file() {
        dir = dir.parent().expect("repo root").to_path_buf();
    }
    dir
}

#[test]
fn engine_can_discover_and_list_specs() {
    let root = find_repo_root();
    let validator_bin = root.join("validator/bin/Debug/net10.0/validator");
    if !validator_bin.is_file() {
        eprintln!("Skipping integration test: validator binary not yet compiled at {}", validator_bin.display());
        return;
    }

    let engine = Engine::discover().expect("engine discovery should succeed");
    let specs = engine.list_specs().expect("list_specs should succeed");
    assert!(!specs.specs.is_empty(), "Specs should not be empty");

    let types: Vec<&str> = specs.specs.iter().map(|s| s.file_type.as_str()).collect();
    assert!(types.contains(&"account"), "Expected 'account' spec in {types:?}");
    assert!(types.contains(&"transactions"), "Expected 'transactions' spec in {types:?}");
}

#[test]
fn engine_can_validate_real_account_csv() {
    let root = find_repo_root();
    let validator_bin = root.join("validator/bin/Debug/net10.0/validator");
    let account_csv = root.join("files/account.csv");
    if !validator_bin.is_file() || !account_csv.is_file() {
        eprintln!("Skipping integration test: required files missing");
        return;
    }

    let engine = Engine::discover().expect("engine discovery should succeed");
    let report = engine.validate("account", &account_csv).expect("validate should succeed");

    assert!(report.success);
    assert_eq!(report.spec.as_deref(), Some("account"));
    let stats = report.stats.expect("stats present");
    assert_eq!(stats.invalid_lines, 0);
    assert!(stats.valid_lines > 0);
    assert!(stats.header_valid);
}

#[test]
fn engine_can_validate_and_report_errors_on_invalid_data() {
    let root = find_repo_root();
    let validator_bin = root.join("validator/bin/Debug/net10.0/validator");
    if !validator_bin.is_file() {
        eprintln!("Skipping integration test: validator binary missing");
        return;
    }

    let temp = tempfile::tempdir().expect("tempdir");
    let corrupt_csv = temp.path().join("corrupt_account.csv");
    let content = "\
AccountId|CustomerId|AccountType|AccountStatus|OpenDate|Currency|Balance\n\
68b8a813-0813-4883-f927-b0f4fed9769b|4fa5727b-2403-b7ee-758d-18c0c636ad0f|Current|Active|2024-01-15|GBP|1250.50\n\
016c87fb-b2a1-ee94-74f2-adcc00448470|62379857-f545-808c-acd5-62d7130423f2|InvalidType|Active|2024-02-20|GBP|not_a_number\n";
    std::fs::write(&corrupt_csv, content).expect("write corrupt csv");

    let engine = Engine::discover().expect("engine discovery should succeed");
    let report = engine.validate("account", &corrupt_csv).expect("validate should parse output");

    assert!(report.success);
    let stats = report.stats.expect("stats present");
    assert_eq!(stats.invalid_lines, 1);
    assert_eq!(stats.valid_lines, 2); // header + 1 valid line
    assert_eq!(stats.field_errors, 2); // InvalidType and not_a_number
    assert!(stats.errors_by_field.contains_key("AccountType"));
    assert!(stats.errors_by_field.contains_key("Balance"));
}
