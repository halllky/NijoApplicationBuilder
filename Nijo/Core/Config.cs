using Nijo.Util.DotnetEx;
using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace Nijo.Core {
    public class Config {
        public required string ApplicationName { get; init; }

        public string RootNamespace => ApplicationName.ToCSharpSafe();

        public string EntityNamespace => RootNamespace;
        public string DbContextNamespace => RootNamespace;
        public required string DbContextName { get; init; }


        internal const string XML_CONFIG_SECTION_NAME = "_Config";

        private const string SECTION_RELATIVE_PATHS = "OutDirRelativePath";
        private const string SECTION_NAMESPACES = "Namespace";

        private const string NAMESPACE_EFCORE_ENTITY = "EntityNamespace";
        private const string NAMESPACE_DBCONTEXT = "DbContextNamespace";

        private const string DBCONTEXT_NAME = "DbContextName";

        public XElement ToXmlWithRoot() {
            var root = new XElement(ApplicationName);

            var configElement = new XElement(XML_CONFIG_SECTION_NAME);
            root.Add(configElement);

            // セクション: 相対パス
            var outDirRelativePathElement = new XElement(SECTION_RELATIVE_PATHS);
            configElement.Add(outDirRelativePathElement);

            // セクション: 名前空間
            var namespaceElement = new XElement(SECTION_NAMESPACES);
            configElement.Add(namespaceElement);

            var dbContextNamespaceElement = new XElement(NAMESPACE_DBCONTEXT, DbContextNamespace);
            var entityNamespaceElement = new XElement(NAMESPACE_EFCORE_ENTITY, EntityNamespace);

            namespaceElement.Add(dbContextNamespaceElement);
            namespaceElement.Add(entityNamespaceElement);

            // セクション: DBコンテキスト名
            var dbContextNameElement = new XElement(DBCONTEXT_NAME, DbContextName);
            configElement.Add(dbContextNameElement);

            return root;
        }

        public static Config FromXml(XDocument xDocument) {
            if (xDocument.Root == null) throw new FormatException($"設定ファイルのXMLの形式が不正です。");

            var configSection = xDocument.Root.Element(XML_CONFIG_SECTION_NAME);
            var ns = configSection?.Element(SECTION_NAMESPACES);

            return new Config {
                ApplicationName = xDocument.Root.Name.LocalName,
                DbContextName = configSection?.Element(DBCONTEXT_NAME)?.Value ?? "MyDbContext",
            };
        }
    }
}
