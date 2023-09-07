using HalApplicationBuilder.CodeRendering.Util;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HalApplicationBuilder.CodeRendering.TemplateTextHelper;

namespace HalApplicationBuilder.CodeRendering.Finding {
    internal class FindFeature {
        internal FindFeature(GraphNode<Aggregate> aggregate, CodeRenderingContext ctx) {
            _aggregate = aggregate;
            _aggregateInstance = aggregate.GetInstanceClass().AsEntry();
            _dbEntity = aggregate.GetDbEntity().AsEntry();
            _ctx = ctx;
        }

        private readonly GraphNode<Aggregate> _aggregate;
        private readonly GraphNode<IAggregateInstance> _aggregateInstance;
        private readonly GraphNode<IEFCoreEntity> _dbEntity;
        private readonly CodeRenderingContext _ctx;

        internal string FindMethodReturnType => _aggregateInstance.Item.ClassName;
        internal string FindMethodName => $"Find{_aggregate.Item.DisplayName.ToCSharpSafe()}";

        internal string RenderController() {
            var _controller = new WebClient.Controller(_aggregate.Item, _ctx);

            return $$"""
            namespace {{_ctx.Config.RootNamespace}} {
                using Microsoft.AspNetCore.Mvc;
                using {{_ctx.Config.EntityNamespace}};
            
                partial class {{_controller.ClassName}} {
                    [HttpGet("{{WebClient.Controller.FIND_ACTION_NAME}}/{instanceKey}")]
                    public virtual IActionResult Find(string instanceKey) {
                        var instance = _dbContext.{{FindMethodName}}(instanceKey);
                        if (instance == null) {
                            return NotFound();
                        } else {
                            return this.JsonContent(instance);
                        }
                    }
                }
            }
            """;
        }

        internal string RenderEFCoreFindMethod() {

            return $$"""
                namespace {{_ctx.Config.EntityNamespace}} {
                    using System;
                    using System.Collections;
                    using System.Collections.Generic;
                    using System.Linq;
                    using Microsoft.EntityFrameworkCore;
                    using Microsoft.EntityFrameworkCore.Infrastructure;
                
                    partial class {{_ctx.Config.DbContextName}} {
                        /// <summary>
                        /// {{_aggregate.Item.DisplayName}}のキー情報から対象データの詳細を検索して返します。
                        /// </summary>
                        public {{FindMethodReturnType}}? {{FindMethodName}}(string serializedInstanceKey) {
                
                            {{WithIndent(RenderDbEntityLoading("this", "entity", "serializedInstanceKey", tracks: false, includeRefs: true), "            ")}}
                
                            if (entity == null) return null;
                
                            var aggregateInstance = {{_aggregateInstance.Item.ClassName}}.{{IAggregateInstance.FROM_DB_ENTITY_METHOD_NAME}}(entity);
                            return aggregateInstance;
                        }
                    }
                }
                """;
        }

        internal string RenderDbEntityLoading(string dbContextVarName, string entityVarName, string serializedInstanceKeyVarName, bool tracks, bool includeRefs) {

            // Include
            var includeEntities = _dbEntity
                .EnumerateThisAndDescendants()
                .ToList();
            if (includeRefs) {
                var refEntities = _dbEntity
                    .EnumerateThisAndDescendants()
                    .SelectMany(entity => entity.GetRefMembers())
                    .Select(edge => edge.Terminal);
                var refTargetAncestors = refEntities
                    .SelectMany(refTarget => refTarget.EnumerateAncestors())
                    .Select(edge => edge.Initial);
                var refTargetDescendants = refEntities
                    .SelectMany(refTarget => refTarget.EnumerateDescendants());

                includeEntities.AddRange(refEntities);
                includeEntities.AddRange(refTargetAncestors);
                includeEntities.AddRange(refTargetDescendants);
            }
            var paths = includeEntities
                .SelectMany(entity => entity.PathFromEntry())
                .Select(edge => edge.As<IEFCoreEntity>())
                .Select(edge => {
                    var source = edge.Source.As<IEFCoreEntity>();
                    var nav = new NavigationProperty(edge, _ctx.Config);
                    var prop = edge.Source.As<IEFCoreEntity>() == nav.Principal.Owner
                        ? nav.Principal.PropertyName
                        : nav.Relevant.PropertyName;
                    return new { source, prop };
                });

            // SingleOrDefault
            var keys = _dbEntity
                .GetColumns()
                .Where(col => col.IsPrimary)
                .SelectTextTemplate((col, i) => {
                    var cast = col.MemberType.GetCSharpTypeName();
                    return $"x.{col.PropertyName} == ({cast})instanceKey.{InstanceKey.OBJECT_ARRAY}[{i}]";
                });

            return $$"""
                var instanceKey = {{InstanceKey.CLASS_NAME}}.{{InstanceKey.PARSE}}({{serializedInstanceKeyVarName}});
                var {{entityVarName}} = {{dbContextVarName}}.{{_dbEntity.Item.DbSetName}}
                {{If(tracks == false, () => $$"""
                    .AsNoTracking()
                """)}}
                {{paths.SelectTextTemplate(path => path.source == _dbEntity ? $$"""
                    .Include(x => x.{{path.prop}})
                """ : $$"""
                    .ThenInclude(x => x.{{path.prop}})
                """)}}
                    .SingleOrDefault(x => {{WithIndent(keys, "                       && ")}});
                """;
        }

    }
}
