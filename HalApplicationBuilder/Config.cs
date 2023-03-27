using System;
using System.Xml.Linq;

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
        internal const string XML_CONFIG_SECTION_NAME = "_Config";
        public static Config FromXml(string xml) {
            var xDocument = XDocument.Parse(xml);
            if (xDocument.Root == null) throw new FormatException($"設定ファイルのXMLの形式が不正です。");

            var configSection = xDocument.Root.Element(XML_CONFIG_SECTION_NAME);
            var rel = configSection?.Element("OutDirRelativePath");
            var ns = configSection?.Element("Namespace");

            return new Config {
                OutProjectDir = configSection?.Element("OutDirRoot")?.Value ?? string.Empty,

                EntityFrameworkDirectoryRelativePath = rel?.Element("EFCore")?.Value ?? string.Empty,
                EntityNamespace = ns?.Element("EntityNamespace")?.Value ?? string.Empty,
                DbContextNamespace = ns?.Element("DbContextNamespace")?.Value ?? string.Empty,
                DbContextName = configSection?.Element("DbContextName")?.Value ?? string.Empty,

                MvcControllerDirectoryRelativePath = rel?.Element("MvcController")?.Value ?? string.Empty,
                MvcControllerNamespace = ns?.Element("MvcControllerNamespace")?.Value ?? string.Empty,

                MvcModelDirectoryRelativePath = rel?.Element("MvcModel")?.Value ?? string.Empty,
                MvcModelNamespace = ns?.Element("MvcModelNamespace")?.Value ?? string.Empty,

                MvcViewDirectoryRelativePath = rel?.Element("MvcView")?.Value ?? string.Empty,
            };
        }
    }
}
