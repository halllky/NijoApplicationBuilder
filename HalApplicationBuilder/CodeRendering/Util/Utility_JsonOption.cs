using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.Util {
    partial class Utility {
        internal const string GET_JSONOPTION = "GetJsonSrializerOptions";
        internal const string MODIFY_JSONOPTION = "ModifyJsonSrializerOptions";
        internal const string TO_JSON = "ToJson";
        internal const string PARSE_JSON = "ParseJson";
        internal const string PARSE_JSON_AS_OBJARR = "ParseJsonAsObjectArray";

        private const string CUSTOM_CONVERTER_NAMESPACE = "CustomJsonConverters";
        private const string INT_CONVERTER = "IntegerValueConverter";

        internal string RenderJsonConversionMethods() {
            return $$"""
                namespace {{_ctx.Config.RootNamespace}} {
                    using System.Text.Json;
                    using System.Text.Json.Nodes;

                    static partial class {{CLASSNAME}} {
                        public static void {{MODIFY_JSONOPTION}}(JsonSerializerOptions option) {
                            // 日本語文字がUnicode変換されるのを避ける
                            option.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All);

                            // json中のenumの値を名前で設定できるようにする
                            var enumConverter = new System.Text.Json.Serialization.JsonStringEnumConverter();
                            option.Converters.Add(enumConverter);

                            // カスタムコンバータ
                            option.Converters.Add(new {{CUSTOM_CONVERTER_NAMESPACE}}.{{INT_CONVERTER}}());
                        }
                        public static JsonSerializerOptions {{GET_JSONOPTION}}() {
                            var option = new System.Text.Json.JsonSerializerOptions();
                            {{MODIFY_JSONOPTION}}(option);
                            return option;
                        }

                        public static string {{TO_JSON}}<T>(T obj) {
                            return JsonSerializer.Serialize(obj, {{GET_JSONOPTION}}());
                        }
                        public static T {{PARSE_JSON}}<T>(string? json) {
                            if (json == null) throw new ArgumentNullException(nameof(json));
                            return JsonSerializer.Deserialize<T>(json, {{GET_JSONOPTION}}())!;
                        }
                        /// <summary>
                        /// 単に <see cref="JsonSerializer.Deserialize(JsonElement, Type, JsonSerializerOptions?)"/> で object?[] を指定すると JsonElement[] 型になり各要素のキャストができないためその回避
                        /// </summary>
                        public static object?[] {{PARSE_JSON_AS_OBJARR}}(string? json) {
                            return ParseJson<JsonElement[]>(json)
                                .Select(jsonElement => (object?)(jsonElement.ValueKind switch {
                                    JsonValueKind.Undefined => null,
                                    JsonValueKind.Null => null,
                                    JsonValueKind.True => true,
                                    JsonValueKind.False => false,
                                    JsonValueKind.String => jsonElement.GetString(),
                                    JsonValueKind.Number => jsonElement.GetDecimal(),
                                    _ => jsonElement,
                                }))
                                .ToArray();
                        }
                    }
                }
                {{RenderIntConverter()}}
                """;
        }

        internal string RenderIntConverter() {
            return $$"""
                namespace {{_ctx.Config.RootNamespace}}.{{CUSTOM_CONVERTER_NAMESPACE}} {
                    using System.Text;
                    using System.Text.Json;
                    using System.Text.Json.Serialization;
                            
                    class {{INT_CONVERTER}} : JsonConverter<int?> {
                        public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {

                            return reader.TryGetDecimal(out var dec)
                                ? (int)dec
                                : null;

                            // var jsonValue = reader.GetString()?.Trim();
                            // if (jsonValue == null) return null;
                            // 
                            // var builder = new StringBuilder();
                            // foreach (var character in jsonValue) {
                            //     if (character == ',' || character == '，') {
                            //         // カンマ区切りは無視
                            //         continue;
                            //     } else if (character == '.' || character == '．') {
                            //         // 小数点以下は切り捨て
                            //         break;
                            //     } else if (char.IsDigit(character)) {
                            //         // 全角数値は半角数値に変換
                            //         builder.Append(char.GetNumericValue(character));
                            //     } else {
                            //         builder.Append(character);
                            //     }
                            // }
                            // 
                            // var converted = builder.ToString();
                            // return string.IsNullOrEmpty(converted) ? null : int.Parse(converted);
                        }
                    
                        public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options) {
                            writer.WriteStringValue(value?.ToString());
                        }
                    }
                }
                """;
        }
    }
}
