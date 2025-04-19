using Nijo.CodeGenerating;
using Nijo.CodeGenerating.Helpers;
using Nijo.ImmutableSchema;
using Nijo.Models.QueryModelModules;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.QueryModelModules {
    /// <summary>
    /// QueryModelの検索結果型。
    /// SQLのSELECT句の形と対応する。
    /// EFCoreによるWhere句の付加が可能。
    /// ChildやRefToがあっても入れ子にならずフラットなデータ構造になる。
    /// <see cref="DisplayData"/> が持つ項目は全て検索結果型にも存在する。
    /// </summary>
    internal class SearchResult : IInstancePropertyOwnerMetadata {

        // Childは親の一部としてレンダリングされるので定義できない
        internal SearchResult(RootAggregate aggregate) {
            _aggregate = aggregate;
        }
        protected SearchResult(ChildrenAggregate children) {
            _aggregate = children;
        }
        private readonly AggregateBase _aggregate;

        /// <summary>楽観排他制御用のバージョン</summary>
        internal const string VERSION = "Version";
        internal bool HasVersion => _aggregate is RootAggregate;

        internal string CsClassName => $"{_aggregate.PhysicalName}SearchResult";

        internal IEnumerable<ISearchResultMember> GetMembers() {
            return GetMembersRecursively(_aggregate, false);

            static IEnumerable<ISearchResultMember> GetMembersRecursively(AggregateBase aggregate, bool enumeratesParent) {
                // aggregateが参照先の場合は親のメンバーも列挙
                if (enumeratesParent) {
                    var parent = aggregate.GetParent();
                    if (parent != null && aggregate.PreviousNode != (ISchemaPathNode)parent) {
                        foreach (var srm in GetMembersRecursively(parent, true)) {
                            yield return srm;
                        }
                    }
                }

                foreach (var member in aggregate.GetMembers()) {
                    if (member is ValueMember vm) {
                        yield return new SearchResultValueMember(vm);

                    } else if (member is ChildrenAggregate children) {
                        // aggregateが参照先の場合、かつ子から親へ辿られたとき、循環参照を防ぐ
                        if (aggregate.PreviousNode == (ISchemaPathNode)children) continue;

                        yield return new SearchResultChildrenMember(children);

                    } else if (member is ChildAggregate child) {
                        // aggregateが参照先の場合、かつ子から親へ辿られたとき、循環参照を防ぐ
                        if (aggregate.PreviousNode == (ISchemaPathNode)child) continue;

                        foreach (var srm in GetMembersRecursively(child, enumeratesParent)) {
                            yield return srm;
                        }

                    } else if (member is RefToMember refTo) {
                        // aggregateが参照先の場合、かつ子から親へ辿られたとき、循環参照を防ぐ
                        if (aggregate.PreviousNode == (ISchemaPathNode)refTo) continue;

                        foreach (var srm in GetMembersRecursively(refTo.RefTo, true)) {
                            yield return srm;
                        }

                    } else {
                        throw new InvalidOperationException($"予期しないメンバー: {member}");
                    }
                }
            }
        }

        IEnumerable<IInstancePropertyMetadata> IInstancePropertyOwnerMetadata.GetMembers() {
            return GetMembers();
        }

        internal static string RenderTree(RootAggregate rootAggregate) {
            var tree = rootAggregate
                .EnumerateThisAndDescendants()
                .Where(agg => agg is RootAggregate || agg is ChildrenAggregate)
                .Select(agg => agg switch {
                    RootAggregate root => new SearchResult(root),
                    ChildrenAggregate children => new SearchResultChildrenMember(children),
                    _ => throw new InvalidOperationException(),
                });

            return $$"""
                #region 検索クエリ型
                {{tree.SelectTextTemplate(sr => $$"""
                /// <summary>
                /// {{sr._aggregate.DisplayName}}の検索結果型。
                /// SQLのSELECT句の形と対応する。
                /// </summary>
                public partial class {{sr.CsClassName}} {
                {{sr.GetMembers().SelectTextTemplate(srm => $$"""
                    {{WithIndent(srm.RenderDeclaration(), "    ")}}
                """)}}
                {{If(sr.HasVersion, () => $$"""
                    /// <summary>
                    /// 楽観排他制御用のバージョン。
                    /// null許容でないのは、データベース上に存在する時点で必ずバージョンも存在するため。
                    /// </summary>
                    public int {{VERSION}} { get; set; }
                """)}}
                }
                """)}}
                #endregion 検索クエリ型
                """;
        }


        #region メンバー
        internal interface ISearchResultMember : IInstancePropertyMetadata {
            IAggregateMember Member { get; }
            string RenderDeclaration();
            ISchemaPathNode IInstancePropertyMetadata.SchemaPathNode => Member;

            /// <summary>
            /// 検索条件のプロパティはChildやRefといった親と1対1のリレーションについては
            /// DisplayData等のネストされたオブジェクトではなく"_"で接続されたフラットな構造になる（以下例）。
            /// こうなっている理由は、もしパフォーマンスに問題が出た場合に、EFCoreのLINQ to Entity からSQLへ実装を変更しやすい余地を残すため。
            /// <code>
            /// // DisplayDataなどのネストされたオブジェクトにおける親と1対1対応のオブジェクトの構造
            /// var displayData = new なんらかのDisplayData {
            ///     親のID = "xxx",
            ///     子 = new() {
            ///         子のID = "xxx",
            ///     },
            /// };
            /// 
            /// // SearchResultにおける親と1対1対応のオブジェクトの構造
            /// var searchResult = new なんらかのSearchResult {
            ///     親のID = "xxx",
            ///     子_子のID = "xxx",
            /// };
            /// </code>
            /// </summary>
            internal string GetPhysicalName() {
                var list = new List<string>();
                foreach (var node in Member.GetPathFromEntry().SinceNearestChildren()) {
                    // エントリーを除外
                    if (node.PreviousNode == null) continue;

                    // Refの名前はこの1つ前で列挙済みのためスキップ
                    if (node.PreviousNode is RefToMember) continue;

                    list.Add(node.XElement.Name.LocalName);
                }
                return list.Join("_");
            }
        }

        internal class SearchResultValueMember : ISearchResultMember, IInstanceValuePropertyMetadata {
            internal SearchResultValueMember(ValueMember valueMember) {
                _valueMember = valueMember;
            }
            private readonly ValueMember _valueMember;
            private string? _physicalName;

            public string PropertyName => _physicalName ??= ((ISearchResultMember)this).GetPhysicalName();
            IValueMemberType IInstanceValuePropertyMetadata.Type => _valueMember.Type;
            IAggregateMember ISearchResultMember.Member => _valueMember;

            string ISearchResultMember.RenderDeclaration() {
                var type = _valueMember.Type.CsPrimitiveTypeName;

                return $$"""
                    /// <summary>{{_valueMember.DisplayName}}</summary>
                    public {{type}}? {{PropertyName}} { get; set; }
                    """;
            }
        }

        internal class SearchResultChildrenMember : SearchResult, ISearchResultMember, IInstanceStructurePropertyMetadata {
            internal SearchResultChildrenMember(ChildrenAggregate children) : base(children) {
                _children = children;
            }
            private readonly ChildrenAggregate _children;
            private string? _physicalName;

            public string PropertyName => _physicalName ??= ((ISearchResultMember)this).GetPhysicalName();
            bool IInstanceStructurePropertyMetadata.IsArray => true;
            IAggregateMember ISearchResultMember.Member => _children;
            IEnumerable<IInstancePropertyMetadata> IInstancePropertyOwnerMetadata.GetMembers() => GetMembers();

            string ISearchResultMember.RenderDeclaration() {
                return $$"""
                    /// <summary>{{_children.DisplayName}}</summary>
                    public List<{{CsClassName}}> {{PropertyName}} { get; set; } = new();
                    """;
            }
        }
        #endregion メンバー
    }
}

