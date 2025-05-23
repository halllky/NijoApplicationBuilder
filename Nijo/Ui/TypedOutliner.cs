using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System;

namespace Nijo.Ui;

// TypeScriptの型定義に合わせたC#クラス
public class TypedOutlinerDto {
    [JsonPropertyName("typeId")]
    public string TypeId { get; set; } = string.Empty;
    [JsonPropertyName("typeName")]
    public string TypeName { get; set; } = string.Empty;
    [JsonPropertyName("attributes")]
    public List<OutlinerAttributeDto> Attributes { get; set; } = new();
    [JsonPropertyName("items")]
    public List<OutlinerItemDto> Items { get; set; } = new();
}

public class OutlinerAttributeDto {
    [JsonPropertyName("attributeId")]
    public string AttributeId { get; set; } = string.Empty;
    [JsonPropertyName("attributeName")]
    public string AttributeName { get; set; } = string.Empty;
}

public class OutlinerItemDto {
    [JsonPropertyName("itemId")]
    public string ItemId { get; set; } = string.Empty;
    [JsonPropertyName("itemName")]
    public string ItemName { get; set; } = string.Empty;
    [JsonPropertyName("indent")]
    public int Indent { get; set; }
    [JsonPropertyName("attributes")]
    public Dictionary<string, string> Attributes { get; set; } = new();
}

// 一覧表示用のDTO
public class TypedOutlinerListItemDto {
    [JsonPropertyName("typeId")]
    public string TypeId { get; set; } = string.Empty;
    [JsonPropertyName("typeName")]
    public string TypeName { get; set; } = string.Empty;
}

internal class TypedOutliner {
    private readonly GeneratedProject _project;
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // TypeScript側はキャメルケースのため
        WriteIndented = true,
        AllowTrailingCommas = true, // 読み込み時の柔軟性を高める
        ReadCommentHandling = JsonCommentHandling.Skip, // コメントを無視
    };

    internal TypedOutliner(GeneratedProject project) {
        _project = project;
    }

    private string GetMemoDirectoryPath() {
        var memoDir = Path.Combine(_project.ProjectRoot, "memo");
        if (!Directory.Exists(memoDir)) {
            Directory.CreateDirectory(memoDir);
        }
        return memoDir;
    }

    private string GetMemoFilePath(string typeId) {
        if (string.IsNullOrEmpty(typeId)) {
            throw new ArgumentException("typeId cannot be null or empty.", nameof(typeId));
        }

        // ファイル名として不正な文字が含まれていないかチェック
        if (typeId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) {
            throw new ArgumentException($"typeId contains invalid characters for a file name: {typeId}", nameof(typeId));
        }

        // ディレクトリ区切り文字が含まれていないかチェック (ディレクトリトラバーサル対策)
        if (typeId.Contains(Path.DirectorySeparatorChar) || typeId.Contains(Path.AltDirectorySeparatorChar)) {
            throw new ArgumentException($"typeId cannot contain directory separator characters: {typeId}", nameof(typeId));
        }

        return Path.Combine(GetMemoDirectoryPath(), $"{typeId}.json");
    }

    internal void ConfigureWebApplication(WebApplication app) {
        app.MapGet("/typed-outliner/list", ListMemos);
        app.MapGet("/typed-outliner/load", LoadData);
        app.MapPost("/typed-outliner/save", SaveData);
    }

    private async Task ListMemos(HttpContext context) {
        var memoDir = GetMemoDirectoryPath();
        var results = new List<TypedOutlinerListItemDto>();

        if (!Directory.Exists(memoDir)) {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(results, _jsonSerializerOptions, context.RequestAborted);
            return;
        }

        var jsonFiles = Directory.GetFiles(memoDir, "*.json");

        foreach (var filePath in jsonFiles) {
            try {
                var json = await File.ReadAllTextAsync(filePath, context.RequestAborted);
                var data = JsonSerializer.Deserialize<TypedOutlinerDto>(json, _jsonSerializerOptions);

                if (data != null && !string.IsNullOrEmpty(data.TypeId)) {
                    results.Add(new TypedOutlinerListItemDto {
                        TypeId = data.TypeId,
                        TypeName = data.TypeName ?? string.Empty // TypeNameがnullの場合も考慮
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
        var data = JsonSerializer.Deserialize<TypedOutlinerDto>(json, _jsonSerializerOptions);
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(data, _jsonSerializerOptions, context.RequestAborted);
    }

    private async Task SaveData(HttpContext context) {
        TypedOutlinerDto? data;
        try {
            data = await context.Request.ReadFromJsonAsync<TypedOutlinerDto>(_jsonSerializerOptions, context.RequestAborted);
        } catch (JsonException ex) {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync($"Invalid JSON format: {ex.Message}", context.RequestAborted);
            return;
        }

        if (data == null || string.IsNullOrEmpty(data.TypeId)) {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Request body is empty or 'typeId' is missing.", context.RequestAborted);
            return;
        }

        var filePath = GetMemoFilePath(data.TypeId);
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
