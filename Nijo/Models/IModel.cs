using Nijo.Core;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models {
    public interface IModel {
        void GenerateCode(CodeRenderingContext context, GraphNode<Aggregate> rootAggregate);
    }
}
