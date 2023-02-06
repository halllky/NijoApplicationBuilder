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
            const string SECTION = "HalApplicationBuiilder.Test.DistMvc";
            const string KEY = "CsprojDir";
            var setting = _configuration.GetSection(SECTION)[KEY];
            if (setting == null) throw new InvalidOperationException($"Missing cofig: \"{SECTION}\": {{ \"{{KEY}}\": ... }}");
            return Path.Combine(Directory.GetCurrentDirectory(), setting);
        }
    }
}
