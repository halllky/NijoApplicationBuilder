using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using System;
using System.Reflection.Metadata;

namespace Nijo.Models.QueryModelModules {
    /// <summary>
    /// <see cref="SearchCondition.Entry"/> のインスタンスとURLを相互に変換する処理
    /// </summary>
    internal class SearchConditionUrlConversion {
        internal SearchConditionUrlConversion(SearchCondition.Entry searchCondition) {
            _searchCondition = searchCondition;
        }
        private readonly SearchCondition.Entry _searchCondition;

        internal string ParseQueryParameter => $"parseQueryParameterAs{_searchCondition.TsTypeName}";
        internal string ToQueryParameter => $"toQueryParameterOf{_searchCondition.TsTypeName}";

        // 画面初期表示時の検索条件をMultiViewに来る前の画面で指定するためのURLクエリパラメータの名前
        internal const string URL_FILTER = "f";
        internal const string URL_SORT = "s";
        internal const string URL_TAKE = "t";
        internal const string URL_SKIP = "p";

        internal string ConvertUrlToTypeScript(CodeRenderingContext ctx) {
            return $$"""
                /** クエリパラメータを解釈して画面初期表示時検索条件オブジェクトを返します。 */
                export const {{ParseQueryParameter}} = (urlSearch: string): {{_searchCondition.TsTypeName}} => {
                  const searchCondition = {{_searchCondition.TsNewObjectFunction}}()
                  if (!urlSearch) return searchCondition

                  const searchParams = new URLSearchParams(urlSearch)
                  if (searchParams.has('{{URL_FILTER}}'))
                    searchCondition.{{SearchCondition.Entry.FILTER_TS}} = JSON.parse(searchParams.get('{{URL_FILTER}}')!)
                  if (searchParams.has('{{URL_SORT}}'))
                    searchCondition.{{SearchCondition.Entry.SORT_TS}} = JSON.parse(searchParams.get('{{URL_SORT}}')!)
                  if (searchParams.has('{{URL_TAKE}}'))
                    searchCondition.{{SearchCondition.Entry.TAKE_TS}} = Number(searchParams.get('{{URL_TAKE}}'))
                  if (searchParams.has('{{URL_SKIP}}'))
                    searchCondition.{{SearchCondition.Entry.SKIP_TS}} = Number(searchParams.get('{{URL_SKIP}}'))

                  return searchCondition
                }
                """;
        }

        internal string ConvertTypeScriptToUrl(CodeRenderingContext ctx) {
            return $$"""
                /** 画面初期表示時検索条件オブジェクトをクエリパラメータに変換します。結果は第2引数のオブジェクト内に格納されます。 */
                export const {{ToQueryParameter}} = (searchCondition: {{_searchCondition.TsTypeName}}, searchParams: URLSearchParams): void => {
                  searchParams.append('{{URL_FILTER}}', JSON.stringify(searchCondition.{{SearchCondition.Entry.FILTER_TS}}))
                  if (searchCondition.{{SearchCondition.Entry.SORT_TS}} && searchCondition.{{SearchCondition.Entry.SORT_TS}}.length > 0) searchParams.append('{{URL_SORT}}', JSON.stringify(searchCondition.{{SearchCondition.Entry.SORT_TS}}))
                  if (searchCondition.{{SearchCondition.Entry.TAKE_TS}} !== undefined) searchParams.append('{{URL_TAKE}}', searchCondition.{{SearchCondition.Entry.TAKE_TS}}.toString())
                  if (searchCondition.{{SearchCondition.Entry.SKIP_TS}} !== undefined) searchParams.append('{{URL_SKIP}}', searchCondition.{{SearchCondition.Entry.SKIP_TS}}.toString())
                }
                """;
        }
    }
}
