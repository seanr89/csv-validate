//! egui/eframe user interface for the csv-validate desktop client.

use std::path::{Path, PathBuf};
use std::sync::mpsc::{Receiver, Sender, channel};
use std::time::{Duration, Instant};

use eframe::egui::{
    self, Align, Align2, Color32, CornerRadius, FontId, Id, Key, KeyboardShortcut, LayerId, Layout,
    Modifiers, Order, RichText, Sense, Stroke, StrokeKind, Vec2,
};

use csv_validate_client::engine::Engine;
use csv_validate_client::inspector::{self, ErrorFilter, ErrorRow};
use csv_validate_client::model::{SpecDescriptor, ValidationReport};
use csv_validate_client::{export, format_bytes, format_count, guess_spec};

const GREEN: Color32 = Color32::from_rgb(46, 160, 67);
const RED: Color32 = Color32::from_rgb(218, 54, 51);
const AMBER: Color32 = Color32::from_rgb(210, 153, 34);
const ACCENT: Color32 = Color32::from_rgb(56, 132, 255);
const ROW_HEIGHT: f32 = 22.0;
const FILE_EXTENSIONS: [&str; 6] = ["csv", "tsv", "txt", "psv", "dat", "dsv"];

enum Msg {
    Specs(Result<Vec<SpecDescriptor>, String>),
    Validated {
        result: Result<Box<ValidationReport>, String>,
        elapsed: Duration,
    },
}

enum SpecsState {
    Loading,
    Ready,
    Failed(String),
}

enum RunState {
    Idle,
    Running {
        started: Instant,
        spec: String,
        file: PathBuf,
    },
    Done {
        report: Box<ValidationReport>,
        elapsed: Duration,
    },
    Failed(String),
}

#[derive(Clone, Copy, PartialEq, Eq)]
enum Tab {
    Summary,
    Errors,
}

struct SelectedFile {
    path: PathBuf,
    size: Option<u64>,
}

/// Cached inspector state, rebuilt when the report or filters change.
#[derive(Default)]
struct Inspector {
    rows: Vec<ErrorRow>,
    fields: Vec<String>,
    filtered: Vec<usize>,
    filter: ErrorFilter,
    applied_filter: Option<ErrorFilter>,
    invalid_only: bool,
    applied_invalid_only: Option<bool>,
    selected: Option<usize>,
}

impl Inspector {
    fn reset(&mut self) {
        *self = Self {
            invalid_only: true,
            ..Default::default()
        };
    }

    fn refresh(&mut self, report: &ValidationReport) {
        if self.applied_invalid_only != Some(self.invalid_only) {
            self.rows = inspector::build_rows(report, self.invalid_only);
            self.fields = inspector::distinct_fields(&self.rows);
            self.applied_invalid_only = Some(self.invalid_only);
            self.applied_filter = None;
            self.selected = None;
        }
        if self.applied_filter.as_ref() != Some(&self.filter) {
            self.filtered = self.filter.apply(&self.rows);
            self.applied_filter = Some(self.filter.clone());
            if self.selected.is_some_and(|s| !self.filtered.contains(&s)) {
                self.selected = None;
            }
        }
    }
}

pub struct ClientApp {
    engine: Result<Engine, String>,
    specs: Vec<SpecDescriptor>,
    specs_state: SpecsState,
    selected_spec: Option<String>,
    file: Option<SelectedFile>,
    run: RunState,
    tab: Tab,
    inspector: Inspector,
    status: Option<(String, bool)>,
    tx: Sender<Msg>,
    rx: Receiver<Msg>,
}

impl ClientApp {
    pub fn new(cc: &eframe::CreationContext<'_>) -> Self {
        let (tx, rx) = channel();
        let engine = Engine::discover().map_err(|e| e.to_string());
        let mut app = Self {
            engine,
            specs: Vec::new(),
            specs_state: SpecsState::Loading,
            selected_spec: None,
            file: None,
            run: RunState::Idle,
            tab: Tab::Summary,
            inspector: Inspector {
                invalid_only: true,
                ..Default::default()
            },
            status: None,
            tx,
            rx,
        };
        app.load_specs(&cc.egui_ctx);
        app
    }

    fn load_specs(&mut self, ctx: &egui::Context) {
        let engine = match &self.engine {
            Ok(engine) => engine.clone(),
            Err(err) => {
                self.specs_state = SpecsState::Failed(err.clone());
                return;
            }
        };
        self.specs_state = SpecsState::Loading;
        let tx = self.tx.clone();
        let ctx = ctx.clone();
        std::thread::spawn(move || {
            let result = engine
                .list_specs()
                .map(|r| r.specs)
                .map_err(|e| format!("{e:#}"));
            let _ = tx.send(Msg::Specs(result));
            ctx.request_repaint();
        });
    }

