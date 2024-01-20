using Nijo.Parts;
using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Features.Debugging {
    internal class DebuggingFeature : NijoFeatureBaseNonAggregate {

        public override void GenerateCode(CodeRenderingContext context) {

            context.EditWebApiDirectory(dir => {
                dir.Directory(App.ASP_CONTROLLER_DIR, controllerDir => {
                    controllerDir.Generate(DebuggerController.Render(context));
                });
            });

            context.EditReactDirectory(dir => {
                dir.Directory(App.REACT_UTIL_DIR, reactUtilDir => {
                    reactUtilDir.Generate(DummyDataGenerator.Render(context));
                });
            });
        }

    }
}
