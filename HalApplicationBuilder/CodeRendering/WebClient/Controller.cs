using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.WebClient {
    internal class Controller {
        internal Controller(string physicalName, CodeRenderingContext ctx) {
            _physicalName = physicalName;
            _ctx = ctx;
        }
        internal Controller(Aggregate aggregate, CodeRenderingContext ctx)
            : this(aggregate.DisplayName.ToCSharpSafe(), ctx) { }

        private readonly string _physicalName;
        private readonly CodeRenderingContext _ctx;

        internal string Namespace => _ctx.Config.RootNamespace;
        internal string ClassName => $"{_physicalName}Controller";

        internal const string SEARCH_ACTION_NAME = "list";
        internal const string CREATE_ACTION_NAME = "create";
        internal const string UPDATE_ACTION_NAME = "update";
        internal const string DELETE_ACTION_NAME = "delete";
        internal const string FIND_ACTION_NAME = "detail";
        internal const string KEYWORDSEARCH_ACTION_NAME = "list-by-keyword";

        internal const string SUBDOMAIN = "api";

        internal string SubDomain => $"{SUBDOMAIN}/{_physicalName}";
        internal string SearchCommandApi => $"/{SubDomain}/{SEARCH_ACTION_NAME}";
        internal string CreateCommandApi => $"/{SubDomain}/{CREATE_ACTION_NAME}";
        internal string UpdateCommandApi => $"/{SubDomain}/{UPDATE_ACTION_NAME}";
        internal string DeleteCommandApi => $"/{SubDomain}/{DELETE_ACTION_NAME}";
        internal string FindCommandApi => $"/{SubDomain}/{FIND_ACTION_NAME}";
        internal string KeywordSearchCommandApi => $"/{SubDomain}/{KEYWORDSEARCH_ACTION_NAME}";

        internal string Render() {
            return $$"""
                namespace {{Namespace}} {
                    using Microsoft.AspNetCore.Mvc;
                    using {{_ctx.Config.EntityNamespace}};

                    [ApiController]
                    [Route("{{SUBDOMAIN}}/[controller]")]
                    public partial class {{ClassName}} : ControllerBase {
                        public {{ClassName}}(ILogger<{{ClassName}}> logger, {{_ctx.Config.DbContextNamespace}}.{{_ctx.Config.DbContextName}} dbContext) {
                            _logger = logger;
                            _dbContext = dbContext;
                        }
                        protected readonly ILogger<{{ClassName}}> _logger;
                        protected readonly {{_ctx.Config.DbContextNamespace}}.{{_ctx.Config.DbContextName}} _dbContext;
                    }
                }
                """;
        }
    }
}