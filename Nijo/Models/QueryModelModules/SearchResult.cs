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
    /// <see cref="DisplayData"/> が持つ項目は全て検索結果型にも存在する。
    ///
    /// <para>
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
    ///
    /// 各構造ごとの詳細なルールは以下。
    ///
    /// * ValueMember: 親集約のプロパティとして定義される。
    /// * Child: 親と別のクラスとしてではなく、 `(親のインスタンス).(Childの物理名)_(ValueMemberの物理名)` という風にアンダースコアつながりで親の直下のメンバーとして生成される。
    /// * Children: 親と別のクラスとして定義される。プロパティのパスは、Childrenが起点となる。
    /// * Ref:
    ///   * 参照先のSearchConditionのすべてのメンバーは、参照元のSearchConditionクラスのメンバーとして現れる。
    ///   * Childと同様に、参照元集約直下のプロパティとして定義される。
    ///   * 参照先がルート集約ではなく子孫集約の場合、参照元の親集約のメンバーも参照元クラスに定義される。
    ///   * 参照先がChildrenを持つ場合、参照元にChildrenがある場合と同様に、別のクラスとして定義される。アンダースコアのパスは当該参照先のChildrenが起点となる。
    ///   * 参照先が子孫集約の場合、親への参照がParentという名前で表れる。
    /// </para>
    /// </summary>
    internal class SearchResult : IInstancePropertyOwnerMetadata {

        // Childは親の一部としてレンダリングされるので定義できない
        internal SearchResult(RootAggregate aggregate) {
            Aggregate = aggregate;
        }
        protected SearchResult(ChildrenAggregate children) {
            Aggregate = children;
        }
        internal AggregateBase Aggregate { get; }

        /// <summary>楽観排他制御用のバージョン</summary>
        internal const string VERSION = "Version";
        internal bool HasVersion => Aggregate is RootAggregate;

        internal string CsClassName => $"{Aggregate.PhysicalName}SearchResult";

        internal IEnumerable<ISearchResultMember> GetMembers() {
            return GetMembersRecursively(Aggregate, false);

            static IEnumerable<ISearchResultMember> GetMembersRecursively(AggregateBase aggregate, bool isOutOfEntryTree) {
                // aggregateが参照先の場合は親のメンバーも列挙
                if (isOutOfEntryTree) {
                    var parent = aggregate.GetParent();
                    if (parent != null && aggregate.PreviousNode != (ISchemaPathNode)parent) {
                        foreach (var srm in GetMembersRecursively(parent, true)) {
                            yield return srm;
                        }
                    }
                }

                foreach (var member in aggregate.GetMembers()) {
                    if (member is ValueMember vm) {
                        yield return new SearchResultValueMember(vm, isOutOfEntryTree);

                    } else if (member is ChildrenAggregate children) {
                        // aggregateが参照先の場合、かつ子から親へ辿られたとき、循環参照を防ぐ
                        if (aggregate.PreviousNode == (ISchemaPathNode)children) continue;

                        // 参照先のChildrenは、コンフィグで明示的に指定されていない場合は生成しない
                        if (isOutOfEntryTree && !CodeRenderingContext.CurrentContext.Config.GenerateRefToChildrenDisplayData) continue;

                        yield return new SearchResultChildrenMember(children, isOutOfEntryTree);

                    } else if (member is ChildAggregate child) {
                        // aggregateが参照先の場合、かつ子から親へ辿られたとき、循環参照を防ぐ
                        if (aggregate.PreviousNode == (ISchemaPathNode)child) continue;

                        foreach (var srm in GetMembersRecursively(child, isOutOfEntryTree)) {
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
                    ChildrenAggregate children => new SearchResultChildrenMember(children, false),
                    _ => throw new InvalidOperationException(),
                });

            return $$"""
                #region 検索クエリ型
                {{tree.SelectTextTemplate(sr => $$"""
                /// <summary>
                /// {{sr.Aggregate.DisplayName}}の検索結果型。
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
            bool IsOutOfEntryTree { get; }
            IAggregateMember Member { get; }
            string RenderDeclaration();
            ISchemaPathNode IInstancePropertyMetadata.SchemaPathNode => Member;

            /// <summary>
            /// プロパティ名の計算。
            /// 詳細なルールはこのクラスのXMLコメントを参照。
            /// </summary>
            internal string GetPhysicalName() {
                var list = new List<string>();
                var previousOfPrevious = (ISchemaPathNode?)null;
                foreach (var node in Member.GetPathFromEntry()) {

                    // エントリーを除外
                    if (node.PreviousNode == null) {
                        previousOfPrevious = node.PreviousNode;
                        continue;
                    }

                    // Childrenはその親とは別のクラスのためアンダースコアによるパス結合をクリア
                    if (node.PreviousNode is ChildrenAggregate
                        // 親から子に向かって辿った場合のみクリア（参照先の場合は子から親に辿ることがある）
                        && previousOfPrevious?.XElement == node.PreviousNode.XElement.Parent) {
                        list.Clear();
                    }

                    // Refの名前はこの1つ前で列挙済みのためスキップ
                    if (node.PreviousNode is RefToMember) {
                        previousOfPrevious = node.PreviousNode;
                        continue;
                    }

                    // 参照先のメンバーで子から親に辿る場合は "Parent" という名前になる
                    if (node.XElement == node.PreviousNode.XElement.Parent) {
                        list.Add("Parent");
                    } else {
                        list.Add(node.XElement.Name.LocalName);
                    }

                    previousOfPrevious = node.PreviousNode;
                }
                return list.Join("_");
            }
        }

        internal class SearchResultValueMember : ISearchResultMember, IInstanceValuePropertyMetadata {
            internal SearchResultValueMember(ValueMember valueMember, bool isOutOfEntryTree) {
                _valueMember = valueMember;
                IsOutOfEntryTree = isOutOfEntryTree;
            }
            private readonly ValueMember _valueMember;
            private string? _physicalName;

            public bool IsOutOfEntryTree { get; }

            public string GetPropertyName(E_CsTs csts) => _physicalName ??= ((ISearchResultMember)this).GetPhysicalName();
            IValueMemberType IInstanceValuePropertyMetadata.Type => _valueMember.Type;
            IAggregateMember ISearchResultMember.Member => _valueMember;

            string ISearchResultMember.RenderDeclaration() {
                var type = _valueMember.Type.CsPrimitiveTypeName;

                return $$"""
                    /// <summary>{{_valueMember.DisplayName}}</summary>
                    public {{type}}? {{GetPropertyName(E_CsTs.CSharp)}} { get; set; }
                    """;
            }
        }

        internal class SearchResultChildrenMember : SearchResult, ISearchResultMember, IInstanceStructurePropertyMetadata {
            internal SearchResultChildrenMember(ChildrenAggregate children, bool isOutOfEntryTree) : base(children) {
                Aggregate = children;
                IsOutOfEntryTree = isOutOfEntryTree;
            }
            internal new ChildrenAggregate Aggregate { get; }
            private string? _physicalName;

            public bool IsOutOfEntryTree { get; }

            public string GetPropertyName(E_CsTs csts) => _physicalName ??= ((ISearchResultMember)this).GetPhysicalName();
            bool IInstanceStructurePropertyMetadata.IsArray => true;
            string IInstanceStructurePropertyMetadata.GetTypeName(E_CsTs csts) => CsClassName;
            IAggregateMember ISearchResultMember.Member => Aggregate;
            IEnumerable<IInstancePropertyMetadata> IInstancePropertyOwnerMetadata.GetMembers() => GetMembers();

            string ISearchResultMember.RenderDeclaration() {
                return $$"""
                    /// <summary>{{Aggregate.DisplayName}}</summary>
                    public List<{{CsClassName}}> {{GetPropertyName(E_CsTs.CSharp)}} { get; set; } = new();
                    """;
            }
        }
        #endregion メンバー
    }
}
