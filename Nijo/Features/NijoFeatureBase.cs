using Nijo.Core;
using Nijo.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Features {
    public abstract class NijoFeatureBase {
        public virtual void BuildSchema(AppSchemaBuilder builder) { }
    }
    public abstract class NijoFeatureBaseNonAggregate : NijoFeatureBase {
        public virtual void GenerateCode(ICodeRenderingContext context) { }
    }
    public abstract class NijoFeatureBaseByAggregate : NijoFeatureBase {
        public virtual void GenerateCode(ICodeRenderingContext context, GraphNode<Aggregate> rootAggregate) { }
    }
}
