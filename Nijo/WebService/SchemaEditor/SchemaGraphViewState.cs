using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Nijo.WebService.SchemaEditor;

/// <summary>
/// nijo.viewState.json の内容。スキーマ定義グラフの見た目の状態（グラフのモードごと）。
/// </summary>
public class SchemaGraphViewState {
    public const string KEY_SCHEMA_DEFINITION = "schemaDefinition";
    [JsonPropertyName(KEY_SCHEMA_DEFINITION)]
    public GraphViewData SchemaDefinition { get; set; } = new();

    /// <summary>
    /// nijo.viewState.json を読み込む。ファイルが無い、または読み込みに失敗した場合はnullを返す。
    /// </summary>
    internal static SchemaGraphViewState? Load(GeneratedProject project) {
        var viewStatePath = project.ViewStateJsonPath;
        if (!File.Exists(viewStatePath)) return null;

        try {
            var viewStateJson = File.ReadAllText(viewStatePath);
            return JsonSerializer.Deserialize<SchemaGraphViewState>(viewStateJson);
        } catch (Exception) {
            // ファイル読み込みやデシリアライズに失敗した場合はnullを返す
            return null;
        }
    }

    /// <summary>
    /// nijo.viewState.json を保存する。
    /// </summary>
    internal async Task SaveAsync(GeneratedProject project, CancellationToken cancellationToken) {
        var viewStatePath = project.ViewStateJsonPath;
        var jsonOptions = new JsonSerializerOptions {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters = { new SortedJsonConverter() },
        };
        var jsonString = JsonSerializer.Serialize(this, jsonOptions);
        await File.WriteAllTextAsync(viewStatePath, jsonString, new System.Text.UTF8Encoding(false, false), cancellationToken);
    }

    /// <summary>
    /// GraphView のプロパティとして設定されることになるデータ
    /// </summary>
    public class GraphViewData {
        public const string KEY_NODES = "nodes";
        public const string KEY_EDGES = "edges";
        public const string KEY_NODE_POSITIONS = "nodePositions";

        [JsonPropertyName(KEY_NODES)]
        [JsonConverter(typeof(SortedJsonConverter))]
        public JsonObject Nodes { get; set; } = new();
        [JsonPropertyName(KEY_EDGES)]
        public JsonArray Edges { get; set; } = new();
        [JsonPropertyName(KEY_NODE_POSITIONS)]
        [JsonConverter(typeof(SortedJsonConverter))]
        public JsonObject NodePositions { get; set; } = new();
    }

    /// <summary>
    /// JSONシリアライズ時にキーを昇順にソートして、保存内容を固定するためのカスタムJsonConverter
    /// </summary>
    private class SortedJsonConverter : JsonConverter<JsonObject> {
        public override JsonObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            // 通常の読み込み処理
            return JsonSerializer.Deserialize<JsonObject>(ref reader, options) ?? new JsonObject();
        }

        public override void Write(Utf8JsonWriter writer, JsonObject value, JsonSerializerOptions options) {
            // キーを昇順にソートして書き込み
            var sortedProperties = value
                .OrderBy(kvp => kvp.Key)
                .ToList();

            writer.WriteStartObject();

            foreach (var property in sortedProperties) {
                writer.WritePropertyName(property.Key);
                JsonSerializer.Serialize(writer, property.Value, options);
            }

            writer.WriteEndObject();
        }
    }
}
