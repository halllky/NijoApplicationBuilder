// このファイルは自動生成処理によって上書きされます。

namespace NIJO_APPLICATION_TEMPLATE {
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using System.Text.Json.Serialization;

    public static partial class Util {
        public static void ModifyJsonSrializerOptions(JsonSerializerOptions option) {
            // 日本語文字がUnicode変換されるのを避ける
            option.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All);

            // json中のenumの値を名前で設定できるようにする
            var enumConverter = new JsonStringEnumConverter();
            option.Converters.Add(enumConverter);

            // 値がnullの場合はレンダリングしない
            option.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        }
        public static JsonSerializerOptions GetJsonSrializerOptions() {
            if (_jsonSerializerOptions == null) {
                _jsonSerializerOptions = new JsonSerializerOptions();
                ModifyJsonSrializerOptions(_jsonSerializerOptions);
            }
            return _jsonSerializerOptions;
        }
        private static JsonSerializerOptions? _jsonSerializerOptions;

        public static string ToJson<T>(this T obj) {
            return JsonSerializer.Serialize(obj, GetJsonSrializerOptions());
        }
        public static T ParseJson<T>(string? json) {
            if (json == null) throw new ArgumentNullException(nameof(json));
            return JsonSerializer.Deserialize<T>(json, GetJsonSrializerOptions())!;
        }
        public static object ParseJson(string? json, Type type) {
            if (json == null) throw new ArgumentNullException(nameof(json));
            return JsonSerializer.Deserialize(json, type, GetJsonSrializerOptions())!;
        }
    }
}
