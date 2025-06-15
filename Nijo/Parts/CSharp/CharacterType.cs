using Nijo.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts.CSharp;

/// <summary>
/// 文字種
/// </summary>
internal class CharacterType {
    internal const string ENUM_NAME = "E_CharacterType";

    internal static SourceFile Render(CodeRenderingContext ctx) {
        return new SourceFile {
            FileName = "E_CharacterType.cs",
            Contents = $$"""
                namespace {{ctx.Config.RootNamespace}};

                /// <summary>
                /// 文字種
                /// </summary>
                public enum {{ENUM_NAME}} {
                {{ctx.GetCharacterTypes().SelectTextTemplate(enumName => $$"""
                    {{enumName}},
                """)}}
                }
                """,
        };
    }
}
