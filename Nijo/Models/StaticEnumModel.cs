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

        public string HelpText => $$"""
            静的区分値。値がソースコード上にハードコードされる区分値。
            C#はenumとして、TypeScriptはリテラル型として表れる。
            """;

        public void Validate(XElement rootAggregateElement, SchemaParseContext context, Action<XElement, string> addError) {
        }

        public void GenerateCode(CodeRenderingContext ctx, RootAggregate rootAggregate) {
            var enumFile = ctx.Use<EnumFile>();
            var parser = new EnumDefParser(((ISchemaPathNode)rootAggregate).XElement, ctx.SchemaParser);

            // データ型: enum, リテラル型定義
            var staticEnum = new StaticEnumDef(parser, rootAggregate);
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
