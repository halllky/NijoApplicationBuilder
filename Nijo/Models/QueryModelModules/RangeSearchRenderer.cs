using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Util.DotnetEx;
using System;
using System.Linq;

namespace Nijo.Models.QueryModelModules {
    /// <summary>
    /// 範囲検索のレンダリングを共通化するユーティリティクラス
    /// </summary>
    public static class RangeSearchRenderer {
        /// <summary>
        /// 範囲検索のレンダリングを行う
        /// </summary>
        /// <param name="ctx">フィルタリングのレンダリングコンテキスト</param>
        /// <returns>レンダリングされたC#コード</returns>
        public static string RenderRangeSearchFiltering(FilterStatementRenderingContext ctx) {
            var query = ctx.Query.Root.Name;
            var cast = ctx.SearchCondition.Metadata.Type.RenderCastToPrimitiveType();

            var fullpathNullable = ctx.SearchCondition.GetJoinedPathFromInstance(E_CsTs.CSharp, "?.");
            var fullpathNotNull = ctx.SearchCondition.GetJoinedPathFromInstance(E_CsTs.CSharp, ".");

            var queryFullPath = ctx.Query.GetFlattenArrayPath(E_CsTs.CSharp, out var isMany);
            var queryOwnerFullPath = queryFullPath.SkipLast(1);

            return $$"""
                if ({{fullpathNullable}}?.From != null && {{fullpathNullable}}?.To != null) {
                    var from = {{cast}}{{fullpathNotNull}}!.From;
                    var to = {{cast}}{{fullpathNotNull}}!.To;
                {{If(isMany, () => $$"""
                    {{query}} = {{query}}.Where(x => x.{{queryOwnerFullPath.Join(".")}}.Any(y => y.{{ctx.Query.Metadata.GetPropertyName(E_CsTs.CSharp)}} >= from && y.{{ctx.Query.Metadata.GetPropertyName(E_CsTs.CSharp)}} <= to));
                """).Else(() => $$"""
                    {{query}} = {{query}}.Where(x => x.{{queryFullPath.Join(".")}} >= from && x.{{queryFullPath.Join(".")}} <= to);
                """)}}
                } else if ({{fullpathNullable}}?.From != null) {
                    var from = {{cast}}{{fullpathNotNull}}!.From;
                {{If(isMany, () => $$"""
                    {{query}} = {{query}}.Where(x => x.{{queryOwnerFullPath.Join(".")}}.Any(y => y.{{ctx.Query.Metadata.GetPropertyName(E_CsTs.CSharp)}} >= from));
                """).Else(() => $$"""
                    {{query}} = {{query}}.Where(x => x.{{queryFullPath.Join(".")}} >= from);
                """)}}
                } else if ({{fullpathNullable}}?.To != null) {
                    var to = {{cast}}{{fullpathNotNull}}!.To;
                {{If(isMany, () => $$"""
                    {{query}} = {{query}}.Where(x => x.{{queryOwnerFullPath.Join(".")}}.Any(y => y.{{ctx.Query.Metadata.GetPropertyName(E_CsTs.CSharp)}} <= to));
                """).Else(() => $$"""
                    {{query}} = {{query}}.Where(x => x.{{queryFullPath.Join(".")}} <= to);
                """)}}
                }
                """;
        }
    }
}
