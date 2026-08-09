using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Nijo.WebService.Previewing;

/// <summary>
/// nijo.xml と同階層に配置される固定名JSON（nijo.preview.json）の内容。
/// 生成後アプリをデバッグ起動するためのプロセス起動情報を持つ。
/// Nijo は生成後プロジェクトのフォルダ構成を知らないため、
/// 起動対象のプロセスやログの出力先はすべてこのファイルの内容として利用者が定義する。
/// </summary>
public class PreviewSetting {

    public const string FILE_NAME = "nijo.preview.json";

    private static readonly JsonSerializerOptions SERIALIZER_OPTIONS = new() {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>起動完了時にブラウザで開くURL。空文字・未指定なら開かない</summary>
    [JsonPropertyName("browser")]
    public string? Browser { get; set; }

    /// <summary>nijo serve の起動と同時にプロセス群を自動起動するか</summary>
    [JsonPropertyName("startOnNijoServe")]
    public bool StartOnNijoServe { get; set; }

    /// <summary>並列実行するプロセスの定義</summary>
    [JsonPropertyName("concurrently")]
    public List<PreviewProcessSetting> Concurrently { get; set; } = [];

    /// <summary>
    /// nijo.preview.json を読み込む。ファイルが無い・壊れている場合は空の既定値を返す。
    /// </summary>
    public static PreviewSetting Load(GeneratedProject project) {
        var path = project.PreviewJsonPath;
        if (!File.Exists(path)) {
            return new PreviewSetting();
        }
        try {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<PreviewSetting>(json) ?? new PreviewSetting();
        } catch (Exception) {
            // ファイル読み込みやデシリアライズに失敗した場合は既定値のまま
            return new PreviewSetting();
        }
    }

    /// <summary>
    /// nijo.preview.json を保存する。
    /// </summary>
    public async Task SaveAsync(GeneratedProject project, CancellationToken cancellationToken) {
        var json = JsonSerializer.Serialize(this, SERIALIZER_OPTIONS);
        await File.WriteAllTextAsync(project.PreviewJsonPath, json, new UTF8Encoding(false, false), cancellationToken);
    }
}

/// <summary>concurrently で並列起動する1プロセスの設定</summary>
public class PreviewProcessSetting {
    /// <summary>プロセスの識別名。GUI上の表示やログ取得のキーに使う</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("process")]
    public PreviewProcessStartSetting Process { get; set; } = new();

    [JsonPropertyName("log")]
    public PreviewProcessLogSetting Log { get; set; } = new();

    /// <summary>コード再生成が成功した直後にこのプロセスを再起動するか</summary>
    [JsonPropertyName("restartOnGenerateCode")]
    public bool RestartOnGenerateCode { get; set; }
}

/// <summary>プロセスの起動コマンド</summary>
public class PreviewProcessStartSetting {
    /// <summary>nijo.xml のディレクトリからの相対パス</summary>
    [JsonPropertyName("cwd")]
    public string Cwd { get; set; } = string.Empty;

    [JsonPropertyName("filename")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("args")]
    public string Args { get; set; } = string.Empty;
}

/// <summary>プロセスの標準出力・標準エラー出力の書き出し先</summary>
public class PreviewProcessLogSetting {
    /// <summary>標準出力の書き出し先。nijo.xml のディレクトリからの相対パス。空文字ならログファイルを作らない</summary>
    [JsonPropertyName("stdout")]
    public string Stdout { get; set; } = string.Empty;

    /// <summary>標準エラー出力の書き出し先。nijo.xml のディレクトリからの相対パス。空文字ならログファイルを作らない</summary>
    [JsonPropertyName("stderr")]
    public string Stderr { get; set; } = string.Empty;

    /// <summary>true=起動の度に追記 / false=起動の度にクリア</summary>
    [JsonPropertyName("appendStdout")]
    public bool AppendStdout { get; set; }

    /// <summary>true=起動の度に追記 / false=起動の度にクリア</summary>
    [JsonPropertyName("appendStderr")]
    public bool AppendStderr { get; set; }
}
