using Nijo.Core;
using Nijo.Parts.Utility;
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
        private string AppSrvLoadMethod => $"Load{_aggregate.Item.PhysicalName}";
        internal string AppSrvCreateQueryMethod => $"Create{_aggregate.Item.PhysicalName}QuerySource";
        private string AppSrvAfterLoadedMethod => $"OnAfter{_aggregate.Item.PhysicalName}Loaded";

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
            var searchResult = new SearchResult(_aggregate);
            var forDisplay = new DataClassForDisplay(_aggregate);

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

                /// <summary>
                /// {{_aggregate.Item.DisplayName}}の画面表示用データの、インメモリでのカスタマイズ処理。
                /// 任意の項目のC#上での計算、読み取り専用項目の設定、画面に表示するメッセージの設定などを行います。
                /// この処理はSQLに変換されるのではなくインメモリ上で実行されるため、
                /// データベースから読み込んだデータにしかアクセスできない代わりに、
                /// C#のメソッドやインターフェースなどを無制限に利用することができます。
                /// </summary>
                protected virtual {{forDisplay.CsClassName}} {{AppSrvAfterLoadedMethod}}({{forDisplay.CsClassName}} searchResult) {
                    return searchResult;
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
            var pkDict = new Dictionary<AggregateMember.ValueMember, string>();

            string RenderNewDisplayData(DataClassForDisplay forDisplay, string instance, GraphNode<Aggregate> instanceAgg) {
                // 主キー。レンダリング中の集約がChildrenの場合は親のキーをラムダ式の外の変数から参照する必要がある
                var keys = new List<string>();
                foreach (var key in forDisplay.Aggregate.GetKeys().OfType<AggregateMember.ValueMember>()) {
                    if (!pkDict.TryGetValue(key.Declared, out var keyString)) {
                        keyString = $"{instance}.{key.Declared.GetFullPathAsSearchResult(instanceAgg).Join("?.")}";
                        pkDict.Add(key, keyString);
                    }
                    keys.Add(keyString);
                }

                var searchResultMembers = new SearchResult(forDisplay.Aggregate)
                    .GetOwnMembers()
                    .ToArray();
                var depth = forDisplay.Aggregate
                    .EnumerateAncestors()
                    .Count();
                var loopVar = depth == 0 ? "item" : $"item{depth}";

                return $$"""
                    new {{forDisplay.CsClassName}} {
                    {{If(forDisplay.HasInstanceKey, () => $$"""
                        {{DataClassForDisplay.INSTANCE_KEY_CS}} = {{InstanceKey.CS_CLASS_NAME}}.{{InstanceKey.FROM_PK}}({{keys.Join(", ")}}),
                    """)}}
                    {{If(forDisplay.HasLifeCycle, () => $$"""
                        {{DataClassForDisplay.EXISTS_IN_DB_CS}} = true,
                        {{DataClassForDisplay.WILL_BE_CHANGED_CS}} = false,
                        {{DataClassForDisplay.WILL_BE_DELETED_CS}} = false,
                        {{DataClassForDisplay.VERSION_CS}} = {{instance}}.{{SearchResult.VERSION}},
                    """)}}
                        {{DataClassForDisplay.VALUES_CS}} = new {{forDisplay.ValueCsClassName}} {
                    {{forDisplay.GetOwnMembers().SelectTextTemplate(m1 => $$"""
                            {{m1.MemberName}} = {{instance}}.{{searchResultMembers.Single(m2 => m2 == m1).GetFullPathAsSearchResult(instanceAgg).Join("?.")}},
                    """)}}
                        },
                    {{forDisplay.GetChildMembers().SelectTextTemplate(child => child.IsArray ? $$"""
                        {{child.MemberName}} = {{instance}}.{{child.GetFullPath(instanceAgg).Join("?.")}}?.Select({{loopVar}} => {{WithIndent(RenderNewDisplayData(child, loopVar, child.Aggregate), "    ")}}).ToList() ?? [],
                    """ : $$"""
                        {{child.MemberName}} = {{WithIndent(RenderNewDisplayData(child, instance, instanceAgg), "    ")}},
                    """)}}
                    }
                    """;
            }

            var argType = new SearchCondition(_aggregate);
            var returnType = new DataClassForDisplay(_aggregate);
            var searchResult = new SearchResult(_aggregate);
            var sortMemberePaths = argType
                .EnumerateSortMembersRecursively()
                .Select(m => new {
                    AscLiteral = SearchCondition.GetSortLiteral(m, E_AscDesc.ASC),
                    DescLiteral = SearchCondition.GetSortLiteral(m, E_AscDesc.DESC),
                    Path = m.Member.Declared.GetFullPathAsSearchResult().Join("."),
                })
                .ToArray();

            return $$"""
                /// <summary>
                /// {{_aggregate.Item.DisplayName}}の一覧検索を行います。
                /// </summary>
                public virtual IEnumerable<{{returnType.CsClassName}}> {{AppSrvLoadMethod}}({{argType.CsClassName}} searchCondition) {
                    var query = {{AppSrvCreateQueryMethod}}(searchCondition);

                #pragma warning disable CS8602 // null 参照の可能性があるものの逆参照です。
                {{argType.EnumerateFilterMembersRecursively().SelectTextTemplate(m => $$"""
                    // #35 フィルタリング
                """)}}
                #pragma warning restore CS8602 // null 参照の可能性があるものの逆参照です。

                    // ソート
                #pragma warning disable CS8602 // null 参照の可能性があるものの逆参照です。
                    IOrderedQueryable<{{searchResult.CsClassName}}>? sorted = null;
                    foreach (var sortOption in searchCondition.{{SearchCondition.SORT_CS}}) {
                {{sortMemberePaths.SelectTextTemplate((m, i) => $$"""
                        {{(i == 0 ? "if" : "} else if")}} (sortOption == "{{m.AscLiteral}}") {
                            sorted = sorted == null
                                ? query.OrderBy(e => e.{{m.Path}})
                                : sorted.ThenBy(e => e.{{m.Path}});
                        } else if (sortOption == "{{m.DescLiteral}}") {
                            sorted = sorted == null
                                ? query.OrderByDescending(e => e.{{m.Path}})
                                : sorted.ThenByDescending(e => e.{{m.Path}});

                """)}}
                {{If(sortMemberePaths.Length > 0, () => $$"""
                        }
                """)}}
                    }
                #pragma warning restore CS8602 // null 参照の可能性があるものの逆参照です。

                    // ページング
                    if (searchCondition.{{SearchCondition.SKIP_CS}} != null) {
                        query = query.Skip(searchCondition.{{SearchCondition.SKIP_CS}}.Value);
                    }
                    if (searchCondition.{{SearchCondition.TAKE_CS}} != null) {
                        query = query.Take(searchCondition.{{SearchCondition.TAKE_CS}}.Value);
                    }

                    // 検索結果を画面表示用の型に変換
                    var displayDataList = query.AsEnumerable().Select(searchResult => {{WithIndent(RenderNewDisplayData(returnType, "searchResult", _aggregate), "    ")}});

                    // 読み取り専用項目の設定や追加情報などを付す
                    var returnValue = displayDataList.Select({{AppSrvAfterLoadedMethod}});
                    return returnValue;
                }
                """;
        }
    }
}
