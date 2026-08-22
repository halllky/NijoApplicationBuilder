using System.Text.Json;
using DevTool.Server;

var builder = WebApplication.CreateBuilder(args);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// JSONオプション
builder.Services.ConfigureHttpJsonOptions(options => {
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
});

// クライアント側のデバッグのためにポートが異なっていてもアクセスできるようにする
const string CORS_POLICY_NAME = "AllowAll";
builder.Services.AddCors(options => {
    options.AddPolicy(CORS_POLICY_NAME, policy => {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 開発中のアプリケーションのデバッグ実行。
// シングルトンとして登録することで、アプリケーション終了時のプロセス破棄をDIコンテナに委ねる。
var projectRoot = Path.GetFullPath(
    builder.Configuration["DevTool:ProjectRoot"] ?? "..",
    builder.Environment.ContentRootPath);
builder.Services.AddSingleton(new Preview(projectRoot, [
    new PreviewProcess(
        Name: "vite",
        Cwd: "client",
        FileName: "npm",
        Args: "run dev",
        StdoutLog: "WebApi/run_npm.log",
        StderrLog: "WebApi/run_npm.error.log",
        AppendStdout: false,
        AppendStderr: true),
    new PreviewProcess(
        Name: "dotnet",
        Cwd: "WebApi",
        FileName: "dotnet",
        Args: "run --launch-profile http",
        StdoutLog: "WebApi/run_dotnet.log",
        StderrLog: "WebApi/run_dotnet.error.log",
        AppendStdout: false,
        AppendStderr: true),
]));

var app = builder.Build();

if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
}

app.UseRouting();
app.UseCors(CORS_POLICY_NAME);

var preview = app.Services.GetRequiredService<Preview>();
var logger = app.Services.GetRequiredService<ILogger<Preview>>();

// デバッグ実行のプロセス群の起動・停止・再起動。
// process クエリパラメータを指定すればそのプロセスのみ、未指定なら全プロセスを対象とする。
// このAPIは /devtool-api 配下に置く。/api 配下は iframe に埋め込む生成後アプリ自身が
// 自身のバックエンド（WebApi）を呼ぶために使うため、衝突を避ける。
app.MapPost("/devtool-api/preview/start", (string? process) => {
    preview.Start(logger, process);
    return Results.Ok();
});
app.MapPost("/devtool-api/preview/stop", (string? process) => {
    preview.Stop(logger, process);
    return Results.Ok();
});
app.MapPost("/devtool-api/preview/restart", (string? process) => {
    preview.Restart(logger, process);
    return Results.Ok();
});

// デバッグ実行のプロセス群の稼働状態と、リクエストで指定されたオフセット以降のログ増分
app.MapPost("/devtool-api/preview/state", (PreviewStateRequest? request) => {
    var offsets = request?.Offsets ?? [];
    var processes = preview.GetState().Select(state => {
        var offset = offsets.GetValueOrDefault(state.Name) ?? new PreviewLogOffsets();
        return new {
            state.Name,
            state.IsRunning,
            state.ProcessId,
            state.ExitCode,
            Stdout = preview.ReadLog(state.Name, E_STD.StdOut, offset.Stdout),
            Stderr = preview.ReadLog(state.Name, E_STD.StdErr, offset.Stderr),
        };
    }).ToList();
    return Results.Ok(new { Processes = processes });
});

// アプリケーション終了時（Ctrl+Cを含む）は稼働中のプロセスを確実に停止する
app.Lifetime.ApplicationStopping.Register(() => preview.Stop(logger));

app.Run();

/// <summary>デバッグ実行の状態取得のリクエストボディ</summary>
record PreviewStateRequest(Dictionary<string, PreviewLogOffsets> Offsets);

/// <summary>1プロセス分の標準出力・標準エラー出力それぞれの既読オフセット</summary>
record PreviewLogOffsets {
    public long Stdout { get; init; }
    public long Stderr { get; init; }
}
