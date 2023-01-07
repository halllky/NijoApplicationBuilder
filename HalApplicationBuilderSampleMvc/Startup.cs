using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HalApplicationBuilderSampleMvc {
    public class Startup {
        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            services.AddControllersWithViews();

            services.AddDbContext<EntityFramework.SampleDbContext>(option => {
                var connStr = $"Data Source=\"{System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "bin", "Debug", "debug.sqlite3")}\"";
                option.UseSqlite(connStr);
            });

            // SaveやDetailでDbContextをダイレクトに参照しているため
            services.AddScoped<DbContext>(provider => provider.GetService<EntityFramework.SampleDbContext>());

            var dllPath = "/__local__/20221211_haldoc_csharp/haldoc/HalApplicationBuilderSampleSchema/bin/Debug/net5.0/HalApplicationBuilderSampleSchema.dll";
            HalApplicationBuilder.Program.ConfigureServices(services, dllPath);
            services.AddScoped(provider => {
                var schemaAssembly = Assembly.LoadFile(dllPath);
                var runtimeAssembly = Assembly.GetExecutingAssembly();
                return new HalApplicationBuilder.Runtime.RuntimeContext(schemaAssembly, runtimeAssembly, provider);
            });

            // HTMLのエンコーディングをUTF-8にする(日本語のHTMLエンコード防止)
            services.Configure<Microsoft.Extensions.WebEncoders.WebEncoderOptions>(options => {
                options.TextEncoderSettings = new System.Text.Encodings.Web.TextEncoderSettings(System.Text.Unicode.UnicodeRanges.All);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            } else {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
