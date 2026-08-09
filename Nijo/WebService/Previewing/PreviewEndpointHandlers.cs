using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nijo.WebService.Common;

namespace Nijo.WebService.Previewing;

/// <summary>
/// プレビュー（生成後アプリのデバッグプロセス群）の起動・停止・状態取得エンドポイントハンドラ
/// </summary>
internal class PreviewEndpointHandlers {

    internal PreviewEndpointHandlers(NijoWebService webService) {
        _webService = webService;
    }

    private readonly NijoWebService _webService;

    /// <summary>
    /// nijo.preview.json の内容に従ってプレビュープロセス群を起動する
    /// </summary>
    internal async Task HandleStartPreview(HttpContext context) {
        try {
            var project = await NijoWebService.OpenProjectOrWriteErrorAsync(context);
            if (project == null) return;

            var setting = PreviewSetting.Load(project);
            var logger = context.RequestServices.GetRequiredService<ILogger<NijoWebService>>();
            _webService.GetPreview(project).Start(setting, logger);

            context.Response.StatusCode = StatusCodes.Status200OK;
        } catch (Exception ex) {
            await Console.Error.WriteLineAsync(ex.ToString());

            await context.WriteErrorAsync(
                (int)HttpStatusCode.InternalServerError,
                ex.Message,
                context.RequestAborted);
        }
    }

    /// <summary>
    /// 稼働中のプレビュープロセス群をツリーごと停止する
    /// </summary>
    internal async Task HandleStopPreview(HttpContext context) {
        try {
            var project = await NijoWebService.OpenProjectOrWriteErrorAsync(context);
            if (project == null) return;

            var logger = context.RequestServices.GetRequiredService<ILogger<NijoWebService>>();
            _webService.GetPreview(project).Stop(logger);

            context.Response.StatusCode = StatusCodes.Status200OK;
        } catch (Exception ex) {
            await Console.Error.WriteLineAsync(ex.ToString());

            await context.WriteErrorAsync(
                (int)HttpStatusCode.InternalServerError,
                ex.Message,
                context.RequestAborted);
        }
    }

    /// <summary>
    /// プレビュープロセス群の稼働状態と、リクエストで指定されたオフセット以降のログ増分を返す
    /// </summary>
    internal async Task HandleGetPreviewState(HttpContext context) {
        try {
            var project = await NijoWebService.OpenProjectOrWriteErrorAsync(context);
            if (project == null) return;

            var request = await context.Request.ReadFromJsonAsync<PreviewStateRequest>(context.RequestAborted)
                ?? new PreviewStateRequest();

            var preview = _webService.GetPreview(project);
            var processes = preview.GetState().Select(state => {
                var offsets = request.Offsets.GetValueOrDefault(state.Name) ?? new PreviewLogOffsets();
                return new {
                    name = state.Name,
                    isRunning = state.IsRunning,
                    processId = state.ProcessId,
                    exitCode = state.ExitCode,
                    stdout = preview.ReadLog(state.Name, E_STD.StdOut, offsets.Stdout),
                    stderr = preview.ReadLog(state.Name, E_STD.StdErr, offsets.Stderr),
                };
            }).ToList();

            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.WriteJsonAsync(new { processes }, cancellationToken: context.RequestAborted);
        } catch (Exception ex) {
            await Console.Error.WriteLineAsync(ex.ToString());

            await context.WriteErrorAsync(
                (int)HttpStatusCode.InternalServerError,
                ex.Message,
                context.RequestAborted);
        }
    }

    /// <summary>/api/preview/state のリクエストボディ</summary>
    private class PreviewStateRequest {
        [JsonPropertyName("offsets")]
        public Dictionary<string, PreviewLogOffsets> Offsets { get; set; } = new();
    }

    /// <summary>1プロセス分の標準出力・標準エラー出力それぞれの既読オフセット</summary>
    private class PreviewLogOffsets {
        [JsonPropertyName("stdout")]
        public long Stdout { get; set; }
        [JsonPropertyName("stderr")]
        public long Stderr { get; set; }
    }
}
