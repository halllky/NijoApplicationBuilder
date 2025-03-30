using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using Nijo.Ver1.Models.QueryModelModules;
using Nijo.Ver1.Parts.CSharp;
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

        void IValueMemberType.RegisterDependencies(IMultiAggregateSourceFileManager ctx) {
            // 特になし
        }
        void IValueMemberType.RenderStaticSources(CodeRenderingContext ctx) {
            // 特になし
        }

        #region 採番処理
        internal const string SET_METHOD = "GenerateAndSetSequenceValue";

        #endregion 採番処理
    }
}
