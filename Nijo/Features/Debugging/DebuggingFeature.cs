using Nijo.Core;
using Nijo.Parts;
using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Features.Debugging {
    internal class DebuggingFeature : IFeature {
        public void BuildSchema(AppSchemaBuilder builder) {
        }

        public void GenerateCode(CodeRenderingContext context) {

            context.WebApiProject.ControllerDir(controllerDir => {
                controllerDir.Generate(DebuggerController.Render(context));
            });

            context.EditReactDirectory(dir => {
                dir.Directory(App.REACT_UTIL_DIR, reactUtilDir => {
                    reactUtilDir.Generate(DummyDataGenerator.Render(context));
                });
            });
        }

    }
}
