using System;
using System.Xml;
using System.Xml.Linq;

namespace HalApplicationBuilder.Core {
    public class Config {
        public required string ApplicationName { get; init; }

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

        public string ToXml() {
            var xmlDoc = new XmlDocument();

            var root = xmlDoc.CreateElement(ApplicationName);
            xmlDoc.AppendChild(root);

            var configElement = xmlDoc.CreateElement(XML_CONFIG_SECTION_NAME);
            root.AppendChild(configElement);

            // セクション: 出力先ディレクトリ
            var outDirRootElement = xmlDoc.CreateElement(SECTION_OUT_DIR_ROOT);
            outDirRootElement.InnerText = OutProjectDir;
            configElement.AppendChild(outDirRootElement);

            // セクション: 相対パス
            var outDirRelativePathElement = xmlDoc.CreateElement(SECTION_RELATIVE_PATHS);
            configElement.AppendChild(outDirRelativePathElement);

            var efCoreElement = xmlDoc.CreateElement(RELATIVEPATH_EFCORE);
            efCoreElement.InnerText = EntityFrameworkDirectoryRelativePath;
            outDirRelativePathElement.AppendChild(efCoreElement);

            var mvcModelElement = xmlDoc.CreateElement(RELATIVEPATH_MVC_MODEL);
            mvcModelElement.InnerText = MvcModelDirectoryRelativePath;
            outDirRelativePathElement.AppendChild(mvcModelElement);

            var mvcViewElement = xmlDoc.CreateElement(RELATIVEPATH_MVC_VIEW);
            mvcViewElement.InnerText = MvcViewDirectoryRelativePath;
            outDirRelativePathElement.AppendChild(mvcViewElement);

            var mvcControllerElement = xmlDoc.CreateElement(RELATIVEPATH_MVC_CONTROLLER);
            mvcControllerElement.InnerText = MvcControllerDirectoryRelativePath;
            outDirRelativePathElement.AppendChild(mvcControllerElement);

            // セクション: 名前空間
            var namespaceElement = xmlDoc.CreateElement(SECTION_NAMESPACES);
            configElement.AppendChild(namespaceElement);

            var dbContextNamespaceElement = xmlDoc.CreateElement(NAMESPACE_DBCONTEXT);
            dbContextNamespaceElement.InnerText = DbContextNamespace;
            namespaceElement.AppendChild(dbContextNamespaceElement);

            var entityNamespaceElement = xmlDoc.CreateElement(NAMESPACE_EFCORE_ENTITY);
            entityNamespaceElement.InnerText = EntityNamespace;
            namespaceElement.AppendChild(entityNamespaceElement);

            var mvcModelNamespaceElement = xmlDoc.CreateElement(NAMESPACE_MVC_MODEL);
            mvcModelNamespaceElement.InnerText = MvcModelNamespace;
            namespaceElement.AppendChild(mvcModelNamespaceElement);

            var mvcControllerNamespaceElement = xmlDoc.CreateElement(NAMESPACE_MVC_CONTROLLER);
            mvcControllerNamespaceElement.InnerText = MvcControllerNamespace;
            namespaceElement.AppendChild(mvcControllerNamespaceElement);

            var dbContextNameElement = xmlDoc.CreateElement(DBCONTEXT_NAME);
            dbContextNameElement.InnerText = DbContextName;
            configElement.AppendChild(dbContextNameElement);

            return xmlDoc.OuterXml;
        }
        public static Config FromXml(string xml) {
            var xDocument = XDocument.Parse(xml);
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
