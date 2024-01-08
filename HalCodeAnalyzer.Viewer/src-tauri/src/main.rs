// Prevents additional console window on Windows in release, DO NOT REMOVE!!
#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

use std::env::{self, current_dir};
use std::fs::File;
use std::io::{Read, Write};
use std::path::PathBuf;

fn main() {
    tauri::Builder::default()
        .invoke_handler(tauri::generate_handler![load_file, save_file,])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}

fn get_openedfile_fullpath(filename_suffix: String) -> String {
    let args: Vec<String> = env::args().collect();
    if args.len() < 2 {
        return String::new();
    }
    let file_path = &args[1];

    let cd = match current_dir() {
        Ok(cd) => cd,
        Err(_) => PathBuf::new(),
    };
    let joined = cd.join(file_path);
    let joined_str = match joined.to_str() {
        Some(value) => value.to_owned() + &filename_suffix,
        None => file_path.to_owned() + &filename_suffix,
    };
    return joined_str.to_string();
}

#[derive(serde::Serialize)]
struct OpenedFile {
    fullpath: String,
    contents: String,
}
impl OpenedFile {
    fn empty() -> OpenedFile {
        OpenedFile {
            fullpath: String::new(),
            contents: String::new(),
        }
    }
}

#[tauri::command]
fn load_file(suffix: String) -> OpenedFile {
    let file_path = &get_openedfile_fullpath(suffix);
    let mut file = match File::open(file_path) {
        Ok(f) => f,
        Err(err) => {
            println!("Failed to open file '{}': {}", file_path, err);
            match current_dir() {
                Ok(cd) => println!("Current directory: {}", cd.display()),
                Err(err) => println!("{}", err),
            }
            return OpenedFile::empty();
        }
    };
    let mut contents = String::new();
    if let Err(err) = file.read_to_string(&mut contents) {
        println!("Failed to read file: {}", err);
        return OpenedFile::empty();
    }
    return OpenedFile {
        fullpath: file_path.to_string(),
        contents,
    };
}

#[tauri::command]
fn save_file(suffix: String, contents: String) {
    let file_path = &get_openedfile_fullpath(suffix);
    let mut file = match File::create(file_path) {
        Ok(f) => f,
        Err(err) => {
            println!("Failed to open file '{}': {}", file_path, err);
            match current_dir() {
                Ok(cd) => println!("Current directory: {}", cd.display()),
                Err(err) => println!("{}", err),
            };
            return;
        }
    };
    match file.write_all(contents.as_bytes()) {
        Ok(_) => println!("Successfully wrote to the file."),
        Err(e) => println!("Failed to write to the file: {}", e),
    }
    match file.flush() {
        Ok(_) => println!("Successfully closed the file."),
        Err(e) => println!("Failed to close the file: {}", e),
    }
}
