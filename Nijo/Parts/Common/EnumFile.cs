using Nijo.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts.Common {
    /// <summary>
    /// 区分値の定義がレンダリングされるファイル
    /// </summary>
    internal class EnumFile : IMultiAggregateSourceFile {

        internal const string TS_FILENAME = "enum-defs.ts";

        private readonly List<string> _csSourceCode = [];
        private readonly List<string> _tsSourceCode = [];

        internal EnumFile AddCSharpSource(string sourceCode) {
            _csSourceCode.Add(sourceCode);
            return this;
        }
        internal EnumFile AddTypeScriptSource(string sourceCode) {
            _tsSourceCode.Add(sourceCode);
            return this;
        }

        void IMultiAggregateSourceFile.RegisterDependencies(IMultiAggregateSourceFileManager ctx) {
            // 特になし
        }

        void IMultiAggregateSourceFile.Render(CodeRenderingContext ctx) {

            ctx.CoreLibrary(dir => {
                dir.Generate(new SourceFile {
                    FileName = "EnumDefs.cs",
                    Contents = $$"""
                        using System.ComponentModel.DataAnnotations;

                        namespace {{ctx.Config.RootNamespace}};

                        {{_csSourceCode.SelectTextTemplate(source => $$"""
                        {{source}}

                        """)}}
                        """,
                });
            });

            ctx.ReactProject(dir => {
                dir.Generate(new SourceFile {
                    FileName = TS_FILENAME,
                    Contents = $$"""
                        {{_tsSourceCode.SelectTextTemplate(source => $$"""
                        {{source}}

                        """)}}
                        """,
                });
            });
        }
    }
}
