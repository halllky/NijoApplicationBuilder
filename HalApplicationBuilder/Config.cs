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


        public Serialized.ConfigJson ToJson(bool onlyRuntimeConfig) {
            return new Serialized.ConfigJson {
                OutProjectDir = onlyRuntimeConfig ? null : OutProjectDir,

                EntityFrameworkDirectoryRelativePath = EntityFrameworkDirectoryRelativePath,
                EntityNamespace = EntityNamespace,
                DbContextNamespace = DbContextNamespace,
                DbContextName = DbContextName,

                MvcControllerDirectoryRelativePath = MvcControllerDirectoryRelativePath,
                MvcControllerNamespace = MvcControllerNamespace,

                MvcModelDirectoryRelativePath = MvcModelDirectoryRelativePath,
                MvcModelNamespace = MvcModelNamespace,

                MvcViewDirectoryRelativePath = MvcViewDirectoryRelativePath,
            };
        }
        public static Config FromJson(Serialized.ConfigJson? json) {
            if (json == null) throw new ArgumentNullException(nameof(json));
            return new Config {
                OutProjectDir = json.OutProjectDir ?? string.Empty,

                EntityFrameworkDirectoryRelativePath = json.EntityFrameworkDirectoryRelativePath ?? string.Empty,
                EntityNamespace = json.EntityNamespace ?? string.Empty,
                DbContextNamespace = json.DbContextNamespace ?? string.Empty,
                DbContextName = json.DbContextName ?? string.Empty,

                MvcControllerDirectoryRelativePath = json.MvcControllerDirectoryRelativePath ?? string.Empty,
                MvcControllerNamespace = json.MvcControllerNamespace ?? string.Empty,

                MvcModelDirectoryRelativePath = json.MvcModelDirectoryRelativePath ?? string.Empty,
                MvcModelNamespace = json.MvcModelNamespace ?? string.Empty,

                MvcViewDirectoryRelativePath = json.MvcViewDirectoryRelativePath ?? string.Empty,
            };
        }
    }
}
