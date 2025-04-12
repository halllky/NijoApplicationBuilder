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
        public string SchemaName => "value-object";

        public string HelpTextMarkdown => $$"""
            # ValueObject 値オブジェクト
            識別子や複合値を表すためのオブジェクト。
            主に集約のキーとして使用される。
            
            ValueObjectの定義からは以下のモジュールが生成される。
            
            ## C#の値オブジェクトクラス
            値オブジェクトを表すC#のクラスが生成される。
            このクラスは値の等価性比較を行うためのメソッドを持つ。
            
            ## TypeScriptの型定義
            TypeScript側での型定義が生成される。
            """;

        public void Validate(XElement rootAggregateElement, SchemaParseContext context, Action<XElement, string> addError) {
            // 特に検証ロジックなし
        }

        public void GenerateCode(CodeRenderingContext ctx, RootAggregate rootAggregate) {
            var valueObjectFile = new SourceFileByAggregate(rootAggregate);

            // 値オブジェクトクラスの生成
            var csharpCode = RenderCSharp(rootAggregate, ctx);
            valueObjectFile.AddCSharpClass(csharpCode);

            // TypeScript定義の生成
            var typeScriptCode = RenderTypeScript(rootAggregate);
            valueObjectFile.AddTypeScriptTypeDef(typeScriptCode);

            valueObjectFile.ExecuteRendering(ctx);
        }

        private string RenderCSharp(RootAggregate rootAggregate, CodeRenderingContext ctx) {
            var aggregateName = rootAggregate.PhysicalName;

            return $$"""
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
                    public static explicit operator string({{aggregateName}} value) {
                        return value._value;
                    }
                    // string型の変数を ({{aggregateName}}) でキャストできるようにする
                    public static explicit operator {{aggregateName}}(string value) {
                        return new {{aggregateName}}(value);
                    }

                    public override string ToString() {
                        return _value;
                    }
                }
                """;
        }

        private string RenderTypeScript(RootAggregate rootAggregate) {
            var aggregateName = rootAggregate.PhysicalName;

            return $$"""
                /**
                 * {{rootAggregate.DisplayName}}。
                 * 誤って{{rootAggregate.DisplayName}}ではない文字列型の項目に代入してしまったときにエラーで気付けるよう、公称型で定義している。
                 * stringと相互に変換するときは明示的に as で変換する。
                 */
                export type {{aggregateName}} = string & { readonly __brand: unique symbol }
                """;
        }

        public void GenerateCode(CodeRenderingContext ctx) {
            // 特になし
        }
    }
}
