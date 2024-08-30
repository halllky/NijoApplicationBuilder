using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts.WebClient {
    /// <summary>
    /// 自動生成されるReact hook
    /// </summary>
    internal class AutoGeneratedHooks : ISummarizedFile {

        private readonly List<string> _sourceCode = new();
        internal void Add(string code) {
            _sourceCode.Add(code);
        }

        int ISummarizedFile.RenderingOrder => 999;
        void ISummarizedFile.OnEndGenerating(CodeRenderingContext context) {
            context.ReactProject.AutoGeneratedDir(dir => {
                dir.Generate(Render());
            });
        }

        private SourceFile Render() => new SourceFile {
            FileName = "autogenerated-hooks.tsx",
            RenderContent = ctx => {
                return $$"""
                    import React from 'react'
                    import useEvent from 'react-use-event-hook'
                    import * as ReactRouter from 'react-router-dom'
                    import * as ReactHookForm from 'react-hook-form'
                    import * as Types from './autogenerated-types'
                    import * as Layout from './collection'
                    import * as Input from './input'
                    import * as Util from './util'

                    {{_sourceCode.SelectTextTemplate(code => $$"""
                    {{code}}

                    """)}}
                    """;
            },
        };
    }
}