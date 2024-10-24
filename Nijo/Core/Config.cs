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

        /// <summary>DBカラム名（作成者）</summary>
        public required string? CreateUserDbColumnName { get; init; }
        /// <summary>DBカラム名（更新者）</summary>
        public required string? UpdateUserDbColumnName { get; init; }
        /// <summary>DBカラム名（作成時刻）</summary>
        public required string? CreatedAtDbColumnName { get; init; }
        /// <summary>DBカラム名（更新時刻）</summary>
        public required string? UpdatedAtDbColumnName { get; init; }
        /// <summary>DBカラム名（楽観排他用バージョン）</summary>
        public required string? VersionDbColumnName { get; init; }

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

        private const string CREATE_USER_DB_COLUMN_NAME = "CreateUserDbColumnName";
        private const string UPDATE_USER_DB_COLUMN_NAME = "UpdateUserDbColumnName";
        private const string CREATED_AT_DB_COLUMN_NAME = "CreatedAtDbColumnName";
        private const string UPDATED_AT_DB_COLUMN_NAME = "UpdatedAtDbColumnName";
        private const string VERSION_DB_COLUMN_NAME = "VersionDbColumnName";

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
                CreateUserDbColumnName = xDocument.Root.Attribute(CREATE_USER_DB_COLUMN_NAME)?.Value,
                UpdateUserDbColumnName = xDocument.Root.Attribute(UPDATE_USER_DB_COLUMN_NAME)?.Value,
                CreatedAtDbColumnName = xDocument.Root.Attribute(CREATED_AT_DB_COLUMN_NAME)?.Value,
                UpdatedAtDbColumnName = xDocument.Root.Attribute(UPDATED_AT_DB_COLUMN_NAME)?.Value,
                VersionDbColumnName = xDocument.Root.Attribute(VERSION_DB_COLUMN_NAME)?.Value,
                MultiViewDetailLinkBehavior = xDocument.Root.Attribute(MULTI_VIEW_DETAIL_LINK_BEHAVIOR)?.Value == E_MultiViewDetailLinkBehavior.NavigateToReadOnlyMode.ToString()
                    ? E_MultiViewDetailLinkBehavior.NavigateToReadOnlyMode
                    : E_MultiViewDetailLinkBehavior.NavigateToEditMode,
            };
        }
    }
}
