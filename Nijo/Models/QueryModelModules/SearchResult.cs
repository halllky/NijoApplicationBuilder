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
        internal SearchResult(ChildrenAggregate aggregate) {
            _aggregate = aggregate;
        }
        private readonly AggregateBase _aggregate;

        /// <summary>楽観排他制御用のバージョン</summary>
        internal const string VERSION = "Version";
        internal bool HasVersion => _aggregate is RootAggregate;

        internal string CsClassName => $"{_aggregate.PhysicalName}SearchResult";

        internal IEnumerable<SearchResultMember> GetMembers() {
            return GetMembersRecursively(_aggregate);

            static IEnumerable<SearchResultMember> GetMembersRecursively(AggregateBase aggregate) {
                foreach (var member in aggregate.GetMembers()) {
                    if (member is ValueMember vm) {
                        yield return new SearchResultValueMember(vm);

                    } else if (member is ChildrenAggregate children) {
                        yield return new SearchResultChildrenMember(children);

                    } else if (member is RefToMember refTo) {
                        foreach (var srm in GetMembersRecursively(refTo.RefTo)) {
                            yield return srm;
                        }

                    } else if (member is ChildAggregate child) {
                        foreach (var srm in GetMembersRecursively(child)) {
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
                    ChildrenAggregate children => new SearchResult(children),
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
        internal abstract class SearchResultMember : IInstancePropertyMetadata {
            internal abstract string PhysicalName { get; }
            internal abstract ISchemaPathNode MappingKey { get; }

            internal abstract string RenderDeclaration();

            string IInstancePropertyMetadata.PropertyName => PhysicalName;
            ISchemaPathNode IInstancePropertyMetadata.SchemaPathNode => MappingKey;
        }

        internal class SearchResultValueMember : SearchResultMember, IInstanceValuePropertyMetadata {
            internal SearchResultValueMember(ValueMember valueMember) {
                _valueMember = valueMember;
            }
            private readonly ValueMember _valueMember;

            internal override string PhysicalName => _physicalName ??= _valueMember
                .GetPathFromEntry()
                .Skip(1) // エントリーを除外
                .SinceNearestChildren()
                .SelectPhysicalName()
                .Join("_");

            private string? _physicalName;

            internal override ISchemaPathNode MappingKey => _valueMember;
            IValueMemberType IInstanceValuePropertyMetadata.Type => _valueMember.Type;

            internal override string RenderDeclaration() {
                var type = _valueMember.Type.CsPrimitiveTypeName;

                return $$"""
                    /// <summary>{{_valueMember.DisplayName}}</summary>
                    public {{type}}? {{PhysicalName}} { get; set; }
                    """;
            }
        }

        internal class SearchResultChildrenMember : SearchResultMember, IInstanceStructurePropertyMetadata {
            internal SearchResultChildrenMember(ChildrenAggregate children) {
                _children = children;
            }
            private readonly ChildrenAggregate _children;

            internal override string PhysicalName => _children.PhysicalName;
            internal override ISchemaPathNode MappingKey => _children;

            bool IInstanceStructurePropertyMetadata.IsArray => true;

            IEnumerable<IInstancePropertyMetadata> IInstancePropertyOwnerMetadata.GetMembers() {
                // Childrenの要素型（内部のSearchResult）のメンバーを返す
                return new SearchResult(_children).GetMembers();
            }

            internal override string RenderDeclaration() {
                var sr = new SearchResult(_children);

                return $$"""
                    /// <summary>{{_children.DisplayName}}</summary>
                    public List<{{sr.CsClassName}}> {{PhysicalName}} { get; set; } = new();
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
                    yield return member.PhysicalName;
                    continue;
                }

                // 末端のメンバー
                if (node is ValueMember vm) {
                    var member = new SearchResult.SearchResultValueMember(vm);
                    yield return member.PhysicalName;
                    continue;
                }

                throw new InvalidOperationException("予期しない型");
            }
        }
    }
}
