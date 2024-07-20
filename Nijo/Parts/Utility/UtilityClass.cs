using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Nijo.Core;
using Nijo.Features.Storing;
using Nijo.Util.CodeGenerating;

namespace Nijo.Parts.Utility {
    internal class UtilityClass : ISummarizedFile {
        internal const string CLASSNAME = "Util";
        internal const string GET_JSONOPTION = "GetJsonSrializerOptions";
        internal const string MODIFY_JSONOPTION = "ModifyJsonSrializerOptions";
        internal const string TO_JSON = "ToJson";
        internal const string PARSE_JSON = "ParseJson";
        internal const string ENSURE_OBJECT_TYPE = "EnsureObjectType";
        internal const string TRY_PARSE_AS_OBJECT_TYPE = "TryParseAsObjectType";
        internal const string PARSE_JSON_AS_OBJARR = "ParseJsonAsObjectArray";

        private const string CUSTOM_CONVERTER_NAMESPACE = "CustomJsonConverters";
        private const string INT_CONVERTER = "IntegerValueConverter";
        private const string DATETIME_CONVERTER = "DateTimeValueConverter";

        int ISummarizedFile.RenderingOrder => 999;
        void ISummarizedFile.OnEndGenerating(CodeRenderingContext context) {
            context.CoreLibrary.UtilDir(dir => {
                dir.Generate(RenderJsonConversionMethods());
            });
        }

        private SourceFile RenderJsonConversionMethods() => new SourceFile {
            FileName = "JsonConversion.cs",
            RenderContent = context => {
                var refTargetJsonConverters = context.Schema
                    .RootAggregates()
                    .Where(root => root.Item.Options.Handler == NijoCodeGenerator.Models.WriteModel.Key)
                    .SelectMany(root => root.EnumerateThisAndDescendants())
                    .Select(agg => new DataClassForSaveRefTarget(agg))
                    .ToArray();

                return $$"""
                    namespace {{context.Config.RootNamespace}} {
                        using System.Text.Json;
                        using System.Text.Json.Nodes;

                        public static partial class {{CLASSNAME}} {
                            public static void {{MODIFY_JSONOPTION}}(JsonSerializerOptions option) {
                                // 日本語文字がUnicode変換されるのを避ける
                                option.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All);

                                // json中のenumの値を名前で設定できるようにする
                                var enumConverter = new System.Text.Json.Serialization.JsonStringEnumConverter();
                                option.Converters.Add(enumConverter);

                                // カスタムコンバータ
                                option.Converters.Add(new {{CUSTOM_CONVERTER_NAMESPACE}}.{{INT_CONVERTER}}());
                                option.Converters.Add(new {{CUSTOM_CONVERTER_NAMESPACE}}.{{DATETIME_CONVERTER}}());

                    {{refTargetJsonConverters.SelectTextTemplate(converter => $$"""
                                option.Converters.Add(new {{CUSTOM_CONVERTER_NAMESPACE}}.{{converter.CsJsonConverterName}}());
                    """)}}
                    {{_jsonConverters.SelectTextTemplate(converter => $$"""
                                option.Converters.Add(new {{CUSTOM_CONVERTER_NAMESPACE}}.{{converter.ConverterClassName}}());
                    """)}}
                            }
                            public static JsonSerializerOptions {{GET_JSONOPTION}}() {
                                var option = new System.Text.Json.JsonSerializerOptions();
                                {{MODIFY_JSONOPTION}}(option);
                                return option;
                            }

                            public static string {{TO_JSON}}<T>(this T obj) {
                                return JsonSerializer.Serialize(obj, {{GET_JSONOPTION}}());
                            }
                            public static T {{PARSE_JSON}}<T>(string? json) {
                                if (json == null) throw new ArgumentNullException(nameof(json));
                                return JsonSerializer.Deserialize<T>(json, {{GET_JSONOPTION}}())!;
                            }
                            public static object {{PARSE_JSON}}(string? json, Type type) {
                                if (json == null) throw new ArgumentNullException(nameof(json));
                                return JsonSerializer.Deserialize(json, type, {{GET_JSONOPTION}}())!;
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
                            /// <summary>
                            /// JSONから復元されたオブジェクトを事後的に特定の型として扱いたいときに用いる
                            /// </summary>
                            public static T {{ENSURE_OBJECT_TYPE}}<T>(object? obj) where T : new() {
                                return (T){{ENSURE_OBJECT_TYPE}}(obj, typeof(T));
                            }
                            /// <summary>
                            /// JSONから復元されたオブジェクトを事後的に特定の型として扱いたいときに用いる
                            /// </summary>
                            public static object {{ENSURE_OBJECT_TYPE}}(object? obj, Type type) {
                                if (obj == null) return Activator.CreateInstance(type) ?? throw new ArgumentException(nameof(type));
                                var json = obj as string ?? {{TO_JSON}}(obj);
                                return {{PARSE_JSON}}(json, type);
                            }
                            /// <summary>
                            /// JSONから復元されたオブジェクトを事後的に特定の型として扱いたいときに用いる
                            /// </summary>
                            public static bool {{TRY_PARSE_AS_OBJECT_TYPE}}<T>(object? obj, out T parsed) where T : new() {
                                try {
                                    var json = obj as string ?? {{TO_JSON}}(obj);
                                    parsed = {{PARSE_JSON}}<T>(json);
                                    return true;
                                } catch {
                                    parsed = new();
                                    return false;
                                }
                            }
                        }
                    }

                    namespace {{context.Config.RootNamespace}}.{{CUSTOM_CONVERTER_NAMESPACE}} {
                        using System.Text;
                        using System.Text.Json;
                        using System.Text.Json.Serialization;

                        {{WithIndent(RenderIntConverter(context), "    ")}}
                        {{WithIndent(RenderDateTimeConverter(context), "    ")}}
                    {{refTargetJsonConverters.SelectTextTemplate(converter => $$"""

                        {{WithIndent(converter.RenderServerSideJsonConverter(), "    ")}}
                    """)}}
                    {{_jsonConverters.SelectTextTemplate(converter => $$"""

                        {{WithIndent(converter.ConverterClassDeclaring, "    ")}}
                    """)}}
                    }
                    """;
            },
        };

