using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using static HalApplicationBuilder.CodeRendering.TemplateTextHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.InstanceHandling {
    internal class DeleteFeature {
        internal DeleteFeature(GraphNode<Aggregate> aggregate) {
            _aggregate = aggregate;
        }

        private readonly GraphNode<Aggregate> _aggregate;

        internal string ArgType => _aggregate.Item.ClassName;
        internal string MethodName => $"Delete{_aggregate.Item.DisplayName.ToCSharpSafe()}";

        internal string RenderController(CodeRenderingContext ctx) {
            var controller = new WebClient.Controller(_aggregate.Item);
            var args = GetEFCoreMethodArgs();

            return $$"""
                namespace {{ctx.Config.RootNamespace}} {
                    using Microsoft.AspNetCore.Mvc;
                    using {{ctx.Config.EntityNamespace}};

                    partial class {{controller.ClassName}} {
                        [HttpDelete("{{WebClient.Controller.DELETE_ACTION_NAME}}/{{args.Select(a => "{" + a.MemberName + "}").Join("/")}}")]
                        public virtual IActionResult Delete({{args.Select(m => $"{m.CSharpTypeName} {m.MemberName}").Join(", ")}}) {
                            if (_dbContext.{{MethodName}}({{args.Select(a => a.MemberName).Join(", ")}}, out var errors)) {
                                return Ok();
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
            var args = GetEFCoreMethodArgs().ToArray();
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
                        public bool {{MethodName}}({{args.Select(m => $"{m.CSharpTypeName} {m.MemberName}").Join(", ")}}, out ICollection<string> errors) {

                            {{WithIndent(find.RenderDbEntityLoading("this", "entity", args.Select(a => a.MemberName).ToArray(), tracks: true, includeRefs: false), "            ")}}

                            if (entity == null) {
                                errors = new[] { "削除対象のデータが見つかりません。" };
                                return false;
                            }

                            this.Remove(entity);
                            try {
                                this.SaveChanges();
                            } catch (DbUpdateException ex) {
                                errors = ex.GetMessagesRecursively().ToArray();
                                return false;
                            }

                            errors = Array.Empty<string>();
                            return true;
                        }
                    }
                }
                """;
        }

        private IEnumerable<AggregateMember.AggregateMemberBase> GetEFCoreMethodArgs() {
            return _aggregate
                .GetKeys()
                .Where(m => m is AggregateMember.ValueMember);
        }
    }
}
