using Nijo.Util.DotnetEx;
using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using Nijo.Ver1.Parts.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nijo.Ver1.Models.QueryModelModules {

    /// <summary>
    /// 検索条件クラス。
    /// 通常の一覧検索と参照先検索とで共通
    /// </summary>
    internal class SearchCondition {
        internal SearchCondition(RootAggregate rootAggregate) {
            _rootAggregate = rootAggregate;
            FilterRoot = new Filter(_rootAggregate);
        }
        private readonly RootAggregate _rootAggregate;

        internal virtual string CsClassName => $"{_rootAggregate.PhysicalName}SearchCondition";
        internal virtual string TsTypeName => $"{_rootAggregate.PhysicalName}SearchCondition";


        internal const string FILTER_CS = "Filter";
        internal const string FILTER_TS = "filter";
        internal const string SORT_CS = "Sort";
        internal const string SORT_TS = "sort";
        internal const string SKIP_CS = "Skip";
        internal const string SKIP_TS = "skip";
        internal const string TAKE_CS = "Take";
        internal const string TAKE_TS = "take";

        internal string RenderCSharp(CodeRenderingContext ctx) {
            return $$"""
                #region 検索条件クラス
                /// <summary>
                /// {{_rootAggregate.DisplayName}}の一覧検索条件
                /// </summary>
                public partial class {{CsClassName}} {
                    /// <summary>絞り込み条件</summary>
                    [JsonPropertyName("{{FILTER_TS}}")]
                    public {{FilterRoot.CsClassName}} {{FILTER_CS}} { get; set; } = new();
                    /// <summary>並び順</summary>
                    [JsonPropertyName("{{SORT_TS}}")]
                    public List<string> {{SORT_CS}} { get; set; } = [];
                    /// <summary>ページングに使用。検索結果のうち先頭から何件スキップするか。</summary>
                    [JsonPropertyName("{{SKIP_TS}}")]
                    public int? {{SKIP_CS}} { get; set; }
                    /// <summary>ページングに使用。検索結果のうち先頭から何件抽出するか。</summary>
                    [JsonPropertyName("{{TAKE_TS}}")]
                    public int? {{TAKE_CS}} { get; set; }
                }
                {{Filter.RenderTree(_rootAggregate, ctx)}}
                #endregion 検索条件クラス
                """;
        }

        internal string RenderTypeScript(CodeRenderingContext ctx) {
            return $$"""
                /** {{_rootAggregate.DisplayName}}の検索時の検索条件の型。 */
                export type {{TsTypeName}} = {
                  /** 絞り込み条件 */
                  {{FILTER_TS}}: {
                {{FilterRoot.RenderTypeScriptDeclaringLiteral(ctx).SelectTextTemplate(source => $$"""
                    {{WithIndent(source, "    ")}}
                """)}}
                  }
                  /** 並び順 */
                  {{SORT_TS}}: (`${{{TypeScriptSortableMemberType}}}{{ASC_SUFFIX}}` | `${{{TypeScriptSortableMemberType}}}{{DESC_SUFFIX}}`)[]
                  /** ページングに使用。検索結果のうち先頭から何件スキップするか。 */
                  {{SKIP_TS}}?: number
                  /** ページングに使用。検索結果のうち先頭から何件抽出するか。 */
                  {{TAKE_TS}}?: number
                }
                """;
        }


        #region フィルタリング
        /// <summary>
        /// フィルタリング
        /// </summary>
        private Filter FilterRoot { get; }

        /// <summary>
        /// 検索条件クラスの絞り込み条件部分。
        /// 通常の一覧検索と参照先検索とで共通
        /// </summary>
        internal class Filter {
            internal Filter(AggregateBase aggregate) {
                _aggregate = aggregate;
            }
            private readonly AggregateBase _aggregate;

            internal virtual string CsClassName => $"{_aggregate.PhysicalName}SearchConditionFilter";
            internal virtual string TsTypeName => $"{_aggregate.PhysicalName}SearchConditionFilter";

            /// <summary>
            /// 検索条件のフィルターに指定できるメンバーを列挙する
            /// </summary>
            internal IEnumerable<IAggregateMember> GetSearchConditionMembers() {
                foreach (var member in _aggregate.GetMembers()) {
                    if (member is ValueMember vm && vm.Type.SearchBehavior != null) {
                        yield return member;

                    } else if (member is IRelationalMember) {
                        yield return member;
                    }
                }
            }

            #region GetFullPath
            /// <summary>
            /// エントリーから引数の集約までのパスをRefTargetのルールに従って返す
            /// </summary>
            internal static IEnumerable<string> GetPathFromEntry(ValueMember vm, E_CsTs csts) {
                if (csts == E_CsTs.CSharp) {
                    yield return FILTER_CS;
                } else {
                    yield return FILTER_TS;
                }

                foreach (var node in vm.GetFullPath()) {
                    yield return node.XElement.Name.LocalName; // LocalNameでよいかどうかは不明
                }
            }
            /// <summary>
            /// Refエントリーから引数の集約までのパス
            /// </summary>
            internal static IEnumerable<string> GetPathFromRefEntry(DisplayDataRefEntry refDisplayData) {
                throw new NotImplementedException();
            }
            #endregion GetFullPath

            #region レンダリング
            /// <summary>
            /// 子孫集約のフィルターも含めてレンダリングする
            /// </summary>
            internal static string RenderTree(RootAggregate rootAggregate, CodeRenderingContext ctx) {
                var tree = rootAggregate
                    .EnumerateThisAndDescendants()
                    .Select(agg => new Filter(agg));

                return $$"""
                    {{tree.SelectTextTemplate(filter => $$"""
                    {{filter.RenderCSharpDeclaring(ctx)}}
                    """)}}
                    """;
            }

            /// <summary>
            /// C#の型定義のレンダリング
            /// </summary>
            private string RenderCSharpDeclaring(CodeRenderingContext ctx) {
                return $$"""
                    public partial class {{CsClassName}} {
                    {{GetSearchConditionMembers().SelectTextTemplate(member => $$"""
                        {{WithIndent(RenderMember(member), "    ")}}
                    """)}}
                    }
                    """;

                string RenderMember(IAggregateMember member) {
                    if (member is ValueMember vm) {
                        return $$"""
                            public {{vm.Type.SearchBehavior?.FilterCsTypeName}}? {{vm.PhysicalName}} { get; set; }
                            """;

                    } else if (member is IRelationalMember rm) {
                        var filter = new Filter(rm.MemberAggregate);
                        return $$"""
                            public {{filter.CsClassName}} {{rm.PhysicalName}} { get; set; } = new();
                            """;

                    } else {
                        throw new NotImplementedException();
                    }
                }
            }

            /// <summary>
            /// TypeScriptの型定義のレンダリング。
            /// export const は <see cref="RenderTypeScript"/> 側で行なう
            /// </summary>
            internal IEnumerable<string> RenderTypeScriptDeclaringLiteral(CodeRenderingContext ctx) {
                foreach (var member in GetSearchConditionMembers()) {
                    if (member is ValueMember vm) {
                        yield return $$"""
                            {{vm.PhysicalName}}?: {{vm.Type.SearchBehavior?.FilterTsTypeName}}
                            """;

                    } else if (member is IRelationalMember rm) {
                        var filter = new Filter(rm.MemberAggregate);
                        yield return $$"""
                            {{rm.PhysicalName}}: {
                              {{WithIndent(filter.RenderTypeScriptDeclaringLiteral(ctx), "  ")}}
                            }
                            """;

                    } else {
                        throw new NotImplementedException();
                    }

                }
            }

            /// <summary>
            /// TypeScriptの新規オブジェクト作成関数のレンダリング
            /// </summary>
            internal IEnumerable<string> RenderNewObjectFunctionMemberLiteral() {
                foreach (var member in GetSearchConditionMembers()) {

                    if (member is ValueMember vm) {
                        yield return $$"""
                            {{member.PhysicalName}}: {{vm.Type.SearchBehavior?.RenderTsNewObjectFunctionValue()}},
                            """;

                    } else if (member is IRelationalMember rm) {
                        var filter = new Filter(rm.MemberAggregate);

                        yield return $$"""
                            {{member.PhysicalName}}: {
                              {{WithIndent(filter.RenderNewObjectFunctionMemberLiteral(), "  ")}}
                            },
                            """;
                    }
                }
            }
            #endregion レンダリング
        }
        #endregion フィルタリング


        #region ソート
        internal const string ASC_SUFFIX = "（昇順）";
        internal const string DESC_SUFFIX = "（降順）";

        internal string TypeScriptSortableMemberType => $"SortableMemberOf{_rootAggregate.PhysicalName}";
        internal string GetTypeScriptSortableMemberType => $"get{TypeScriptSortableMemberType}";

        internal string RenderTypeScriptSortableMemberType() {
            var sortableMembers = EnumerateSortMembersRecursively().ToArray();

            return $$"""
                /** {{_rootAggregate.DisplayName}}のメンバーのうちソート可能なものを表すリテラル型 */
                export type {{TypeScriptSortableMemberType}}
                {{If(sortableMembers.Length == 0, () => $$"""
                  = never
                """)}}
                {{sortableMembers.SelectTextTemplate((m, i) => $$"""
                  {{(i == 0 ? "=" : "|")}} '{{m.GetLiteral().Replace("'", "\\'")}}'
                """)}}

                /** {{_rootAggregate.DisplayName}}のメンバーのうちソート可能なものを文字列で返します。 */
                export const {{GetTypeScriptSortableMemberType}} = (): {{TypeScriptSortableMemberType}}[] => [
                {{sortableMembers.SelectTextTemplate(m => $$"""
                  '{{m.GetLiteral().Replace("'", "\\'")}}',
                """)}}
                ]
                """;
        }

        /// <summary>
        /// 並び順に指定することができるメンバーを、子孫要素や参照先のそれも含めて列挙します。
        /// </summary>
        internal IEnumerable<SortableMember> EnumerateSortMembersRecursively() {
            return EnumerateRecursively(_rootAggregate);

            static IEnumerable<SortableMember> EnumerateRecursively(AggregateBase aggregate) {
                foreach (var member in aggregate.GetMembers()) {
                    if (member is ValueMember vm) {
                        yield return new SortableMember(vm);

                    } else if (member is ChildrenAggreagte) {
                        // 子配列の要素でのソートは論理的に定義できない
                        continue;

                    } else if (member is IRelationalMember rm) {
                        // 子集約または参照先
                        foreach (var vm2 in EnumerateRecursively(rm.MemberAggregate)) {
                            yield return vm2;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// ソート可能なメンバー
        /// </summary>
        internal class SortableMember {
            internal SortableMember(ValueMember member) {
                Member = member;
            }

            internal ValueMember Member { get; }

            /// <summary>
            /// '子要素.孫要素.プロパティ名' のような並び順候補の文字列を返します。
            /// </summary>
            internal string GetLiteral() {
                return Filter
                    .GetPathFromEntry(Member, E_CsTs.CSharp)
                    .Skip(1) // "Filter"という名称を除外
                    .Join(".");
            }
            /// <summary>
            /// '子要素.孫要素.プロパティ名（昇順）' のような並び順候補の文字列の一覧を返します。
            /// </summary>
            internal IEnumerable<string> GetLiteralWithAscDesc() {
                var fullpath = GetLiteral();
                yield return $"{fullpath}{ASC_SUFFIX}";
                yield return $"{fullpath}{DESC_SUFFIX}";
            }
        }
        #endregion ソート


        #region TypeScript側のオブジェクト新規作成関数
        /// <summary>
        /// TypeScriptの新規オブジェクト作成関数の名前
        /// </summary>
        internal string TsNewObjectFunction => $"createNew{TsTypeName}";
        internal string RenderNewObjectFunction() {
            return $$"""
                /** {{_rootAggregate.DisplayName}}の検索条件クラスの空オブジェクトを作成して返します。 */
                export const {{TsNewObjectFunction}} = (): {{TsTypeName}} => ({
                  {{FILTER_TS}}: {
                    {{WithIndent(FilterRoot.RenderNewObjectFunctionMemberLiteral(), "    ")}}
                  },
                  {{SORT_TS}}: [],
                  {{SKIP_TS}}: undefined,
                  {{TAKE_TS}}: undefined,
                })
                """;
        }
        #endregion TypeScript側のオブジェクト新規作成関数
    }
}
