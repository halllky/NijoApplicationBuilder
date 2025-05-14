using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Parts.Common;
using Nijo.SchemaParsing;
using Nijo.Models.StaticEnumModelModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.Models {
    /// <summary>
    /// 静的区分値。値がソースコード上にハードコードされる区分値。
    /// C#はenumとして、TypeScriptはリテラル型として表れる。
    /// </summary>
    internal class StaticEnumModel : IModel {
        public string SchemaName => EnumDefParser.SCHEMA_NAME;

        public void Validate(XElement rootAggregateElement, SchemaParseContext context, Action<XElement, string> addError) {
            // キー（key属性）のバリデーション
            var enumValues = rootAggregateElement.Elements().ToList();
            var keyValues = new HashSet<int>();

            foreach (var valueElement in enumValues) {
                // (1) キー（key="1" など）が定義されていない値が含まれる場合はエラー
                var keyAttr = valueElement.Attribute(StaticEnumValueDef.ATTR_KEY);
                if (keyAttr == null) {
                    addError(valueElement, $"key属性が定義されていません。整数値を指定してください。");
                    continue;
                }

                // (2) キーが整数でないものはエラー
                if (!int.TryParse(keyAttr.Value, out int keyValue)) {
                    addError(valueElement, $"key属性の値「{keyAttr.Value}」は整数値である必要があります。");
                    continue;
                }

                // (3) キーに重複があるものはエラー
                if (!keyValues.Add(keyValue)) {
                    addError(valueElement, $"key値「{keyValue}」は既に他の値で使用されています。重複しない値を指定してください。");
                }
            }
        }

        public void GenerateCode(CodeRenderingContext ctx, RootAggregate rootAggregate) {
            var enumFile = ctx.Use<EnumFile>();
            var parser = new EnumDefParser(((ISchemaPathNode)rootAggregate).XElement, ctx.SchemaParser);


            // データ型: enum, リテラル型定義
            var staticEnum = new StaticEnumDef(parser, rootAggregate);
            enumFile.Register(staticEnum);
            enumFile.AddCSharpSource(staticEnum.RenderCSharp());
            enumFile.AddTypeScriptSource(staticEnum.RenderTypeScript());

            // データ型: 検索条件オブジェクト
            var searchCondition = new StaticEnumSearchCondition(staticEnum, parser);
            enumFile.AddCSharpSource(searchCondition.RenderCSharp());
            enumFile.AddTypeScriptSource(searchCondition.RenderTypeScript());
        }

        public void GenerateCode(CodeRenderingContext ctx) {
            // 特になし
        }
    }
}
