using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering {
    partial class Configure : ITemplate {
        internal Configure(CodeRenderingContext ctx) {
            _ctx = ctx;
        }
        private readonly CodeRenderingContext _ctx;

        public const string CLASSNAME = "HalappConfigurer";
        public const string INIT_WEB_HOST_BUILDER = "InitWebHostBuilder";
        public const string INIT_BATCH_PROCESS = "InitAsBatchProces";
        public const string CONFIGURE_SERVICES = "ConfigureServices";
        public const string INIT_WEBAPPLICATION= "InitWebApplication";

        public string FileName => "HalappConfigurer.cs";
        public string Namespace => _ctx.Config.RootNamespace;
        public string ClassFullname => $"{_ctx.Config.RootNamespace}.{CLASSNAME}";

        private string RuntimeServerSettings => new Util.RuntimeSettings(_ctx).ServerSetiingTypeFullName;
    }
}
