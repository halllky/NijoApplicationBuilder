using Nijo.Core;
using Nijo.DotnetEx;
using Nijo.Features.InstanceHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Features.Command {
    public class CommandFeature : NijoFeatureBaseByAggregate {
        public Func<GraphNode<Aggregate>, string> CommandName { get; set; } = root => root.Item.ClassName;
        public Func<GraphNode<Aggregate>, string> ActionName { get; set; } = root => "実行";

        public string? ControllerAction { get; set; }
        public string? AppSrvMethod { get; set; }

        public override void GenerateCode(ICodeRenderingContext context, GraphNode<Aggregate> rootAggregate) {
            var createView = new SingleView(rootAggregate, SingleView.E_Type.Create);
            context.Render<Infrastucture>(infra => {
                infra.ReactPages.Add(createView);
            });

            context.EditReactDirectory(reactDir => {
                reactDir.Directory("pages", pages => {
                    pages.Directory(rootAggregate.Item.DisplayName.ToFileNameSafe(), aggregateDir => {
                        aggregateDir.Generate(createView.Render());
                    });
                });
            });

            context.Render<Infrastucture>(infra => {
                var command = new AggregateCreateCommand(rootAggregate) {
                    // ToDbEntityはApplicationServiceメソッド未生成の場合は使わないので
                    RendersDbEntity = AppSrvMethod != null,
                };
                var commandName = CommandName(rootAggregate);
                var actionName = ActionName(rootAggregate);

                infra.Aggregate(rootAggregate, builder => {
                    builder.DataClassDeclaring.Add(command.RenderCSharp(context));

                    builder.ControllerActions.Add(ControllerAction ?? $$"""
                        [HttpPost("{{actionName}}")]
                        public virtual IActionResult {{actionName}}([FromBody] {{command.ClassName}}? param) {
                            if (param == null) return BadRequest();
                            if (_applicationService.{{actionName}}(param, out var errors)) {
                                return Ok();
                            } else {
                                return BadRequest(this.JsonContent(errors));
                            }
                        }
                        """);

                    var appSrv = new ApplicationService(context.Config);
                    builder.AppServiceMethods.Add(AppSrvMethod ?? $$"""
                        public virtual bool {{actionName}}({{command.ClassName}} command, out ICollection<string> errors) {
                            // このメソッドは自動生成の対象外です。
                            // {{appSrv.ConcreteClass}}クラスでこのメソッドをオーバーライドして実装してください。
                            errors = Array.Empty<string>();
                            return true;
                        }
                        """);
                });
            });
        }
    }
}
