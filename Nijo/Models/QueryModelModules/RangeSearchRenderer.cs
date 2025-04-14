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
        /// <param name="castToPrimitiveType">プリミティブ型へのキャスト処理（必要な場合のみ）</param>
        /// <returns>レンダリングされたC#コード</returns>
        public static string RenderRangeSearchFiltering(FilterStatementRenderingContext ctx, string castToPrimitiveType = "") {
            var fullpath = ctx.Member.GetPathFromEntry().ToArray();
            var pathFromSearchCondition = fullpath.AsSearchConditionFilter(E_CsTs.CSharp).ToArray();
            var whereFullpath = fullpath.AsSearchResult().ToArray();
            var fullpathNullable = $"{ctx.SearchCondition}.{pathFromSearchCondition.Join("?.")}";
            var fullpathNotNull = $"{ctx.SearchCondition}.{pathFromSearchCondition.Join(".")}";
            var isArray = fullpath.Any(node => node is ChildrenAggreagte);

            return $$"""
                if ({{fullpathNullable}}?.From != null && {{fullpathNullable}}?.To != null) {
                    {{GetCastStatements(castToPrimitiveType, fullpathNotNull)}}
                {{If(isArray, () => $$"""
                    {{ctx.Query}} = {{ctx.Query}}.Where(x => x.{{whereFullpath.SkipLast(1).Join(".")}}.Any(y => y.{{ctx.Member.PhysicalName}} >= from && y.{{ctx.Member.PhysicalName}} <= to));
                """).Else(() => $$"""
                    {{ctx.Query}} = {{ctx.Query}}.Where(x => x.{{whereFullpath.Join(".")}} >= from && x.{{whereFullpath.Join(".")}} <= to);
                """)}}
                } else if ({{fullpathNullable}}?.From != null) {
                    {{GetFromCastStatement(castToPrimitiveType, fullpathNotNull)}}
                {{If(isArray, () => $$"""
                    {{ctx.Query}} = {{ctx.Query}}.Where(x => x.{{whereFullpath.SkipLast(1).Join(".")}}.Any(y => y.{{ctx.Member.PhysicalName}} >= from));
                """).Else(() => $$"""
                    {{ctx.Query}} = {{ctx.Query}}.Where(x => x.{{whereFullpath.Join(".")}} >= from);
                """)}}
                } else if ({{fullpathNullable}}?.To != null) {
                    {{GetToCastStatement(castToPrimitiveType, fullpathNotNull)}}
                {{If(isArray, () => $$"""
                    {{ctx.Query}} = {{ctx.Query}}.Where(x => x.{{whereFullpath.SkipLast(1).Join(".")}}.Any(y => y.{{ctx.Member.PhysicalName}} <= to));
                """).Else(() => $$"""
                    {{ctx.Query}} = {{ctx.Query}}.Where(x => x.{{whereFullpath.Join(".")}} <= to);
                """)}}
                }
                """;
        }

        private static string GetCastStatements(string castToPrimitiveType, string fullpathNotNull) {
            if (string.IsNullOrEmpty(castToPrimitiveType)) {
                return $$"""
                    var from = {{fullpathNotNull}}.From;
                    var to = {{fullpathNotNull}}.To;
                """;
            } else {
                return $$"""
                    var from = {{castToPrimitiveType}}{{fullpathNotNull}}.From;
                    var to = {{castToPrimitiveType}}{{fullpathNotNull}}.To;
                """;
            }
        }

        private static string GetFromCastStatement(string castToPrimitiveType, string fullpathNotNull) {
            if (string.IsNullOrEmpty(castToPrimitiveType)) {
                return $"var from = {fullpathNotNull}.From;";
            } else {
                return $"var from = {castToPrimitiveType}{fullpathNotNull}.From;";
            }
        }

        private static string GetToCastStatement(string castToPrimitiveType, string fullpathNotNull) {
            if (string.IsNullOrEmpty(castToPrimitiveType)) {
                return $"var to = {fullpathNotNull}.To;";
            } else {
                return $"var to = {castToPrimitiveType}{fullpathNotNull}.To;";
            }
        }
    }
}
