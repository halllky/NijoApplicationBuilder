using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nijo.Parts.Utility;
using Nijo.Util.CodeGenerating;

namespace Nijo.Features.Logging {
    internal class HttpResponseExceptionFilter {
        internal const string CLASSNAME = "HttpResponseExceptionFilter";

        internal static SourceFile Render(ICodeRenderingContext ctx) => new SourceFile {
            FileName = "HttpResponseExceptionFilter.cs",
            RenderContent = () => $$"""
                namespace {{ctx.Config.RootNamespace}} {
                    using Microsoft.AspNetCore.Mvc.Filters;
                    using Microsoft.AspNetCore.Mvc;
                    using System.Net;
                    using System.Text.Json;
                    using Microsoft.AspNetCore.Http.Extensions;

                    public class {{CLASSNAME}} : IActionFilter {
                        public {{CLASSNAME}}(ILogger logger) {
                            _logger = logger;
                        }
                        private readonly ILogger _logger;

                        public void OnActionExecuting(ActionExecutingContext context) { }

                        public void OnActionExecuted(ActionExecutedContext context) {
                            if (context.Exception != null) {
                                _logger.LogCritical(context.Exception, "Internal Server Error: {Url}", context.HttpContext.Request.GetDisplayUrl());

                                context.Result = ((ControllerBase)context.Controller).JsonContent(new {
                                    content = {{UtilityClass.CLASSNAME}}.{{UtilityClass.TO_JSON}}(new[] {
                                        context.Exception.ToString(),
                                    }),
                                });
                                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                                context.ExceptionHandled = true;
                            }
                        }
                    }
                }
                """,
        };
    }
}
