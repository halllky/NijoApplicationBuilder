using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Nijo.Ver1.CodeGenerating {
    /// <summary>
    /// スキーマ定義で設定できるアプリケーション単位のコンフィグ
    /// </summary>
    public class ApplicationConfig {

        internal const string ROOT_ELEMENT_NAME = "NijoApplicationBuilder";

        /// <summary>
        /// アプリケーションの新規作成時はXMLのルート要素に一切の属性が付されないXMLを保存すればよいので
        /// このコンストラクタが使われるのは既存のアプリケーションの読み込み時のみ
        /// </summary>
        internal ApplicationConfig(XDocument xDocument) {
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
