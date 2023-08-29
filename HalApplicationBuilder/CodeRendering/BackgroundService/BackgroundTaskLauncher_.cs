using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.BackgroundService {
    partial class BackgroundTaskLauncher : ITemplate {
        internal BackgroundTaskLauncher(CodeRenderingContext ctx) {
            _ctx = ctx;
        }
        private readonly CodeRenderingContext _ctx;

        public string FileName => "BackgroundTaskLauncher.cs";
        internal string ClassFullname => $"{RootNamespace}.{CLASSNAME}";
        private const string CLASSNAME = "BackgroundTaskLauncher";

        private string RootNamespace => _ctx.Config.RootNamespace;
        private string DbContextNamespace => _ctx.Config.DbContextNamespace;
        private string EntityNamespace => _ctx.Config.EntityNamespace;
        private string DbContextName => _ctx.Config.DbContextName;
        private string DbContextFullName => $"{_ctx.Config.DbContextNamespace}.{_ctx.Config.DbContextName}";
    }
}
