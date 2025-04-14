using Nijo.CodeGenerating;
using Nijo.CodeGenerating.Helpers;
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

            var pathFromSearchCondition = ctx.SearchCondition.GetPathFromInstance().Select(p => p.Metadata.PropertyName).ToArray();
            var fullpathNullable = $"{ctx.SearchCondition.Root.Name}.{pathFromSearchCondition.Join("?.")}";
            var fullpathNotNull = $"{ctx.SearchCondition.Root.Name}.{pathFromSearchCondition.Join(".")}";

            var whereFullpath = ctx.Query.GetFlattenArrayPath(E_CsTs.CSharp, out var isMany);

            return $$"""
                if ({{fullpathNullable}}?.From != null && {{fullpathNullable}}?.To != null) {
                    var from = {{cast}}{{fullpathNotNull}}.From;
                    var to = {{cast}}{{fullpathNotNull}}.To;
                {{If(isMany, () => $$"""
                    {{query}} = {{query}}.Where(x => x.{{whereFullpath.Join(".")}}.Any(y => y.{{ctx.Query.Metadata.PropertyName}} >= from && y.{{ctx.Query.Metadata.PropertyName}} <= to));
                """).Else(() => $$"""
                    {{query}} = {{query}}.Where(x => x.{{whereFullpath.Join(".")}} >= from && x.{{whereFullpath.Join(".")}} <= to);
                """)}}
                } else if ({{fullpathNullable}}?.From != null) {
                    var from = {{cast}}{{fullpathNotNull}}.From;
                {{If(isMany, () => $$"""
                    {{query}} = {{query}}.Where(x => x.{{whereFullpath.Join(".")}}.Any(y => y.{{ctx.Query.Metadata.PropertyName}} >= from));
                """).Else(() => $$"""
                    {{query}} = {{query}}.Where(x => x.{{whereFullpath.Join(".")}} >= from);
                """)}}
                } else if ({{fullpathNullable}}?.To != null) {
                    var to = {{cast}}{{fullpathNotNull}}.To;
                {{If(isMany, () => $$"""
                    {{query}} = {{query}}.Where(x => x.{{whereFullpath.Join(".")}}.Any(y => y.{{ctx.Query.Metadata.PropertyName}} <= to));
                """).Else(() => $$"""
                    {{query}} = {{query}}.Where(x => x.{{whereFullpath.Join(".")}} <= to);
                """)}}
                }
                """;
        }
    }
}
