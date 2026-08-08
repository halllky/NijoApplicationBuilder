#if DEBUG

using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Debugging;
using MyApp.WebApi.Base;

namespace MyApp.WebApi;

/// <summary>
/// 自動生成に頼らず自前でControllerを定義する場合の実装例
/// </summary>
[ApiController]
[Route("api/example")]
[AllowAnonymous]
public class ExampleContoller : ControllerBase {

    // 設定やサービスクラスは DI コンテナ経由で受け取ってください。
    public ExampleContoller(OverridedApplicationService app) {
        _app = app;
    }
    private readonly OverridedApplicationService _app;

    /// <summary>
    /// クライアント側とサーバー側の疎通確認の例。
    /// </summary>
    [HttpGet]
    public IActionResult Index() {

        var jsonOptions = new JsonSerializerOptions().EditDefaultJsonSerializerOptions();

        return Ok($$"""
            ASP.NET Core サーバーとの接続に成功しました。

            実行時設定ファイル(appsettings.json)の内容は以下です。

            {{JsonSerializer.Serialize(_app.Settings, jsonOptions)}}
            """);
    }

    /// <summary>
    /// データベースを削除して再作成する。
    /// </summary>
    [HttpPost("destroy-and-recreate-database")]
    public async Task<IActionResult> DestroyAndRecreateDatabase() {
        try {
            // データベースを削除して再作成
            await _app.DbContext.Database.EnsureDeletedAsync();
            await _app.DbContext.EnsureCreatedAsyncEx(_app.Settings);

            // ダミーデータの生成
            var generator = new OverridedDummyDataGenerator(messages => new PresentationContextInWebApi<MessageSetter> {
                IgnoreConfirm = true,
                Messages = messages.As<MessageSetter>(),
                Confirms = [],
            });
            var result = await generator.GenerateAsync(_app);

            if (result.HasError()) {
                return Problem(string.Join("\n", [
                    "ダミーデータの生成に失敗しました。",
                    ..result.GetAllMessages(),
                ]));
            }

            return Ok();

        } catch (Exception ex) {
            return Problem(ex.ToString());
        }
    }
}

#endif
