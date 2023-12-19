using Nijo.Core;
using Nijo.DotnetEx;
using static Nijo.Features.TemplateTextHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nijo.Features.Searching;

namespace Nijo.Features.InstanceHandling {
    internal class CreateFeature {
        internal CreateFeature(GraphNode<Aggregate> aggregate) {
            _aggregate = aggregate;
        }

        private readonly GraphNode<Aggregate> _aggregate;

        internal string ArgType => _aggregate.Item.ClassName;
        internal string MethodName => $"Create{_aggregate.Item.DisplayName.ToCSharpSafe()}";

        internal string RenderController(ICodeRenderingContext ctx) {
            if (_aggregate.GetRoot().Item.Options.Type == E_AggreateType.Command) {
                return new CommandExecuteFeature(_aggregate).RenderController(ctx);
            }

            var controller = new WebClient.Controller(_aggregate.Item);
            var param = new AggregateCreateCommand(_aggregate);

            return $$"""
                namespace {{ctx.Config.RootNamespace}} {
                    using Microsoft.AspNetCore.Mvc;
                    using {{ctx.Config.EntityNamespace}};

                    partial class {{controller.ClassName}} : ControllerBase {
                        [HttpPost("{{WebClient.Controller.CREATE_ACTION_NAME}}")]
                        public virtual IActionResult Create([FromBody] {{param.ClassName}} param) {
                            if (_applicationService.{{MethodName}}(param, out var created, out var errors)) {
                                return this.JsonContent(created);
                            } else {
                                return BadRequest(this.JsonContent(errors));
                            }
                        }
                    }
                }
                """;
        }

        internal string RenderEFCoreMethod(ICodeRenderingContext ctx) {
            if (_aggregate.GetRoot().Item.Options.Type == E_AggreateType.Command) {
                return new CommandExecuteFeature(_aggregate).RenderEFCoreMethod(ctx);
            }

            var appSrv = new ApplicationService(ctx.Config);
            var controller = new WebClient.Controller(_aggregate.Item);
            var param = new AggregateCreateCommand(_aggregate);
            var find = new FindFeature(_aggregate);

            var searchKeys = _aggregate
                .GetKeys()
                .Where(m => m is AggregateMember.ValueMember)
                .Select(m => $"dbEntity.{m.GetFullPath().Join(".")}");

            return $$"""
                namespace {{ctx.Config.RootNamespace}} {
                    using System;
                    using System.Collections;
                    using System.Collections.Generic;
                    using System.Linq;
                    using Microsoft.EntityFrameworkCore;
                    using Microsoft.EntityFrameworkCore.Infrastructure;

                    partial class {{appSrv.ClassName}} {
                        public virtual bool {{MethodName}}({{param.ClassName}} command, out {{_aggregate.Item.ClassName}} created, out ICollection<string> errors) {
                            var dbEntity = command.{{AggregateDetail.TO_DBENTITY}}();
                            {{appSrv.DbContext}}.Add(dbEntity);

                            try {
                                {{appSrv.DbContext}}.SaveChanges();
                            } catch (DbUpdateException ex) {
                                created = new {{_aggregate.Item.ClassName}}();
                                errors = ex.GetMessagesRecursively("  ").ToList();
                                return false;
                            }

                            var afterUpdate = this.{{find.FindMethodName}}({{searchKeys.Join(", ")}});
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

        /// <summary>
        /// is="command" の集約のCreate処理
        /// </summary>
        [Obsolete]
        internal class CommandExecuteFeature {
            internal CommandExecuteFeature(GraphNode<Aggregate> aggregate) {
                _aggregate = aggregate;
            }
            private readonly GraphNode<Aggregate> _aggregate;

            internal string RenderController(ICodeRenderingContext ctx) {
                var create = new CreateFeature(_aggregate);
                var controller = new WebClient.Controller(_aggregate.Item);
                var param = new AggregateCreateCommand(_aggregate);

                return $$"""
                namespace {{ctx.Config.RootNamespace}} {
                    using Microsoft.AspNetCore.Mvc;
                    using {{ctx.Config.EntityNamespace}};

                    partial class {{controller.ClassName}} : ControllerBase {
                        [HttpPost("{{WebClient.Controller.CREATE_ACTION_NAME}}")]
                        public virtual IActionResult Create([FromBody] {{param.ClassName}} param) {
                            if (_applicationService.{{create.MethodName}}(param, out var errors)) {
                                return Ok();
                            } else {
                                return BadRequest(this.JsonContent(errors));
                            }
                        }
                    }
                }
                """;
            }
            internal string RenderEFCoreMethod(ICodeRenderingContext ctx) {
                var create = new CreateFeature(_aggregate);
                var appSrv = new ApplicationService(ctx.Config);
                var param = new AggregateCreateCommand(_aggregate);

                return $$"""
                namespace {{ctx.Config.RootNamespace}} {
                    using System;
                    using System.Collections;
                    using System.Collections.Generic;
                    using System.Linq;
                    using Microsoft.EntityFrameworkCore;
                    using Microsoft.EntityFrameworkCore.Infrastructure;

                    partial class {{appSrv.ClassName}} {
                        public virtual bool {{create.MethodName}}({{param.ClassName}} command, out ICollection<string> errors) {
                            // このメソッドは自動生成の対象外です。
                            // {{appSrv.ConcreteClass}}クラスでこのメソッドをオーバーライドして実装してください。
                            errors = Array.Empty<string>();
                            return true;
                        }
                    }
                }
                """;
            }
        }
    }
}
