using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.Util {
    partial class InstanceKey : TemplateBase {
        internal InstanceKey(CodeRenderingContext ctx) {
            _ctx = ctx;
            _namespace = ctx.Config.RootNamespace;
        }
        private readonly CodeRenderingContext _ctx;

        private readonly string _namespace;

        public override string FileName => "InstanceKey.cs";

        internal const string CLASS_NAME = "InstanceKey";
        internal const string CREATE = "Create";
        internal const string PARSE = "Parse";
        internal const string OBJECT_ARRAY = "ObjectArray";

        protected override string Template() {
            return $$"""
                namespace {{_namespace}} {
                    using System;
                    using System.Collections;
                    using System.Collections.Generic;
                    using System.Linq;
                    using System.Text.Json;

                    public class {{CLASS_NAME}} {

                        public static {{CLASS_NAME}} Empty(int keyCount = 0) => {{CREATE}}(new object?[keyCount]);

                        public static {{CLASS_NAME}} {{CREATE}}(IEnumerable<object?> values) {
                            var objArray = values.ToArray();
                            var json = JsonSerializer.Serialize(objArray, new JsonSerializerOptions {
                                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
                            });
                            return new {{CLASS_NAME}}(json, objArray);
                        }
                        public static {{CLASS_NAME}} {{PARSE}}(string str) {
                            object? ToObject(JsonElement jsonElement) {
                                switch (jsonElement.ValueKind) {
                                    case JsonValueKind.Array:
                                        return jsonElement
                                            .EnumerateArray()
                                            .Select(x => ToObject(x))
                                            .ToArray();

                                    case JsonValueKind.True:
                                    case JsonValueKind.False:
                                        return jsonElement.GetBoolean();

                                    case JsonValueKind.Number:
                                        return jsonElement.GetDouble();

                                    case JsonValueKind.String:
                                        return jsonElement.GetString();

                                    case JsonValueKind.Null:
                                    case JsonValueKind.Undefined:
                                        return null;

                                    case JsonValueKind.Object:
                                    default:
                                        throw new InvalidOperationException();
                                }
                            }

                            if (string.IsNullOrWhiteSpace(str)) {
                                return Empty();
                            }
                            var deserialized = JsonSerializer.Deserialize<JsonElement[]>(str)!;
                            var objArray = deserialized.Select(jsonElement => ToObject(jsonElement)).ToArray();
                            return new {{CLASS_NAME}}(str, objArray);
                        }

                        private {{CLASS_NAME}}(string json, object?[] values) {
                            {{OBJECT_ARRAY}} = values;
                            _json = json;
                        }
                        internal object?[] {{OBJECT_ARRAY}} { get; }
                        private readonly string _json;

                        public override string ToString() {
                            return _json;
                        }
                    }
                }
                """;
        }
    }
}
