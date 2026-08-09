using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using System.Text.Json.Nodes;
using Nijo.WebService.Previewing;

namespace Nijo.WebService.SchemaEditor;

/// <summary>
/// nijo.xml と同階層に置かれる固定名ファイル群の内容の組。
/// </summary>
public class NijoProjectFiles {
    /// <summary>
    /// アプリケーション全体の状態（nijo.xmlの内容）
    /// </summary>
    [JsonPropertyName("applicationState")]
    public ApplicationState ApplicationState { get; set; } = new();

    /// <summary>
    /// 生成後アプリのデバッグ起動設定（nijo.preview.jsonの内容）
    /// </summary>
    [JsonPropertyName("previewSetting")]
    public PreviewSetting PreviewSetting { get; set; } = new();

    #region スキーマ定義グラフの見た目の状態
    /// <summary>
    /// スキーマ定義グラフの見た目の状態（nijo.viewState.jsonの内容）。
    /// nullの場合は保存をスキップする。
    /// </summary>
    [JsonPropertyName("schemaGraphViewState")]
    public SchemaGraphViewStateTypeByViewMode? SchemaGraphViewState { get; set; }

    /// <summary>
    /// グラフのモードごとの外観状態
    /// </summary>
    public class SchemaGraphViewStateTypeByViewMode {
        public const string KEY_SCHEMA_DEFINITION = "schemaDefinition";
        [JsonPropertyName(KEY_SCHEMA_DEFINITION)]
        public SchemaGraphViewStateType SchemaDefinition { get; set; } = new();
    }
    /// <summary>
    /// GraphView のプロパティとして設定されることになるデータ
    /// </summary>
    public class SchemaGraphViewStateType {
        public const string KEY_NODES = "nodes";
        public const string KEY_EDGES = "edges";
        public const string KEY_NODE_POSITIONS = "nodePositions";

        [JsonPropertyName(KEY_NODES)]
        [JsonConverter(typeof(SortedJsonConverter))]
        public JsonObject Nodes { get; set; } = new();
        [JsonPropertyName(KEY_EDGES)]
        [JsonConverter(typeof(SortedJsonArrayConverter))]
        public JsonArray Edges { get; set; } = new();
        [JsonPropertyName(KEY_NODE_POSITIONS)]
        [JsonConverter(typeof(SortedJsonConverter))]
        public JsonObject NodePositions { get; set; } = new();
    }

    /// <summary>
    /// JSONシリアライズ時にキーを昇順にソートして、保存内容を固定するためのカスタムJsonConverter
    /// </summary>
    public class SortedJsonConverter : JsonConverter<JsonObject> {
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
    /// <summary>
    /// JsonArrayの要素を安定化するためのカスタムJsonConverter
    /// </summary>
    public class SortedJsonArrayConverter : JsonConverter<JsonArray> {
        public override JsonArray Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            // 通常の読み込み処理
            return JsonSerializer.Deserialize<JsonArray>(ref reader, options) ?? new JsonArray();
        }

        public override void Write(Utf8JsonWriter writer, JsonArray value, JsonSerializerOptions options) {
            // 配列の要素を順序を保って書き込み
            writer.WriteStartArray();

            foreach (var item in value) {
                JsonSerializer.Serialize(writer, item, options);
            }

            writer.WriteEndArray();
        }
    }
    #endregion スキーマ定義グラフの見た目の状態
}
