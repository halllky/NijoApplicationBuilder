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
                    using Microsoft.AspNetCore.Mvc.Infrastructure;
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
                        private IDisposable? _logScope;

                        public void OnActionExecuting(ActionExecutingContext context) {
                            _logScope = NLog.ScopeContext.PushProperties([
                                KeyValuePair.Create("ClientUrl", context.HttpContext.Request.Headers["Nijo-Client-URL"].ToString()),
                                KeyValuePair.Create("ServerUrl", System.Web.HttpUtility.UrlDecode(context.HttpContext.Request.GetEncodedPathAndQuery()) ?? string.Empty),
                                ]);
                            _logger.Info("START");
                        }

                        public void OnActionExecuted(ActionExecutedContext context) {
                            string? strStatusCode;
                            if (context.Result is IStatusCodeActionResult statusCodeActionResult) {
                                strStatusCode = statusCodeActionResult.StatusCode?.ToString();
                            } else {
                                strStatusCode = string.Empty;
                            }
                            _logger.Info(new LogEventInfo {
                                Message = "END (Http Status Code: {0})",
                                Parameters = [strStatusCode],
                                Properties = { ["Option"] = strStatusCode },
                            });
                            _logScope?.Dispose();
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
