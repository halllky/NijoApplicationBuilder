// Prevents additional console window on Windows in release, DO NOT REMOVE!!
#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

use std::env::{self, current_dir};
use std::fs::File;
use std::io::Read;

fn main() {
    tauri::Builder::default()
        .invoke_handler(tauri::generate_handler![get_cli_arg_file,])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}

#[tauri::command]
fn get_cli_arg_file() -> String {
    let args: Vec<String> = env::args().collect();
    if args.len() < 2 {
        return String::new();
    }
    let file_path = &args[1];

    let mut file = match File::open(file_path) {
        Ok(f) => f,
        Err(err) => {
            println!("Failed to open file '{}': {}", file_path, err);
            match current_dir() {
                Ok(cd) => println!("Current directory: {}", cd.display()),
                Err(err) => println!("{}", err),
            }
            return String::new();
        }
    };

    let mut contents = String::new();
    if let Err(err) = file.read_to_string(&mut contents) {
        println!("Failed to read file: {}", err);
        return String::new();
    }
    return contents;
}
