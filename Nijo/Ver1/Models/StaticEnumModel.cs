using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using Nijo.Ver1.Models.StaticEnumModelModules;
using Nijo.Ver1.Parts.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.Models {
    /// <summary>
    /// 静的区分値。値がソースコード上にハードコードされる区分値。
    /// C#はenumとして、TypeScriptはリテラル型として表れる。
    /// </summary>
    internal class StaticEnumModel : IModel {
        public void GenerateCode(CodeRenderingContext ctx, RootAggregate rootAggregate) {
            var enumFile = ctx.Use<EnumFile>();

            // データ型: enum, リテラル型定義
            var staticEnum = new StaticEnumDef(rootAggregate);
            enumFile.AddCSharpSource(staticEnum.RenderCSharp());
            enumFile.AddTypeScriptSource(staticEnum.RenderTypeScript());

            // データ型: 検索条件オブジェクト
            var searchCondition = new StaticEnumSearchCondition(rootAggregate);
            enumFile.AddCSharpSource(searchCondition.RenderCSharp());
            enumFile.AddTypeScriptSource(searchCondition.RenderTypeScript());
        }

        public void GenerateCode(CodeRenderingContext ctx) {
            // 特になし
        }
    }
}
