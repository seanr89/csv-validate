//! Locates and invokes the .NET validation engine as a sidecar process.
//!
//! The engine is the existing `validator` console app run in headless mode
//! (`--list-specs` / `--spec <type> --file <path>`), which prints a JSON document to stdout.
//!
//! Resolution order for the engine binary:
//! 1. `CSV_VALIDATE_ENGINE` environment variable (path to the `validator` apphost or `validator.dll`).
//! 2. Bundled engine inside a macOS `.app` (`Contents/Resources/engine/validator`).
//! 3. An `engine/` directory next to the client executable (Linux / Windows distributions).
//! 4. Development fallback: `validator/bin/{Release,Debug}/net10.0/` found by walking up from
//!    the client executable and the current working directory.

use std::ffi::OsString;
use std::path::{Path, PathBuf};
use std::process::Command;

use anyhow::{Context as _, Result, anyhow, bail};

use crate::model::{SUPPORTED_SCHEMA_VERSION, SpecListReport, ValidationReport};

/// Environment variable overriding the engine location.
pub const ENGINE_ENV: &str = "CSV_VALIDATE_ENGINE";
/// Environment variable overriding the specifications directory passed to the engine.
pub const SPEC_DIR_ENV: &str = "CSV_VALIDATE_SPEC_DIR";

#[cfg(windows)]
const APPHOST_NAME: &str = "validator.exe";
#[cfg(not(windows))]
const APPHOST_NAME: &str = "validator";
const DLL_NAME: &str = "validator.dll";

/// A resolved, invokable engine.
#[derive(Debug, Clone, PartialEq, Eq)]
pub struct Engine {
    /// Program to execute (apphost, or the `dotnet` muxer when running a `.dll`).
    pub program: PathBuf,
    /// Arguments placed before the headless arguments (e.g. the `.dll` path for `dotnet`).
    pub prefix_args: Vec<OsString>,
    /// Optional specifications directory override.
    pub spec_dir: Option<PathBuf>,
}

impl Engine {
    /// Build an engine from an explicit path to either the apphost or `validator.dll`.
    pub fn from_path(path: &Path) -> Result<Self> {
        if !path.is_file() {
            bail!("Engine not found at {}", path.display());
        }
        if path
            .extension()
            .is_some_and(|ext| ext.eq_ignore_ascii_case("dll"))
        {
            let dotnet = find_dotnet().ok_or_else(|| {
                anyhow!(
                    "'dotnet' runtime not found; required to run {}",
                    path.display()
                )
            })?;
            Ok(Self {
                program: dotnet,
                prefix_args: vec![path.into()],
                spec_dir: None,
            })
        } else {
            Ok(Self {
                program: path.to_path_buf(),
                prefix_args: Vec::new(),
                spec_dir: None,
            })
        }
    }

    /// Discover the engine using the documented resolution order.
    pub fn discover() -> Result<Self> {
        let exe_dir = std::env::current_exe()
            .ok()
            .and_then(|p| p.parent().map(Path::to_path_buf));
        let cwd = std::env::current_dir().ok();
        let env_override = std::env::var_os(ENGINE_ENV).map(PathBuf::from);
        let spec_dir = std::env::var_os(SPEC_DIR_ENV).map(PathBuf::from);

        let candidates =
            candidate_paths(env_override.as_deref(), exe_dir.as_deref(), cwd.as_deref());
        for candidate in &candidates {
            if candidate.is_file() {
                let mut engine = Self::from_path(candidate)?;
                engine.spec_dir = spec_dir;
                return Ok(engine);
            }
        }

        let searched = candidates
            .iter()
            .map(|p| format!("  - {}", p.display()))
            .collect::<Vec<_>>()
            .join("\n");
        bail!(
            "Validation engine not found. Build it with `make build` (or set {ENGINE_ENV}).\nSearched:\n{searched}"
        )
    }

    /// Human readable description of the engine command.
    pub fn describe(&self) -> String {
        let mut parts = vec![self.program.display().to_string()];
        parts.extend(
            self.prefix_args
                .iter()
                .map(|a| a.to_string_lossy().into_owned()),
        );
        parts.join(" ")
    }

