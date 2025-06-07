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
        app.MapGet("/typed-document/list", ListMemos);
        app.MapGet("/typed-document/load", LoadData);
        app.MapPost("/typed-document/save", SaveData);
    }

    private async Task ListMemos(HttpContext context) {
        var memoDir = GetMemoDirectoryPath();
        var results = new JsonArray();

        if (!Directory.Exists(memoDir)) {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(results, _jsonSerializerOptions, context.RequestAborted);
            return;
        }

        var jsonFiles = Directory.GetFiles(memoDir, "*.json");

        foreach (var filePath in jsonFiles) {
            try {
                // ファイル名は "(ドキュメントのUUID).json" となっている。
                // クライアント側URLはUUID。
                // 画面表示用名称は、JSONファイル中から名前を取得できたらそれを表示、取得できない場合はUUIDを表示する。
                var json = await File.ReadAllTextAsync(filePath, context.RequestAborted);
                var data = JsonSerializer.Deserialize<JsonObject>(json, _jsonSerializerOptions);

                if (data == null) continue;

                var entityId = Path.GetFileNameWithoutExtension(filePath);
                var entityName = data[ATTRIBUTE_NAME_NAME]?.ToString() ?? entityId;

                results.Add(new JsonArray {
                    entityId,
                    entityName,
                });

            } catch (JsonException ex) {
                // JSONのパースに失敗したファイルはログに出力してスキップ (本番ではより詳細なロギングを検討)
                Console.WriteLine($"Error deserializing {filePath}: {ex.Message}");
            } catch (Exception ex) {
                // その他の予期せぬエラー
                Console.WriteLine($"Error processing file {filePath}: {ex.Message}");
            }
        }

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(results, _jsonSerializerOptions, context.RequestAborted);
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
