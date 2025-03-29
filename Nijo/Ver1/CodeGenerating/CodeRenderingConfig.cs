using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Nijo.Ver1.CodeGenerating {
    /// <summary>
    /// スキーマ定義で設定できるアプリケーション単位のソースコードレンダリング設定
    /// </summary>
    public class CodeRenderingConfig {

        internal const string ROOT_ELEMENT_NAME = "NijoApplicationBuilder";

        /// <summary>
        /// アプリケーションの新規作成時はXMLのルート要素に一切の属性が付されないXMLを保存すればよいので
        /// このコンストラクタが使われるのは既存のアプリケーションの読み込み時のみ
        /// </summary>
        internal CodeRenderingConfig(XDocument xDocument) {
            _xDocument = xDocument;
        }

        private readonly XDocument _xDocument;

        /// <summary>
        /// C#側ソースコードの名前空間
        /// </summary>
        public string RootNamespace => _xDocument.Root?.Attribute(nameof(RootNamespace))?.Value ?? "Nijo";
        /// <summary>
        /// DBコンテキストのクラス名
        /// </summary>
        public string DbContextName => _xDocument.Root?.Attribute(nameof(DbContextName))?.Value ?? "MyDbContext";

        /// <summary>DBカラム名（作成者）</summary>
        public string CreateUserDbColumnName => _xDocument.Root?.Attribute(nameof(CreateUserDbColumnName))?.Value ?? Models.DataModelModules.EFCoreEntity.CREATE_USER;
        /// <summary>DBカラム名（更新者）</summary>
        public string UpdateUserDbColumnName => _xDocument.Root?.Attribute(nameof(UpdateUserDbColumnName))?.Value ?? Models.DataModelModules.EFCoreEntity.UPDATE_USER;
        /// <summary>DBカラム名（作成時刻）</summary>
        public string CreatedAtDbColumnName => _xDocument.Root?.Attribute(nameof(CreatedAtDbColumnName))?.Value ?? Models.DataModelModules.EFCoreEntity.CREATED_AT;
        /// <summary>DBカラム名（更新時刻）</summary>
        public string UpdatedAtDbColumnName => _xDocument.Root?.Attribute(nameof(UpdatedAtDbColumnName))?.Value ?? Models.DataModelModules.EFCoreEntity.UPDATED_AT;
        /// <summary>DBカラム名（楽観排他用バージョン）</summary>
        public string VersionDbColumnName => _xDocument.Root?.Attribute(nameof(VersionDbColumnName))?.Value ?? Models.DataModelModules.EFCoreEntity.VERSION;

        /// <summary>
        /// 開発環境におけるReact.jsのサーバーが展開されるURL。httpから始める必要あり。
        /// ASP.NET Core サーバーでのCORS設定に必要。
        /// </summary>
        public string ReactDebuggingUrl => _xDocument.Root?.Attribute(nameof(ReactDebuggingUrl))?.Value ?? "http://localhost:5173";

        /// <summary>
        /// このインスタンスの情報を引数のXElementに反映させます。
        /// </summary>
        internal void Save(XElement rootElement) {

            if (!string.IsNullOrWhiteSpace(RootNamespace)) {
                rootElement.SetAttributeValue(nameof(RootNamespace), RootNamespace);
            }
        }
    }
}