    fn command(&self) -> Command {
        let mut cmd = Command::new(&self.program);
        cmd.args(&self.prefix_args);
        if let Some(dir) = &self.spec_dir {
            cmd.arg("--spec-dir").arg(dir);
        }
        // GUI apps launched from Finder have a minimal environment; make sure a framework-dependent
        // apphost can still locate the runtime.
        if std::env::var_os("DOTNET_ROOT").is_none()
            && let Some(root) = find_dotnet_root()
        {
            cmd.env("DOTNET_ROOT", root);
        }
        cmd
    }

    /// Run `--list-specs` and parse the result.
    pub fn list_specs(&self) -> Result<SpecListReport> {
        let output = self
            .command()
            .arg("--list-specs")
            .output()
            .with_context(|| format!("Failed to launch engine: {}", self.describe()))?;
        if !output.status.success() {
            bail!(
                "Engine exited with {}: {}",
                output.status,
                String::from_utf8_lossy(&output.stderr).trim()
            );
        }
        let report: SpecListReport = serde_json::from_slice(&output.stdout)
            .context("Engine returned invalid JSON for --list-specs")?;
        check_schema(report.schema_version)?;
        Ok(report)
    }

    /// Validate `file` against `spec`, returning the parsed report.
    ///
    /// Exit code 0 (all valid) and 1 (validation errors) both yield `Ok`; exit code 2 yields
    /// the engine's error payload as an `Err`.
    pub fn validate(&self, spec: &str, file: &Path) -> Result<ValidationReport> {
        let output = self
            .command()
            .arg("--spec")
            .arg(spec)
            .arg("--file")
            .arg(file)
            .output()
            .with_context(|| format!("Failed to launch engine: {}", self.describe()))?;
        parse_validation_output(output.status.code(), &output.stdout, &output.stderr)
    }
}

/// Interpret raw engine output for a validation run.
pub fn parse_validation_output(
    exit_code: Option<i32>,
    stdout: &[u8],
    stderr: &[u8],
) -> Result<ValidationReport> {
    let parsed: Option<ValidationReport> = serde_json::from_slice(stdout).ok();
    match (exit_code, parsed) {
        (Some(0 | 1), Some(report)) => {
            check_schema(report.schema_version)?;
            if report.success {
                Ok(report)
            } else {
                bail!(report.error.unwrap_or_else(|| "Validation failed".into()))
            }
        }
        (_, Some(report)) if report.error.is_some() => bail!(report.error.unwrap_or_default()),
        (code, _) => {
            let stderr = String::from_utf8_lossy(stderr);
            let detail = stderr
                .lines()
                .rev()
                .find(|l| !l.trim().is_empty())
                .unwrap_or("no output");
            bail!(
                "Engine failed (exit code {}): {}",
                code.map_or("signal".into(), |c| c.to_string()),
                detail.trim()
            )
        }
    }
}

fn check_schema(version: u32) -> Result<()> {
    if version != SUPPORTED_SCHEMA_VERSION {
        bail!(
            "Engine schema version {version} is not supported (expected {SUPPORTED_SCHEMA_VERSION}). Rebuild the engine and client together."
        );
    }
    Ok(())
}

/// Ordered list of candidate engine paths.
pub fn candidate_paths(
    env_override: Option<&Path>,
    exe_dir: Option<&Path>,
    cwd: Option<&Path>,
) -> Vec<PathBuf> {
    let mut out = Vec::new();
    if let Some(path) = env_override {
        out.push(path.to_path_buf());
    }
    if let Some(exe_dir) = exe_dir {
        // macOS bundle: Contents/MacOS/<exe> -> Contents/Resources/engine/validator
        out.push(exe_dir.join("../Resources/engine").join(APPHOST_NAME));
        out.push(exe_dir.join("engine").join(APPHOST_NAME));
        out.push(exe_dir.join("engine").join(DLL_NAME));
    }
    for root in [exe_dir, cwd].into_iter().flatten() {
        for ancestor in root.ancestors() {
            for config in ["Release", "Debug"] {
                let bin = ancestor.join("validator/bin").join(config).join("net10.0");
                out.push(bin.join(APPHOST_NAME));
                out.push(bin.join(DLL_NAME));
            }
        }
    }
    out.dedup();
    out
}

