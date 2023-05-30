using HalApplicationBuilder.DotnetEx;
using System;
using System.Xml;
using System.Xml.Linq;

namespace HalApplicationBuilder.Core20230514 {
    public class Config {
        public required string ApplicationName { get; init; }

        public required string OutProjectDir { get; init; }

        public string RootNamespace => ApplicationName.ToCSharpSafe();

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
        public static Config FromJson(Serialized.AppSchemaJson? json) {
            if (json == null) throw new ArgumentNullException(nameof(json));
            if (json.Config == null) throw new ArgumentNullException(nameof(json.Config));
            return new Config {
                ApplicationName = json.Name ?? string.Empty,

                OutProjectDir = json.Config.OutProjectDir ?? string.Empty,

                EntityFrameworkDirectoryRelativePath = json.Config.EntityFrameworkDirectoryRelativePath ?? string.Empty,
                EntityNamespace = json.Config.EntityNamespace ?? string.Empty,
                DbContextNamespace = json.Config.DbContextNamespace ?? string.Empty,
                DbContextName = json.Config.DbContextName ?? string.Empty,

                MvcControllerDirectoryRelativePath = json.Config.MvcControllerDirectoryRelativePath ?? string.Empty,
                MvcControllerNamespace = json.Config.MvcControllerNamespace ?? string.Empty,

                MvcModelDirectoryRelativePath = json.Config.MvcModelDirectoryRelativePath ?? string.Empty,
                MvcModelNamespace = json.Config.MvcModelNamespace ?? string.Empty,

                MvcViewDirectoryRelativePath = json.Config.MvcViewDirectoryRelativePath ?? string.Empty,
            };
        }


        internal const string XML_CONFIG_SECTION_NAME = "_Config";

        private const string SECTION_OUT_DIR_ROOT = "OutDirRoot";
        private const string SECTION_RELATIVE_PATHS = "OutDirRelativePath";
        private const string SECTION_NAMESPACES = "Namespace";

        private const string RELATIVEPATH_EFCORE = "EFCore";
        private const string RELATIVEPATH_MVC_MODEL = "MvcModel";
        private const string RELATIVEPATH_MVC_VIEW = "MvcView";
        private const string RELATIVEPATH_MVC_CONTROLLER = "MvcController";

        private const string NAMESPACE_MVC_CONTROLLER = "MvcControllerNamespace";
        private const string NAMESPACE_MVC_MODEL = "MvcModelNamespace";
        private const string NAMESPACE_EFCORE_ENTITY = "EntityNamespace";
        private const string NAMESPACE_DBCONTEXT = "DbContextNamespace";

        private const string DBCONTEXT_NAME = "DbContextName";

        public XElement ToXmlWithRoot() {
            var root = new XElement(ApplicationName);

            var configElement = new XElement(XML_CONFIG_SECTION_NAME);
            root.Add(configElement);

            // セクション: 出力先ディレクトリ
            var outDirRootElement = new XElement(SECTION_OUT_DIR_ROOT, OutProjectDir);
            configElement.Add(outDirRootElement);

            // セクション: 相対パス
            var outDirRelativePathElement = new XElement(SECTION_RELATIVE_PATHS);
            configElement.Add(outDirRelativePathElement);

            var efCoreElement = new XElement(RELATIVEPATH_EFCORE, EntityFrameworkDirectoryRelativePath);
            var mvcModelElement = new XElement(RELATIVEPATH_MVC_MODEL, MvcModelDirectoryRelativePath);
            var mvcViewElement = new XElement(RELATIVEPATH_MVC_VIEW, MvcViewDirectoryRelativePath);
            var mvcControllerElement = new XElement(RELATIVEPATH_MVC_CONTROLLER, MvcControllerDirectoryRelativePath);

            outDirRelativePathElement.Add(efCoreElement);
            outDirRelativePathElement.Add(mvcModelElement);
            outDirRelativePathElement.Add(mvcViewElement);
            outDirRelativePathElement.Add(mvcControllerElement);

            // セクション: 名前空間
            var namespaceElement = new XElement(SECTION_NAMESPACES);
            configElement.Add(namespaceElement);

            var dbContextNamespaceElement = new XElement(NAMESPACE_DBCONTEXT, DbContextNamespace);
            var entityNamespaceElement = new XElement(NAMESPACE_EFCORE_ENTITY, EntityNamespace);
            var mvcModelNamespaceElement = new XElement(NAMESPACE_MVC_MODEL, MvcModelNamespace);
            var mvcControllerNamespaceElement = new XElement(NAMESPACE_MVC_CONTROLLER, MvcControllerNamespace);

            namespaceElement.Add(dbContextNamespaceElement);
            namespaceElement.Add(entityNamespaceElement);
            namespaceElement.Add(mvcModelNamespaceElement);
            namespaceElement.Add(mvcControllerNamespaceElement);

            // セクション: DBコンテキスト名
            var dbContextNameElement = new XElement(DBCONTEXT_NAME, DbContextName);
            configElement.Add(dbContextNameElement);

            return root;
        }

        public static Config FromXml(XDocument xDocument) {
            if (xDocument.Root == null) throw new FormatException($"設定ファイルのXMLの形式が不正です。");

            var configSection = xDocument.Root.Element(XML_CONFIG_SECTION_NAME);
            var rel = configSection?.Element(SECTION_RELATIVE_PATHS);
            var ns = configSection?.Element(SECTION_NAMESPACES);

            return new Config {
                ApplicationName = xDocument.Root.Name.LocalName,

                OutProjectDir = configSection?.Element(SECTION_OUT_DIR_ROOT)?.Value ?? string.Empty,

                EntityFrameworkDirectoryRelativePath = rel?.Element(RELATIVEPATH_EFCORE)?.Value ?? string.Empty,
                EntityNamespace = ns?.Element(NAMESPACE_EFCORE_ENTITY)?.Value ?? string.Empty,
                DbContextNamespace = ns?.Element(NAMESPACE_DBCONTEXT)?.Value ?? string.Empty,
                DbContextName = configSection?.Element(DBCONTEXT_NAME)?.Value ?? string.Empty,

                MvcControllerDirectoryRelativePath = rel?.Element(RELATIVEPATH_MVC_CONTROLLER)?.Value ?? string.Empty,
                MvcControllerNamespace = ns?.Element(NAMESPACE_MVC_CONTROLLER)?.Value ?? string.Empty,

                MvcModelDirectoryRelativePath = rel?.Element(RELATIVEPATH_MVC_MODEL)?.Value ?? string.Empty,
                MvcModelNamespace = ns?.Element(NAMESPACE_MVC_MODEL)?.Value ?? string.Empty,

                MvcViewDirectoryRelativePath = rel?.Element(RELATIVEPATH_MVC_VIEW)?.Value ?? string.Empty,
            };
        }
    }
}
