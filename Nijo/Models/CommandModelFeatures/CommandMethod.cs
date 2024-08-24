using Nijo.Core;
using Nijo.Parts.WebServer;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.CommandModelFeatures {
    /// <summary>
    /// <see cref="CommandMethod"/> における本処理を呼び出す処理
    /// </summary>
    internal class CommandMethod {
        internal CommandMethod(GraphNode<Aggregate> rootAggregate) {
            _rootAggregate = rootAggregate;
        }
        private readonly GraphNode<Aggregate> _rootAggregate;

        internal string HookName => $"use{_rootAggregate.Item.PhysicalName}Launcher";

        private string Url => $"/{Parts.WebServer.Controller.SUBDOMAIN}/{CommandController.SUBDOMAIN}/{_rootAggregate.Item.UniqueId}";
        private string AppSrvMethod => $"Execute{_rootAggregate.Item.PhysicalName}";

        private const string USE_COMMAND_LAUNCHER = "useCommandLauncher";

        internal string RenderHook(CodeRenderingContext context) {
            var param = new CommandParameter(_rootAggregate);

            return $$"""
                /** {{_rootAggregate.Item.DisplayName}}処理を呼び出す関数を返します。 */
                export const {{HookName}} = () => {
                  const launchCommand = {{USE_COMMAND_LAUNCHER}}()
                  return React.useCallback(async (param: Types.{{param.TsTypeName}}) => {
                    await launchCommand({ url: `{{Url}}`, param, defaultSuccessMessage: '{{_rootAggregate.Item.DisplayName}}処理が成功しました。' })
                  }, [launchCommand])
                }
                """;
        }
        internal static string RenderCommonHook(CodeRenderingContext context) {
            return $$"""
                /** コマンドを呼び出し、その処理結果を解釈して画面遷移したりファイルダウンロードを開始したりする */
                export const {{USE_COMMAND_LAUNCHER}} = () => {
                  const navigate = ReactRouter.useNavigate()
                  const { post } = Util.useHttpRequest()
                  const [, dispatchToast] = Util.useToastContext()
                  const [, dispatchDialog] = Layout.useDialogContext()

                  return React.useCallback(async <T extends object>({ url, param, defaultSuccessMessage }: {
                    url: string
                    param: T
                    defaultSuccessMessage?: string
                  }) => {
                    const response = await post<Types.{{CommandResult.TS_TYPE_NAME}}>(url, param)
                    if (!response.ok) return

                    if (response.data.type === 'message') {
                      const message = response.data.text
                        ?? defaultSuccessMessage
                        ?? '処理が成功しました。'

                      if (!response.data.detail) {
                        // 処理結果の詳細情報がない場合、トーストで処理成功の旨だけ表示。
                        dispatchToast(msg => msg.info(message))

                      } else {
                        // 処理結果の詳細情報がある場合、ダイアログで結果を表示。
                        const detail = response.data.detail
                        dispatchDialog(state => state.pushDialog(message, ({ closeDialog }) => {
                          return (
                            <div className="h-full flex flex-col gap-1">
                              <div className="flex-1 overflow-y-auto">
                                <Layout.UnknownObjectViewer label="詳細" value={detail} />
                              </div>
                              <div className="flex justify-end">
                                <Input.IconButton fill onClick={closeDialog}>OK</Input.IconButton>
                              </div>
                            </div>
                          )
                        }))
                      }

                    } else if (response.data.type === 'redirect') {
                      // 特定の画面に遷移。初期値はクエリパラメータに付される。
                      navigate(response.data.url)

                    } else if (response.data.type === 'file') {
                      // ファイルダウンロード
                      // TODO #3: HTTPレスポンスのContent-Typeを見て response.json() するか response.blob() するかを分けるようにする必要がある。
                      //          そのためには Http.ts の sendHttpRequest の中でこれらを実行するようであってはならず、dataプロパティを参照する処理全般を併せて修正する必要がある。
                      dispatchToast(msg => msg.warn('ファイルダウンロード処理は実装されていません。'))

                    } else {
                      dispatchToast(msg => msg.warn('処理結果を解釈できません。'))
                    }
                  }, [post, navigate, dispatchToast, dispatchDialog])
                }
                """;
        }

        internal string RenderController(CodeRenderingContext context) {
            var param = new CommandParameter(_rootAggregate);

            return $$"""
                /// <summary>
                /// {{_rootAggregate.Item.DisplayName}}処理をWebAPI経由で実行するためのエンドポイント
                /// </summary>
                [HttpPost("{{_rootAggregate.Item.UniqueId}}")]
                public virtual IActionResult {{_rootAggregate.Item.PhysicalName}}([FromBody] {{param.CsClassName}} param) {
                    var result = new {{CommandResult.GENERATOR_WEB_CLASS_NAME}}(this);
                    var commandResult = _applicationService.{{AppSrvMethod}}(param, result);
                    return (({{CommandResult.GENERATOR_WEB_CLASS_NAME}}.{{CommandResult.ACTION_RESULT_CONTAINER}})commandResult).ActionResult;
                }
                """;
        }

        internal string RenderAbstractMethod(CodeRenderingContext context) {
            var param = new CommandParameter(_rootAggregate);

            return $$"""
                /// <summary>
                /// {{_rootAggregate.Item.DisplayName}}処理
                /// </summary>
                /// <param name="param">処理パラメータ</param>
                /// <param name="result">処理結果。return result.XXXXX(); のような形で記述してください。</param>
                public virtual {{CommandResult.RESULT_INTERFACE_NAME}} {{AppSrvMethod}}({{param.CsClassName}} param, {{CommandResult.GENERATOR_INTERFACE_NAME}} result) {
                    throw new NotImplementedException("{{_rootAggregate.Item.DisplayName}}処理は実装されていません。");
                }
                """;
        }
    }
}
