using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using System;

namespace Nijo.Ver1.Models.CommandModelModules {
    /// <summary>
    /// コマンドのパラメータ型定義
    /// </summary>
    internal class ParameterType {
        internal ParameterType(RootAggregate aggregate) {
            _rootAggregate = aggregate;
        }
        private readonly RootAggregate _rootAggregate;

        internal string CsClassName => $"{_rootAggregate.PhysicalName}Parameter";
        internal string TsTypeName => $"{_rootAggregate.PhysicalName}Parameter";

        internal string RenderCSharp(CodeRenderingContext ctx) {
            var param = _rootAggregate.GetCommandModelParameterChild();

            return $$"""
                /// <summary>
                /// {{_rootAggregate.DisplayName}}の実行時にクライアント側から渡される引数
                /// </summary>
                public partial class {{CsClassName}} {
                    // TODO ver.1
                }
                """;
        }

        internal string RenderTypeScript(CodeRenderingContext ctx) {
            var param = _rootAggregate.GetCommandModelParameterChild();

            return $$"""
                /** {{_rootAggregate.DisplayName}}の実行時にクライアント側から渡される引数 */
                export type {{TsTypeName}} = {
                    // TODO ver.1
                }
                """;
        }


        #region クライアント側新規オブジェクト作成関数
        internal string NewObjectFnName => $"createNew{TsTypeName}Parameter";
        internal string RenderNewObjectFn() {
            return $$"""
                /** {{_rootAggregate.DisplayName}}のパラメータオブジェクトの新規作成関数 */
                export const {{NewObjectFnName}} = (): {{TsTypeName}} => ({
                  // TODO ver.1
                })
                """;
        }
        #endregion クライアント側新規オブジェクト作成関数
    }
}
