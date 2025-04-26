using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MyApp.WebApi.Base;

/// <summary>
/// HTTPレベルでのログ出力処理。
/// </summary>
public class LoggingActionFilter : IAsyncActionFilter {
    private readonly NLog.Logger _logger;

    public LoggingActionFilter(NLog.Logger logger) {
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
        var stopwatch = Stopwatch.StartNew();

        // アクション実行
        var executedContext = await next();

        // ログ出力
        stopwatch.Stop();
        var response = executedContext.HttpContext.Response;

        _logger.Info("Request End: {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds}ms",
            context.HttpContext.Request.Method,
            context.HttpContext.Request.Path,
            response.StatusCode,
            stopwatch.ElapsedMilliseconds);
    }
}
