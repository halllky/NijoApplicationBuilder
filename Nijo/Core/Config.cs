using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Nijo.Core {
    public class Config {
        public required string ApplicationName { get; init; }

        public string RootNamespace => ApplicationName.ToCSharpSafe();

        public string EntityNamespace => RootNamespace;
        public string DbContextNamespace => RootNamespace;
        public required string DbContextName { get; init; }

        public IEnumerable<string> OverridedApplicationServiceCodeForUnitTest { get; init; } = Enumerable.Empty<string>();

        /// <summary>
        /// <see cref="Features.Storing.FindManyFeature"/> で検索上限件数が指定されなかった場合に自動的に件数を絞る制限を外す
        /// </summary>
        public required bool DiscardSearchLimit { get; init; }

        internal const string XML_CONFIG_SECTION_NAME = "_Config";

        private const string SECTION_RELATIVE_PATHS = "OutDirRelativePath";
        private const string SECTION_NAMESPACES = "Namespace";

        private const string NAMESPACE_EFCORE_ENTITY = "EntityNamespace";
        private const string NAMESPACE_DBCONTEXT = "DbContextNamespace";

        private const string DBCONTEXT_NAME = "DbContextName";

        private const string DISCARD_SEARCH_LIMIT = "DiscardSearchLimit";

        internal const string REPLACE_OVERRIDED_APPLICATION_SERVICE_CODE_FOR_UNIT_TEST = "ReplaceOverridedApplicationServiceCodeForUnitTest";

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

            var overrridedCode = xDocument.Root
                .Elements(REPLACE_OVERRIDED_APPLICATION_SERVICE_CODE_FOR_UNIT_TEST)
                .Select(el => el.Value.Replace("\n", "\r\n"));

            return new Config {
                ApplicationName = xDocument.Root.Name.LocalName,
                DiscardSearchLimit = xDocument.Root.Attribute(DISCARD_SEARCH_LIMIT) != null,
                DbContextName = configSection?.Element(DBCONTEXT_NAME)?.Value ?? "MyDbContext",
                OverridedApplicationServiceCodeForUnitTest = overrridedCode,
            };
        }
    }
}
