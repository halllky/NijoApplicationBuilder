using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using System;
using System.Reflection.Metadata;

namespace Nijo.Ver1.Models.QueryModelModules {
    /// <summary>
    /// <see cref="SearchCondition"/> のインスタンスとURLを相互に変換する処理
    /// </summary>
    internal class SearchConditionUrlConversion {
        internal SearchConditionUrlConversion(SearchCondition searchCondition) {
            _searchCondition = searchCondition;
        }
        private readonly SearchCondition _searchCondition;

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
                    searchCondition.{{SearchCondition.FILTER_TS}} = JSON.parse(searchParams.get('{{URL_FILTER}}')!)
                  if (searchParams.has('{{URL_SORT}}'))
                    searchCondition.{{SearchCondition.SORT_TS}} = JSON.parse(searchParams.get('{{URL_SORT}}')!)
                  if (searchParams.has('{{URL_TAKE}}'))
                    searchCondition.{{SearchCondition.TAKE_TS}} = Number(searchParams.get('{{URL_TAKE}}'))
                  if (searchParams.has('{{URL_SKIP}}'))
                    searchCondition.{{SearchCondition.SKIP_TS}} = Number(searchParams.get('{{URL_SKIP}}'))

                  return searchCondition
                }
                """;
        }

        internal string ConvertTypeScriptToUrl(CodeRenderingContext ctx) {
            return $$"""
                /** 画面初期表示時検索条件オブジェクトをクエリパラメータに変換します。結果は第2引数のオブジェクト内に格納されます。 */
                export const {{ToQueryParameter}} = (searchCondition: {{_searchCondition.TsTypeName}}, URLSearchParams searchParams): void => {
                  searchParams.append('{{URL_FILTER}}', JSON.stringify(searchCondition.{{SearchCondition.FILTER_TS}}))
                  if (searchCondition.{{SearchCondition.SORT_TS}} && searchCondition.{{SearchCondition.SORT_TS}}.length > 0) searchParams.append('{{URL_SORT}}', JSON.stringify(searchCondition.{{SearchCondition.SORT_TS}}))
                  if (searchCondition.{{SearchCondition.TAKE_TS}} !== undefined) searchParams.append('{{URL_TAKE}}', searchCondition.{{SearchCondition.TAKE_TS}}.toString())
                  if (searchCondition.{{SearchCondition.SKIP_TS}} !== undefined) searchParams.append('{{URL_SKIP}}', searchCondition.{{SearchCondition.SKIP_TS}}.toString())
                }
                """;
        }
    }
}
