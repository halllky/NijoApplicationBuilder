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

        private string Url => $"/{Controller.SUBDOMAIN}/{CommandController.SUBDOMAIN}/{_rootAggregate.Item.UniqueId}";

        private string AppSrvMethod => $"Execute{_rootAggregate.Item.PhysicalName}";
        private string AppSrvValidateMethod => $"Validate{_rootAggregate.Item.PhysicalName}Parameter";
        private string AppSrvBodyMethod => $"Execute{_rootAggregate.Item.PhysicalName}";

        private const string USE_COMMAND_LAUNCHER = "useCommandLauncher";
        internal const string HTTP_PARAM_IGNORECONFIRM = "ignoreConfirm";

        internal string RenderHook(CodeRenderingContext context) {
            var param = new CommandParameter(_rootAggregate);

            return $$"""
                /** {{_rootAggregate.Item.DisplayName}}処理を呼び出す関数を返します。 */
                export const {{HookName}} = (setError?: ReactHookForm.UseFormSetError<Types.{{param.TsTypeName}}>) => {
                  // エラーを react hook form にマッピングする処理
                  const onValidationError = React.useMemo(() => {
                    if (setError === undefined) return undefined
                    return (err: object) => {
                      const errors = err as [ReactHookForm.FieldPath<Types.{{param.TsTypeName}}>, { types: { [key: string]: string } }][]
                      for (const [name, error] of errors) {
                        setError(name, error)
                      }
                    }
                  }, [setError])

                  // 呼び出し処理
                  const launchCommand = {{USE_COMMAND_LAUNCHER}}()
                  return React.useCallback(async (param: Types.{{param.TsTypeName}}) => {
                    return await launchCommand({
                      url: `{{Url}}`,
                      param,
                      defaultSuccessMessage: '{{_rootAggregate.Item.DisplayName}}処理が成功しました。',
                      onValidationError,
                    })
                  }, [launchCommand, onValidationError])
                }
                """;
        }
        internal static string RenderCommonHook(CodeRenderingContext context) {
            return $$"""
                /** コマンドを呼び出し、その処理結果を解釈して画面遷移したりファイルダウンロードを開始したりする */
                export const {{USE_COMMAND_LAUNCHER}} = () => {
                  const navigate = ReactRouter.useNavigate()
                  const { postWithHandler } = Util.useHttpRequest()
                  const [, dispatchToast] = Util.useToastContext()
                  const [, dispatchMsg] = Util.useMsgContext()
                  const [, dispatchDialog] = Layout.useDialogContext()

                  const executeCommandApi = useEvent(async <T extends object>({ url, param, defaultSuccessMessage, onValidationError, defaultFileName }: {
                    url: string
                    param: T
                    defaultSuccessMessage?: string
                    /** パラメータの入力内容が不正だった場合にそれを画面に表示する処理 */
                    onValidationError?: (err: object) => void
                    /** 処理結果のファイルダウンロード時の既定の名前 */
                    defaultFileName?: string
                  }) => {
                    return await postWithHandler(url, param, async response => {
                      if (response.ok) {
                        const contentType = response.headers.get('Content-Type')?.toLowerCase()
                        if (contentType?.includes('application/json')) {
                          const data = await response.json() as Types.{{CommandResult.TS_TYPE_NAME}}

                          // 特定の画面に遷移。初期値はクエリパラメータに付される。
                          if (data.type === '{{CommandResult.TYPE_REDIRECT}}') {
                            navigate(data.url)
                            return { success: true }
                          }

                          // 処理結果表示
                          if (data.type === '{{CommandResult.TYPE_MESSAGE}}') {
                            const message = data.text ?? defaultSuccessMessage ?? '処理が成功しました。'
                            if (!data.detail) {
                              // 処理結果の詳細情報がない場合、トーストで処理成功の旨だけ表示。
                              dispatchToast(msg => msg.info(message))
                              return { success: true }

                            } else {
                              // 処理結果の詳細情報がある場合、ダイアログで結果を表示。
                              // TODO #3: フックが入れ子になっているせいでここのdispatchDialogがコンテキストに入っていない。detailだけ呼び元に返してもっと浅いところでUIをレンダリングさせる。
                              const detail = data.detail
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
                              return { success: true }
                            }
                          }
                          dispatchToast(msg => msg.warn(`処理結果を解釈できません。(type: ${data.type})`))
                          return { success: true }

                        } else {
                          // 処理結果ファイルダウンロード
                          const a = document.createElement('a')
                          let blobUrl: string | undefined = undefined
                          try {
                            const blob = await response.blob()
                            const blobUrl = window.URL.createObjectURL(blob)
                            a.href = blobUrl
                            a.download = defaultFileName ?? ''
                            document.body.appendChild(a)
                            a.click()
                          } catch (error) {
                            dispatchMsg(msg => msg.error(`ファイルダウンロードに失敗しました: ${error}`))
                          } finally {
                            if (blobUrl !== undefined) window.URL.revokeObjectURL(blobUrl)
                            document.body.removeChild(a)
                            return { success: true }
                          }
                        }

                      } else if (response.status === 422 /* Unprocessable Content. エラーまたは警告 */) {
                        const data = (await response.json()) as
                          { type: '{{CommandResult.TYPE_CONFIRM}}', {{CommandResult.HTTP_CONFIRM_DETAIL}}: string[] } |
                          { type: '{{CommandResult.TYPE_ERROR}}', {{CommandResult.HTTP_ERROR_DETAIL}}: object }

                        if (data.type === '{{CommandResult.TYPE_CONFIRM}}') {
                          // 「～してもよいですか？」の確認メッセージ表示
                          for (const msg of data.{{CommandResult.HTTP_CONFIRM_DETAIL}}) {
                            if (!window.confirm(msg)) {
                              return { success: false } // "OK"が選択されなかった場合は処理実行APIを呼ばずに処理中断
                            }
                          }
                          // すべての確認メッセージで"OK"が選ばれた場合は再度処理実行APIを呼ぶ。確認メッセージを表示しない旨のオプションをつけたうえで呼ぶ。
                          executeCommandApi({
                            url: `${url}?{{HTTP_PARAM_IGNORECONFIRM}}=true`,
                            param,
                            defaultSuccessMessage,
                          })
                          return { success: false }

                        } else {
                          // 入力内容エラー
                          if (onValidationError) {
                            onValidationError(data.{{CommandResult.HTTP_ERROR_DETAIL}})
                          } else {
                            dispatchMsg(msg => msg.error(`入力内容が不正です: ${JSON.stringify(data.{{CommandResult.HTTP_ERROR_DETAIL}})}`))
                          }
                          return { success: false }
                        }
                      }
                    })
                  })
                  return executeCommandApi
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
                public virtual IActionResult {{_rootAggregate.Item.PhysicalName}}([FromBody] {{param.CsClassName}} param, [FromQuery] bool {{HTTP_PARAM_IGNORECONFIRM}}) {
                    var result = new {{CommandResult.GENERATOR_WEB_CLASS_NAME}}(this);
                    var commandResult = _applicationService.{{AppSrvMethod}}(param, result, {{HTTP_PARAM_IGNORECONFIRM}});
                    return (({{CommandResult.GENERATOR_WEB_CLASS_NAME}}.{{CommandResult.ACTION_RESULT_CONTAINER}})commandResult).ActionResult;
                }
                """;
        }

        internal string RenderAbstractMethod(CodeRenderingContext context) {
            var param = new CommandParameter(_rootAggregate);

            return $$"""
                /// <summary>
                /// {{_rootAggregate.Item.DisplayName}}処理を実行します。
                /// </summary>
                /// <param name="param">処理パラメータ</param>
                /// <param name="result">処理結果ハンドリング用オブジェクト</param>
                /// <param name="ignoreConfirm">ワーニングがあった場合に無視するかどうか</param>
                public {{CommandResult.RESULT_INTERFACE_NAME}} {{AppSrvMethod}}<T>({{param.CsClassName}} param, T result, bool ignoreConfirm)
                    where T : {{CommandResult.GENERATOR_INTERFACE_NAME}}, {{CommandResult.ERROR_GENERATOR_INTERFACE_NAME}} {

                    // エラーチェック
                    var errors = new {{param.MessageDataCsClassName}}();
                    {{AppSrvValidateMethod}}(param, errors);
                    if (errors.HasError()) {
                        return result.Error(errors);
                    }
                    if (errors.HasConfirm() && !ignoreConfirm) {
                        return result.Confirm(errors.GetConfirms());
                    }
                    // 本処理
                    return {{AppSrvBodyMethod}}(param, result);
                }
                /// <summary>
                /// {{_rootAggregate.Item.DisplayName}}のパラメータのエラーチェック処理
                /// </summary>
                /// <param name="param">処理パラメータ</param>
                /// <param name="errors">エラー情報がある場合はこのオブジェクトに設定してください。</param>
                protected virtual void {{AppSrvValidateMethod}}({{param.CsClassName}} param, {{param.MessageDataCsClassName}} errors) {
                    // このメソッドをオーバーライドしてエラーチェック処理を記述してください。
                }
                /// <summary>
                /// {{_rootAggregate.Item.DisplayName}}の本処理
                /// </summary>
                /// <param name="param">処理パラメータ</param>
                /// <param name="result">処理結果。return result.XXXXX(); のような形で記述してください。</param>
                protected virtual {{CommandResult.RESULT_INTERFACE_NAME}} {{AppSrvBodyMethod}}({{param.CsClassName}} param, {{CommandResult.GENERATOR_INTERFACE_NAME}} result) {
                    throw new NotImplementedException("{{_rootAggregate.Item.DisplayName}}処理は実装されていません。");
                }
                """;
        }
    }
}
