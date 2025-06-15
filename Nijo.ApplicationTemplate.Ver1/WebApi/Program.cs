using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyApp.Core;
using MyApp.WebApi.Base;

var builder = WebApplication.CreateBuilder(args);

// コントローラーの追加
builder.Services.AddControllers(options => {
    options.Filters.Add<LoggingActionFilter>(); // ログ出力
    options.Filters.Add<GlobalExceptionFilter>(); // グローバル例外ハンドリング
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Razorページとビューの追加
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

// CORS設定
builder.Services.AddCors(options => {
    options.AddDefaultPolicy(policy => {

        // 開発環境では npm run dev のサーバー（React + Vite）とASP.NET Coreのサーバーが別なので
        // npmのサーバーからのアクセスを許容するようにする
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// NijoApplicationBuilderの設定
var appConfig = new OverridedApplicationConfigure();
appConfig.ConfigureServices(builder.Services);
builder.Services.AddScoped<DefaultConfigurationInWebApi, ConfigurationInWebApi>();

// JSONシリアライズ設定
builder.Services.ConfigureHttpJsonOptions(options => {
    appConfig.EditDefaultJsonSerializerOptions(options.SerializerOptions);
});

// --------------------------------

var app = builder.Build();

// 開発環境の場合はSwaggerを有効にする
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
} else {
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseStaticFiles(); // プロダクション環境でのみ静的ファイルを使用
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
