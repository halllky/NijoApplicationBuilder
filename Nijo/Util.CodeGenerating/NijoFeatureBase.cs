using Nijo.Core;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Util.CodeGenerating {
    public abstract class NijoFeatureBase {
        public virtual void BuildSchema(AppSchemaBuilder builder) { }
    }
    public abstract class NijoFeatureBaseNonAggregate : NijoFeatureBase {
        public virtual void GenerateCode(CodeRenderingContext context) { }
    }
    public abstract class NijoFeatureBaseByAggregate : NijoFeatureBase {
        public virtual void GenerateCode(CodeRenderingContext context, GraphNode<Aggregate> rootAggregate) { }
    }
}
