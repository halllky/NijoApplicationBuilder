using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering20230514.Presentation {
    partial class SearchConditionBase : ITemplate {
        internal SearchConditionBase(CodeRenderingContext ctx) {
            _ctx = ctx;
        }
        private readonly CodeRenderingContext _ctx;

        internal string Namespace => _ctx.Config.RootNamespace;
        internal string ClassFullname => $"{Namespace}.{CLASS_NAME}";
        internal static string CLASS_NAME => Core20230514.SearchCondition.BASE_CLASS_NAME;
        internal static string PAGE => Core20230514.SearchCondition.PAGE_PROP_NAME;

        public string FileName => "SearchConditionBase.cs";
    }
}