    fn start_validation(&mut self, ctx: &egui::Context) {
        let (Ok(engine), Some(spec), Some(file)) = (&self.engine, &self.selected_spec, &self.file)
        else {
            return;
        };
        if matches!(self.run, RunState::Running { .. }) {
            return;
        }
        let engine = engine.clone();
        let spec = spec.clone();
        let path = file.path.clone();
        self.run = RunState::Running {
            started: Instant::now(),
            spec: spec.clone(),
            file: path.clone(),
        };
        self.status = None;
        let tx = self.tx.clone();
        let ctx = ctx.clone();
        std::thread::spawn(move || {
            let started = Instant::now();
            let result = engine
                .validate(&spec, &path)
                .map(Box::new)
                .map_err(|e| format!("{e:#}"));
            let _ = tx.send(Msg::Validated {
                result,
                elapsed: started.elapsed(),
            });
            ctx.request_repaint();
        });
    }

    fn poll_messages(&mut self) {
        while let Ok(msg) = self.rx.try_recv() {
            match msg {
                Msg::Specs(Ok(specs)) => {
                    self.specs = specs;
                    self.specs_state = SpecsState::Ready;
                    let still_valid = self
                        .selected_spec
                        .as_ref()
                        .is_some_and(|s| self.specs.iter().any(|d| &d.file_type == s));
                    if !still_valid {
                        self.selected_spec = self.specs.first().map(|s| s.file_type.clone());
                        self.auto_select_spec();
                    }
                }
                Msg::Specs(Err(err)) => self.specs_state = SpecsState::Failed(err),
                Msg::Validated {
                    result: Ok(report),
                    elapsed,
                } => {
                    self.inspector.reset();
                    let has_errors = report.stats.as_ref().is_some_and(|s| s.invalid_lines > 0);
                    self.status = Some((
                        if has_errors {
                            "Validation finished with errors".into()
                        } else {
                            "Validation passed".into()
                        },
                        !has_errors,
                    ));
                    self.run = RunState::Done { report, elapsed };
                    self.tab = Tab::Summary;
                }
                Msg::Validated {
                    result: Err(err), ..
                } => {
                    self.status = Some((err.clone(), false));
                    self.run = RunState::Failed(err);
                }
            }
        }
    }

    fn set_file(&mut self, path: PathBuf) {
        let size = std::fs::metadata(&path).ok().map(|m| m.len());
        self.file = Some(SelectedFile { path, size });
        self.auto_select_spec();
        if !matches!(self.run, RunState::Running { .. }) {
            self.run = RunState::Idle;
        }
        self.status = None;
    }

    fn auto_select_spec(&mut self) {
        let Some(file) = &self.file else { return };
        let name = file
            .path
            .file_name()
            .map(|n| n.to_string_lossy().into_owned())
            .unwrap_or_default();
        if let Some(spec) = guess_spec(&name, self.specs.iter().map(|s| s.file_type.as_str())) {
            self.selected_spec = Some(spec.to_string());
        }
    }

    fn browse_file(&mut self) {
        if let Some(path) = rfd::FileDialog::new()
            .set_title("Select a delimited file to validate")
            .add_filter("Delimited files", &FILE_EXTENSIONS)
            .add_filter("All files", &["*"])
            .pick_file()
        {
            self.set_file(path);
        }
    }

    fn can_validate(&self) -> bool {
        self.engine.is_ok()
            && self.selected_spec.is_some()
            && self.file.is_some()
            && !matches!(self.run, RunState::Running { .. })
    }

    fn current_spec(&self) -> Option<&SpecDescriptor> {
        let name = self.selected_spec.as_ref()?;
        self.specs.iter().find(|s| &s.file_type == name)
    }

    // ------------------------------------------------------------------
    // Input handling
    // ------------------------------------------------------------------

    fn handle_input(&mut self, ctx: &egui::Context) {
        let dropped: Vec<PathBuf> = ctx.input(|i| {
            i.raw
                .dropped_files
                .iter()
                .map(|f| f.path().to_path_buf())
                .collect()
        });
        if let Some(path) = dropped.into_iter().find(|p| p.is_file()) {
            self.set_file(path);
        }

        let open = KeyboardShortcut::new(Modifiers::COMMAND, Key::O);
        let run = KeyboardShortcut::new(Modifiers::COMMAND, Key::Enter);
        if ctx.input_mut(|i| i.consume_shortcut(&open)) {
            self.browse_file();
        }
        if ctx.input_mut(|i| i.consume_shortcut(&run)) && self.can_validate() {
            self.start_validation(ctx);
        }
    }

