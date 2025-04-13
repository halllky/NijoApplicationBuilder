using Nijo.CodeGenerating;
using Nijo.CodeGenerating.Helpers;
using Nijo.ImmutableSchema;
using Nijo.Models;
using System;
using System.Collections.Generic;

namespace Nijo.Models.CommandModelModules {
    /// <summary>
    /// コマンドの戻り値型定義
    /// </summary>
    internal class ReturnValue : IInstancePropertyOwnerMetadata {
        internal ReturnValue(RootAggregate aggregate) {
            _rootAggregate = aggregate;
        }
        private readonly RootAggregate _rootAggregate;

        internal string CsClassName => $"{_rootAggregate.PhysicalName}ReturnValue";
        internal string TsTypeName => $"{_rootAggregate.PhysicalName}ReturnValue";

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

        IEnumerable<IInstancePropertyMetadata> IInstancePropertyOwnerMetadata.GetMembers() {
            // TODO: ver.1 では実際のメンバーはないため空の配列を返す
            return Array.Empty<IInstancePropertyMetadata>();
        }
    }
}
