using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts.Utility {
    /// <summary>
    /// <para>
    /// 画面に表示されるインスタンスを一意に表す文字列。
    /// 新規作成の場合はUUID。閲覧・更新・削除のときは主キーの値の配列のJSONになる。
    /// </para>
    /// <para>
    /// 特に新規作成データの場合は画面上で主キー項目を編集可能であり、
    /// 別途何らかの識別子を設けないと同一性を判定する方法が無いため、この項目が必要になる。
    /// </para>
    /// </summary>
    internal class InstanceKey {

        internal const string CS_CLASS_NAME = "InstanceKey";

        internal const string EMPTY = "Empty";
        /// <summary>新しいUUIDを発番し <see cref="InstanceKey"/> を作成します。</summary>
        internal const string FROM_UUID = "FromUuid";
        /// <summary>DBに保存された主キーから <see cref="InstanceKey"/> を作成します。</summary>
        internal const string FROM_PK = "FromPrimaryKey";
        /// <summary>クライアント側から送られてきた文字列を復元し <see cref="InstanceKey"/> のオブジェクトを作成します。</summary>
        internal const string RESTORE = "Restore";

        internal static SourceFile RenderCSharp() => new SourceFile {
            FileName = "InstanceKey.cs",
            RenderContent = context => {
                return $$"""
                    namespace {{context.Config.RootNamespace}};

                    /// <summary>
                    /// <para>
                    /// 画面に表示されるインスタンスを一意に表す文字列。
                    /// 新規作成の場合はUUID。閲覧・更新・削除のときは主キーの値の配列のJSONになる。
                    /// </para>
                    /// <para>
                    /// 特に新規作成データの場合は画面上で主キー項目を編集可能であり、
                    /// 別途何らかの識別子を設けないと同一性を判定する方法が無いため、この項目が必要になる。
                    /// </para>
                    /// </summary>
                    public class {{CS_CLASS_NAME}} {

                        /// <summary>
                        /// 空の <see cref="{{CS_CLASS_NAME}}"/> を作成します。
                        /// </summary>
                        public static {{CS_CLASS_NAME}} {{EMPTY}}() {
                            return new {{CS_CLASS_NAME}}(null, Array.Empty<object?>());
                        }
                        /// <summary>
                        /// 新しいUUIDを発番し <see cref="{{CS_CLASS_NAME}}"/> を作成します。
                        /// </summary>
                        public static {{CS_CLASS_NAME}} {{FROM_UUID}}() {
                            var guid = Guid.NewGuid().ToString();
                            return new {{CS_CLASS_NAME}}(guid, null);
                        }
                        /// <summary>
                        /// DBに保存された主キーから <see cref="{{CS_CLASS_NAME}}"/> を作成します。
                        /// </summary>
                        public static {{CS_CLASS_NAME}} {{FROM_PK}}(params object?[] primaryKeys) {
                            return new {{CS_CLASS_NAME}}(null, primaryKeys);
                        }
                        /// <summary>
                        /// クライアント側から送られてきた文字列を復元し <see cref="{{CS_CLASS_NAME}}"/> のオブジェクトを作成します。
                        /// </summary>
                        public static {{CS_CLASS_NAME}} {{RESTORE}}(string fromClient) {
                            if (fromClient.StartsWith("[")) {
                                var objectArray = {{UtilityClass.CLASSNAME}}.{{UtilityClass.PARSE_JSON_AS_OBJARR}}(fromClient);
                                return new {{CS_CLASS_NAME}}(null, objectArray);
                            } else {
                                return new {{CS_CLASS_NAME}}(fromClient, null);
                            }
                        }

                        private {{CS_CLASS_NAME}}(string? uuid, object?[]? primaryKeys) {
                            _uuid = uuid;
                            _primaryKeys = primaryKeys;
                        }

                        /// <summary>新規作成されてまだDBに登録されていないインスタンスの場合はこの項目にUUIDが入る</summary>
                        private readonly string? _uuid;
                        /// <summary>既にDBに登録済みのインスタンスの場合はこの項目に主キーの値が入る</summary>
                        private readonly object?[]? _primaryKeys;

                        public override string ToString() {
                            if (_uuid != null) {
                                return _uuid.ToJson();
                            } else if (_primaryKeys != null) {
                                return _primaryKeys.ToJson();
                            } else {
                                throw new NotImplementedException(); // ありえないパターン
                            }
                        }
                    }
                    """;
            },
        };

        internal static UtilityClass.CustomJsonConverter GetCustomJsonConverter() => new UtilityClass.CustomJsonConverter {
            ConverterClassName = "InstanceKeyJsonConverter",
            ConverterClassDeclaring = $$"""
                class InstanceKeyJsonConverter : JsonConverter<{{CS_CLASS_NAME}}> {
                    public override {{CS_CLASS_NAME}}? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
                        var stringValue = reader.GetString();
                        return string.IsNullOrWhiteSpace(stringValue)
                            ? null
                            : {{CS_CLASS_NAME}}.{{RESTORE}}(stringValue);
                    }
                    public override void Write(Utf8JsonWriter writer, {{CS_CLASS_NAME}}? value, JsonSerializerOptions options) {
                        if (value == null) {
                            writer.WriteNullValue();
                        } else {
                            writer.WriteStringValue(value.ToString());
                        }
                    }
                }

                """,
        };
    }
}
