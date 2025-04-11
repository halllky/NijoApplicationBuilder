using Nijo.ImmutableSchema;
using Nijo.SchemaParsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.CodeGenerating {
    /// <summary>
    /// その集約からどういったソースコードが生成されるかを既定する集約の種類
    /// </summary>
    public interface IModel {

        /// <summary>
        /// スキーマ定義上でこのモデルを指定するときの名前
        /// </summary>
        string SchemaName { get; }

        /// <summary>
        /// このモデルがどういった責務を負っているか、このモデルからどういったソースコードが生成されるかの概要
        /// </summary>
        string HelpText { get; }

        /// <summary>
        /// モデルの指定内容の検証を行ないます。
        /// </summary>
        public void Validate(XElement rootAggregateElement, SchemaParseContext context, Action<XElement, string> addError);

        /// <summary>
        /// ルート集約1個と対応するソースコードを生成します。
        /// </summary>
        void GenerateCode(CodeRenderingContext ctx, RootAggregate rootAggregate);

        /// <summary>
        /// ユーティリティクラスなどのような、ルート集約1個と対応しないソースコードを生成します。
        /// </summary>
        void GenerateCode(CodeRenderingContext ctx);

    }
}
