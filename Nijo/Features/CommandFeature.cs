using Nijo.Core;
using Nijo.Util.DotnetEx;
using Nijo.Parts.WebServer;
using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nijo.Parts;
using Nijo.Features.Storing;

namespace Nijo.Features {
    public class CommandFeature {
        public Func<GraphNode<Aggregate>, string> CommandName { get; set; } = root => root.Item.PhysicalName;
        public Func<GraphNode<Aggregate>, string> ActionName { get; set; } = root => "実行";

        public string? ControllerAction { get; set; }
        public string? AppSrvMethod { get; set; }

        public void GenerateCode(CodeRenderingContext context, GraphNode<Aggregate> rootAggregate) {
            var createView = new SingleView(rootAggregate, SingleView.E_Type.Create);
            context.ReactProject.AddPage(createView);

            context.ReactProject.PagesDir(pages => {
                pages.Directory(rootAggregate.Item.DisplayName.ToFileNameSafe(), aggregateDir => {
                    aggregateDir.Generate(createView.Render());
                });
            });

            var command = new AggregateCreateCommand(rootAggregate) {
                // ToDbEntityはApplicationServiceメソッド未生成の場合は使わないので
                RendersDbEntity = AppSrvMethod != null,
            };
            var commandName = CommandName(rootAggregate);
            var actionName = ActionName(rootAggregate);

            context.UseAggregateFile(rootAggregate, builder => {
                builder.DataClassDeclaring.Add(command.RenderCSharp(context));

                builder.ControllerActions.Add(ControllerAction ?? $$"""
                    [HttpPost("{{actionName}}")]
                    public virtual IActionResult {{actionName}}([FromBody] {{command.CsClassName}}? param) {
                        if (param == null) return BadRequest();
                        if (_applicationService.{{actionName}}(param, out var errors)) {
                            return Ok();
                        } else {
                            return BadRequest(this.JsonContent(errors));
                        }
                    }
                    """);

                var appSrv = new ApplicationService();
                builder.AppServiceMethods.Add(AppSrvMethod ?? $$"""
                    public virtual bool {{actionName}}({{command.CsClassName}} command, out ICollection<string> errors) {
                        // このメソッドは自動生成の対象外です。
                        // {{appSrv.ConcreteClass}}クラスでこのメソッドをオーバーライドして実装してください。
                        errors = Array.Empty<string>();
                        return true;
                    }
                    """);
            });
        }
    }
}
