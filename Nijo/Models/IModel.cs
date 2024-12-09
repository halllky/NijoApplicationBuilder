using Nijo.Core;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models {
    /// <summary>
    /// モデル。ルート集約の種類。
    /// 例えばあるルート集約がWriteModelである場合はWriteModelとしてのソースコードが、
    /// ReadModelである場合はReadModelとしてのソースコードが生成される。
    /// </summary>
    public interface IModel {
        /// <summary>
        /// ルート集約1個と対応するソースコードを生成します。
        /// </summary>
        void GenerateCode(CodeRenderingContext context, GraphNode<Aggregate> rootAggregate);
        /// <summary>
        /// ユーティリティクラスなどのような、ルート集約1個と対応しないソースコードを生成します。
        /// </summary>
        void GenerateCode(CodeRenderingContext context);
        /// <summary>
        /// 不正な集約定義が無いかを検査し、エラーメッセージの一覧を返します。
        /// 例えば、キーが1つも無いなど。
        /// </summary>
        IEnumerable<string> ValidateAggregate(GraphNode<Aggregate> rootAggregate);
    }
}
