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

        /// <summary>
        /// どの集約からも参照されていないRefTo関連部品を生成するかどうか
        /// </summary>
        public required bool GenerateUnusedRefToModules { get; set; }

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
        /// TypeScript側のソースは型定義などのみを生成し、
        /// UIコンポーネントなどのソースは生成しない。
        /// </summary>
        public required bool CustomizeAllUi { get; set; }
        /// <summary>
        /// 一時保存を使用しない
        /// </summary>
        public required bool DisableLocalRepository { get; set; }
        /// <summary>
        /// ボタンの色。既定では"cyan"。Tailwind CSS で定義されている色名のみ有効。
        /// </summary>
        public required string? ButtonColor { get; set; }

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

        #region VForm2
        /// <summary>
        /// VFormでレスポンシブに変化する列数の最大列数。
        /// </summary>
        public required int? VFormMaxColumnCount { get; set; }
        /// <summary>
        /// VFormの中に入ることができる最大のメンバーの数。
        /// これより多くのメンバー数をもつ集約が定義された場合はレイアウトが崩れる。
        /// </summary>
        public required int? VFormMaxMemberCount { get; set; }
        /// <summary>
        /// VFormの列数が切り替わる閾値。単位はpx
        /// </summary>
        public required int? VFormThreshold { get; set; }
        #endregion VForm2

        private const string GENERATE_UNUSED_REFTO_MODULES = "GenerateUnusedRefToModules";

        private const string DBCONTEXT_NAME = "DbContextName";

        private const string CREATE_USER_DB_COLUMN_NAME = "CreateUserDbColumnName";
        private const string UPDATE_USER_DB_COLUMN_NAME = "UpdateUserDbColumnName";
        private const string CREATED_AT_DB_COLUMN_NAME = "CreatedAtDbColumnName";
        private const string UPDATED_AT_DB_COLUMN_NAME = "UpdatedAtDbColumnName";
        private const string VERSION_DB_COLUMN_NAME = "VersionDbColumnName";

        private const string DISABLE_LOCAL_REPOSITORY = "DisableLocalRepository";
        private const string CUSTOMIZE_ALL_UI = "CustomizeAllUi";
        private const string BUTTON_COLOR = "ButtonColor";

        private const string MULTI_VIEW_DETAIL_LINK_BEHAVIOR = "MultiViewDetailLinkBehavior";

        private const string VFORM_MAX_COLUMN_COUNT = "VFormMaxColumnCount";
        private const string VFORM_MAX_MEMBER_COUNT = "VFormMaxMemberCount";
        private const string VFORM_THRESHOLD = "VFormThreshold";

        public void ToXElement(XElement root) {

            root.Name = XName.Get(RootNamespace);

            if (GenerateUnusedRefToModules) {
                root.SetAttributeValue(GENERATE_UNUSED_REFTO_MODULES, "True");
            }

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

            if (CustomizeAllUi) {
                root.SetAttributeValue(CUSTOMIZE_ALL_UI, "True");
            } else {
                root.Attribute(CUSTOMIZE_ALL_UI)?.Remove();
            }

            if (DisableLocalRepository) {
                root.SetAttributeValue(DISABLE_LOCAL_REPOSITORY, "True");
            } else {
                root.Attribute(DISABLE_LOCAL_REPOSITORY)?.Remove();
            }

            if (string.IsNullOrWhiteSpace(ButtonColor)) {
                root.Attribute(BUTTON_COLOR)?.Remove();
            } else {
                root.SetAttributeValue(BUTTON_COLOR, ButtonColor.Trim().ToLower());
            }

            if (MultiViewDetailLinkBehavior == E_MultiViewDetailLinkBehavior.NavigateToReadOnlyMode) {
                root.SetAttributeValue(MULTI_VIEW_DETAIL_LINK_BEHAVIOR, E_MultiViewDetailLinkBehavior.NavigateToReadOnlyMode.ToString());
            } else {
                root.Attribute(MULTI_VIEW_DETAIL_LINK_BEHAVIOR)?.Remove();
            }

            if (VFormMaxColumnCount != null) {
                root.SetAttributeValue(VFORM_MAX_COLUMN_COUNT, VFormMaxColumnCount.ToString());
            } else {
                root.Attribute(VFORM_MAX_COLUMN_COUNT)?.Remove();
            }

            if (VFormMaxMemberCount != null) {
                root.SetAttributeValue(VFORM_MAX_MEMBER_COUNT, VFormMaxMemberCount.ToString());
            } else {
                root.Attribute(VFORM_MAX_MEMBER_COUNT)?.Remove();
            }

            if (VFormThreshold != null) {
                root.SetAttributeValue(VFORM_THRESHOLD, VFormThreshold.ToString());
            } else {
                root.Attribute(VFORM_THRESHOLD)?.Remove();
            }
        }

        public static Config FromXml(XDocument xDocument) {
            if (xDocument.Root == null) throw new FormatException($"設定ファイルのXMLの形式が不正です。");

            return new Config {
                RootNamespace = xDocument.Root.Name.LocalName.ToCSharpSafe(),
                GenerateUnusedRefToModules = xDocument.Root.Attribute(GENERATE_UNUSED_REFTO_MODULES) != null,
                CustomizeAllUi = xDocument.Root.Attribute(CUSTOMIZE_ALL_UI) != null,
                DisableLocalRepository = xDocument.Root.Attribute(DISABLE_LOCAL_REPOSITORY) != null,
                ButtonColor = xDocument.Root.Attribute(BUTTON_COLOR)?.Value.Trim().ToLower(),
                DbContextName = xDocument.Root.Attribute(DBCONTEXT_NAME)?.Value ?? "MyDbContext",
                CreateUserDbColumnName = xDocument.Root.Attribute(CREATE_USER_DB_COLUMN_NAME)?.Value,
                UpdateUserDbColumnName = xDocument.Root.Attribute(UPDATE_USER_DB_COLUMN_NAME)?.Value,
                CreatedAtDbColumnName = xDocument.Root.Attribute(CREATED_AT_DB_COLUMN_NAME)?.Value,
                UpdatedAtDbColumnName = xDocument.Root.Attribute(UPDATED_AT_DB_COLUMN_NAME)?.Value,
                VersionDbColumnName = xDocument.Root.Attribute(VERSION_DB_COLUMN_NAME)?.Value,
                MultiViewDetailLinkBehavior = xDocument.Root.Attribute(MULTI_VIEW_DETAIL_LINK_BEHAVIOR)?.Value == E_MultiViewDetailLinkBehavior.NavigateToReadOnlyMode.ToString()
                    ? E_MultiViewDetailLinkBehavior.NavigateToReadOnlyMode
                    : E_MultiViewDetailLinkBehavior.NavigateToEditMode,
                VFormMaxColumnCount = xDocument.Root.Attribute(VFORM_MAX_COLUMN_COUNT) != null
                    ? int.Parse(xDocument.Root.Attribute(VFORM_MAX_COLUMN_COUNT)!.Value)
                    : null,
                VFormMaxMemberCount = xDocument.Root.Attribute(VFORM_MAX_MEMBER_COUNT) != null
                    ? int.Parse(xDocument.Root.Attribute(VFORM_MAX_MEMBER_COUNT)!.Value)
                    : null,
                VFormThreshold = xDocument.Root.Attribute(VFORM_THRESHOLD) != null
                    ? int.Parse(xDocument.Root.Attribute(VFORM_THRESHOLD)!.Value)
                    : null,
            };
        }
    }
}
