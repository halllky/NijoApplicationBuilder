using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.Util {
    partial class Utility {
        internal const string GET_JSONOPTION = "GetJsonSrializerOptions";
        internal const string MODIFY_JSONOPTION = "ModifyJsonSrializerOptions";
        internal const string TO_JSON = "ToJson";
        internal const string PARSE_JSON = "ParseJson";

        internal string RenderJsonConversionMethods() {
            return $$"""
                namespace {{_ctx.Config.RootNamespace}} {
                    using System.Text.Json;

                    static partial class {{CLASSNAME}} {
                        public static void {{MODIFY_JSONOPTION}}(JsonSerializerOptions option) {
                            // 日本語文字がUnicode変換されるのを避ける
                            option.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All);

                            // json中のenumの値を名前で設定できるようにする
                            var enumConverter = new System.Text.Json.Serialization.JsonStringEnumConverter();
                            option.Converters.Add(enumConverter);
                        }
                        public static JsonSerializerOptions {{GET_JSONOPTION}}() {
                            var option = new System.Text.Json.JsonSerializerOptions();
                            {{MODIFY_JSONOPTION}}(option);
                            return option;
                        }

                        public static string {{TO_JSON}}<T>(T obj) {
                            return JsonSerializer.Serialize(obj, {{GET_JSONOPTION}}());
                        }
                        public static T {{PARSE_JSON}}<T>(string json) {
                            return JsonSerializer.Deserialize<T>(json, {{GET_JSONOPTION}}())!;
                        }
                    }
                }
                """;
        }
    }
}
