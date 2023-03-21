using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using HalApplicationBuilder.DotnetEx;

namespace HalApplicationBuilder.ReArchTo関数型.Runtime {
    /// <summary>
    /// 複合キーをHTTPでやりとりするためにJSON化して取り扱う仕組み
    /// </summary>
    internal class InstanceKey : ValueObject {

        internal static InstanceKey Empty => new InstanceKey("[]", Array.Empty<object>());

        internal static InstanceKey FromObjects(IEnumerable<object?> values) {
            var objArray = values.ToArray();
            var serialized = JsonSerializer.Serialize(objArray);
            return new InstanceKey(serialized, objArray);
        }
        internal static InstanceKey FromSerializedString(string stringValue) {
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

            var deserialized = JsonSerializer.Deserialize<JsonElement[]>(stringValue)!;
            var objArray = deserialized.Select(jsonElement => ToObject(jsonElement)).ToArray();
            return new InstanceKey(stringValue, objArray);
        }

        private InstanceKey(string stringValue, object?[] objectValue) {
            StringValue = stringValue;
            ObjectValue = objectValue;
        }
        internal string StringValue { get; }
        internal object?[] ObjectValue { get; }

        internal object?[] GetFlattenObjectValues() {
            IEnumerable<object?> Flattenned(object? obj) {
                if (obj == null) {
                    yield break;
                } else if (obj is object?[] array) {
                    foreach (var item in array) yield return item;
                } else {
                    yield return obj;
                }
            }
            return ObjectValue
                .SelectMany(obj => Flattenned(obj))
                .ToArray();
        }

        protected override IEnumerable<object?> ValueObjectIdentifiers()
        {
            yield return StringValue;
        }
    }
}
