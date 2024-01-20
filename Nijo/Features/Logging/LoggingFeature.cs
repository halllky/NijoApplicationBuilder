using Nijo.Parts;
using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Features.Logging {
    internal class LoggingFeature : NijoFeatureBaseNonAggregate {

        public override void GenerateCode(CodeRenderingContext context) {

            context.EditWebApiDirectory(dir => {
                dir.Directory(App.ASP_UTIL_DIR, utilDir => {
                    utilDir.Generate(HttpResponseExceptionFilter.Render(context));
                    utilDir.Generate(DefaultLogger.Render(context));
                });
            });

        }

    }
}
