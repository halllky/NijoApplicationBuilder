using Nijo.Core;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models {
    /// <summary>
    /// 値オブジェクト。
    /// </summary>
    internal class ValueObjectModel : IModel {

        /// <summary>
        /// 当面、文字列系のID型しか使わないので、文字列型のみサポート
        /// </summary>
        internal const string PRIMITIVE_TYPE= "string";
        internal const string TO_JSON_VALUE = "GetString";
        internal const string FROM_JSON_VALUE = "WriteStringValue";

        public void GenerateCode(CodeRenderingContext context, GraphNode<Aggregate> rootAggregate) {
            var className = rootAggregate.Item.PhysicalName;

            // EFCore用型変換処理を登録
            var dbContext = context.UseSummarizedFile<Parts.WebServer.DbContextClass>();
            dbContext.AddOnModelCreatingPropConverter(className, $"{className}.{EFCORE_VALUE_CONVERTER}");

            // HTTP用型変換処理を登録
            var util = context.UseSummarizedFile<Parts.Utility.UtilityClass>();
            util.AddJsonConverter(new() { ConverterClassName = $"{className}.{JSON_VALUE_CONVERTER}", ConverterClassDeclaring = string.Empty });

            // クラス本体を定義
            context.CoreLibrary.UtilDir(dir => {
                dir.Generate(new() {
                    FileName = $"{rootAggregate.Item.PhysicalName.ToFileNameSafe()}.cs",
                    RenderContent = ctx => {
                        var primitiveType = PRIMITIVE_TYPE;
                        var fromJsonValue = TO_JSON_VALUE;
                        var toJsonValue = FROM_JSON_VALUE;

                        string IsEmpty(string value) {
                            return primitiveType == "string"
                                ? $"string.IsNullOrWhiteSpace({value})"
                                : $"{value} == null";
                        }

                        return $$"""
                            using Microsoft.EntityFrameworkCore;
                            using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
                            using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
                            using System.Linq.Expressions;
                            using System.Text.Json;
                            using System.Text.Json.Serialization;

                            namespace {{ctx.Config.RootNamespace}};

                            /// <summary>
                            /// {{rootAggregate.Item.DisplayName}}。
                            /// partial宣言されているため、このクラスに独自の処理を追加する場合は別ファイルで定義してください。
                            /// </summary>
                            public sealed partial class {{className}} {
                                public {{className}}({{primitiveType}} value) {
                                    _value = value;
                                }

                                private readonly {{primitiveType}} _value;

                                public override bool Equals(object? obj) {
                                    if (obj is not {{className}} other) return false;
                                    if (other._value != _value) return false;
                                    return true;
                                }
                                public override int GetHashCode() {
                                    return _value.GetHashCode();
                                }
                                public override {{primitiveType}} ToString() {
                                    return _value;
                                }


                                public static bool operator ==({{className}}? left, {{className}}? right) {
                                    if (left is null ^ right is null) return false;
                                    return ReferenceEquals(left, right) || left!.Equals(right);
                                }
                                public static bool operator !=({{className}}? left, {{className}}? right) {
                                    return !(left == right);
                                }
                                public static explicit operator {{primitiveType}}({{className}} vo) => vo._value;
                                public static explicit operator {{className}}({{primitiveType}} value) => new {{className}}(value);


                                /// <summary>
                                /// Entity Frameword Core 関連の処理で使用される、
                                /// <see cref="{{className}}"/> 型のプロパティと、DBのカラムの型変換。
                                /// </summary>
                                public class {{EFCORE_VALUE_CONVERTER}} : ValueConverter<{{className}}, {{primitiveType}}> {
                                    public {{EFCORE_VALUE_CONVERTER}}() : base(
                                        csValue => csValue._value,
                                        dbValue => new {{className}}(dbValue),
                                        new ConverterMappingHints(size: 255)) { }
                                }
                                /// <summary>
                                /// HTTPリクエスト・レスポンスの処理で使用される、
                                /// <see cref="{{className}}"/> 型のプロパティと、JSONプロパティの型変換。
                                /// </summary>
                                public class {{JSON_VALUE_CONVERTER}} : JsonConverter<{{className}}> {
                                    public override {{className}}? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
                                        var clrValue = reader.{{fromJsonValue}}();
                                        return {{IsEmpty("clrValue")}}
                                            ? null
                                            : new {{className}}(clrValue);
                                    }
                                    public override void Write(Utf8JsonWriter writer, {{className}}? value, JsonSerializerOptions options) {
                                        if (value == null) {
                                            writer.WriteNullValue();
                                        } else {
                                            writer.{{toJsonValue}}(value.ToString());
                                        }
                                    }
                                }
                            }
                            """;
                    },
                });
            });
        }

        /// <summary>
        /// HTTPリクエスト・レスポンスのJSONの型とC#のクラスの型変換を定義するクラス
        /// </summary>
        private const string JSON_VALUE_CONVERTER = "JsonValueConverter";
        /// <summary>
        /// DBのカラムの型とC#のクラスの型変換を定義するクラス
        /// </summary>
        private const string EFCORE_VALUE_CONVERTER = "EFCoreValueConverter";

        public void GenerateCode(CodeRenderingContext context) {
        }

        public IEnumerable<string> ValidateAggregate(GraphNode<Aggregate> rootAggregate) {
            yield break;
        }
    }
}
