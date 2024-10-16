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
        /// 一覧画面の詳細リンクの挙動
        /// </summary>
        public required E_MultiViewDetailLinkBehavior MultiViewDetailLinkBehavior { get; init; } = E_MultiViewDetailLinkBehavior.NavigateToEditMode;
        /// <summary>
        /// 一覧画面の詳細リンクの挙動
        /// </summary>
        public enum E_MultiViewDetailLinkBehavior {
            /// <summary>「詳細」リンクで読み取り専用モードの詳細画面に遷移する</summary>
            NavigateToReadOnlyMode,
            /// <summary>「詳細」リンクで編集モードの詳細画面に遷移する（既定値）</summary>
            NavigateToEditMode,
        }

        internal const string XML_CONFIG_SECTION_NAME = "_Config";

        private const string SECTION_RELATIVE_PATHS = "OutDirRelativePath";
        private const string SECTION_NAMESPACES = "Namespace";

        private const string NAMESPACE_EFCORE_ENTITY = "EntityNamespace";
        private const string NAMESPACE_DBCONTEXT = "DbContextNamespace";

        private const string DBCONTEXT_NAME = "DbContextName";

        private const string DISABLE_LOCAL_REPOSITORY = "DisableLocalRepository";

        private const string MULTI_VIEW_DETAIL_LINK_BEHAVIOR = "MultiViewDetailLinkBehavior";

        public XElement ToXmlWithRoot() {
            var root = new XElement(RootNamespace);

            var configElement = new XElement(XML_CONFIG_SECTION_NAME);
            root.Add(configElement);

            // 各種機能の有効無効など
            if (DisableLocalRepository) root.SetAttributeValue(DISABLE_LOCAL_REPOSITORY, "True");

            if (MultiViewDetailLinkBehavior == E_MultiViewDetailLinkBehavior.NavigateToReadOnlyMode)
                root.SetAttributeValue(MULTI_VIEW_DETAIL_LINK_BEHAVIOR, E_MultiViewDetailLinkBehavior.NavigateToReadOnlyMode.ToString());

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
                DbContextName = configSection?.Element(DBCONTEXT_NAME)?.Value ?? "MyDbContext",
                MultiViewDetailLinkBehavior = xDocument.Root.Attribute(MULTI_VIEW_DETAIL_LINK_BEHAVIOR)?.Value == E_MultiViewDetailLinkBehavior.NavigateToReadOnlyMode.ToString()
                    ? E_MultiViewDetailLinkBehavior.NavigateToReadOnlyMode
                    : E_MultiViewDetailLinkBehavior.NavigateToEditMode,
            };
        }
    }
}
