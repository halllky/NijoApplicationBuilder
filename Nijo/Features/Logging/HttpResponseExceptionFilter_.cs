using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nijo.Parts.WebServer;
using Nijo.Util.CodeGenerating;

namespace Nijo.Features.Logging {
    internal class HttpResponseExceptionFilter {
        internal const string CLASSNAME = "HttpResponseExceptionFilter";

        internal static SourceFile Render(CodeRenderingContext ctx) => new SourceFile {
            FileName = "HttpResponseExceptionFilter.cs",
            RenderContent = context => $$"""
                namespace {{ctx.Config.RootNamespace}} {
                    using Microsoft.AspNetCore.Mvc.Filters;
                    using Microsoft.AspNetCore.Mvc;
                    using System.Net;
                    using System.Text.Json;
                    using Microsoft.AspNetCore.Http.Extensions;
                    using NLog;

                    public class {{CLASSNAME}} : IExceptionFilter, IActionFilter {
                        public {{CLASSNAME}}(Logger logger) {
                            _logger = logger;
                        }
                        private readonly Logger _logger;

                        public void OnActionExecuting(ActionExecutingContext context) {
                            _logger.Properties["ClientUrl"] = context.HttpContext.Request.Headers["Nijo-Client-URL"];
                            _logger.Properties["ServerUrl"] = context.HttpContext.Request.Path;
                            _logger.Properties["RequestBody"] = context.HttpContext.Request.Body;
                            _logger.Info("OnActionExecuting");
                        }

                        public void OnActionExecuted(ActionExecutedContext context) {
                            _logger.Properties["ClientUrl"] = context.HttpContext.Request.Headers["Nijo-Client-URL"];
                            _logger.Properties["ServerUrl"] = context.HttpContext.Request.Path;
                            _logger.Properties["ResponseStatusCode"] = context.HttpContext.Response.StatusCode;
                            _logger.Info("OnActionExecuted");
                        }

                        public void OnException(ExceptionContext context) {
                            _logger.Error(context.Exception, "Internal Server Error: {Url}", context.HttpContext.Request.GetDisplayUrl());
                            var contentResult = new ContentResult();
                            contentResult.ContentType = "application/json";
                            contentResult.Content = Util.ToJson(new[] {
                                context.Exception.ToString(),
                            });
                            context.Result = contentResult;
                            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                            context.ExceptionHandled = true;
                        }
                    }
                }
                """,
        };
    }
}
