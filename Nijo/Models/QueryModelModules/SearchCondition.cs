using Nijo.CodeGenerating;
using Nijo.CodeGenerating.Helpers;
using Nijo.ImmutableSchema;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nijo.Models.QueryModelModules {

    /// <summary>
    /// 検索条件クラス。
    /// 通常の一覧検索と参照先検索とで共通
    /// </summary>
    internal static class SearchCondition {
        internal const string TS_BASE_TYPE_NAME = "SearchConditionBaseType";

        internal const string ASC_SUFFIX = "（昇順）";
        internal const string DESC_SUFFIX = "（降順）";

        /// <summary>
        /// 検索条件オブジェクトのエントリー。
        /// フィルタ、ソート、ページングの属性を持つ。
        /// </summary>
        internal class Entry : IInstancePropertyOwnerMetadata {
            internal Entry(RootAggregate entryAggregate) {
                _entryAggregate = entryAggregate;
                FilterRoot = new Filter(_entryAggregate);
            }
            private readonly RootAggregate _entryAggregate;

            internal virtual string CsClassName => $"{_entryAggregate.PhysicalName}SearchCondition";
            internal virtual string TsTypeName => $"{_entryAggregate.PhysicalName}SearchCondition";

            /// <summary>フィルタリング</summary>
            internal Filter FilterRoot { get; }

            internal string TypeScriptSortableMemberType => $"SortableMemberOf{_entryAggregate.PhysicalName}";
            internal string GetTypeScriptSortableMemberType => $"get{TypeScriptSortableMemberType}";

            internal const string FILTER_CS = "Filter";
            internal const string FILTER_TS = "filter";
            internal const string SORT_CS = "Sort";
            internal const string SORT_TS = "sort";
            internal const string SKIP_CS = "Skip";
            internal const string SKIP_TS = "skip";
            internal const string TAKE_CS = "Take";
            internal const string TAKE_TS = "take";

            IEnumerable<IInstancePropertyMetadata> IInstancePropertyOwnerMetadata.GetMembers() {
                yield return FilterRoot;
            }

            /// <summary>
            /// ルート集約の検索条件エントリークラスをレンダリングします。
            /// </summary>
            internal static string RenderCSharpRecursively(RootAggregate rootAggregate, CodeRenderingContext ctx) {
                var entry = new Entry(rootAggregate);

                return $$"""
                    #region 検索条件エントリーポイント
                    /// <summary>
                    /// {{rootAggregate.DisplayName}}の一覧検索条件
                    /// </summary>
                    public partial class {{entry.CsClassName}} {
                        /// <summary>絞り込み条件</summary>
                        [JsonPropertyName("{{FILTER_TS}}")]
                        public {{entry.FilterRoot.CsClassName}} {{FILTER_CS}} { get; set; } = new();
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
                    #endregion 検索条件エントリーポイント

                    #region 検索条件フィルター
                    {{Filter.RenderTree(rootAggregate, ctx)}}
                    #endregion 検索条件フィルター
                    """;
            }


            /// <summary>
            /// 検索条件の基底型の型定義をレンダリングします。
            /// </summary>
            internal static SourceFile RenderTsBaseType() {
                return new SourceFile {
                    FileName = "search-condition-base-type.ts",
                    Contents = $$"""
                        /** 検索条件の基底型 */
                        export type {{TS_BASE_TYPE_NAME}}<TFilter, TSortMember extends string> = {
                          /** 絞り込み条件 */
                          {{FILTER_TS}}: TFilter
                          /** 並び順 */
                          {{SORT_TS}}: (`${TSortMember}{{ASC_SUFFIX}}` | `${TSortMember}{{DESC_SUFFIX}}`)[]
                          /** ページングに使用。検索結果のうち先頭から何件スキップするか。 */
                          {{SKIP_TS}}?: number
                          /** ページングに使用。検索結果のうち先頭から何件抽出するか。 */
                          {{TAKE_TS}}?: number
                        }
                        """,
                };
            }
            /// <summary>
            /// ルート集約またはほかの集約から参照されている子孫集約の検索条件エントリークラスをレンダリングします。
            /// </summary>
            internal static string RenderTypeScriptRecursively(RootAggregate rootAggregate, CodeRenderingContext ctx) {
                var entry = new Entry(rootAggregate);

                return $$"""
                    /** {{rootAggregate.DisplayName}}の検索時の検索条件の型。 */
                    export type {{entry.TsTypeName}} = Util.{{TS_BASE_TYPE_NAME}}<{{entry.FilterRoot.TsTypeName}}, {{entry.TypeScriptSortableMemberType}}>

                    /** {{rootAggregate.DisplayName}}の検索時の検索条件の絞り込み条件の型。 */
                    export type {{entry.FilterRoot.TsTypeName}} = {
                    {{entry.FilterRoot.RenderTypeScriptDeclaringLiteral().SelectTextTemplate(source => $$"""
                      {{WithIndent(source, "  ")}}
                    """)}}
                    }
                    """;
            }


            #region ソート
            internal string RenderTypeScriptSortableMemberType() {
                var sortableMembers = EnumerateSortMembersRecursively().ToArray();

                return $$"""
                    /** {{_entryAggregate.DisplayName}}のメンバーのうちソート可能なものを表すリテラル型 */
                    export type {{TypeScriptSortableMemberType}}
                    {{If(sortableMembers.Length == 0, () => $$"""
                      = never
                    """)}}
                    {{sortableMembers.SelectTextTemplate((m, i) => $$"""
                      {{(i == 0 ? "=" : "|")}} '{{m.GetLiteral().Replace("'", "\\'")}}'
                    """)}}

                    /** {{_entryAggregate.DisplayName}}のメンバーのうちソート可能なものを文字列で返します。 */
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
                return EnumerateRecursively(_entryAggregate);

                static IEnumerable<SortableMember> EnumerateRecursively(AggregateBase aggregate) {
                    foreach (var member in aggregate.GetMembers()) {
                        if (member is ValueMember vm) {
                            yield return new SortableMember(vm);

                        } else if (member is ChildrenAggregate) {
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
            #endregion ソート


            #region TypeScript側のオブジェクト新規作成関数
            /// <summary>
            /// TypeScriptの新規オブジェクト作成関数の名前
            /// </summary>
            internal string TsNewObjectFunction => $"createNew{TsTypeName}";
            internal string RenderNewObjectFunction() {
                return $$"""
                    /** {{_entryAggregate.DisplayName}}の検索条件クラスの空オブジェクトを作成して返します。 */
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


        #region フィルター
        /// <summary>
        /// 検索条件クラスの絞り込み条件部分。
        /// 通常の一覧検索と参照先検索とで共通
        /// </summary>
        internal class Filter : IInstanceStructurePropertyMetadata, IInstancePropertyOwnerMetadata {
            internal Filter(AggregateBase aggregate) {
                _aggregate = aggregate;
            }
            private readonly AggregateBase _aggregate;

            internal virtual string CsClassName => $"{_aggregate.PhysicalName}SearchConditionFilter";
            internal virtual string TsTypeName => $"{_aggregate.PhysicalName}SearchConditionFilter";

            bool IInstanceStructurePropertyMetadata.IsArray => false;
            string IInstanceStructurePropertyMetadata.GetTypeName(E_CsTs csts) => csts == E_CsTs.CSharp ? CsClassName : TsTypeName;
            ISchemaPathNode IInstancePropertyMetadata.SchemaPathNode => ISchemaPathNode.Empty;
            string IInstancePropertyMetadata.GetPropertyName(E_CsTs csts) => csts == E_CsTs.CSharp ? Entry.FILTER_CS : Entry.FILTER_TS;
            IEnumerable<IInstancePropertyMetadata> IInstancePropertyOwnerMetadata.GetMembers() => GetOwnMembers();

            /// <summary>
            /// 検索条件のフィルターに指定できるメンバーを列挙する
            /// </summary>
            internal IEnumerable<IFilterMember> GetOwnMembers() {
                foreach (var member in _aggregate.GetMembers()) {
                    if (member is ValueMember vm && vm.Type.SearchBehavior != null) {
                        yield return new FilterValueMember(vm);

                    } else if (member is IRelationalMember rm) {
                        yield return new FilterRelationalMember(rm);
                    }
                }
            }

            /// <summary>
            /// フィルタリングに使える値を子孫要素のそれも含め再帰的に列挙
            /// </summary>
            internal IEnumerable<FilterValueMember> GetValueMembersRecursively() {
                return GetRecursively(this);

                static IEnumerable<FilterValueMember> GetRecursively(Filter filter) {
                    foreach (var member in filter.GetOwnMembers()) {
                        if (member is FilterValueMember vm) {
                            yield return vm;

                        } else if (member is FilterRelationalMember rm) {
                            foreach (var vm2 in rm.ChildFilter.GetValueMembersRecursively()) {
                                yield return vm2;
                            }

                        } else {
                            throw new NotImplementedException();
                        }
                    }
                }
            }

            #region GetFullPath
            /// <summary>
            /// エントリーから引数の集約までのパスをRefTargetのルールに従って返す
            /// </summary>
            internal static IEnumerable<string> GetPathFromEntry(ValueMember vm, E_CsTs csts) {
                if (csts == E_CsTs.CSharp) {
                    yield return Entry.FILTER_CS;
                } else {
                    yield return Entry.FILTER_TS;
                }

                foreach (var node in vm.GetPathFromEntry()) {
                    yield return node.XElement.Name.LocalName; // LocalNameでよいかどうかは不明
                }
            }
            /// <summary>
            /// Refエントリーから引数の集約までのパス
            /// </summary>
            internal static IEnumerable<string> GetPathFromRefEntry(DisplayDataRef.Entry refDisplayData) {
                throw new NotImplementedException();
            }
            #endregion GetFullPath

            #region レンダリング
            /// <summary>
            /// 子孫集約のフィルターも含めてレンダリングする
            /// </summary>
            internal static string RenderTree(AggregateBase rootAggregate, CodeRenderingContext ctx) {
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
                    {{GetOwnMembers().SelectTextTemplate(member => $$"""
                        {{WithIndent(member.RenderCSharpDeclaring(), "    ")}}
                    """)}}
                    }
                    """;
            }

            /// <summary>
            /// TypeScriptの型定義のレンダリング。
            /// export const は <see cref="RenderTypeScript"/> 側で行なう
            /// </summary>
            internal IEnumerable<string> RenderTypeScriptDeclaringLiteral() {
                foreach (var member in GetOwnMembers()) {
                    yield return member.RenderTypeScriptDeclaring();
                }
            }

            /// <summary>
            /// TypeScriptの新規オブジェクト作成関数のレンダリング
            /// </summary>
            internal IEnumerable<string> RenderNewObjectFunctionMemberLiteral() {
                foreach (var member in GetOwnMembers()) {
                    yield return $$"""
                        {{member.GetPropertyName(E_CsTs.TypeScript)}}: {{member.RenderTsNewObjectFunctionValue()}},
                        """;
                }
            }
            #endregion レンダリング
        }

        /// <summary>
        /// <see cref="Filter"/> のメンバー
        /// </summary>
        internal interface IFilterMember : IInstancePropertyMetadata {
            string RenderCSharpDeclaring();
            string RenderTypeScriptDeclaring();
            string RenderTsNewObjectFunctionValue();
        }
        /// <summary>
        /// 文字列や数値など単一の値に対するフィルター
        /// </summary>
        internal class FilterValueMember : IFilterMember, IInstanceValuePropertyMetadata {
            internal FilterValueMember(ValueMember member) {
                if (member.Type.SearchBehavior == null) throw new ArgumentException();
                Member = member;
            }

            internal ValueMember Member { get; }
            public string DisplayName => Member.DisplayName;
            internal ValueMemberSearchBehavior SearchBehavior => Member.Type.SearchBehavior!;

            string IFilterMember.RenderCSharpDeclaring() {
                return $$"""
                    public {{Member.Type.SearchBehavior?.FilterCsTypeName}}? {{Member.PhysicalName}} { get; set; }
                    """;
            }
            string IFilterMember.RenderTypeScriptDeclaring() {
                return $$"""
                    {{Member.PhysicalName}}?: {{Member.Type.SearchBehavior?.FilterTsTypeName}}
                    """;
            }
            string IFilterMember.RenderTsNewObjectFunctionValue() => Member.Type.SearchBehavior!.RenderTsNewObjectFunctionValue();

            ISchemaPathNode IInstancePropertyMetadata.SchemaPathNode => Member;
            string IInstancePropertyMetadata.GetPropertyName(E_CsTs csts) => Member.PhysicalName;
            IValueMemberType IInstanceValuePropertyMetadata.Type => Member.Type;
        }
        /// <summary>
        /// Child, Children, Ref 部分のフィルターのコンテナ
        /// </summary>
        internal class FilterRelationalMember : IFilterMember, IInstanceStructurePropertyMetadata {
            internal FilterRelationalMember(IRelationalMember member) {
                _rm = member;
                ChildFilter = new Filter(member.MemberAggregate);
            }

            private readonly IRelationalMember _rm;
            internal Filter ChildFilter { get; }
            public string DisplayName => _rm.DisplayName;

            string IFilterMember.RenderCSharpDeclaring() {
                return $$"""
                    public {{ChildFilter.CsClassName}} {{_rm.PhysicalName}} { get; set; } = new();
                    """;
            }
            string IFilterMember.RenderTypeScriptDeclaring() {
                if (_rm is RefToMember refTo) {
                    var refToFilter = new Filter(refTo.RefTo.GetRoot()); // refの場合はref自体ではなくルート
                    return $$"""
                        {{_rm.PhysicalName}}?: {{refToFilter.TsTypeName}}
                        """;
                } else {
                    return $$"""
                        {{_rm.PhysicalName}}: {
                          {{WithIndent(ChildFilter.RenderTypeScriptDeclaringLiteral(), "  ")}}
                        }
                        """;
                }
            }
            string IFilterMember.RenderTsNewObjectFunctionValue() {
                return $$"""
                    {
                      {{WithIndent(ChildFilter.RenderNewObjectFunctionMemberLiteral(), "  ")}}
                    }
                    """;
            }

            ISchemaPathNode IInstancePropertyMetadata.SchemaPathNode => _rm;
            string IInstancePropertyMetadata.GetPropertyName(E_CsTs csts) => _rm.PhysicalName;
            bool IInstanceStructurePropertyMetadata.IsArray => false; // 検索条件なのでChildrenであっても配列ではない
            string IInstanceStructurePropertyMetadata.GetTypeName(E_CsTs csts) => csts == E_CsTs.CSharp ? ChildFilter.CsClassName : ChildFilter.TsTypeName;

            IEnumerable<IInstancePropertyMetadata> IInstancePropertyOwnerMetadata.GetMembers() {
                return ((IInstancePropertyOwnerMetadata)ChildFilter).GetMembers();
            }
        }
        #endregion フィルター


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
    }
}
