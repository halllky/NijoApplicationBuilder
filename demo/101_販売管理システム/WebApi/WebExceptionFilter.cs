
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;

namespace MyApp.WebApi;

/// <summary>
/// コントローラーアクション内でハンドルされなかった例外をキャッチし、
/// エラーログの出力と統一されたエラーレスポンスの返却を行う。
/// </summary>
internal class WebExceptionFilter : IExceptionFilter {
    public void OnException(ExceptionContext context) {
        var app = context.HttpContext.RequestServices.GetRequiredService<OverridedApplicationService>();

        app.Log.LogError(context.Exception,
            "Unhandled exception: {Method} {Path}",
            context.HttpContext.Request.Method,
            context.HttpContext.Request.Path);

        context.Result = new ObjectResult(new { message = $"システムエラーが発生しました（{context.Exception.Message}）" }) {
            StatusCode = (int)HttpStatusCode.InternalServerError,
        };
        context.ExceptionHandled = true;
    }
}
