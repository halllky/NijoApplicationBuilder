using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace MyApp.WebApi.Base;

/// <summary>
/// 一般例外発生時のハンドリング処理。
/// </summary>
public class GlobalExceptionFilter : IAsyncExceptionFilter {
    private readonly NLog.Logger _logger;

    public GlobalExceptionFilter(NLog.Logger logger) {
        _logger = logger;
    }

    public Task OnExceptionAsync(ExceptionContext context) {
        var exception = context.Exception;
        var request = context.HttpContext.Request;

        // 例外情報をログに出力
        _logger.Error(exception, "Unhandled exception occurred during request: {Method} {Path}", request.Method, request.Path);

        // クライアントへの応答を設定
        var errorMessage = $"システムエラーが発生しました（{exception.Message}）";
        var result = new ObjectResult(new { message = errorMessage }) {
            StatusCode = 500
        };

        context.Result = result;
        context.ExceptionHandled = true; // 例外処理が完了したことを示す

        return Task.CompletedTask;
    }
}
