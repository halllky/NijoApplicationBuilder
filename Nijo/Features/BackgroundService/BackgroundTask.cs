using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nijo.Architecture;
using Nijo.Core;
using Nijo.Util.CodeGenerating;

namespace Nijo.Features.BackgroundService {
    internal partial class BackgroundTask : NijoFeatureBaseNonAggregate {

        public override void BuildSchema(AppSchemaBuilder builder) {
            AddBgTaskEntity(builder);
        }

        public override void GenerateCode(ICodeRenderingContext context) {
            context.EditWebApiDirectory(webDir => {
                webDir.Directory("BackgroundTask", bgDir => {
                    bgDir.Generate(BgTaskBaseClass(context));
                    bgDir.Generate(Launcher(context));
                });
            });

            context.Render<Infrastructure>(infra => {
                infra.ConfigureServicesWhenWebServer.Add(services => $$"""
                    //// バッチ処理
                    {{services}}.AddHostedService<BackgroundTaskLauncher>();
                    """);
            });
        }
    }
}
