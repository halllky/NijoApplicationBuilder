using Nijo.Core;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.WriteModel2Features {
    /// <summary>
    /// Entity Framework Core のエンティティ
    /// </summary>
    internal class EFCoreEntity {
        internal EFCoreEntity(GraphNode<Aggregate> agg) {
            _aggregate = agg;
        }
        private readonly GraphNode<Aggregate> _aggregate;

        internal string Render(CodeRenderingContext context) {
            throw new NotImplementedException();
        }

        internal Func<string, string> RenderCallingOnModelCreating(CodeRenderingContext context) {
            throw new NotImplementedException();
        }
    }
}
