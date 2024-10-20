using Nijo.Parts.WebServer;
using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.CommandModelFeatures {
    /// <summary>
    /// <see cref="CommandModel"/> のすべてのコマンドのWebエンドポイントはこのController内に定義される
    /// </summary>
    internal class CommandController : ISummarizedFile {

        internal const string SUBDOMAIN = "command";

        private readonly List<string> _sourceCodes = new();
        internal void AddAction(string sourceCode) {
            _sourceCodes.Add(sourceCode);
        }

        void ISummarizedFile.OnEndGenerating(CodeRenderingContext context) {
            context.WebApiProject.ControllerDir(dir => {
                dir.Generate(new SourceFile {
                    FileName = "Commands.cs",
                    RenderContent = ctx => {
                        var appSrv = new ApplicationService();

                        return $$"""
                            using System;
                            using System.Linq;
                            using Microsoft.AspNetCore.Mvc;
                            using System.Text.Json;
                            using System.Text.Json.Nodes;
                            using System.Text.Json.Serialization;

                            namespace {{ctx.Config.RootNamespace}};

                            /// <summary>
                            /// 各種コマンドの呼び出し用Webエンドポイントを提供する ASP.NET Core のコントローラー
                            /// </summary>
                            [ApiController]
                            [Route("{{Controller.SUBDOMAIN}}/{{SUBDOMAIN}}")]
                            public partial class AutoGeneratedCommandController : ControllerBase {
                                public AutoGeneratedCommandController(ILogger<AutoGeneratedCommandController> logger, {{appSrv.ConcreteClassName}} applicationService) {
                                    _logger = logger;
                                    _applicationService = applicationService;
                                }
                                protected readonly ILogger<AutoGeneratedCommandController> _logger;
                                protected readonly {{appSrv.ConcreteClassName}} _applicationService;
                            {{_sourceCodes.SelectTextTemplate(source => $$"""

                                {{WithIndent(source, "    ")}}
                            """)}}
                            }
                            """;
                    },
                });
            });
        }
    }
}
