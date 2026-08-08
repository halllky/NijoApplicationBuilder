using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Debugging;

namespace MyApp.WebApi;

public record LoginRequest(string UserId, string Password);

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
    /// ログイン。デモ用のため任意のユーザーIDでログイン可能。
    /// </summary>
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest req) {
        if (string.IsNullOrWhiteSpace(req.UserId))
            return BadRequest("ユーザーIDを入力してください。");

        var userInfo = new ログインユーザー情報 {
            ユーザーID = req.UserId,
            表示名 = req.UserId,
            メールアドレス = $"{req.UserId}@example.local",
            最終ログイン日時 = DateTime.Now,
        };
        return Ok(userInfo);
    }

    /// <summary>
    /// ログアウト。
    /// </summary>
    [HttpPost("logout")]
    public IActionResult Logout() {
        return Ok();
    }

    /// <summary>
    /// ログイン状態確認。Cookieベースのセッションを使わないデモ用なので常に未ログイン扱い。
    /// </summary>
    [HttpGet("login-status")]
    public IActionResult LoginStatus() {
        return NoContent();
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
            var generator = new OverridedDummyDataGenerator();
            await generator.GenerateAsync(_app);

            return Ok();

        } catch (Exception ex) {
            return Problem(ex.ToString());
        }
    }
}
