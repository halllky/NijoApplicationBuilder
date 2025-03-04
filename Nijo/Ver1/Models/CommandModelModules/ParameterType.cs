using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using System;

namespace Nijo.Ver1.Models.CommandModelModules {
    /// <summary>
    /// コマンドのパラメータ型定義
    /// </summary>
    internal class ParameterType {
        internal ParameterType(RootAggregate aggregate) {
            _aggregate = aggregate;
        }
        private readonly RootAggregate _aggregate;

        internal string RenderCSharp(CodeRenderingContext ctx) {
            throw new NotImplementedException();
        }

        internal string RenderTypeScript(CodeRenderingContext ctx) {
            throw new NotImplementedException();
        }
    }
} 