    fn paint_drop_overlay(&self, ctx: &egui::Context) {
        let hovering = ctx.input(|i| !i.raw.hovered_files.is_empty());
        if !hovering {
            return;
        }
        let painter = ctx.layer_painter(LayerId::new(
            Order::Foreground,
            Id::unique("file_drop_overlay"),
        ));
        let rect = ctx.content_rect();
        painter.rect_filled(rect, 0.0, Color32::from_black_alpha(170));
        painter.rect_stroke(
            rect.shrink(12.0),
            12.0,
            Stroke::new(3.0, ACCENT),
            StrokeKind::Inside,
        );
        painter.text(
            rect.center(),
            Align2::CENTER_CENTER,
            "Drop file to validate",
            FontId::proportional(28.0),
            Color32::WHITE,
        );
    }

    // ------------------------------------------------------------------
    // Panels
    // ------------------------------------------------------------------

    fn header(&mut self, ui: &mut egui::Ui) {
        ui.horizontal(|ui| {
            ui.heading(RichText::new("CSV Validate").strong());
            ui.label(RichText::new("delimited file validator").weak());
            ui.with_layout(Layout::right_to_left(Align::Center), |ui| {
                match &self.engine {
                    Ok(engine) => {
                        ui.label(RichText::new("● Engine ready").color(GREEN))
                            .on_hover_text(engine.describe());
                    }
                    Err(err) => {
                        ui.label(RichText::new("● Engine missing").color(RED))
                            .on_hover_text(err.as_str());
                    }
                }
            });
        });
    }

    fn controls(&mut self, ui: &mut egui::Ui) {
        let ctx = ui.ctx().clone();
        egui::ScrollArea::vertical()
            .auto_shrink([false, false])
            .show(ui, |ui| {
                ui.add_space(6.0);
                step_heading(ui, 1, "Choose a specification");
                match &self.specs_state {
                    SpecsState::Loading => {
                        ui.horizontal(|ui| {
                            ui.spinner();
                            ui.label("Loading specifications…");
                        });
                    }
                    SpecsState::Failed(err) => {
                        ui.colored_label(RED, "Unable to load specifications");
                        ui.label(RichText::new(err).small().weak());
                        if ui.button("Retry").clicked() {
                            self.engine = Engine::discover().map_err(|e| e.to_string());
                            self.load_specs(&ctx);
                        }
                    }
                    SpecsState::Ready => {
                        let selected_text = self
                            .selected_spec
                            .clone()
                            .unwrap_or_else(|| "Select…".into());
                        egui::ComboBox::from_id_salt("spec_select")
                            .width(ui.available_width() - 8.0)
                            .selected_text(selected_text)
                            .show_ui(ui, |ui| {
                                for spec in &self.specs {
                                    ui.selectable_value(
                                        &mut self.selected_spec,
                                        Some(spec.file_type.clone()),
                                        &spec.file_type,
                                    );
                                }
                            });
                        if let Some(spec) = self.current_spec() {
                            ui.label(
                                RichText::new(format!(
                                    "{} columns · delimiter '{}' · {}",
                                    spec.columns.len(),
                                    spec.delimiter,
                                    if spec.header_line {
                                        "header row"
                                    } else {
                                        "no header"
                                    }
                                ))
                                .small()
                                .weak(),
                            );
                        }
                    }
                }

                ui.add_space(14.0);
                step_heading(ui, 2, "Select a file");
                if drop_zone(ui, self.file.as_ref()).clicked() {
                    self.browse_file();
                }
                if let Some(file) = &self.file {
                    ui.add_space(4.0);
                    egui::Grid::new("file_meta")
                        .num_columns(2)
                        .spacing([8.0, 2.0])
                        .show(ui, |ui| {
                            ui.label(RichText::new("Name").weak());
                            ui.label(file_name(&file.path));
                            ui.end_row();
                            ui.label(RichText::new("Size").weak());
                            ui.label(
                                file.size
                                    .map(format_bytes)
                                    .unwrap_or_else(|| "unknown".into()),
                            );
                            ui.end_row();
                            ui.label(RichText::new("Folder").weak());
                            ui.add(
                                egui::Label::new(
                                    file.path
                                        .parent()
                                        .map(|p| p.display().to_string())
                                        .unwrap_or_default(),
                                )
                                .truncate(),
                            )
                            .on_hover_text(file.path.display().to_string());
                            ui.end_row();
                        });
                }

                ui.add_space(14.0);
                step_heading(ui, 3, "Validate");
                let enabled = self.can_validate();
                let button =
                    egui::Button::new(RichText::new("▶  Validate file").size(16.0).strong())
                        .min_size(Vec2::new(ui.available_width() - 8.0, 36.0));
                if ui
                    .add_enabled(enabled, button)
                    .on_hover_text("⌘ Enter")
                    .clicked()
                {
                    self.start_validation(&ctx);
                }
                if let RunState::Running {
                    started,
                    spec,
                    file,
                } = &self.run
                {
                    ui.horizontal(|ui| {
                        ui.spinner();
                        ui.label(format!(
                            "Validating {} as '{}' — {:.1}s",
                            file_name(file),
                            spec,
                            started.elapsed().as_secs_f32()
                        ));
                    });
                    ctx.request_repaint_after(Duration::from_millis(100));
                }
            });
    }