/// Locate the `dotnet` muxer, accounting for the minimal PATH of GUI apps on macOS.
pub fn find_dotnet() -> Option<PathBuf> {
    let exe = if cfg!(windows) {
        "dotnet.exe"
    } else {
        "dotnet"
    };
    let mut dirs: Vec<PathBuf> = Vec::new();
    if let Some(root) = std::env::var_os("DOTNET_ROOT") {
        dirs.push(PathBuf::from(root));
    }
    if let Some(path) = std::env::var_os("PATH") {
        dirs.extend(std::env::split_paths(&path));
    }
    dirs.extend(
        [
            "/usr/local/share/dotnet",
            "/opt/homebrew/bin",
            "/usr/local/bin",
            "/usr/share/dotnet",
            "/usr/lib/dotnet",
            "C:\\Program Files\\dotnet",
        ]
        .iter()
        .map(PathBuf::from),
    );
    if let Some(home) = std::env::var_os("HOME") {
        dirs.push(PathBuf::from(home).join(".dotnet"));
    }
    dirs.into_iter().map(|d| d.join(exe)).find(|p| p.is_file())
}

/// Locate the .NET installation root directory containing shared/Microsoft.NETCore.App.
pub fn find_dotnet_root() -> Option<PathBuf> {
    if let Some(root) = std::env::var_os("DOTNET_ROOT") {
        let p = PathBuf::from(root);
        if p.join("shared/Microsoft.NETCore.App").is_dir() {
            return Some(p);
        }
    }
    if let Some(dotnet) = find_dotnet() {
        let resolved = std::fs::canonicalize(&dotnet).unwrap_or(dotnet);
        if let Some(parent) = resolved.parent()
            && parent.join("shared/Microsoft.NETCore.App").is_dir()
        {
            return Some(parent.to_path_buf());
        }
    }
    for candidate in [
        "/usr/local/share/dotnet",
        "/opt/homebrew/share/dotnet",
        "/usr/share/dotnet",
        "/usr/lib/dotnet",
        "C:\\Program Files\\dotnet",
    ] {
        let p = PathBuf::from(candidate);
        if p.join("shared/Microsoft.NETCore.App").is_dir() {
            return Some(p);
        }
    }
    None
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn candidates_prefer_env_then_bundle_then_dev_tree() {
        let paths = candidate_paths(
            Some(Path::new("/custom/validator")),
            Some(Path::new("/App.app/Contents/MacOS")),
            Some(Path::new("/repo/client")),
        );
        assert_eq!(paths[0], PathBuf::from("/custom/validator"));
        assert_eq!(
            paths[1],
            PathBuf::from("/App.app/Contents/MacOS/../Resources/engine").join(APPHOST_NAME)
        );
        let dev = PathBuf::from("/repo/validator/bin/Release/net10.0").join(APPHOST_NAME);
        assert!(
            paths.contains(&dev),
            "dev tree candidate missing: {paths:?}"
        );
    }

    #[test]
    fn from_path_rejects_missing_file() {
        assert!(Engine::from_path(Path::new("/definitely/not/here/validator")).is_err());
    }

    #[test]
    fn parse_output_accepts_invalid_exit_code_with_report() {
        let json = br#"{"schemaVersion":1,"success":true,"spec":"account","lines":[],
            "stats":{"totalLines":0,"dataLines":0,"validLines":0,"invalidLines":0,"fieldErrors":0,
            "headerValid":true,"passRate":0.0,"errorsByField":{}}}"#;
        let report = parse_validation_output(Some(1), json, b"").unwrap();
        assert_eq!(report.spec.as_deref(), Some("account"));
    }

    #[test]
    fn parse_output_surfaces_engine_error_payload() {
        let json = br#"{"schemaVersion":1,"success":false,"error":"Unknown specification 'x'"}"#;
        let err = parse_validation_output(Some(2), json, b"").unwrap_err();
        assert_eq!(err.to_string(), "Unknown specification 'x'");
    }

    #[test]
    fn parse_output_reports_stderr_when_no_json() {
        let err =
            parse_validation_output(Some(134), b"", b"boom\nUnhandled exception\n").unwrap_err();
        assert!(err.to_string().contains("Unhandled exception"), "{err}");
        assert!(err.to_string().contains("134"));
    }

    #[test]
    fn parse_output_rejects_unknown_schema() {
        let json = br#"{"schemaVersion":99,"success":true}"#;
        let err = parse_validation_output(Some(0), json, b"").unwrap_err();
        assert!(err.to_string().contains("schema version 99"));
    }
}
