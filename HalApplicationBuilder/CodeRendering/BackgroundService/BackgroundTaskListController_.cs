using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.BackgroundService {
    partial class BackgroundTaskListController : ITemplate {
        internal BackgroundTaskListController(CodeRenderingContext ctx) {
            _ctx = ctx;
        }
        private readonly CodeRenderingContext _ctx;

        public string FileName => "Controller.cs";
        private string DbContextFullName => $"{_ctx.Config.DbContextNamespace}.{_ctx.Config.DbContextName}";
    }
}