    fn status_bar(&self, ui: &mut egui::Ui) {
        ui.horizontal(|ui| match &self.status {
            Some((msg, ok)) => {
                ui.colored_label(if *ok { GREEN } else { RED }, msg);
            }
            None => {
                ui.label(RichText::new("Drop a file anywhere in the window · ⌘O to browse").weak());
            }
        });
    }

    fn central(&mut self, ui: &mut egui::Ui) {
        let report = match &self.run {
            RunState::Done { report, elapsed } => Some((report, *elapsed)),
            _ => None,
        };
        let Some((report, elapsed)) = report else {
            match &self.run {
                RunState::Failed(err) => empty_state(ui, "Validation could not run", err, RED),
                RunState::Running { .. } => {
                    empty_state(ui, "Validating…", "Results will appear here.", ACCENT)
                }
                _ => empty_state(
                    ui,
                    "No results yet",
                    "Choose a specification, drop a file, then press Validate.",
                    ui.visuals().weak_text_color(),
                ),
            }
            return;
        };

        ui.horizontal(|ui| {
            ui.selectable_value(
                &mut self.tab,
                Tab::Summary,
                RichText::new("Summary").size(15.0),
            );
            let errors = report.stats.as_ref().map_or(0, |s| s.invalid_lines);
            let label = if errors > 0 {
                format!("Errors ({})", format_count(errors))
            } else {
                "Errors".into()
            };
            ui.selectable_value(&mut self.tab, Tab::Errors, RichText::new(label).size(15.0));
        });
        ui.separator();

        // Clone the report pointer out of self to satisfy the borrow checker for &mut self below.
        let report: ValidationReport = (**report).clone();
        match self.tab {
            Tab::Summary => self.summary_view(ui, &report, elapsed),
            Tab::Errors => self.errors_view(ui, &report),
        }
    }

