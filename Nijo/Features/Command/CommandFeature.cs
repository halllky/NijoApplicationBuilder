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
        public override string KeywordInAppSchema => "command";
        public Func<GraphNode<Aggregate>, string> CommandName { get; set; } = root => root.Item.ClassName;
        public Func<GraphNode<Aggregate>, string> ActionName { get; set; } = root => "実行";

        public override void GenerateCode(ICodeRenderingContext context, GraphNode<Aggregate> rootAggregate) {

            context.ReactProject.EditDirectory(reactDir => {
                reactDir.Directory("pages", pages => {
                    pages.Directory(rootAggregate.Item.DisplayName.ToFileNameSafe(), aggregateDir => {
                        aggregateDir.Generate(new SingleView(rootAggregate, SingleView.E_Type.Create).Render());
                    });
                });
            });

            var command = new AggregateCreateCommand(rootAggregate);
            var commandName = CommandName(rootAggregate);
            var actionName = ActionName(rootAggregate);

            context.WebApiProject.RenderControllerAction(controller => $$"""
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

            context.WebApiProject.RenderApplicationServiceMethod(appSrv => $$"""
                public virtual bool {{actionName}}({{command.ClassName}} command, out ICollection<string> errors) {
                    // このメソッドは自動生成の対象外です。
                    // {{appSrv.ConcreteClass}}クラスでこのメソッドをオーバーライドして実装してください。
                    errors = Array.Empty<string>();
                    return true;
                }
                """);
        }
    }
}
