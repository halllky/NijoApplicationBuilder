using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.Presentation {
    partial class SearchResultBase : ITemplate {
        internal SearchResultBase(CodeRenderingContext ctx) {
            _ctx = ctx;
        }
        private readonly CodeRenderingContext _ctx;

        public string FileName => "SearchResultBase.cs";

        internal string Namespace => _ctx.Config.RootNamespace;
        internal string ClassFullname => $"{Namespace}.{CLASS_NAME}";
        internal static string CLASS_NAME => Core.SearchResult.BASE_CLASS_NAME;
        internal static string INSTANCE_KEY = Core.SearchResult.INSTANCE_KEY_PROP_NAME;
        internal static string INSTANCE_NAME = Core.SearchResult.INSTANCE_NAME_PROP_NAME;
    }
}
