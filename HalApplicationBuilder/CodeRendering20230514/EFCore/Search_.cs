using HalApplicationBuilder.Core20230514;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering20230514.EFCore {
    partial class Search {
        internal Search(CodeRenderingContext ctx) {
            _ctx = ctx;
            _graph = ctx.Schema.ToEFCoreGraph();
        }
        private readonly CodeRenderingContext _ctx;

        /// <summary>method argument</summary>
        private const string PARAM = "param";
        /// <summary>IQueryable created from DbSet</summary>
        private const string QUERY = "query";
        /// <summary>lambda expression argument</summary>
        private const string E = "e";

        private class SearchMethod {
            public required string ReturnType { get; init; }
            public required string ReturnItemType { get; init; }
            public required string MethodName { get; init; }
            public required string ArgType { get; init; }
            public required string DbSetName { get; init; }
            public required IEnumerable<string> SelectClause { get; init; }
            public required IEnumerable<string> WhereClause { get; init; }
        }

        private readonly DirectedGraph<EFCoreEntity> _graph;
        private IEnumerable<SearchMethod> BuildSearchMethods() {

            // ------------------- SELECT句 -------------------
            IEnumerable<string> BuildSelectClauseRecursively(GraphNode<EFCoreEntity> dbEntity) {
                var childOrRef = dbEntity as NeighborNode<EFCoreEntity>;
                var path = childOrRef != null
                    ? childOrRef.PathFromEntry().Select(edge => edge.RelationName).ToArray()
                    : Array.Empty<string>();

                // 集約自身のメンバー
                foreach (var member in dbEntity.Item.Source.Members) {
                    // 参照の場合はインスタンス名のみSELECTする
                    if (childOrRef != null && !(bool)childOrRef.Source.Attributes[AppSchema.REL_ATTR_IS_INSTANCE_NAME]) continue;

                    var pathToMember = path.Union(new[] { member.Name });
                    yield return $"{string.Join("_", pathToMember)} = {E}.{string.Join(".", pathToMember)},";
                }

                // 子要素（除: Children）と参照先を再帰処理
                foreach (var edge in dbEntity.Out) {
                    if ((string)edge.Attributes[AppSchema.REL_ATTR_RELATION_TYPE] == AppSchema.REL_ATTRVALUE_REFERENCE
                        || (string)edge.Attributes[AppSchema.REL_ATTR_RELATION_TYPE] == AppSchema.REL_ATTRVALUE_PARENT_CHILD
                        && !edge.Attributes.ContainsKey(AppSchema.REL_ATTR_MULTIPLE)) {

                        foreach (var line in BuildSelectClauseRecursively(edge.Terminal)) {
                            yield return line;
                        }
                    }
                }
            }

            // ------------------- WHERE句 -------------------
            IEnumerable<string> BuildWhereClauseRecursively(SearchCondition searchCondition, GraphNode<EFCoreEntity> dbEntity) {
                var childOrRef = dbEntity as NeighborNode<EFCoreEntity>;
                var path = childOrRef != null
                    ? childOrRef.PathFromEntry().Select(edge => edge.RelationName).ToArray()
                    : Array.Empty<string>();

                foreach (var scMember in searchCondition.GetMembers()) {
                    var pathToMember = string.Join(".", path.Union(new[] { scMember.CorrespondingDbMember.PropertyName }));

                    switch (scMember.Type.SearchBehavior) {
                        case SearchBehavior.Ambiguous:
                            // 検索挙動がAmbiguousの場合はプロパティの型はstringで決め打ち
                            yield return $"if (!string.IsNullOrWhiteSpace({PARAM}.{scMember.Name})) {{";
                            yield return $"    var trimmed = {PARAM}.{scMember.Name}.Trim();";
                            yield return $"    {QUERY} = {QUERY}.Where(x => x.{pathToMember}.Contains(trimmed));";
                            yield return $"}}";
                            break;

                        case SearchBehavior.Range:
                            yield return $"if ({PARAM}.{scMember.Name}.{Util.FromTo.FROM} != default)";
                            yield return $"    {QUERY} = {QUERY}.Where(x => x.{pathToMember} >= {PARAM}.{scMember.Name}.{Util.FromTo.FROM});";
                            yield return $"if ({PARAM}.{scMember.Name}.{Util.FromTo.TO} != default)";
                            yield return $"    {QUERY} = {QUERY}.Where(x => x.{pathToMember} <= {PARAM}.{scMember.Name}.{Util.FromTo.TO});";
                            break;

                        case SearchBehavior.Strict:
                        default:
                            yield return $"if ({PARAM}.{scMember.Name} != default)";
                            yield return $"    {QUERY} = {QUERY}.Where(x => x.{pathToMember} == {PARAM}.{scMember.Name});";
                            break;
                    }
                }
            }

            // ------------------- その他 -------------------
            foreach (var dbEntity in _graph) {
                // ルート集約以外なら処理中断
                if (dbEntity.In.Any(edge => (string)edge.Attributes[AppSchema.REL_ATTR_RELATION_TYPE] == AppSchema.REL_ATTRVALUE_PARENT_CHILD)) {
                    continue;
                }

                var name = dbEntity.Item.Source.DisplayName.ToCSharpSafe();
                var searchResult = $"{_ctx.Config.EntityNamespace}.{name}SearchResult";
                var searchConditionClass = new SearchCondition(dbEntity);

                yield return new SearchMethod {
                    MethodName = $"Search{name}",

                    ReturnType = $"IEnumerable<{searchResult}>",
                    ReturnItemType = searchResult,
                    ArgType = $"{_ctx.Config.EntityNamespace}.{name}SearchCondition",

                    DbSetName = name,

                    SelectClause = BuildSelectClauseRecursively(dbEntity),
                    WhereClause = BuildWhereClauseRecursively(searchConditionClass, dbEntity),
                };
            }
        }
    }
}
