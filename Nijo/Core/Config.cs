using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Nijo.Core {
    public class Config {
        public required string RootNamespace { get; init; }

        public string EntityNamespace => RootNamespace;
        public string DbContextNamespace => RootNamespace;
        public required string DbContextName { get; init; }

        /// <summary>
        /// 一時保存を使用しない
        /// </summary>
        public required bool DisableLocalRepository { get; init; }
        /// <summary>
        /// 一括登録APIを使用しない
        /// </summary>
        public required bool DisableBatchUpdate {  get; init; }
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

        private const string DISABLE_LOCAL_REPOSITORY = "DisableLocalRepository";
        private const string DISABLE_BATCH_UPDATE = "DisableBatchUpdate";
        private const string DISCARD_SEARCH_LIMIT = "DiscardSearchLimit";

        public XElement ToXmlWithRoot() {
            var root = new XElement(RootNamespace);

            var configElement = new XElement(XML_CONFIG_SECTION_NAME);
            root.Add(configElement);

            // 各種機能の有効無効
            if (DisableLocalRepository) root.SetAttributeValue(DISABLE_LOCAL_REPOSITORY, "True");
            if (DisableBatchUpdate) root.SetAttributeValue(DISABLE_BATCH_UPDATE, "True");
            if (DiscardSearchLimit) root.SetAttributeValue(DISCARD_SEARCH_LIMIT, "True");

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

            return new Config {
                RootNamespace = xDocument.Root.Name.LocalName.ToCSharpSafe(),
                DisableLocalRepository = xDocument.Root.Attribute(DISABLE_LOCAL_REPOSITORY) != null,
                DisableBatchUpdate = xDocument.Root.Attribute(DISABLE_BATCH_UPDATE) != null,
                DiscardSearchLimit = xDocument.Root.Attribute(DISCARD_SEARCH_LIMIT) != null,
                DbContextName = configSection?.Element(DBCONTEXT_NAME)?.Value ?? "MyDbContext",
            };
        }
    }
}
