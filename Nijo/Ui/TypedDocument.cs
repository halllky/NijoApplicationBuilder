using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System;
using System.Text.Encodings.Web;
using System.Text.Json.Nodes;

namespace Nijo.Ui;

internal class TypedOutliner {
    private readonly GeneratedProject _project;
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new() {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // TypeScript側はキャメルケースのため
        WriteIndented = true,
        AllowTrailingCommas = true, // 読み込み時の柔軟性を高める
        ReadCommentHandling = JsonCommentHandling.Skip, // コメントを無視
    };

    internal TypedOutliner(GeneratedProject project) {
        _project = project;
    }

    private const string SETTINGS_FILE_NAME = "settings.json";
    private const string MEMO_DIRECTORY_NAME = "memo";
    private const string ATTRIBUTE_NAME_ID = "perspectiveId";
    private const string ATTRIBUTE_NAME_NAME = "name";

    private string GetMemoDirectoryPath() {
        var memoDir = Path.Combine(_project.ProjectRoot, MEMO_DIRECTORY_NAME);
        if (!Directory.Exists(memoDir)) {
            Directory.CreateDirectory(memoDir);
        }
        return memoDir;
    }

    private string GetSettingsFilePath() {
        return Path.Combine(GetMemoDirectoryPath(), SETTINGS_FILE_NAME);
    }
    private string GetMemoFilePath(string entityId) {
        if (string.IsNullOrEmpty(entityId)) {
            throw new ArgumentException("entityId cannot be null or empty.", nameof(entityId));
        }

        // ファイル名として不正な文字が含まれていないかチェック
        if (entityId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) {
            throw new ArgumentException($"entityId contains invalid characters for a file name: {entityId}", nameof(entityId));
        }

        // ディレクトリ区切り文字が含まれていないかチェック (ディレクトリトラバーサル対策)
        if (entityId.Contains(Path.DirectorySeparatorChar) || entityId.Contains(Path.AltDirectorySeparatorChar)) {
            throw new ArgumentException($"entityId cannot contain directory separator characters: {entityId}", nameof(entityId));
        }

        return Path.Combine(GetMemoDirectoryPath(), $"{entityId}.json");
    }

    internal void ConfigureWebApplication(WebApplication app) {
        app.MapGet("/typed-document/load-settings", LoadSettings);
        app.MapPost("/typed-document/save-settings", SaveSettings);
        app.MapGet("/typed-document/load", LoadData);
        app.MapPost("/typed-document/save", SaveData);
    }

