using Nijo.Core;
using Nijo.Parts;
using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Features.Logging {
    internal class LoggingFeature : IFeature {

        public void BuildSchema(AppSchemaBuilder builder) {
        }

        public void GenerateCode(CodeRenderingContext context) {

            context.WebApiProject.UtilDir(utilDir => {
                utilDir.Generate(HttpResponseExceptionFilter.Render(context));
                utilDir.Generate(DefaultLogger.Render(context));
            });

        }

    }
}
