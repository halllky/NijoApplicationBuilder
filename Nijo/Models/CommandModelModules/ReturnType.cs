using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Models;
using System;

namespace Nijo.Models.CommandModelModules {
    /// <summary>
    /// コマンドの戻り値型定義
    /// </summary>
    internal class ReturnType {
        internal ReturnType(RootAggregate aggregate) {
            _rootAggregate = aggregate;
        }
        private readonly RootAggregate _rootAggregate;

        internal string CsClassName => $"{_rootAggregate.PhysicalName}ReturnType";
        internal string TsTypeName => $"{_rootAggregate.PhysicalName}ReturnType";

        internal string RenderCSharp(CodeRenderingContext ctx) {
            var returnType = _rootAggregate.GetCommandModelParameterChild();

            return $$"""
                /// <summary>
                /// {{_rootAggregate.DisplayName}}の実行時にクライアント側に返される戻り値
                /// </summary>
                public partial class {{CsClassName}} {
                    // TODO ver.1
                }
                """;
        }

        internal string RenderTypeScript(CodeRenderingContext ctx) {
            var returnType = _rootAggregate.GetCommandModelParameterChild();

            return $$"""
                /** {{_rootAggregate.DisplayName}}の実行時にクライアント側に返される戻り値 */
                export type {{TsTypeName}} = {
                    // TODO ver.1
                }
                """;
        }
    }
}