namespace Nijo.CodeGenerating {
    partial class SchemaPathNodeExtensions {

        /// <summary>
        /// <see cref="GetPathFromEntry(ISchemaPathNode)"/> の結果を <see cref="SearchResult"/> のルールに沿ったパスとして返す
        /// </summary>
        public static IEnumerable<string> AsSearchResult(this IEnumerable<ISchemaPathNode> path) {
            var entry = path.FirstOrDefault()?.GetEntry();

            foreach (var node in path) {
                if (node == entry) continue; // パスの一番最初（エントリー）はスキップ

                // 検索結果オブジェクトはフラットな構造なので親と1対1の子は表れない
                if (node is ChildAggregate) continue;
                if (node is RefToMember) continue;
                if (node is RootAggregate) continue; // ref-toでルートを参照しているときパスの途中にRootAggregateが表れる

                // Children
                if (node is ChildrenAggregate children) {
                    var member = new SearchResult.SearchResultChildrenMember(children);
                    yield return member.PropertyName;
                    continue;
                }

                // 末端のメンバー
                if (node is ValueMember vm) {
                    var member = new SearchResult.SearchResultValueMember(vm);
                    yield return member.PropertyName;
                    continue;
                }

                throw new InvalidOperationException("予期しない型");
            }
        }
    }
}
