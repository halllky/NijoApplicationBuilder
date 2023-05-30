using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering20230514.Util {
    partial class RuntimeSettings : ITemplate {
        internal RuntimeSettings(CodeRenderingContext ctx) {
            _ctx = ctx;
        }

        private readonly CodeRenderingContext _ctx;

        internal string ServerSetiingTypeFullName => $"{_ctx.Config.RootNamespace}.{nameof(RuntimeSettings)}.{SERVER}";

        public string FileName => $"RuntimeSettings.cs";

        private const string SERVER = "Server";

        internal const string JSON_FILE_NAME = "halapp-runtime-config.json";
        internal const string TO_JSON = "ToJson";
        internal const string GET_DEFAULT = "GetDefault";
        internal const string GET_ACTIVE_CONNSTR = "GetActiveConnectionString";
    }
}
