using Microsoft.Extensions.WebEncoders;
using System.Text.Encodings.Web;
using System.Text.Unicode;

var builder = WebApplication.CreateBuilder(args);

var runtimeRootDir = System.IO.Directory.GetCurrentDirectory();
HalApplicationBuilder.HalApp.Configure(
    builder.Services,
    System.Reflection.Assembly.GetExecutingAssembly(),
    System.IO.Path.Combine(runtimeRootDir, "halapp.json"));

// HTMLのエンコーディングをUTF-8にする(日本語のHTMLエンコード防止)
builder.Services.Configure<WebEncoderOptions>(options => {
    options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All);
});
// SaveやDetailでDbContextをダイレクトに参照しているため // TODO: configの情報からHalApp内でやるべきでは
builder.Services.AddScoped<Microsoft.EntityFrameworkCore.DbContext>(provider => {
    return provider.GetRequiredService<HalApplicationBuilder.Test.DistMvc.EntityFramework.MyDbContext>();
});
builder.Services.AddDbContext<HalApplicationBuilder.Test.DistMvc.EntityFramework.MyDbContext>(option => {
    var connStr = $"Data Source=\"{System.IO.Path.Combine(runtimeRootDir, "bin", "Debug", "debug.sqlite3")}\"";
    Microsoft.EntityFrameworkCore.ProxiesExtensions.UseLazyLoadingProxies(option);
    Microsoft.EntityFrameworkCore.SqliteDbContextOptionsBuilderExtensions.UseSqlite(option, connStr);
});

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
