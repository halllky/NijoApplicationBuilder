using System;
using System.Text.Json;

namespace HalApplicationBuilder.Serialized {
    public class ConfigJson {
        public string? OutProjectDir { get; set; }
        public string? EntityFrameworkDirectoryRelativePath { get; set; }
        public string? EntityNamespace { get; set; }
        public string? DbContextNamespace { get; set; }
        public string? DbContextName { get; set; }
        public string? MvcControllerDirectoryRelativePath { get; set; }
        public string? MvcControllerNamespace { get; set; }
        public string? MvcModelDirectoryRelativePath { get; set; }
        public string? MvcModelNamespace { get; set; }
        public string? MvcViewDirectoryRelativePath { get; set; }
    }
}

