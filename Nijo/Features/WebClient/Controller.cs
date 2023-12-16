using Nijo.Core;
using Nijo.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Features.WebClient {
    internal class Controller {
        internal Controller(string physicalName) {
            _physicalName = physicalName;
        }
        internal Controller(Aggregate aggregate) : this(aggregate.DisplayName.ToCSharpSafe()) {
        }

        private readonly string _physicalName;

        internal string ClassName => $"{_physicalName}Controller";

        internal const string SEARCH_ACTION_NAME = "list";
        internal const string CREATE_ACTION_NAME = "create";
        internal const string UPDATE_ACTION_NAME = "update";
        internal const string DELETE_ACTION_NAME = "delete";
        internal const string FIND_ACTION_NAME = "detail";

        internal const string SUBDOMAIN = "api";

        internal string SubDomain => $"{SUBDOMAIN}/{_physicalName}";
        internal string SearchCommandApi => $"/{SubDomain}/{SEARCH_ACTION_NAME}";
        internal string CreateCommandApi => $"/{SubDomain}/{CREATE_ACTION_NAME}";
        internal string UpdateCommandApi => $"/{SubDomain}/{UPDATE_ACTION_NAME}";
        internal string DeleteCommandApi => $"/{SubDomain}/{DELETE_ACTION_NAME}";

        internal string Render(CodeRenderingContext _ctx) {
            var appSrv = new ApplicationService(_ctx.Config);

            return $$"""
                namespace {{_ctx.Config.RootNamespace}} {
                    using Microsoft.AspNetCore.Mvc;
                    using {{_ctx.Config.EntityNamespace}};

                    [ApiController]
                    [Route("{{SUBDOMAIN}}/[controller]")]
                    public partial class {{ClassName}} : ControllerBase {
                        public {{ClassName}}(ILogger<{{ClassName}}> logger, {{appSrv.ClassName}} applicationService) {
                            _logger = logger;
                            _applicationService = applicationService;
                        }
                        protected readonly ILogger<{{ClassName}}> _logger;
                        protected readonly {{appSrv.ClassName}} _applicationService;
                    }
                }
                """;
        }
    }
}
