using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.SchemaParsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.Ver1.ImmutableSchema {
    /// <summary>
    /// アプリケーションスキーマ。
    /// </summary>
    public class ApplicationSchema {

        /// <summary>
        /// XMLドキュメントがスキーマ定義として不正な状態を持たないかを検証し、
        /// 検証に成功した場合はアプリケーションスキーマのインスタンスを返します。
        /// </summary>
        /// <param name="xDocument">XMLドキュメント</param>
        /// <param name="parseContext">XML解釈ルール</param>
        /// <param name="schema">作成完了後のスキーマ</param>
        /// <param name="errors">エラーがある場合はここにその内容が格納される</param>
        /// <returns>スキーマの作成に成功したかどうか</returns>
        internal static bool TryBuild(XDocument xDocument, SchemaParseContext parseContext, out ApplicationSchema schema, out ICollection<string> errors) {
            schema = new ApplicationSchema(xDocument, parseContext);
            errors = new List<string>();

            // TODO: ここでバリデーション

            return true;
        }

        private ApplicationSchema(XDocument xDocument, SchemaParseContext parseContext) {
            _xDocument = xDocument;
            _parseContext = parseContext;
        }
        private readonly XDocument _xDocument;
        private readonly SchemaParseContext _parseContext;

        /// <summary>
        /// このスキーマで定義されているルート集約を返します。
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public IEnumerable<RootAggregate> GetRootAggregates() {
            foreach (var xElement in _xDocument.Root?.Elements() ?? []) {
                var aggregate = _parseContext.ToAggregateBase(xElement, null);
                if (aggregate is not RootAggregate rootAggregate) {
                    throw new InvalidOperationException();
                }
                yield return rootAggregate;
            }
        }
    }
}
