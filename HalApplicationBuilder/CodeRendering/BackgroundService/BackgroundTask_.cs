using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.BackgroundService {
    partial class BackgroundTask : ITemplate {
        internal BackgroundTask(CodeRenderingContext ctx) {
            _ctx = ctx;
        }
        private readonly CodeRenderingContext _ctx;

        public string FileName => "BackgroundTask.cs";
        private string RootNamespace => _ctx.Config.RootNamespace;
        private string DbContextFullName => $"{_ctx.Config.DbContextNamespace}.{_ctx.Config.DbContextName}";
        private string EntityNamespace => _ctx.Config.EntityNamespace;
    }
}
