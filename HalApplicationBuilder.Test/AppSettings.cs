using System;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace HalApplicationBuilder.Test {
    public class AppSettings {

        public static AppSettings Load() {
            var settings = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            return new AppSettings(settings);
        }

        private AppSettings(IConfiguration configuration) {
            _configuration = configuration;
        }

        private readonly IConfiguration _configuration;

        public string GetTestAppCsprojDir() {
            var setting = _configuration.GetSection("HalApplicationBuiilder.Test.DistMvc")["CsprojDir"];
            return Path.Combine(Directory.GetCurrentDirectory(), setting);
        }
    }
}
