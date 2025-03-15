using Nijo.Ver1.ImmutableSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.Ver1.CodeGenerating {
    /// <summary>
    /// その集約からどういったソースコードが生成されるかを既定する集約の種類
    /// </summary>
    public interface IModel {

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
