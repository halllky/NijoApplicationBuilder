using System;
namespace HalApplicationBuilder.Core {
    public class Config {
        public required string OutProjectDir { get; init; }

        public required string EntityFrameworkDirectoryRelativePath { get; init; }
        public required string EntityNamespace { get; init; }
        public required string DbContextNamespace { get; init; }
        public required string DbContextName { get; init; }

        public required string MvcControllerDirectoryRelativePath { get; init; }
        public required string MvcControllerNamespace { get; init; }

        public required string MvcModelDirectoryRelativePath { get; init; }
        public required string MvcModelNamespace { get; init; }

        public required string MvcViewDirectoryRelativePath { get; init; }
    }
}
