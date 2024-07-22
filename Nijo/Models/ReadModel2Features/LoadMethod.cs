using Nijo.Core;
using Nijo.Parts.WebServer;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.ReadModel2Features {
    /// <summary>
    /// 検索処理
    /// </summary>
    internal class LoadMethod {
        internal LoadMethod(GraphNode<Aggregate> agg) {
            _aggregate = agg;
        }
        private readonly GraphNode<Aggregate> _aggregate;

        internal string ReactHookName => $"use{_aggregate.Item.PhysicalName}Loader";
        internal const string CURRENT_PAGE_ITEMS = "currentPageItems";
        internal const string NOW_LOADING = "nowLoading";
        internal const string RELOAD = "reload";

        private const string CONTROLLER_ACTION = "load";
        private string AppSrvCreateQueryMethod => $"Create{_aggregate.Item.PhysicalName}QuerySource";
        private string AppSrvLoadMethod => $"Load{_aggregate.Item.PhysicalName}";

        /// <summary>
        /// クライアント側から検索処理を呼び出すReact hook をレンダリングします。
        /// </summary>
        internal string RenderReactHook(CodeRenderingContext context) {
            var controller = new Controller(_aggregate.Item);
            var searchCondition = new SearchCondition(_aggregate);
            var searchResult = new DataClassForDisplay(_aggregate);

            return $$"""
                export const {{ReactHookName}} = (searchCondition?: Types.{{searchCondition.TsTypeName}}) => {
                  const [{{CURRENT_PAGE_ITEMS}}, setCurrentPageItems] = React.useState<{{searchResult.TsTypeName}}[]>(() => [])
                  const [{{NOW_LOADING}}, setNowLoading] = React.useState(false)
                  const [, dispatchMsg] = Util.useMsgContext()
                  const { post } = Util.useHttpRequest()

                  const {{RELOAD}} = React.useCallback(async () => {
                    setNowLoading(true)
                    try {
                      const res = await post<{{searchResult.TsTypeName}}[]>(`{{controller.SubDomain}}/{{CONTROLLER_ACTION}}`, searchCondition)
                      if (!res.ok) {
                        dispatchMsg(msg => msg.error('データ読み込みに失敗しました。'))
                        return
                      }
                      setCurrentPageItems(res.data)
                    } finally {
                      setNowLoading(false)
                    }
                  }, [searchCondition, post, dispatchMsg])

                  React.useEffect(() => {
                    if (!{{NOW_LOADING}}) {{RELOAD}}()
                  }, [{{RELOAD}}])

                  return {
                    {{CURRENT_PAGE_ITEMS}},
                    {{NOW_LOADING}},
                    {{RELOAD}},
                  }
                }
                """;
        }

        /// <summary>
        /// 検索処理のASP.NET Core Controller アクションをレンダリングします。
        /// </summary>
        internal string RenderControllerAction(CodeRenderingContext context) {
            var searchCondition = new SearchCondition(_aggregate);

            return $$"""
                [HttpPost("{{CONTROLLER_ACTION}}")]
                public virtual IActionResult Load{{_aggregate.Item.PhysicalName}}([FromBody] {{searchCondition.CsClassName}} searchCondition) {
                    var searchResult = _applicationService.{{AppSrvLoadMethod}}(searchCondition);
                    return this.JsonContent(searchResult.ToArray());
                }
                """;
        }

        /// <summary>
        /// 検索処理の抽象部分をレンダリングします。
        /// </summary>
        internal string RenderAppSrvAbstractMethod(CodeRenderingContext context) {
            var searchCondition = new SearchCondition(_aggregate);
            var searchResult = new DataClassForDisplay(_aggregate);

            return $$"""
                /// <summary>
                /// {{_aggregate.Item.DisplayName}}の検索クエリのソース定義。
                /// <para>
                /// このメソッドでやること
                /// - クエリのソース定義（SQLで言うとFROM句とSELECT句に相当する部分）
                /// - カスタム検索条件による絞り込み
                /// - その他任意の絞り込み（例えばログイン中のユーザーのIDを参照して検索結果に含まれる他者の個人情報を除外するなど）
                /// </para>
                /// <para>
                /// このメソッドで書かなくてよいこと
                /// - 自動生成される検索条件による絞り込み
                /// - ソート
                /// - ページング
                /// </para>
                /// </summary>
                protected virtual IQueryable<{{searchResult.CsClassName}}> {{AppSrvCreateQueryMethod}}({{searchCondition.CsClassName}} searchCondition) {
                    // クエリのソース定義部分は自動生成されません。
                    // このメソッドをオーバーライドしてソース定義処理を記述してください。
                    return Enumerable.Empty<{{searchResult.CsClassName}}>().AsQueryable();
                }
                """;
        }

        /// <summary>
        /// 検索処理の基底処理をレンダリングします。
        /// パラメータの検索条件によるフィルタリング、
        /// パラメータの並び順指定順によるソート、
        /// パラメータのskip, take によるページングを行います。
        /// </summary>
        internal string RenderAppSrvBaseMethod(CodeRenderingContext context) {
            var searchCondition = new SearchCondition(_aggregate);
            var searchResult = new DataClassForDisplay(_aggregate);

            return $$"""
                /// <summary>
                /// {{_aggregate.Item.DisplayName}}の一覧検索を行います。
                /// </summary>
                public virtual IEnumerable<{{searchResult.CsClassName}}> {{AppSrvLoadMethod}}({{searchCondition.CsClassName}} searchCondition) {
                    var query = {{AppSrvCreateQueryMethod}}(searchCondition);

                    // #35 フィルタリング
                    // #35 ソート
                    // #35 ページング

                    return query.AsEnumerable();
                }
                """;
        }
    }
}