    fn summary_view(&mut self, ui: &mut egui::Ui, report: &ValidationReport, elapsed: Duration) {
        let Some(stats) = report.stats.clone() else {
            ui.label("The engine did not return statistics.");
            return;
        };
        egui::ScrollArea::vertical()
            .auto_shrink([false, false])
            .show(ui, |ui| {
                let passed = stats.invalid_lines == 0;
                ui.horizontal(|ui| {
                    let (text, color) = if passed {
                        ("✔ PASSED", GREEN)
                    } else {
                        ("✖ FAILED", RED)
                    };
                    egui::Frame::new()
                        .fill(color)
                        .corner_radius(CornerRadius::same(6))
                        .inner_margin(egui::Margin::symmetric(10, 4))
                        .show(ui, |ui| {
                            ui.label(RichText::new(text).strong().color(Color32::WHITE))
                        });
                    let name = report
                        .summary
                        .as_ref()
                        .map(|s| s.file_name.clone())
                        .or_else(|| report.file_path.as_deref().map(|p| file_name(Path::new(p))))
                        .unwrap_or_default();
                    ui.heading(name);
                    ui.label(
                        RichText::new(format!("spec: {}", report.spec.as_deref().unwrap_or("?")))
                            .weak(),
                    );
                });
                if let Some(at) = report
                    .summary
                    .as_ref()
                    .and_then(|s| s.processed_at.as_deref())
                {
                    ui.label(
                        RichText::new(format!("Processed at {}", pretty_timestamp(at)))
                            .small()
                            .weak(),
                    );
                }
                ui.add_space(10.0);

                ui.horizontal_wrapped(|ui| {
                    stat_card(ui, "Data rows", &format_count(stats.data_lines), None);
                    stat_card(
                        ui,
                        "Valid rows",
                        &format_count(stats.data_lines.saturating_sub(data_invalid(&stats))),
                        Some(GREEN),
                    );
                    stat_card(
                        ui,
                        "Invalid lines",
                        &format_count(stats.invalid_lines),
                        Some(if stats.invalid_lines > 0 { RED } else { GREEN }),
                    );
                    stat_card(
                        ui,
                        "Field errors",
                        &format_count(stats.field_errors),
                        Some(if stats.field_errors > 0 { AMBER } else { GREEN }),
                    );
                    stat_card(ui, "Total lines", &format_count(stats.total_lines), None);
                    stat_card(ui, "File size", &format_bytes(report.file_size_bytes), None);
                    stat_card(
                        ui,
                        "Engine time",
                        &format!("{} ms", format_count(report.duration_ms)),
                        None,
                    );
                    stat_card(
                        ui,
                        "Throughput",
                        &report.throughput().map_or("—".into(), |t| {
                            format!("{} rows/s", format_count(t.round() as u64))
                        }),
                        None,
                    );
                    stat_card(
                        ui,
                        "Round trip",
                        &format!("{:.2} s", elapsed.as_secs_f32()),
                        None,
                    );
                });

                ui.add_space(12.0);
                ui.label(RichText::new("Pass rate").strong());
                let rate = (stats.pass_rate / 100.0).clamp(0.0, 1.0) as f32;
                ui.add(
                    egui::ProgressBar::new(rate)
                        .text(format!("{:.2}%", stats.pass_rate))
                        .fill(if rate >= 1.0 {
                            GREEN
                        } else if rate >= 0.9 {
                            AMBER
                        } else {
                            RED
                        }),
                );
                ui.add_space(6.0);
                ui.horizontal(|ui| {
                    ui.label(RichText::new("Header row:").strong());
                    if stats.header_valid {
                        ui.colored_label(GREEN, "valid");
                    } else {
                        ui.colored_label(RED, "invalid — see Errors (line 1)");
                    }
                });

                let by_field = stats.errors_by_field_sorted();
                if !by_field.is_empty() {
                    ui.add_space(12.0);
                    ui.label(RichText::new("Errors by field").strong());
                    let max = by_field.first().map_or(1, |(_, c)| *c).max(1);
                    egui::Grid::new("errors_by_field")
                        .num_columns(3)
                        .spacing([12.0, 4.0])
                        .show(ui, |ui| {
                            for (field, count) in by_field {
                                if ui.link(field).on_hover_text("Show these errors").clicked() {
                                    self.inspector.filter = ErrorFilter {
                                        field: Some(field.to_string()),
                                        ..Default::default()
                                    };
                                    self.tab = Tab::Errors;
                                }
                                ui.add(
                                    egui::ProgressBar::new(count as f32 / max as f32)
                                        .desired_width(240.0)
                                        .fill(AMBER),
                                );
                                ui.label(format_count(count));
                                ui.end_row();
                            }
                        });
                }

                ui.add_space(16.0);
                ui.separator();
                ui.horizontal(|ui| {
                    ui.label(RichText::new("Export").strong());
                    if ui.button("Save JSON report…").clicked() {
                        self.export(report, ExportKind::Json);
                    }
                    let has_errors = stats.invalid_lines > 0;
                    if ui
                        .add_enabled(has_errors, egui::Button::new("Save errors as CSV…"))
                        .clicked()
                    {
                        self.export(report, ExportKind::Csv);
                    }
                    if has_errors && ui.button("Inspect errors →").clicked() {
                        self.tab = Tab::Errors;
                    }
                });
            });
    }

