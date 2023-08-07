using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.WebClient {
    partial class types : ITemplate {
        internal types(CodeRenderingContext ctx) {
            _ctx = ctx;
        }

        private readonly CodeRenderingContext _ctx;

        public string FileName => "types.ts";

        private IEnumerable<AggregateInstanceTS> GetTsTypes() {
            foreach (var root in _ctx.Schema.RootAggregates()) {
                yield return new AggregateInstanceTS(root.GetInstanceClass(), _ctx.Config);
            }
        }
    }
}
