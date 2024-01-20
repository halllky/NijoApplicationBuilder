using Nijo.Core;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Util.CodeGenerating {
    public interface IFeature {
        void BuildSchema(AppSchemaBuilder builder);
        void GenerateCode(CodeRenderingContext context);
    }
}
