using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering {
    partial class AggFile : ITemplate {

        internal AggFile(GraphNode<Aggregate> aggregate, CodeRenderingContext ctx) {
            if (!aggregate.IsRoot())
                throw new ArgumentException($"{nameof(AggFile)} requires root aggregate.", nameof(aggregate));

            _aggregate = aggregate;
            _dbEntity = aggregate.GetDbEntity().AsEntry().As<EFCoreEntity>();
            _aggregateInstance = aggregate.GetInstanceClass().AsEntry().As<AggregateInstance>();

            _searchCondition = new SearchCondition(_dbEntity);
            _searchResult = new SearchResult(_dbEntity);
            _controller = new Controller(_aggregate);
            _create = new CreateMethod(_dbEntity, ctx);
            _find = new FindMethod(_dbEntity, ctx);
            _search = new SearchMethod(_dbEntity, ctx);
            _update = new UpdateMethod(_dbEntity, ctx);

            _ctx = ctx;
        }

        private readonly GraphNode<Aggregate> _aggregate;
        private readonly GraphNode<EFCoreEntity> _dbEntity;
        private readonly GraphNode<AggregateInstance> _aggregateInstance;

        private readonly CodeRenderingContext _ctx;

        public string FileName => $"{_aggregate.Item.DisplayName.ToFileNameSafe()}.cs";
        private const string E = "e";

        public const string GEINSTANCEKEY_METHOD_NAME = "GetInstanceKey";

        private IEnumerable<NavigationProperty.Item> EnumerateNavigationProperties(GraphNode<EFCoreEntity> entity) {
            foreach (var nav in entity.GetNavigationProperties(_ctx.Config)) {
                if (nav.Principal.Owner == entity) yield return nav.Principal;
                if (nav.Relevant.Owner == entity) yield return nav.Relevant;
            }
        }

        #region CREATE
        private readonly CreateMethod _create;
        internal class CreateMethod {
            internal CreateMethod(GraphNode<EFCoreEntity> dbEntity, CodeRenderingContext ctx) {
                _dbEntity = dbEntity;
                _instance = dbEntity.GetUiInstance().Item;
                _ctx = ctx;
            }

            private readonly GraphNode<EFCoreEntity> _dbEntity;
            private readonly AggregateInstance _instance;
            private readonly CodeRenderingContext _ctx;

            internal string ArgType => _instance.ClassName;
            internal string MethodName => $"Create{_dbEntity.GetCorrespondingAggregate().Item.DisplayName.ToCSharpSafe()}";
        }
        #endregion CREATE

        #region FIND
        private readonly FindMethod _find;
        internal class FindMethod {
            internal FindMethod(GraphNode<EFCoreEntity> dbEntity, CodeRenderingContext ctx) {
                _dbEntity = dbEntity;
                _instance = dbEntity.GetUiInstance().Item;
                _ctx = ctx;
            }

            private readonly GraphNode<EFCoreEntity> _dbEntity;
            private readonly AggregateInstance _instance;
            private readonly CodeRenderingContext _ctx;

            internal string ReturnType => _instance.ClassName;
            internal string MethodName => $"Find{_dbEntity.GetCorrespondingAggregate().Item.DisplayName.ToCSharpSafe()}";
            internal string DbSetName => _dbEntity.Item.DbSetName;
            internal string AggregateInstanceTypeFullName => $"{_ctx.Config.RootNamespace}.{_instance.ClassName}";

            internal IEnumerable<string> Include() {
                var entities = new HashSet<GraphNode>();
                void Collect(GraphNode<EFCoreEntity> entity) {
                    entities.Add(entity);
                    if (entity.Source == null || !entity.Source.IsRef()) {
                        foreach (var child in entity.GetChildMembers()) Collect(child.Terminal);
                        foreach (var child in entity.GetChildrenMembers()) Collect(child.Terminal);
                        foreach (var refTarget in entity.GetRefMembers()) Collect(refTarget.Terminal);
                    }
                }
                Collect(_dbEntity);

                foreach (var entity in entities) {
                    foreach (var edge in entity.PathFromEntry()) {
                        yield return edge.Source.IsRoot()
                            ? $".Include(x => x.{edge.RelationName})"
                            : $".ThenInclude(x => x.{edge.RelationName})";
                    }
                }
            }

            internal IEnumerable<string> SingleOrDefault(string paramName) {
                var keys = _dbEntity
                    .GetColumns()
                    .Where(col => col.IsPrimary)
                    .ToArray();

                for (int i = 0; i < keys.Length; i++) {
                    var col = keys[i].PropertyName;
                    var cast = keys[i].CSharpTypeName;
                    var close = i == keys.Length - 1 ? ");" : "";

                    if (i == 0) {
                        yield return $".SingleOrDefault(x => x.{col} == ({cast}){paramName}[{i}]{close}";
                    } else {
                        yield return $"                   && x.{col} == ({cast}){paramName}[{i}]{close}";
                    }
                }
            }
        }
        #endregion FIND


        #region UPDATE
        private readonly UpdateMethod _update;
        internal class UpdateMethod {
            internal UpdateMethod(GraphNode<EFCoreEntity> dbEntity, CodeRenderingContext ctx) {
                _dbEntity = dbEntity;
                _instance = dbEntity.GetUiInstance().Item;
                _ctx = ctx;
            }

            private readonly GraphNode<EFCoreEntity> _dbEntity;
            private readonly AggregateInstance _instance;
            private readonly CodeRenderingContext _ctx;

            internal string ArgType => _instance.ClassName;
            internal string MethodName => $"Update{_dbEntity.GetCorrespondingAggregate().Item.DisplayName.ToCSharpSafe()}";

            internal void RenderDescendantsAttaching(ITemplate template, string dbContext, string before, string after) {
                var descendantDbEntities = _dbEntity.EnumerateDescendants().ToArray();
                for (int i = 0; i < descendantDbEntities.Length; i++) {
                    var paths = descendantDbEntities[i].PathFromEntry().ToArray();

                    // before, after それぞれの子孫インスタンスを一次配列に格納する
                    void RenderEntityArray(bool renderBefore) {
                        if (paths.Any(path => path.IsChildren())) {
                            // 子集約までの経路の途中に配列が含まれる場合
                            template.WriteLine($"var arr{i}_{(renderBefore ? "before" : "after")} = {(renderBefore ? before : after)}");

                            var select = false;
                            foreach (var path in paths) {
                                if (select && path.IsChildren()) {
                                    template.WriteLine($"    .SelectMany(x => x.{path.RelationName})");
                                } else if (select) {
                                    template.WriteLine($"    .Select(x => x.{path.RelationName})");
                                } else {
                                    template.WriteLine($"    .{path.RelationName}");
                                    if (path.IsChildren()) select = true;
                                }
                            }
                            template.WriteLine($"    .ToArray();");

                        } else {
                            // 子集約までの経路の途中に配列が含まれない場合
                            template.WriteLine($"var arr{i}_{(renderBefore ? "before" : "after")} = new {descendantDbEntities[i].Item.ClassName}[] {{");
                            template.WriteLine($"    {(renderBefore ? before : after)}.{paths.Select(p => p.RelationName).Join(".")},");
                            template.WriteLine($"}};");
                        }
                    }
                    RenderEntityArray(true);
                    RenderEntityArray(false);

                    // ChangeState変更
                    template.WriteLine($"foreach (var a in arr{i}_after) {{");
                    template.WriteLine($"    var b = arr{i}_before.SingleOrDefault(b => b.{EFCoreEntity.KEYEQUALS}(a));");
                    template.WriteLine($"    if (b == null) {{");
                    template.WriteLine($"        {dbContext}.Entry(a).State = EntityState.Added;");
                    template.WriteLine($"    }} else {{");
                    template.WriteLine($"        {dbContext}.Entry(b).State = EntityState.Detached;");
                    template.WriteLine($"        {dbContext}.Entry(a).State = EntityState.Modified;");
                    template.WriteLine($"    }}");
                    template.WriteLine($"}}");

                    template.WriteLine($"foreach (var b in arr{i}_before) {{");
                    template.WriteLine($"    var a = arr{i}_after.SingleOrDefault(a => a.{EFCoreEntity.KEYEQUALS}(b));");
                    template.WriteLine($"    if (a == null) {{");
                    template.WriteLine($"        {dbContext}.Entry(b).State = EntityState.Deleted;");
                    template.WriteLine($"    }}");
                    template.WriteLine($"}}");
                }
            }
        }
        #endregion UPDATE


        #region SEARCH
        private readonly SearchCondition _searchCondition;
        private readonly SearchResult _searchResult;
        private readonly SearchMethod _search;
        internal class SearchMethod {
            internal SearchMethod(GraphNode<EFCoreEntity> dbEntity, CodeRenderingContext ctx) {
                _dbEntity = dbEntity;
                _ctx = ctx;
            }

            internal const string PARAM = "param";
            internal const string QUERY = "query";
            internal const string E = "e";
            internal static string PAGE => SearchCondition.PAGE_PROP_NAME;

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
        #endregion SEARCH


        #region AGGREGATE INSTANCE
        private void ToDbEntity() {

            void WriteBody(GraphNode<AggregateInstance> instance, string right) {
                foreach (var prop in instance.GetSchalarProperties(_ctx.Config)) {
                    var path = new[] { right }
                        .Concat(instance.PathFromEntry().Select(x => x.RelationName.ToCSharpSafe()))
                        .Concat(new[] { prop.CorrespondingDbColumn.PropertyName })
                        .Join(".");
                    WriteLine($"{prop.PropertyName} = {path},");
                }

                foreach (var child in instance.GetChildAggregateProperties(_ctx.Config)) {
                    var childProp = child.CorrespondingNavigationProperty.Principal.PropertyName;
                    var childDbEntity = $"{_ctx.Config.EntityNamespace}.{child.CorrespondingNavigationProperty.Relevant.Owner.Item.ClassName}";
                    if (child.Multiple) {
                        WriteLine($"{child.PropertyName} = this.{childProp}.Select(x => new {childDbEntity} {{");
                        PushIndent("    ");
                        WriteBody(child.ChildAggregateInstance.AsEntry(), "x");
                        PopIndent();
                        WriteLine($"}}).ToList(),");

                    } else {
                        WriteLine($"{child.PropertyName} = new {childDbEntity} {{");
                        PushIndent("    ");
                        WriteBody(child.ChildAggregateInstance, "this");
                        PopIndent();
                        WriteLine($"}},");
                    }
                }
            }

            WriteLine($"return new {_ctx.Config.EntityNamespace}.{_dbEntity.Item.ClassName} {{");
            PushIndent("    ");
            WriteBody(_aggregateInstance, "this");
            PopIndent();
            WriteLine($"}};");
        }
        private void FromDbEntity() {

            void WriteBody(GraphNode<AggregateInstance> instance, string right) {
                foreach (var prop in instance.GetSchalarProperties(_ctx.Config)) {
                    var path = new[] { right }
                        .Concat(instance.PathFromEntry().Select(x => x.RelationName.ToCSharpSafe()))
                        .Concat(new[] { prop.CorrespondingDbColumn.PropertyName })
                        .Join(".");
                    WriteLine($"{prop.PropertyName} = {path},");
                }

                foreach (var child in instance.GetChildAggregateProperties(_ctx.Config)) {
                    if (child.Multiple) {
                        WriteLine($"{child.PropertyName} = {right}.{child.PropertyName}.Select(x => new {child.ChildAggregateInstance.Item.ClassName} {{");
                        PushIndent("    ");
                        WriteBody(child.ChildAggregateInstance.AsEntry(), "x");
                        PopIndent();
                        WriteLine($"}}).ToList(),");

                    } else {
                        WriteLine($"{child.PropertyName} = new {child.ChildAggregateInstance.Item.ClassName} {{");
                        PushIndent("    ");
                        WriteBody(child.ChildAggregateInstance, E);
                        PopIndent();
                        WriteLine($"}},");
                    }
                }
            }

            WriteLine($"return new {_aggregateInstance.Item.ClassName} {{");
            PushIndent("    ");
            WriteBody(_aggregateInstance, E);
            PopIndent();
            WriteLine($"}};");
        }
        #endregion AGGREGATE INSTANCE


        #region CONTROLLER
        private readonly Controller _controller;
        internal class Controller {
            internal Controller(GraphNode<Aggregate> aggregate) {
                _aggregate = aggregate;
            }

            private readonly GraphNode<Aggregate> _aggregate;

            internal string ClassName => $"{_aggregate.Item.DisplayName.ToCSharpSafe()}Controller";

            internal const string SEARCH_ACTION_NAME = "list";
            internal const string CREATE_ACTION_NAME = "create";
            internal const string UPDATE_ACTION_NAME = "update";
            internal const string FIND_ACTION_NAME = "detail";
            internal const string KEYWORDSEARCH_ACTION_NAME = "list-by-keyword";

            internal const string SUBDOMAIN = "api";

            internal string SubDomain => $"{SUBDOMAIN}/{_aggregate.Item.DisplayName.ToCSharpSafe()}";
            internal string SearchCommandApi => $"/{SubDomain}/{SEARCH_ACTION_NAME}";
            internal string CreateCommandApi => $"/{SubDomain}/{CREATE_ACTION_NAME}";
            internal string UpdateCommandApi => $"/{SubDomain}/{UPDATE_ACTION_NAME}";
            internal string KeywordSearchCommandApi => $"/{SubDomain}/{KEYWORDSEARCH_ACTION_NAME}";
        }
        #endregion CONTROLLER
    }
}
