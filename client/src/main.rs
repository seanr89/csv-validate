//! csv-validate desktop client entry point.
//!
//! A cross-platform (macOS-first) egui/eframe frontend that lets users pick a specification,
//! drag-and-drop a delimited file, run the .NET validation engine and review statistics and errors.

#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

mod app;

fn main() -> eframe::Result {
    let options = eframe::NativeOptions {
        viewport: eframe::egui::ViewportBuilder::default()
            .with_title("CSV Validate")
            .with_app_id("com.seanr89.csv-validate")
            .with_inner_size([1120.0, 740.0])
            .with_min_inner_size([860.0, 560.0])
            .with_drag_and_drop(true),
        ..Default::default()
    };
    eframe::run_native(
        "CSV Validate",
        options,
        Box::new(|cc| Ok(Box::new(app::ClientApp::new(cc)))),
    )
}
