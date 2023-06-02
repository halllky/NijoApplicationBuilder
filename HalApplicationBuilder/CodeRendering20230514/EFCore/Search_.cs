using HalApplicationBuilder.CodeRendering20230514.Util;
using HalApplicationBuilder.Core20230514;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering20230514.EFCore {
    partial class Search : ITemplate {
        internal Search(CodeRenderingContext ctx) {
            _ctx = ctx;
            _graph = ctx.Schema.ToEFCoreGraph();
        }
        private readonly CodeRenderingContext _ctx;

        private const string PARAM = "param";
        private const string QUERY = "query";
        private const string E = "e";
        private static string PAGE => SearchCondition.PAGE_PROP_NAME;

        private readonly DirectedGraph<EFCoreEntity> _graph;

        public string FileName => $"{_ctx.Config.DbContextName.ToFileNameSafe()}.Search.cs";

        private IEnumerable<MethodInfo> BuildSearchMethods() {
            return _graph
                .Where(dbEntity => dbEntity.IsRoot())
                .Select(dbEntity => new MethodInfo(dbEntity, _ctx));
        }

        internal class MethodInfo {
            internal MethodInfo(GraphNode<EFCoreEntity> dbEntity, CodeRenderingContext ctx) {
                _dbEntity = dbEntity;
                _ctx = ctx;
            }

            private readonly GraphNode<EFCoreEntity> _dbEntity;
            private readonly CodeRenderingContext _ctx;

            internal string ReturnType => $"IEnumerable<{ReturnItemType}>";
            internal string ReturnItemType => $"{_ctx.Config.RootNamespace}.{new SearchResult(_dbEntity).ClassName}";
            internal string MethodName => $"Search{_dbEntity.Item.Aggregate.Item.DisplayName.ToCSharpSafe()}";
            internal string ArgType => $"{_ctx.Config.RootNamespace}.{new SearchCondition(_dbEntity).ClassName}";
            internal string DbSetName => _dbEntity.Item.Aggregate.Item.DisplayName.ToCSharpSafe();

            internal IEnumerable<string> SelectClause() {
                // Instance Key
                var pk = _dbEntity.Item
                    .GetColumns()
                    .Where(col => col.IsPrimary)
                    .ToArray();
                for (int i = 0; i < pk.Length; i++) {
                    yield return $"__halapp_Key_{i} = {E}.{pk[i].PropertyName},";
                }

                // Instance Key 以外
                foreach (var x in BuildSelectClauseRecursively(_dbEntity)) {
                    yield return $"{x.Left} = {E}.{x.Right},";
                }
            }
            private class SelectClauseLine {
                internal required string Left { get; init; }
                internal required string Right { get; init; }
            }
            private static IEnumerable<SelectClauseLine> BuildSelectClauseRecursively(GraphNode<EFCoreEntity> dbEntity) {
                var path = dbEntity.PathFromEntry().Select(edge => edge.RelationName).ToArray();

                // 集約自身のメンバー
                foreach (var member in dbEntity.Item.Aggregate.Item.Members) {
                    // 参照の場合はインスタンス名のみSELECTする
                    if (dbEntity.Source != null
                        && dbEntity.Source.IsRef()
                        && !member.IsInstanceName) continue;

                    var pathToMember = path.Union(new[] { member.Name });
                    yield return new SelectClauseLine {
                        Left = string.Join("_", pathToMember),
                        Right = string.Join(".", pathToMember),
                    };
                }

                // 子要素（除: Children）と参照先を再帰処理
                foreach (var edge in dbEntity.GetChildMembers()) {
                    foreach (var line in BuildSelectClauseRecursively(edge.Terminal)) yield return line;
                }
                foreach (var edge in dbEntity.GetRefMembers()) {
                    foreach (var line in BuildSelectClauseRecursively(edge.Terminal)) yield return line;
                }
            }

            internal IEnumerable<string> WhereClause() {
                return BuildWhereClauseRecursively(new SearchCondition(_dbEntity), _dbEntity);
            }
            private static IEnumerable<string> BuildWhereClauseRecursively(SearchCondition searchCondition, GraphNode<EFCoreEntity> dbEntity) {
                var path = dbEntity.PathFromEntry().Select(edge => edge.RelationName).ToArray();

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
                            var type = scMember.Type.GetCSharpTypeName();
                            if (type == "string" || type == "string?") {
                                yield return $"if (!string.IsNullOrWhiteSpace({PARAM}.{scMember.Name}))";
                                yield return $"    {QUERY} = {QUERY}.Where(x => x.{pathToMember} == {PARAM}.{scMember.Name});";
                            } else {
                                yield return $"if ({PARAM}.{scMember.Name} != default)";
                                yield return $"    {QUERY} = {QUERY}.Where(x => x.{pathToMember} == {PARAM}.{scMember.Name});";
                            }
                            break;
                    }
                }
            }

            internal IEnumerable<string> EnumerableSection() {
                // Instance Key
                var pk = _dbEntity.Item
                    .GetColumns()
                    .Where(col => col.IsPrimary);

                yield return $"{SearchResult.INSTANCE_KEY_PROP_NAME} = {InstanceKey.CLASS_NAME}.{InstanceKey.CREATE}(new object?[] {{";
                for (int i = 0; i < pk.Count(); i++) {
                    yield return $"    {E}.__halapp_Key_{i},";
                }
                yield return $"}}),";

                // Instance Key 以外
                foreach (var x in BuildSelectClauseRecursively(_dbEntity)) {
                    yield return $"{x.Left} = {E}.{x.Left},";
                }
            }
        }
    }
}
