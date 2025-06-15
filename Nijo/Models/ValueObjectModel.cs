using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Parts.Common;
using Nijo.SchemaParsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.Models {
    /// <summary>
    /// 値オブジェクト。
    /// 識別子や複合値を表すためのオブジェクト。
    /// 主に集約のキーとして使用される。
    /// </summary>
    internal class ValueObjectModel : IModel {
        internal const string SCHEMA_NAME = "value-object";
        internal const string JSON_CONVERTER_NAME = "JsonConverter";

        public string SchemaName => SCHEMA_NAME;

        public void Validate(XElement rootAggregateElement, SchemaParseContext context, Action<XElement, string> addError) {
            // 特に検証ロジックなし
        }

        public void GenerateCode(CodeRenderingContext ctx, RootAggregate rootAggregate) {
            // 値オブジェクトクラスの生成
            ctx.CoreLibrary(dir => {
                dir.Generate(RenderCSharp(rootAggregate, ctx));
            });

            // JSONシリアライズの登録
            ctx.Use<Parts.CSharp.JsonUtil>().AddValueObject(rootAggregate);

            // TypeScript定義の生成
            ctx.ReactProject(dir => {
                dir.Directory("util", utilDir => {
                    utilDir.Generate(RenderTypeScript(rootAggregate));
                });
            });
        }

        private static SourceFile RenderCSharp(RootAggregate rootAggregate, CodeRenderingContext ctx) {
            var aggregateName = rootAggregate.PhysicalName;

            return new SourceFile {
                FileName = $"{rootAggregate.PhysicalName}.cs",
                Contents = $$"""
                    using System.Diagnostics.CodeAnalysis;
                    using System.Text.Json;

                    namespace {{ctx.Config.RootNamespace}};

                    /// <summary>
                    /// {{rootAggregate.DisplayName}}。
                    /// 誤って{{rootAggregate.DisplayName}}ではない文字列型の項目に代入してしまったときにエラーで気付けるよう、値オブジェクトで定義している。
                    /// stringと相互に変換するときは明示的に(string)や({{aggregateName}})でキャストする。
                    /// </summary>
                    public partial class {{aggregateName}} : IEquatable<{{aggregateName}}> {
                        private readonly string _value;

                        public {{aggregateName}}(string value) {
                            _value = value;
                        }

                        public override bool Equals(object? obj) {
                            return obj is {{aggregateName}} other && Equals(other);
                        }

                        public bool Equals({{aggregateName}}? other) {
                            if (other is null) return false;
                            return _value == other._value;
                        }

                        public override int GetHashCode() {
                            return _value?.GetHashCode() ?? 0;
                        }

                        public static bool operator ==({{aggregateName}}? left, {{aggregateName}}? right) {
                            return EqualityComparer<{{aggregateName}}>.Default.Equals(left, right);
                        }
                        public static bool operator !=({{aggregateName}}? left, {{aggregateName}}? right) {
                            return !(left == right);
                        }

                        // {{aggregateName}}型の変数を (string) でキャストできるようにする
                        [return: NotNullIfNotNull(nameof(value))]
                        public static explicit operator string?({{aggregateName}}? value) {
                            return value?._value;
                        }
                        // string型の変数を ({{aggregateName}}) でキャストできるようにする
                        [return: NotNullIfNotNull(nameof(value))]
                        public static explicit operator {{aggregateName}}?(string? value) {
                            return value == null ? null : new {{aggregateName}}(value);
                        }

                        public override string ToString() {
                            return _value;
                        }

                        #region JSONシリアライズ
                        public class {{JSON_CONVERTER_NAME}} : System.Text.Json.Serialization.JsonConverter<{{aggregateName}}?> {
                            public override {{aggregateName}}? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
                                var strValue = reader.GetString();
                                if (strValue == null) {
                                    return null;
                                } else {
                                    return new {{aggregateName}}(strValue);
                                }
                            }

                            public override void Write(Utf8JsonWriter writer, {{aggregateName}}? value, JsonSerializerOptions options) {
                                writer.WriteStringValue(value?._value);
                            }
                        }
                        #endregion JSONシリアライズ
                    }
                    """,
            };
        }

        private static SourceFile RenderTypeScript(RootAggregate rootAggregate) {
            var aggregateName = rootAggregate.PhysicalName;

            return new SourceFile {
                FileName = $"{rootAggregate.PhysicalName}.ts",
                Contents = $$"""
                    /**
                     * {{rootAggregate.DisplayName}}。
                     * 誤って{{rootAggregate.DisplayName}}ではない文字列型の項目に代入してしまったときにエラーで気付けるよう、公称型で定義している。
                     * stringと相互に変換するときは明示的に as で変換する。
                     */
                    export type {{aggregateName}} = string & { readonly __brand: unique symbol }
                    """,
            };
        }

        public void GenerateCode(CodeRenderingContext ctx) {
            // 特になし
        }
    }
}
