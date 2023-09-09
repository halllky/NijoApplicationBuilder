using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.WebClient
{
    partial class DebuggerController : TemplateBase
    {
        internal DebuggerController(CodeRenderingContext ctx)
        {
            _ctx = ctx;
        }
        private readonly CodeRenderingContext _ctx;

        internal string RuntimeServerSettings => new Util.RuntimeSettings(_ctx).ServerSetiingTypeFullName;

        public override string FileName => $"HalappDebugger.cs";

        protected override string Template() {
            return $$"""
                using Microsoft.AspNetCore.Mvc;
                using System.Text.Json;
                using Microsoft.EntityFrameworkCore;

                namespace {{_ctx.Config.RootNamespace}};

                #if DEBUG
                [ApiController]
                [Route("[controller]")]
                public class HalappDebugController : ControllerBase {
                  public HalappDebugController(ILogger<HalappDebugController> logger, IServiceProvider provider) {
                    _logger = logger;
                    _provider = provider;
                  }
                  private readonly ILogger<HalappDebugController> _logger;
                  private readonly IServiceProvider _provider;

                  [HttpPost("recreate-database")]
                  public HttpResponseMessage RecreateDatabase() {
                    var dbContext = _provider.GetRequiredService<{{_ctx.Config.DbContextNamespace}}.{{_ctx.Config.DbContextName}}>();
                    dbContext.Database.EnsureDeleted();
                    dbContext.Database.EnsureCreated();
                    return new HttpResponseMessage {
                      StatusCode = System.Net.HttpStatusCode.OK,
                      Content = new StringContent("DBを再作成しました。"),
                    };
                  }

                  [HttpGet("secret-settings")]
                  public IActionResult GetSecretSettings() {
                    var runtimeSetting = _provider.GetRequiredService<{{RuntimeServerSettings}}>();
                    return this.JsonContent(runtimeSetting);
                  }
                  [HttpPost("secret-settings")]
                  public IActionResult SetSecretSettings([FromBody] {{RuntimeServerSettings}} settings) {
                    var json = settings.{{Util.RuntimeSettings.TO_JSON}}();
                    using var sw = new System.IO.StreamWriter("{{Util.RuntimeSettings.JSON_FILE_NAME}}", false, new System.Text.UTF8Encoding(false));
                    sw.WriteLine(json);
                    return Ok();
                  }
                }
                #endif
                """;
        }
    }
}
