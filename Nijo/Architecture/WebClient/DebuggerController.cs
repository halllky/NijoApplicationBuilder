using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nijo.Util.CodeGenerating;

namespace Nijo.Architecture.WebClient {
    internal class DebuggerController {
        internal const string RECREATE_DB_URL = "/WebDebugger/recreate-database";

        internal static SourceFile Render(ICodeRenderingContext ctx) => new SourceFile {
            FileName = $"WebDebugger.cs",
            RenderContent = () => $$"""
                using Microsoft.AspNetCore.Mvc;
                using System.Text.Json;
                using Microsoft.EntityFrameworkCore;

                namespace {{ctx.Config.RootNamespace}};

                #if DEBUG
                [ApiController]
                [Route("[controller]")]
                public class WebDebuggerController : ControllerBase {
                  public WebDebuggerController(ILogger<WebDebuggerController> logger, IServiceProvider provider) {
                    _logger = logger;
                    _provider = provider;
                  }
                  private readonly ILogger<WebDebuggerController> _logger;
                  private readonly IServiceProvider _provider;

                  [HttpPost("recreate-database")]
                  public HttpResponseMessage RecreateDatabase() {
                    var dbContext = _provider.GetRequiredService<{{ctx.Config.DbContextNamespace}}.{{ctx.Config.DbContextName}}>();
                    dbContext.Database.EnsureDeleted();
                    dbContext.Database.EnsureCreated();
                    return new HttpResponseMessage {
                      StatusCode = System.Net.HttpStatusCode.OK,
                      Content = new StringContent("DBを再作成しました。"),
                    };
                  }

                  [HttpGet("secret-settings")]
                  public IActionResult GetSecretSettings() {
                    var runtimeSetting = _provider.GetRequiredService<{{Util.RuntimeSettings.ServerSetiingTypeFullName}}>();
                    return this.JsonContent(runtimeSetting);
                  }
                  [HttpPost("secret-settings")]
                  public IActionResult SetSecretSettings([FromBody] {{Util.RuntimeSettings.ServerSetiingTypeFullName}} settings) {
                    var json = settings.{{Util.RuntimeSettings.TO_JSON}}();
                    using var sw = new System.IO.StreamWriter("{{Util.RuntimeSettings.JSON_FILE_NAME}}", false, new System.Text.UTF8Encoding(false));
                    sw.WriteLine(json);
                    return Ok();
                  }
                }
                #endif
                """,
        };
    }
}
