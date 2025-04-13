using Nijo.CodeGenerating;
using Nijo.CodeGenerating.Helpers;
using Nijo.ImmutableSchema;
using Nijo.Models;
using System;
using System.Collections.Generic;

namespace Nijo.Models.CommandModelModules {
  /// <summary>
  /// コマンドのパラメータ型定義
  /// </summary>
  internal class ParameterType : IInstancePropertyOwnerMetadata {
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
    internal string TsNewObjectFunction => $"createNew{TsTypeName}";
    internal string RenderNewObjectFn() {
      return $$"""
                /** {{_rootAggregate.DisplayName}}のパラメータオブジェクトの新規作成関数 */
                export const {{TsNewObjectFunction}} = (): {{TsTypeName}} => ({
                  // TODO ver.1
                })
                """;
    }
    #endregion クライアント側新規オブジェクト作成関数

    IEnumerable<IInstancePropertyMetadata> IInstancePropertyOwnerMetadata.GetMembers() {
      // TODO: ver.1 では実際のメンバーはないため空の配列を返す
      return Array.Empty<IInstancePropertyMetadata>();
    }
  }
}
