using Nijo.CodeGenerating;
using Nijo.Models.StaticEnumModelModules;
using Nijo.Util.DotnetEx;
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
        internal const string TS_TYPE_MAP = "EnumTypeMap";
        internal const string TS_VALUE_MAP = "EnumValueMap";

        private readonly List<StaticEnumDef> _staticEnumDefs = [];
        private readonly List<string> _csSourceCode = [];
        private readonly List<string> _tsSourceCode = [];

        internal EnumFile Register(StaticEnumDef staticEnumDef) {
            _staticEnumDefs.Add(staticEnumDef);
            return this;
        }
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
                        /** 列挙体の型マッピング */
                        export interface {{TS_TYPE_MAP}} {
                        {{_staticEnumDefs.SelectTextTemplate(def => $$"""
                          '{{def.TsTypeName}}': {{def.TsTypeName}}
                        """)}}
                        }

                        /** 列挙体の値マッピング */
                        export const {{TS_VALUE_MAP}}: { [K in keyof {{TS_TYPE_MAP}}]: () => {{TS_TYPE_MAP}}[K][] } = {
                        {{_staticEnumDefs.SelectTextTemplate(def => $$"""
                          '{{def.TsTypeName}}': () => [{{def.GetValues().Select(v => $"'{v.DisplayName.Replace("'", "\\'")}'").Join(", ")}}],
                        """)}}
                        }

                        {{_tsSourceCode.SelectTextTemplate(source => $$"""
                        {{source}}

                        """)}}
                        """,
                });
            });
        }
    }
}
