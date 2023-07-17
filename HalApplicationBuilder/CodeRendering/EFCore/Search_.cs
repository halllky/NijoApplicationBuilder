using HalApplicationBuilder.CodeRendering.Util;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.EFCore {
    partial class Search : ITemplate {
        internal Search(CodeRenderingContext ctx) {
            _ctx = ctx;
        }
        private readonly CodeRenderingContext _ctx;

        private const string PARAM = "param";
        private const string QUERY = "query";
        private const string E = "e";
        private static string PAGE => SearchCondition.PAGE_PROP_NAME;

        public string FileName => $"{_ctx.Config.DbContextName.ToFileNameSafe()}.Search.cs";

        private IEnumerable<MethodInfo> BuildSearchMethods() {
            return _ctx.Schema
                .ToEFCoreGraph()
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
            internal string MethodName => $"Search{_dbEntity.GetCorrespondingAggregate().Item.DisplayName.ToCSharpSafe()}";
            internal string ArgType => $"{_ctx.Config.RootNamespace}.{new SearchCondition(_dbEntity).ClassName}";
            internal string DbSetName => _dbEntity.GetCorrespondingAggregate().Item.DisplayName.ToCSharpSafe();

            internal IEnumerable<string> SelectClause() {
                foreach (var m in new SearchResult(_dbEntity).GetMembers()) {
                    var columnName = m.CorrespondingDbMemberOwner
                        .PathFromEntry()
                        .Select(edge => edge.RelationName)
                        .Union(new[] { m.CorrespondingDbMember.PropertyName })
                        .Join(".");
                    yield return $"{m.Name} = {E}.{columnName},";
                }
            }

            internal IEnumerable<string> WhereClause() {
                return BuildWhereClauseRecursively(_dbEntity);
            }
            private static IEnumerable<string> BuildWhereClauseRecursively(GraphNode<EFCoreEntity> dbEntity) {
                var searchCondition = new SearchCondition(dbEntity);
                var searchResult = new SearchResult(dbEntity);

                foreach (var cond in searchCondition.GetMembers()) {
                    var res = searchResult.GetMembers().Single(m => m.CorrespondingDbMember == cond.CorrespondingDbMember);

                    switch (cond.Type.SearchBehavior) {
                        case SearchBehavior.Ambiguous:
                            // 検索挙動がAmbiguousの場合はプロパティの型はstringで決め打ち
                            yield return $"if (!string.IsNullOrWhiteSpace({PARAM}.{cond.Name})) {{";
                            yield return $"    var trimmed = {PARAM}.{cond.Name}.Trim();";
                            yield return $"    {QUERY} = {QUERY}.Where(x => x.{res.Name}.Contains(trimmed));";
                            yield return $"}}";
                            break;

                        case SearchBehavior.Range:
                            yield return $"if ({PARAM}.{cond.Name}.{Util.FromTo.FROM} != default) {{";
                            yield return $"    {QUERY} = {QUERY}.Where(x => x.{res.Name} >= {PARAM}.{cond.Name}.{Util.FromTo.FROM});";
                            yield return $"}}";
                            yield return $"if ({PARAM}.{cond.Name}.{Util.FromTo.TO} != default) {{";
                            yield return $"    {QUERY} = {QUERY}.Where(x => x.{res.Name} <= {PARAM}.{cond.Name}.{Util.FromTo.TO});";
                            yield return $"}}";
                            break;

                        case SearchBehavior.Strict:
                        default:
                            var type = cond.Type.GetCSharpTypeName();
                            if (type == "string" || type == "string?") {
                                yield return $"if (!string.IsNullOrWhiteSpace({PARAM}.{cond.Name})) {{";
                                yield return $"    {QUERY} = {QUERY}.Where(x => x.{res.Name} == {PARAM}.{cond.Name});";
                                yield return $"}}";
                            } else {
                                yield return $"if ({PARAM}.{cond.Name} != default) {{";
                                yield return $"    {QUERY} = {QUERY}.Where(x => x.{res.Name} == {PARAM}.{cond.Name});";
                                yield return $"}}";
                            }
                            break;
                    }
                }
            }

            internal IEnumerable<string> EnumerateKeys() {
                return new SearchResult(_dbEntity)
                    .GetMembers()
                    .Where(member => member.IsKey)
                    .Select(member => member.Name);
            }
            internal string? GetInstanceNamePropName() {
                return new SearchResult(_dbEntity)
                    .GetMembers()
                    .SingleOrDefault(member => member.IsName)?
                    .Name;
            }
        }
    }
}
