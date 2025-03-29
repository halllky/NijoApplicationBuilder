using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using Nijo.Ver1.Models.QueryModelModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.ValueMemberTypes {
    /// <summary>
    /// シーケンス。RDBMS登録時にデータベース側で採番処理がなされる整数型。
    /// </summary>
    internal class SequenceMember : IValueMemberType {
        string IValueMemberType.SchemaTypeName => "sequence";
        string IValueMemberType.CsDomainTypeName => "int";
        string IValueMemberType.CsPrimitiveTypeName => "int";
        string IValueMemberType.TsTypeName => "number";

        ValueMemberSearchBehavior? IValueMemberType.SearchBehavior => new() {
            FilterCsTypeName = "FromTo<int?>",
            FilterTsTypeName = "{ from?: number, to?: number }",
            RenderTsNewObjectFunctionValue = () => "{}",
        };

        UiConstraint.E_Type IValueMemberType.UiConstraintType => UiConstraint.E_Type.NumberMemberConstraint;

        void IValueMemberType.GenerateCode(CodeRenderingContext ctx) {
            // 特になし
        }
    }
}
