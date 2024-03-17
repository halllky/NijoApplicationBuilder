using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nijo.Parts;
using Nijo.Core;
using Nijo.Util.CodeGenerating;
using Nijo.Parts.WebClient;

namespace Nijo.Features.BackgroundService {
    internal partial class BgTaskFeature : IFeature {

        private const string REACT_BG_DIR = "background-task";

        public static string GetScheduleApiURL(CodeRenderingContext ctx) {
            var bgTaskAggregate = ctx.Schema.GetAggregate(GraphNodeId);
            var controller = new Controller(bgTaskAggregate.Item);
            return $"{controller.SubDomain}/{SCHEDULE}";
        }

        public void BuildSchema(AppSchemaBuilder builder) {
            AddBgTaskEntity(builder);
        }

        public void GenerateCode(CodeRenderingContext context) {
            var aggregate = context.Schema.GetAggregate(GraphNodeId);

            context.AddAppSrvMethod(RenderAppSrvMethod(context));

            context.EditWebApiDirectory(webDir => {
                webDir.Directory("BackgroundTask", bgDir => {
                    bgDir.Generate(BgTaskBaseClass(context));
                    bgDir.Generate(JobChainClass(context));
                    bgDir.Generate(Launcher(context));
                });
            });

            context._app.DashBoardImports.Add($$"""
                import { BackgroundTaskList } from '../{{REACT_BG_DIR}}/BackgroundTaskList'
                """);
            context._app.DashBoardContents.Add($$"""
                <BackgroundTaskList />
                """);
            context.EditReactDirectory(reactDir => {
                reactDir.Directory(REACT_BG_DIR, bgDir => {
                    bgDir.Generate(RenderBgTaskListComponent(context));
                });
            });

            context.ConfigureServicesWhenWebServer(services => $$"""
                {{services}}.AddHostedService<BackgroundTaskLauncher>();
                """);

            context.UseAggregateFile(aggregate, builder => {
                builder.OnModelCreating.Add(modelBuilder => $$"""
                    {{ENTITY_CLASSNAME}}.OnModelCreating({{modelBuilder}});
                    """);

                builder.ControllerActions.Add(RenderAspControllerScheduleAction(context));
                builder.ControllerActions.Add(RenderAspControllerListAction(context));
            });
        }
    }
}
