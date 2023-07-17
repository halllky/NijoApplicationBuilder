using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.Presentation {
    partial class SearchConditionBase : ITemplate {
        internal SearchConditionBase(CodeRenderingContext ctx) {
            _ctx = ctx;
        }
        private readonly CodeRenderingContext _ctx;

        internal string Namespace => _ctx.Config.RootNamespace;
        internal string ClassFullname => $"{Namespace}.{CLASS_NAME}";
        internal static string CLASS_NAME => Core.SearchCondition.BASE_CLASS_NAME;
        internal static string PAGE => Core.SearchCondition.PAGE_PROP_NAME;

        public string FileName => "SearchConditionBase.cs";
    }
}
