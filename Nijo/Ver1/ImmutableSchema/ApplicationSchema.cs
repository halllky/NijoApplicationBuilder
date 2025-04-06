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

        internal ApplicationSchema(XDocument xDocument, SchemaParseContext parseContext) {
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
