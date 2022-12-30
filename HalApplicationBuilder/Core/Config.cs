using System;
namespace HalApplicationBuilder.Core {
    public class Config {
        public string EntityNamespace { get; init; }
        public string MvcModelNamespace { get; init; }
        public string MvcControllerNamespace { get; init; }
        public string DbContextNamespace { get; init; }
        public string DbContextName { get; init; }
    }
}