    fn errors_view(&mut self, ui: &mut egui::Ui, report: &ValidationReport) {
        self.inspector.refresh(report);
        // Use the spec the report was produced with (the picker may have changed since).
        let spec = report
            .spec
            .as_ref()
            .and_then(|name| {
                self.specs
                    .iter()
                    .find(|s| s.file_type.eq_ignore_ascii_case(name))
            })
            .cloned();
        let insp = &mut self.inspector;

        // Filter bar
        ui.horizontal_wrapped(|ui| {
            ui.add(
                egui::TextEdit::singleline(&mut insp.filter.text)
                    .hint_text("🔍 Search field, value or message")
                    .desired_width(220.0),
            );
            let field_label = insp
                .filter
                .field
                .clone()
                .unwrap_or_else(|| "All fields".into());
            egui::ComboBox::from_id_salt("field_filter")
                .selected_text(field_label)
                .show_ui(ui, |ui| {
                    ui.selectable_value(&mut insp.filter.field, None, "All fields");
                    for field in &insp.fields {
                        ui.selectable_value(&mut insp.filter.field, Some(field.clone()), field);
                    }
                });
            ui.label("Lines");
            ui.add(
                egui::TextEdit::singleline(&mut insp.filter.line_from)
                    .hint_text("from")
                    .desired_width(56.0),
            );
            ui.label("–");
            ui.add(
                egui::TextEdit::singleline(&mut insp.filter.line_to)
                    .hint_text("to")
                    .desired_width(56.0),
            );
            ui.checkbox(&mut insp.invalid_only, "Invalid rows only");
            if ui
                .add_enabled(insp.filter.is_active(), egui::Button::new("Clear"))
                .clicked()
            {
                insp.filter = ErrorFilter::default();
            }
        });
        insp.refresh(report);
        ui.label(
            RichText::new(format!(
                "Showing {} of {} rows",
                format_count(insp.filtered.len() as u64),
                format_count(insp.rows.len() as u64)
            ))
            .small()
            .weak(),
        );
        ui.add_space(4.0);

        // Details panel for the selected row (drill-down into the full line).
        if let Some(row) = insp.selected.and_then(|i| insp.rows.get(i)).cloned() {
            egui::Panel::bottom("row_details")
                .resizable(true)
                .default_size(170.0)
                .show(ui, |ui| line_details(ui, report, &row, spec.as_ref()));
        }

        if insp.rows.is_empty() {
            empty_state(ui, "No errors 🎉", "Every line passed validation.", GREEN);
            return;
        }

        let columns = column_layout(ui.available_width());
        table_header(ui, &columns);
        egui::ScrollArea::vertical()
            .auto_shrink([false, false])
            .show_rows(ui, ROW_HEIGHT, insp.filtered.len(), |ui, range| {
                for view_idx in range {
                    let row_idx = insp.filtered[view_idx];
                    let row = &insp.rows[row_idx];
                    let selected = insp.selected == Some(row_idx);
                    if table_row(ui, &columns, row, selected, view_idx).clicked() {
                        insp.selected = if selected { None } else { Some(row_idx) };
                    }
                }
            });
    }

    fn export(&mut self, report: &ValidationReport, kind: ExportKind) {
        let stem = report
            .file_path
            .as_deref()
            .and_then(|p| Path::new(p).file_stem())
            .map(|s| s.to_string_lossy().into_owned())
            .unwrap_or_else(|| "report".into());
        let (name, filter, ext) = match kind {
            ExportKind::Json => (format!("{stem}_report.json"), "JSON", "json"),
            ExportKind::Csv => (format!("{stem}_errors.csv"), "CSV", "csv"),
        };
        let Some(path) = rfd::FileDialog::new()
            .set_file_name(&name)
            .add_filter(filter, &[ext])
            .save_file()
        else {
            return;
        };
        let result = match kind {
            ExportKind::Json => export::write_json(report, &path),
            ExportKind::Csv => export::write_errors_csv(report, &path),
        };
        self.status = Some(match result {
            Ok(()) => (format!("Saved {}", path.display()), true),
            Err(err) => (format!("Export failed: {err:#}"), false),
        });
    }
}

#[derive(Clone, Copy)]
enum ExportKind {
    Json,
    Csv,
}

impl eframe::App for ClientApp {
    fn ui(&mut self, ui: &mut egui::Ui, _frame: &mut eframe::Frame) {
        let ctx = ui.ctx().clone();
        self.poll_messages();
        self.handle_input(&ctx);

        egui::Panel::top("header").show(ui, |ui| {
            ui.add_space(4.0);
            self.header(ui);
            ui.add_space(2.0);
        });
        egui::Panel::bottom("status").show(ui, |ui| self.status_bar(ui));
        egui::Panel::left("controls")
            .resizable(true)
            .default_size(320.0)
            .size_range(260.0..=480.0)
            .show(ui, |ui| self.controls(ui));
        egui::CentralPanel::default().show(ui, |ui| self.central(ui));

        self.paint_drop_overlay(&ctx);
    }
}

// ----------------------------------------------------------------------
// Widgets & helpers
// ----------------------------------------------------------------------

fn step_heading(ui: &mut egui::Ui, step: u8, text: &str) {
    ui.horizontal(|ui| {
        ui.label(RichText::new(format!("{step}")).strong().color(ACCENT));
        ui.label(RichText::new(text).strong());
    });
    ui.add_space(2.0);
}