    /// <summary>
    /// アプリケーション全体の設定を読み込む。
    /// エンティティ種類の一覧はディレクトリ内にあるJSONファイルを正とする。
    /// アプリケーション名や、エンティティ種類の並べ方の順番は、設定ファイルの内容を読み込んで取得する。
    /// </summary>
    private async Task LoadSettings(HttpContext context) {
        var memoDir = GetMemoDirectoryPath();

        var appSettingsForDisplay = new JsonObject();
        var entityTypeOrder = new List<string>(); // IDのリスト
        var entityTypeList = new List<JsonObject>();

        if (!Directory.Exists(memoDir)) {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(entityTypeList, _jsonSerializerOptions, context.RequestAborted);
            return;
        }

        var jsonFiles = Directory.GetFiles(memoDir, "*.json");

        foreach (var filePath in jsonFiles) {
            try {
                var json = await File.ReadAllTextAsync(filePath, context.RequestAborted);
                var data = JsonSerializer.Deserialize<JsonObject>(json, _jsonSerializerOptions);
                if (data == null) continue;

                // 設定ファイルの場合
                if (Path.GetFileName(filePath) == SETTINGS_FILE_NAME) {
                    appSettingsForDisplay["applicationName"] = data["applicationName"]?.ToString();
                    entityTypeOrder = data["entityTypeOrder"]?.Deserialize<List<string>>() ?? new List<string>();
                }

                // エンティティ種類のJSONの場合
                else {
                    // ファイル名は "(ドキュメントのUUID).json" となっている。
                    // クライアント側URLはUUID。
                    // 画面表示用名称は、JSONファイル中から名前を取得できたらそれを表示、取得できない場合はUUIDを表示する。
                    var entityId = Path.GetFileNameWithoutExtension(filePath);
                    var entityName = data[ATTRIBUTE_NAME_NAME]?.ToString() ?? entityId;

                    entityTypeList.Add(new JsonObject {
                        ["entityTypeId"] = entityId,
                        ["entityTypeName"] = entityName,
                    });
                }

            } catch (JsonException ex) {
                // JSONのパースに失敗したファイルはログに出力してスキップ (本番ではより詳細なロギングを検討)
                Console.WriteLine($"Error deserializing {filePath}: {ex.Message}");
            } catch (Exception ex) {
                // その他の予期せぬエラー
                Console.WriteLine($"Error processing file {filePath}: {ex.Message}");
            }
        }

        // 設定ファイル未指定の場合
        if (!appSettingsForDisplay.TryGetPropertyValue("applicationName", out var _)) {
            appSettingsForDisplay["applicationName"] = "アプリケーション名未設定";
        }

        // エンティティ種類の一覧を、保存された順番で並べ替える。
        // 設定ファイルで順番が指定されていないエンティティ種類は、配列の末尾に追加する。
        var ordered = entityTypeList
            .OrderBy(e => {
                var index = entityTypeOrder.IndexOf(e["entityTypeId"]?.ToString() ?? "");
                return index == -1 ? int.MaxValue : index;
            })
            .ToList();
        var entityTypeListJsonArray = new JsonArray();
        foreach (var entityType in ordered) {
            entityTypeListJsonArray.Add(entityType);
        }
        appSettingsForDisplay["entityTypeList"] = entityTypeListJsonArray;

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(appSettingsForDisplay, _jsonSerializerOptions, context.RequestAborted);
    }

    private async Task SaveSettings(HttpContext context) {
        // HTTPリクエストのデータを読み込む
        var data = await context.Request.ReadFromJsonAsync<JsonObject>(_jsonSerializerOptions, context.RequestAborted);
        if (data == null) {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Request body is empty.", context.RequestAborted);
            return;
        }

        // データを保存する
        var filePath = GetSettingsFilePath();
        var json = JsonSerializer.Serialize(data, _jsonSerializerOptions);
        await File.WriteAllTextAsync(filePath, json, context.RequestAborted);
    }

    private async Task LoadData(HttpContext context) {
        var typeIdValues = context.Request.Query["typeId"];
        var typeId = typeIdValues.FirstOrDefault();

        if (string.IsNullOrEmpty(typeId)) {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Query parameter 'typeId' is required.", context.RequestAborted);
            return;
        }

        var filePath = GetMemoFilePath(typeId);

        if (!File.Exists(filePath)) {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync("File not found.", context.RequestAborted);
            return;
        }

        var json = await File.ReadAllTextAsync(filePath, context.RequestAborted);
        var data = JsonSerializer.Deserialize<JsonObject>(json, _jsonSerializerOptions);
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(data, _jsonSerializerOptions, context.RequestAborted);
    }

    private async Task SaveData(HttpContext context) {
        JsonObject? data;
        try {
            data = await context.Request.ReadFromJsonAsync<JsonObject>(_jsonSerializerOptions, context.RequestAborted);
        } catch (Exception ex) {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync($"Invalid JSON format: {ex.Message}", context.RequestAborted);
            return;
        }

        if (data == null) {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Request body is empty.", context.RequestAborted);
            return;
        }

        var entityId = data[ATTRIBUTE_NAME_ID]?.ToString();
        if (string.IsNullOrEmpty(entityId)) {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Request body is empty or 'entityId' is missing.", context.RequestAborted);
            return;
        }

        var filePath = GetMemoFilePath(entityId);
        var json = JsonSerializer.Serialize(data, _jsonSerializerOptions);

        try {
            await File.WriteAllTextAsync(filePath, json, context.RequestAborted);
        } catch (Exception ex) {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync($"Failed to save data: {ex.Message}", context.RequestAborted);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status200OK;
        await context.Response.WriteAsync("Data saved successfully.", context.RequestAborted);
    }
}
