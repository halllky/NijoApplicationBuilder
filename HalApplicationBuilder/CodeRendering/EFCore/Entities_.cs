using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.EFCore {
    partial class Entities : ITemplate {
        internal Entities(CodeRenderingContext ctx) {
            _ctx = ctx;
        }
        private readonly CodeRenderingContext _ctx;

        public string FileName => "Entities.cs";

        private IEnumerable<NavigationProperty.Item> EnumerateNavigationProperties(GraphNode<EFCoreEntity> dbEntity) {
            foreach (var nav in dbEntity.GetNavigationProperties(_ctx.Config)) {
                if (nav.Principal.Owner == dbEntity) yield return nav.Principal;
                if (nav.Relevant.Owner ==  dbEntity) yield return nav.Relevant;
            }
        }
    }
}