        private static string RenderIntConverter(CodeRenderingContext ctx) {
            return $$"""
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
                        if (value == null) {
                            writer.WriteNullValue();
                        } else {
                            writer.WriteNumberValue((decimal)value);
                        }
                        // writer.WriteStringValue(value?.ToString());
                    }
                }
                """;
        }
        private static string RenderDateTimeConverter(CodeRenderingContext ctx) {
            return $$"""
                class {{DATETIME_CONVERTER}} : JsonConverter<DateTime?> {
                    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
                        var strDateTime = reader.GetString();
                        return string.IsNullOrWhiteSpace(strDateTime)
                            ? null
                            : DateTime.Parse(strDateTime);
                    }

                    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options) {
                        if (value == null) {
                            writer.WriteNullValue();
                        } else {
                            writer.WriteStringValue(value.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                        }
                    }
                }
                """;
        }

        #region JSONコンバータ
        internal class CustomJsonConverter {
            /// <summary>コンバータクラス名</summary>
            internal required string ConverterClassName { get; init; }
            /// <summary>コンバータクラス定義</summary>
            internal required string ConverterClassDeclaring { get; init; }
        }
        private readonly List<CustomJsonConverter> _jsonConverters = new();
        /// <summary>
        /// 特定の型をJSON変換する処理を登録します。
        /// </summary>
        internal void AddJsonConverter(CustomJsonConverter customJsonConverter) {
            _jsonConverters.Add(customJsonConverter);
        }
        #endregion JSONコンバータ
    }
}