fn drop_zone(ui: &mut egui::Ui, file: Option<&SelectedFile>) -> egui::Response {
    let hovering_files = ui.ctx().input(|i| !i.raw.hovered_files.is_empty());
    let size = Vec2::new(ui.available_width() - 8.0, 120.0);
    let (rect, response) = ui.allocate_exact_size(size, Sense::click());
    let visuals = ui.visuals();
    let active = hovering_files || response.hovered();
    let stroke = Stroke::new(
        if active { 2.0 } else { 1.0 },
        if active {
            ACCENT
        } else {
            visuals.weak_text_color()
        },
    );
    let fill = if active {
        ACCENT.gamma_multiply(0.12)
    } else {
        visuals.extreme_bg_color
    };
    let painter = ui.painter();
    painter.rect_filled(rect, 10.0, fill);
    painter.rect_stroke(rect, 10.0, stroke, StrokeKind::Inside);

    let (title, subtitle) = match file {
        Some(f) => (
            format!("📄 {}", file_name(&f.path)),
            "Drop or click to choose a different file".to_string(),
        ),
        None => (
            "⬇  Drop a file here".to_string(),
            format!("or click to browse ({})", FILE_EXTENSIONS.join(", ")),
        ),
    };
    let text_color = visuals.strong_text_color();
    let weak = visuals.weak_text_color();
    painter.text(
        rect.center() - Vec2::new(0.0, 12.0),
        Align2::CENTER_CENTER,
        title,
        FontId::proportional(17.0),
        text_color,
    );
    painter.text(
        rect.center() + Vec2::new(0.0, 14.0),
        Align2::CENTER_CENTER,
        subtitle,
        FontId::proportional(12.0),
        weak,
    );
    response.on_hover_cursor(egui::CursorIcon::PointingHand)
}

fn stat_card(ui: &mut egui::Ui, title: &str, value: &str, color: Option<Color32>) {
    egui::Frame::group(ui.style())
        .inner_margin(egui::Margin::same(12))
        .corner_radius(8)
        .show(ui, |ui| {
            ui.set_min_width(130.0);
            ui.vertical(|ui| {
                ui.label(RichText::new(title).small().weak());
                let mut text = RichText::new(value).size(22.0).strong();
                if let Some(color) = color {
                    text = text.color(color);
                }
                ui.label(text);
            });
        });
}

fn empty_state(ui: &mut egui::Ui, title: &str, subtitle: &str, color: Color32) {
    ui.vertical_centered(|ui| {
        ui.add_space(ui.available_height() * 0.3);
        ui.label(RichText::new(title).size(22.0).color(color));
        ui.add_space(4.0);
        ui.label(RichText::new(subtitle).weak());
    });
}

/// Number of invalid *data* lines (excludes an invalid header line).
fn data_invalid(stats: &csv_validate_client::model::ReportStats) -> u64 {
    let header_invalid = u64::from(!stats.header_valid && stats.total_lines > stats.data_lines);
    stats.invalid_lines.saturating_sub(header_invalid)
}

fn file_name(path: &Path) -> String {
    path.file_name()
        .map(|n| n.to_string_lossy().into_owned())
        .unwrap_or_else(|| path.display().to_string())
}

/// Trim an ISO timestamp to `YYYY-MM-DD HH:MM:SS` for display.
fn pretty_timestamp(ts: &str) -> String {
    let trimmed: String = ts.chars().take(19).collect();
    trimmed.replace('T', " ")
}

struct Columns {
    widths: [f32; 5],
}

const COLUMN_TITLES: [&str; 5] = ["Line", "#", "Field", "Value", "Message"];

fn column_layout(total: f32) -> Columns {
    let line = 64.0;
    let idx = 36.0;
    let field = 150.0;
    let value = 170.0;
    let message = (total - line - idx - field - value - 16.0).max(160.0);
    Columns {
        widths: [line, idx, field, value, message],
    }
}

fn table_header(ui: &mut egui::Ui, columns: &Columns) {
    let (rect, _) =
        ui.allocate_exact_size(Vec2::new(ui.available_width(), ROW_HEIGHT), Sense::hover());
    let painter = ui.painter();
    painter.rect_filled(rect, 4.0, ui.visuals().faint_bg_color);
    let mut x = rect.left() + 6.0;
    for (title, width) in COLUMN_TITLES.iter().zip(columns.widths) {
        painter.text(
            egui::pos2(x, rect.center().y),
            Align2::LEFT_CENTER,
            title,
            FontId::proportional(12.5),
            ui.visuals().strong_text_color(),
        );
        x += width;
    }
}

