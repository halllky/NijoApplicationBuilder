using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using static HalApplicationBuilder.CodeRendering.TemplateTextHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.InstanceHandling {
    internal class CreateFeature {
        internal CreateFeature(GraphNode<Aggregate> aggregate) {
            _aggregate = aggregate;
        }

        private readonly GraphNode<Aggregate> _aggregate;

        internal string ArgType => _aggregate.Item.ClassName;
        internal string MethodName => $"Create{_aggregate.Item.DisplayName.ToCSharpSafe()}";

        internal string RenderController(CodeRenderingContext ctx) {
            var controller = new WebClient.Controller(_aggregate.Item);
            var param = new AggregateCreateCommand(_aggregate);

            return $$"""
                namespace {{ctx.Config.RootNamespace}} {
                    using Microsoft.AspNetCore.Mvc;
                    using {{ctx.Config.EntityNamespace}};

                    partial class {{controller.ClassName}} : ControllerBase {
                        [HttpPost("{{WebClient.Controller.CREATE_ACTION_NAME}}")]
                        public virtual IActionResult Create([FromBody] {{param.ClassName}} param) {
                            if (_dbContext.{{MethodName}}(param, out var created, out var errors)) {
                                return this.JsonContent(created);
                            } else {
                                return BadRequest(this.JsonContent(errors));
                            }
                        }
                    }
                }
                """;
        }

        internal string RenderEFCoreMethod(CodeRenderingContext ctx) {
            var controller = new WebClient.Controller(_aggregate.Item);
            var param = new AggregateCreateCommand(_aggregate);
            var find = new FindFeature(_aggregate);

            return $$"""
                namespace {{ctx.Config.EntityNamespace}} {
                    using System;
                    using System.Collections;
                    using System.Collections.Generic;
                    using System.Linq;
                    using Microsoft.EntityFrameworkCore;
                    using Microsoft.EntityFrameworkCore.Infrastructure;
                
                    partial class {{ctx.Config.DbContextName}} {
                        public bool {{MethodName}}({{param.ClassName}} command, out {{_aggregate.Item.ClassName}} created, out ICollection<string> errors) {
                            var dbEntity = command.{{AggregateDetail.TO_DBENTITY}}();
                            this.Add(dbEntity);
                
                            try {
                                this.SaveChanges();
                            } catch (DbUpdateException ex) {
                                created = new {{_aggregate.Item.ClassName}}();
                                errors = ex.GetMessagesRecursively("  ").ToList();
                                return false;
                            }
                
                            var afterUpdate = this.{{WithIndent(find.RenderCaller(m => $"dbEntity.{m.MemberName}"), "            ")}};
                            if (afterUpdate == null) {
                                created = new {{_aggregate.Item.ClassName}}();
                                errors = new[] { "更新後のデータの再読み込みに失敗しました。" };
                                return false;
                            }
                
                            created = afterUpdate;
                            errors = new List<string>();
                            return true;
                        }
                    }
                }
                """;
        }
    }
}