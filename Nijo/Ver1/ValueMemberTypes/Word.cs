using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.ValueMemberTypes {
    /// <summary>
    /// 単語型
    /// </summary>
    internal class Word : IValueMemberType {
        string IValueMemberType.SchemaTypeName => "word";

        string IValueMemberType.CsDomainTypeName => "string";
        string IValueMemberType.CsPrimitiveTypeName => "string";
        string IValueMemberType.TsTypeName => "string";

        void IValueMemberType.GenerateCode(CodeRenderingContext ctx) {
            // 特になし
        }
    }
}
