using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Nijo.Core {
    public class Config {
        public required string RootNamespace { get; set; }

        public string EntityNamespace => RootNamespace;
        public string DbContextNamespace => RootNamespace;
        public required string DbContextName { get; set; }

        /// <summary>DBカラム名（作成者）</summary>
        public required string? CreateUserDbColumnName { get; set; }
        /// <summary>DBカラム名（更新者）</summary>
        public required string? UpdateUserDbColumnName { get; set; }
        /// <summary>DBカラム名（作成時刻）</summary>
        public required string? CreatedAtDbColumnName { get; set; }
        /// <summary>DBカラム名（更新時刻）</summary>
        public required string? UpdatedAtDbColumnName { get; set; }
        /// <summary>DBカラム名（楽観排他用バージョン）</summary>
        public required string? VersionDbColumnName { get; set; }

        /// <summary>
        /// 一時保存を使用しない
        /// </summary>
        public required bool DisableLocalRepository { get; set; }

        /// <summary>
        /// 一覧画面の詳細リンクの挙動
        /// </summary>
        public required E_MultiViewDetailLinkBehavior MultiViewDetailLinkBehavior { get; set; } = E_MultiViewDetailLinkBehavior.NavigateToEditMode;
        /// <summary>
        /// 一覧画面の詳細リンクの挙動
        /// </summary>
        public enum E_MultiViewDetailLinkBehavior {
            /// <summary>「詳細」リンクで読み取り専用モードの詳細画面に遷移する</summary>
            NavigateToReadOnlyMode,
            /// <summary>「詳細」リンクで編集モードの詳細画面に遷移する（既定値）</summary>
            NavigateToEditMode,
        }

        private const string DBCONTEXT_NAME = "DbContextName";

        private const string CREATE_USER_DB_COLUMN_NAME = "CreateUserDbColumnName";
        private const string UPDATE_USER_DB_COLUMN_NAME = "UpdateUserDbColumnName";
        private const string CREATED_AT_DB_COLUMN_NAME = "CreatedAtDbColumnName";
        private const string UPDATED_AT_DB_COLUMN_NAME = "UpdatedAtDbColumnName";
        private const string VERSION_DB_COLUMN_NAME = "VersionDbColumnName";

        private const string DISABLE_LOCAL_REPOSITORY = "DisableLocalRepository";

        private const string MULTI_VIEW_DETAIL_LINK_BEHAVIOR = "MultiViewDetailLinkBehavior";

        public void ToXElement(XElement root) {

            root.Name = XName.Get(RootNamespace);

            if (string.IsNullOrWhiteSpace(DbContextName)) {
                root.Attribute(DBCONTEXT_NAME)?.Remove();
            } else {
                root.SetAttributeValue(DBCONTEXT_NAME, DbContextName);
            }

            if (string.IsNullOrWhiteSpace(CreateUserDbColumnName)) {
                root.Attribute(CREATE_USER_DB_COLUMN_NAME)?.Remove();
            } else {
                root.SetAttributeValue(CREATE_USER_DB_COLUMN_NAME, CreateUserDbColumnName);
            }

            if (string.IsNullOrWhiteSpace(UpdateUserDbColumnName)) {
                root.Attribute(UPDATE_USER_DB_COLUMN_NAME)?.Remove();
            } else {
                root.SetAttributeValue(UPDATE_USER_DB_COLUMN_NAME, UpdateUserDbColumnName);
            }

            if (string.IsNullOrWhiteSpace(CreatedAtDbColumnName)) {
                root.Attribute(CREATED_AT_DB_COLUMN_NAME)?.Remove();
            } else {
                root.SetAttributeValue(CREATED_AT_DB_COLUMN_NAME, CreatedAtDbColumnName);
            }

            if (string.IsNullOrWhiteSpace(UpdatedAtDbColumnName)) {
                root.Attribute(UPDATED_AT_DB_COLUMN_NAME)?.Remove();
            } else {
                root.SetAttributeValue(UPDATED_AT_DB_COLUMN_NAME, UpdatedAtDbColumnName);
            }

            if (string.IsNullOrWhiteSpace(VersionDbColumnName)) {
                root.Attribute(VERSION_DB_COLUMN_NAME)?.Remove();
            } else {
                root.SetAttributeValue(VERSION_DB_COLUMN_NAME, VersionDbColumnName);
            }

            if (DisableLocalRepository) {
                root.SetAttributeValue(DISABLE_LOCAL_REPOSITORY, "True");
            } else {
                root.Attribute(DISABLE_LOCAL_REPOSITORY)?.Remove();
            }

            if (MultiViewDetailLinkBehavior == E_MultiViewDetailLinkBehavior.NavigateToReadOnlyMode) {
                root.SetAttributeValue(MULTI_VIEW_DETAIL_LINK_BEHAVIOR, E_MultiViewDetailLinkBehavior.NavigateToReadOnlyMode.ToString());
            } else {
                root.Attribute(MULTI_VIEW_DETAIL_LINK_BEHAVIOR)?.Remove();
            }
        }

        public static Config FromXml(XDocument xDocument) {
            if (xDocument.Root == null) throw new FormatException($"設定ファイルのXMLの形式が不正です。");

            return new Config {
                RootNamespace = xDocument.Root.Name.LocalName.ToCSharpSafe(),
                DisableLocalRepository = xDocument.Root.Attribute(DISABLE_LOCAL_REPOSITORY) != null,
                DbContextName = xDocument.Root.Attribute(DBCONTEXT_NAME)?.Value ?? "MyDbContext",
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
