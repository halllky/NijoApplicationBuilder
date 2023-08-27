using HalApplicationBuilder.CodeRendering.Presentation;
using HalApplicationBuilder.CodeRendering.Util;
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
            _delete = new DeleteMethod(_dbEntity, ctx);

            _ctx = ctx;
        }


        private readonly GraphNode<Aggregate> _aggregate;
        private readonly GraphNode<EFCoreEntity> _dbEntity;
        private readonly GraphNode<AggregateInstance> _aggregateInstance;

        private readonly CodeRenderingContext _ctx;

        public string FileName => $"{_aggregate.Item.DisplayName.ToFileNameSafe()}.cs";
        private const string E = "e";

        public const string GETINSTANCEKEY_METHOD_NAME = "GetInstanceKey";
        public const string GETINSTANCENAME_METHOD_NAME = "GetInstanceName";
        public const string TOKEYNAMEPAIR_METHOD_NAME = "ToKeyNamePair";

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

            [Obsolete]
            internal IEnumerable<string> Include() {
                var entities = new HashSet<GraphNode>();
                void Collect(GraphNode<EFCoreEntity> entity) {
                    entities.Add(entity);
                    if (entity.Source == null || !entity.Source.IsRef()) {
                        foreach (var child in entity.GetChildMembers()) Collect(child.Terminal);
                        foreach (var child in entity.GetChildrenMembers()) Collect(child.Terminal);
                        foreach (var child in entity.GetVariationGroups().SelectMany(group => group.VariationAggregates.Values)) Collect(child.Terminal);
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

            [Obsolete]
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
        private void RenderDbEntityLoading(string entityVarName, string serializedInstanceKeyVarName, bool tracks) {
            // Include
            var descendants = new HashSet<GraphNode>();
            void Collect(GraphNode<EFCoreEntity> entity) {
                descendants.Add(entity);
                if (entity.Source == null || !entity.Source.IsRef()) {
                    foreach (var child in entity.GetChildMembers()) Collect(child.Terminal);
                    foreach (var child in entity.GetChildrenMembers()) Collect(child.Terminal);
                    foreach (var child in entity.GetVariationGroups().SelectMany(group => group.VariationAggregates.Values)) Collect(child.Terminal);
                    foreach (var refTarget in entity.GetRefMembers()) Collect(refTarget.Terminal);
                }
            }
            Collect(_dbEntity);

            // Rendering
            WriteLine($"var instanceKey = {InstanceKey.CLASS_NAME}.{InstanceKey.PARSE}({serializedInstanceKeyVarName});");
            WriteLine($"var {entityVarName} = this.{_dbEntity.Item.DbSetName}");

            if (tracks == false) {
                WriteLine($"    .AsNoTracking()");
            }

            foreach (var edge in descendants.SelectMany(entity => entity.PathFromEntry())) {
                if (edge.Source.IsRoot()) {
                    WriteLine($"    .Include(x => x.{edge.RelationName})");
                } else {
                    WriteLine($"    .ThenInclude(x => x.{edge.RelationName})");
                }
            }

            var keys = _dbEntity.GetColumns().Where(col => col.IsPrimary).ToArray();
            for (int i = 0; i < keys.Length; i++) {
                var col = keys[i].PropertyName;
                var cast = keys[i].CSharpTypeName;
                var close = i == keys.Length - 1 ? ");" : "";
                if (i == 0) {
                    WriteLine($"    .SingleOrDefault(x => x.{col} == ({cast})instanceKey.{InstanceKey.OBJECT_ARRAY}[{i}]{close}");
                } else {
                    WriteLine($"                       && x.{col} == ({cast})instanceKey.{InstanceKey.OBJECT_ARRAY}[{i}]{close}");
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

            internal string MethodName => $"Update{_dbEntity.GetCorrespondingAggregate().Item.DisplayName.ToCSharpSafe()}";

            internal void RenderDescendantsAttaching(ITemplate template, string dbContext, string before, string after) {
                var descendantDbEntities = _dbEntity.EnumerateDescendants().ToArray();
                for (int i = 0; i < descendantDbEntities.Length; i++) {
                    var paths = descendantDbEntities[i].PathFromEntry().ToArray();

                    // before, after それぞれの子孫インスタンスを一次配列に格納する
                    void RenderEntityArray(bool renderBefore) {
                        if (paths.Any(path => path.Terminal.IsChildrenMember())) {
                            // 子集約までの経路の途中に配列が含まれる場合
                            template.WriteLine($"var arr{i}_{(renderBefore ? "before" : "after")} = {(renderBefore ? before : after)}");

                            var select = false;
                            foreach (var path in paths) {
                                if (select && path.Terminal.IsChildrenMember()) {
                                    template.WriteLine($"    .SelectMany(x => x.{path.RelationName})");
                                } else if (select) {
                                    template.WriteLine($"    .Select(x => x.{path.RelationName})");
                                } else {
                                    template.WriteLine($"    .{path.RelationName}");
                                    if (path.Terminal.IsChildrenMember()) select = true;
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


        #region DELETE
        private readonly DeleteMethod _delete;
        internal class DeleteMethod {
            internal DeleteMethod(GraphNode<EFCoreEntity> dbEntity, CodeRenderingContext ctx) {
                _dbEntity = dbEntity;
                _instance = dbEntity.GetUiInstance().Item;
                _ctx = ctx;
            }

            private readonly GraphNode<EFCoreEntity> _dbEntity;
            private readonly AggregateInstance _instance;
            private readonly CodeRenderingContext _ctx;

            internal string MethodName => $"Delete{_dbEntity.GetCorrespondingAggregate().Item.DisplayName.ToCSharpSafe()}";
        }
        #endregion DELETE


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


        #region LIST BY KEYWORD
        private const int LIST_BY_KEYWORD_MAX = 100;
        private string ListByKeywordMethodName => $"SearchByKeyword{_dbEntity.GetCorrespondingAggregate().Item.DisplayName.ToCSharpSafe()}";
        private IEnumerable<ListByKeywordTargetColumn> EnumerateListByKeywordTargetColumns() {
            return _dbEntity
                .GetColumns()
                .Where(col => col.IsPrimary || col.IsInstanceName)
                .Select(col => new ListByKeywordTargetColumn {
                    Name = col.PropertyName,
                    NameAsString = col.CSharpTypeName.Contains("string")
                        ? col.PropertyName
                        : $"{col.PropertyName}.ToString()",
                    Path = col.Owner
                        .PathFromEntry()
                        .Select(edge => edge.RelationName)
                        .Concat(new[] { col.PropertyName })
                        .Join("."),
                    IsInstanceKey = col.IsPrimary,
                    IsInstanceName = col.IsInstanceName,
                });
        }
        private class ListByKeywordTargetColumn {
            internal required string Path { get; init; }
            internal required string Name { get; init; }
            internal required string NameAsString { get; init; }
            internal required bool IsInstanceKey { get; init; }
            internal required bool IsInstanceName { get; init; }
        }
        #endregion LIST BY KEYWORD


        #region AGGREGATE INSTANCE & CREATE COMMAND
        private string CreateCommandClassName => $"{_aggregate.Item.DisplayName.ToCSharpSafe()}CreateCommand";
        private string CreateCommandToDbEntityMethodName => AggregateInstance.TO_DB_ENTITY_METHOD_NAME;
        private string CreateCommandGetInstanceKeyMethodName => GETINSTANCENAME_METHOD_NAME;

        private void ToDbEntity() {

            void WriteBody(GraphNode<AggregateInstance> instance, string parentPath, string instancePath, int depth) {
                // 親のPK
                var parent = instance.GetParent()?.Initial;
                if (parent != null) {
                    var parentPkColumns = instance
                        .GetDbEntity()
                        .GetColumns()
                        .Where(col => col.IsPrimary && col.CorrespondingParentColumn != null);
                    foreach (var col in parentPkColumns) {
                        WriteLine($"{col.PropertyName} = {parentPath}.{col.CorrespondingParentColumn!.PropertyName},");
                    }
                }
                // 自身のメンバー
                foreach (var prop in instance.GetSchalarProperties(_ctx.Config)) {
                    WriteLine($"{prop.CorrespondingDbColumn.PropertyName} = {instancePath}.{prop.PropertyName},");
                }
                // Ref
                foreach (var prop in instance.GetRefProperties(_ctx.Config)) {
                    for (int i = 0; i < prop.CorrespondingDbColumns.Length; i++) {
                        var col = prop.CorrespondingDbColumns[i];
                        WriteLine($"{col.PropertyName} = ({col.CSharpTypeName}){InstanceKey.CLASS_NAME}.{InstanceKey.PARSE}({instancePath}.{prop.PropertyName}.{AggregateInstanceKeyNamePair.KEY}).{InstanceKey.OBJECT_ARRAY}[{i}],");
                    }
                }
                // 子要素
                foreach (var child in instance.GetChildrenProperties(_ctx.Config)) {
                    var childProp = child.CorrespondingNavigationProperty.Principal.PropertyName;
                    var childDbEntity = $"{_ctx.Config.EntityNamespace}.{child.CorrespondingNavigationProperty.Relevant.Owner.Item.ClassName}";

                    WriteLine($"{child.PropertyName} = this.{childProp}.Select(x{depth} => new {childDbEntity} {{");
                    PushIndent("    ");
                    WriteBody(child.ChildAggregateInstance.AsEntry(), instancePath, $"x{depth}", depth + 1);
                    PopIndent();
                    WriteLine($"}}).ToList(),");
                }
                foreach (var child in instance.GetChildProperties(_ctx.Config)) {
                    var childProp = child.CorrespondingNavigationProperty.Principal.PropertyName;
                    var childDbEntity = $"{_ctx.Config.EntityNamespace}.{child.CorrespondingNavigationProperty.Relevant.Owner.Item.ClassName}";

                    WriteLine($"{child.PropertyName} = new {childDbEntity} {{");
                    PushIndent("    ");
                    WriteBody(child.ChildAggregateInstance, instancePath, $"{instancePath}.{child.ChildAggregateInstance.Source!.RelationName}", depth + 1);
                    PopIndent();
                    WriteLine($"}},");
                }
                foreach (var child in instance.GetVariationProperties(_ctx.Config)) {
                    var childProp = child.CorrespondingNavigationProperty.Principal.PropertyName;
                    var childDbEntity = $"{_ctx.Config.EntityNamespace}.{child.CorrespondingNavigationProperty.Relevant.Owner.Item.ClassName}";

                    WriteLine($"{child.PropertyName} = new {childDbEntity} {{");
                    PushIndent("    ");
                    WriteBody(child.ChildAggregateInstance, instancePath, $"{instancePath}.{child.ChildAggregateInstance.Source!.RelationName}", depth + 1);
                    PopIndent();
                    WriteLine($"}},");
                }
            }

            WriteLine($"return new {_ctx.Config.EntityNamespace}.{_dbEntity.Item.ClassName} {{");
            PushIndent("    ");
            WriteBody(_aggregateInstance, "", "this", 0);
            PopIndent();
            WriteLine($"}};");
        }
        private void FromDbEntity() {

            void WriteBody(GraphNode<AggregateInstance> instance, string parentPath, string instancePath, int depth) {
                // 自身のメンバー
                foreach (var prop in instance.GetSchalarProperties(_ctx.Config)) {
                    WriteLine($"{prop.PropertyName} = {instancePath}.{prop.CorrespondingDbColumn.PropertyName},");
                }
                // Ref
                foreach (var prop in instance.GetRefProperties(_ctx.Config)) {
                    WriteLine($"{prop.PropertyName} = {prop.RefTarget.Item.ClassName}.{AggregateInstance.FROM_DB_ENTITY_METHOD_NAME}({instancePath}.{prop.CorrespondingNavigationProperty.Relevant.PropertyName}).{TOKEYNAMEPAIR_METHOD_NAME}(),");
                }
                // 子要素
                foreach (var child in instance.GetChildrenProperties(_ctx.Config)) {
                    WriteLine($"{child.PropertyName} = {instancePath}.{child.PropertyName}.Select(x{depth} => new {child.ChildAggregateInstance.Item.ClassName} {{");
                    PushIndent("    ");
                    WriteBody(child.ChildAggregateInstance.AsEntry(), instancePath, $"x{depth}", depth + 1);
                    PopIndent();
                    WriteLine($"}}).ToList(),");
                }
                foreach (var child in instance.GetChildProperties(_ctx.Config)) {
                    WriteLine($"{child.PropertyName} = new {child.ChildAggregateInstance.Item.ClassName} {{");
                    PushIndent("    ");
                    WriteBody(child.ChildAggregateInstance, instancePath, $"{instancePath}.{child.CorrespondingNavigationProperty.Principal.PropertyName}", depth + 1);
                    PopIndent();
                    WriteLine($"}},");
                }
                foreach (var child in instance.GetVariationProperties(_ctx.Config)) {
                    WriteLine($"{child.PropertyName} = new {child.ChildAggregateInstance.Item.ClassName} {{");
                    PushIndent("    ");
                    WriteBody(child.ChildAggregateInstance, instancePath, $"{instancePath}.{child.CorrespondingNavigationProperty.Principal.PropertyName}", depth + 1);
                    PopIndent();
                    WriteLine($"}},");
                }
            }

            WriteLine($"var instance = new {_aggregateInstance.Item.ClassName} {{");
            PushIndent("    ");
            WriteBody(_aggregateInstance, "", E, 0);
            PopIndent();
            WriteLine($"}};");
            WriteLine($"instance.{AggregateInstanceBase.INSTANCE_KEY} = instance.{GETINSTANCEKEY_METHOD_NAME}().ToString();");
            WriteLine($"instance.{AggregateInstanceBase.INSTANCE_NAME} = instance.{GETINSTANCENAME_METHOD_NAME}();");
            WriteLine($"return instance;");
        }

        private IEnumerable<string> GetInstanceNameProps() {
            var useKeyInsteadOfName = _aggregateInstance
                .GetSchalarProperties(_ctx.Config)
                .Any(p => p.CorrespondingDbColumn.IsInstanceName) == false;
            var props = useKeyInsteadOfName
                ? _aggregateInstance
                    .GetSchalarProperties(_ctx.Config)
                    .Where(p => p.CorrespondingDbColumn.IsPrimary)
                    .ToArray()
                : _aggregateInstance
                    .GetSchalarProperties(_ctx.Config)
                    .Where(p => p.CorrespondingDbColumn.IsInstanceName)
                    .ToArray();
            if (props.Length == 0) {
                yield return $"return string.Empty;";
            } else {
                for (int i = 0; i < props.Length; i++) {
                    var head = i == 0 ? "return " : "    + ";
                    yield return $"{head}this.{props[i].PropertyName}?.ToString()";
                }
                yield return $"    ?? string.Empty;";
            }
        }
        #endregion AGGREGATE INSTANCE & CREATE COMMAND


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
            internal const string DELETE_ACTION_NAME = "delete";
            internal const string FIND_ACTION_NAME = "detail";
            internal const string KEYWORDSEARCH_ACTION_NAME = "list-by-keyword";

            internal const string SUBDOMAIN = "api";

            internal string SubDomain => $"{SUBDOMAIN}/{_aggregate.Item.DisplayName.ToCSharpSafe()}";
            internal string SearchCommandApi => $"/{SubDomain}/{SEARCH_ACTION_NAME}";
            internal string CreateCommandApi => $"/{SubDomain}/{CREATE_ACTION_NAME}";
            internal string UpdateCommandApi => $"/{SubDomain}/{UPDATE_ACTION_NAME}";
            internal string DeleteCommandApi => $"/{SubDomain}/{DELETE_ACTION_NAME}";
            internal string FindCommandApi => $"/{SubDomain}/{FIND_ACTION_NAME}";
            internal string KeywordSearchCommandApi => $"/{SubDomain}/{KEYWORDSEARCH_ACTION_NAME}";
        }
        #endregion CONTROLLER
    }
}
