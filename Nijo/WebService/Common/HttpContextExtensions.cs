using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Nijo.WebService.Common;

/// <summary>
/// HTTPレスポンス・リクエストの共通処理。
/// ASP.NET Core が本来提供していてもおかしくない汎用処理の穴埋め。
/// </summary>
internal static class HttpContextExtensions {
    private static readonly JsonSerializerOptions DEFAULT_JSON_OPTIONS = new() {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    /// <summary>
    /// JSONレスポンスを返す。
    /// ステータスコードは呼び出し側で事前に設定する必要がある。
    /// </summary>
    internal static async Task WriteJsonAsync<T>(
        this HttpContext context,
        T data,
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default) {

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(data, options ?? DEFAULT_JSON_OPTIONS, cancellationToken);
    }

    /// <summary>
    /// エラーレスポンス（テキスト）を返す
    /// </summary>
    internal static async Task WriteErrorAsync(
        this HttpContext context,
        int statusCode,
        string errorMessage,
        CancellationToken cancellationToken = default) {

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "text/plain";
        await context.Response.WriteAsync(errorMessage, cancellationToken);
    }

    /// <summary>
    /// クエリパラメータを取得する。存在しない場合はfalseを返す
    /// </summary>
    internal static bool TryGetQueryParameter(
        this HttpContext context,
        string parameterName,
        out string value) {

        var values = context.Request.Query[parameterName];
        var firstValue = values.FirstOrDefault();

        if (string.IsNullOrEmpty(firstValue)) {
            value = string.Empty;
            return false;
        }

        value = firstValue;
        return true;
    }
}