fn table_row(
    ui: &mut egui::Ui,
    columns: &Columns,
    row: &ErrorRow,
    selected: bool,
    stripe: usize,
) -> egui::Response {
    let (rect, response) =
        ui.allocate_exact_size(Vec2::new(ui.available_width(), ROW_HEIGHT), Sense::click());
    let visuals = ui.visuals();
    let bg = if selected {
        visuals.selection.bg_fill
    } else if response.hovered() {
        visuals.widgets.hovered.weak_bg_fill
    } else if stripe % 2 == 1 {
        visuals.faint_bg_color
    } else {
        Color32::TRANSPARENT
    };
    let painter = ui.painter();
    painter.rect_filled(rect, 2.0, bg);

    let status_color = if row.valid { GREEN } else { RED };
    let text_color = if selected {
        visuals.selection.stroke.color
    } else {
        visuals.text_color()
    };
    let cells = [
        row.line.to_string(),
        row.field_index.map(|i| i.to_string()).unwrap_or_default(),
        row.field_name.clone(),
        if row.value.is_empty() {
            "∅".into()
        } else {
            row.value.clone()
        },
        row.message.clone(),
    ];
    let mut x = rect.left() + 6.0;
    for (i, (cell, width)) in cells.iter().zip(columns.widths).enumerate() {
        let cell_rect = egui::Rect::from_min_max(
            egui::pos2(x, rect.top()),
            egui::pos2(x + width - 6.0, rect.bottom()),
        );
        let color = match i {
            0 => status_color,
            3 if row.value.is_empty() => visuals.weak_text_color(),
            _ => text_color,
        };
        let font = if i == 3 {
            FontId::monospace(12.5)
        } else {
            FontId::proportional(13.0)
        };
        painter.with_clip_rect(cell_rect).text(
            egui::pos2(x, rect.center().y),
            Align2::LEFT_CENTER,
            cell,
            font,
            color,
        );
        x += width;
    }
    response.on_hover_text(format!(
        "Line {} · {}\n{}",
        row.line, row.field_name, row.message
    ))
}

fn line_details(
    ui: &mut egui::Ui,
    report: &ValidationReport,
    row: &ErrorRow,
    spec: Option<&SpecDescriptor>,
) {
    let Some(line) = report.lines.get(row.line_idx) else {
        return;
    };
    ui.add_space(4.0);
    ui.horizontal(|ui| {
        ui.label(
            RichText::new(format!("Line {}", line.line_count))
                .strong()
                .size(15.0),
        );
        if line.valid {
            ui.colored_label(GREEN, "valid");
        } else {
            ui.colored_label(RED, "invalid");
        }
        if let Some(msg) = line.message.as_deref().filter(|m| !m.is_empty()) {
            ui.label(RichText::new(msg).weak());
        }
    });
    egui::ScrollArea::vertical()
        .id_salt("details_scroll")
        .auto_shrink([false, false])
        .show(ui, |ui| {
            let failing: Vec<_> = line.records().iter().filter(|r| !r.valid).collect();
            if failing.is_empty() {
                ui.label(RichText::new("No field-level failures recorded for this line.").weak());
                return;
            }
            egui::Grid::new("details_grid")
                .num_columns(4)
                .striped(true)
                .spacing([14.0, 4.0])
                .show(ui, |ui| {
                    ui.label(RichText::new("Field").strong());
                    ui.label(RichText::new("Value").strong());
                    ui.label(RichText::new("Rule").strong());
                    ui.label(RichText::new("Error").strong());
                    ui.end_row();
                    for record in failing {
                        let name = record
                            .field_name
                            .clone()
                            .unwrap_or_else(|| inspector::LINE_LEVEL.into());
                        let index = record
                            .field_index
                            .map(|i| format!(" [{i}]"))
                            .unwrap_or_default();
                        ui.label(format!("{name}{index}"));
                        ui.label(
                            RichText::new(if record.key.is_empty() {
                                "∅"
                            } else {
                                &record.key
                            })
                            .monospace(),
                        );
                        let rule = spec
                            .and_then(|s| {
                                s.columns
                                    .iter()
                                    .find(|c| Some(c.index) == record.field_index)
                            })
                            .map(|c| c.rule_summary())
                            .unwrap_or_default();
                        ui.label(RichText::new(rule).small().weak());
                        ui.add(
                            egui::Label::new(record.error_message.clone().unwrap_or_default())
                                .wrap(),
                        );
                        ui.end_row();
                    }
                });
        });
}
