using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using Nijo.Ver1.Parts.Common;
using Nijo.Ver1.Parts.CSharp;
using Nijo.Ver1.Parts.JavaScript;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nijo.Ver1.Models.CommandModelModules {
    /// <summary>
    /// コマンド処理
    /// </summary>
    internal class CommandProcessing {
        internal CommandProcessing(RootAggregate aggregate) {
            _rootAggregate = aggregate;
        }
        private readonly RootAggregate _rootAggregate;


        private const string CONTROLLER_ACTION_EXECUTE = "execute";
        private const string EXECUTE_METHOD = "Execute";


        #region TypeScript用
        internal static string RenderTsTypeMap(IEnumerable<RootAggregate> commandModels) {

            var items = commandModels.Select(rootAggregate => {
                var controller = new AspNetController(rootAggregate);
                var param = new ParameterType(rootAggregate);
                var returnValue = new ReturnType(rootAggregate);

                return new {
                    EscapedPhysicalName = rootAggregate.PhysicalName.Replace("'", "\\'"),
                    Endpoint = controller.GetActionNameForClient(CONTROLLER_ACTION_EXECUTE),
                    ParamType = param.TsTypeName,
                    ReturnType = returnValue.TsTypeName,
                };
            }).ToArray();

            return $$"""
                /** コマンド起動処理 */
                export namespace ExecuteFeature {
                  /** コマンドの実行エンドポイントの一覧 */
                  export const Endpoint: { [key in {{MappingsForCustomize.COMMAND_MODEL_TYPE}}]: string } = {
                {{items.SelectTextTemplate(x => $$"""
                    '{{x.EscapedPhysicalName}}': '{{x.Endpoint}}',
                """)}}
                  }

                  /** コマンドのパラメータ型の一覧 */
                  export interface ParamType {
                {{items.SelectTextTemplate(x => $$"""
                    '{{x.EscapedPhysicalName}}': {{x.ParamType}}
                """)}}
                  }

                  /** コマンドのサーバーからの戻り値の型の一覧 */
                  export interface ReturnType {
                {{items.SelectTextTemplate(x => $$"""
                    '{{x.EscapedPhysicalName}}': {{x.ReturnType}}
                """)}}
                  }
                }
                """;
        }
        #endregion TypeScript用

        internal string RenderAspNetCoreControllerAction(CodeRenderingContext ctx) {
            var param = new ParameterType(_rootAggregate);
            var paramMessages = new ParameterTypeMessageContainer(_rootAggregate);
            var returnValue = new ReturnType(_rootAggregate);

            return $$"""
                /// <summary>
                /// {{_rootAggregate.DisplayName}}のWebからの実行用のエンドポイント
                /// </summary>
                [HttpPost("{{CONTROLLER_ACTION_EXECUTE}}")]
                public async Task<IActionResult> Execute({{ComplexPost.REQUEST_CS}}<{{param.CsClassName}}> request) {
                    _applicationService.Log.Debug("Execute {{_rootAggregate.DisplayName.Replace("\"", "\\\"")}}");
                    if (_applicationService.GetAuthorizedLevel(E_AuthorizedAction.{{_rootAggregate.PhysicalName}}) == E_AuthLevel.None) return Forbid();

                    var messages = new {{paramMessages.CsClassName}}([]);
                    var context = new {{PresentationContext.CLASS_NAME}}(request.Options, messages);

                    // 実行
                    var returnValue = await _applicationService.{{EXECUTE_METHOD}}(request.Data, context);
                    return new {{ComplexPost.RESULT_CS}}(context.ToResult(), returnValue);
                }
                """;
        }

        internal string RenderAppSrvMethods(CodeRenderingContext ctx) {
            var param = new ParameterType(_rootAggregate);
            var returnValue = new ReturnType(_rootAggregate);

            return $$"""
                /// <summary>
                /// {{_rootAggregate.DisplayName}}処理実行
                /// </summary>
                /// <param name="param">処理パラメータ</param>
                /// <param name="context">実行時コンテキスト。エラーメッセージを保持したり、起動時オプションを持っていたりする。</param>
                public abstract Task<{{returnValue.CsClassName}}> {{EXECUTE_METHOD}}({{param.CsClassName}} param, {{PresentationContext.CLASS_NAME}} context);
                """;
        }
    }
}
