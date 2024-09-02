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

        private const string USE_COMMAND_RESULT_PARSER = "useCommandResultParser";
        private const string HTTP_PARAM_IGNORECONFIRM = "ignoreConfirm";

        internal string RenderHook(CodeRenderingContext context) {
            var param = new CommandParameter(_rootAggregate);
            var steps = _rootAggregate
                .GetMembers()
                .OfType<AggregateMember.RelationMember>()
                .Where(m => m.MemberAggregate.Item.Options.Step != null)
                .Select(m => m.MemberAggregate.Item.Options.Step!.Value)
                .OrderBy(step => step)
                .ToArray();

            return $$"""
                /** {{_rootAggregate.Item.DisplayName}}処理を呼び出す関数を返します。 */
                export const {{HookName}} = (setError?: ReactHookForm.UseFormSetError<Types.{{param.TsTypeName}}>) => {
                  const { executeCommandApi, resultDetail } = {{USE_COMMAND_RESULT_PARSER}}(setError)
                {{If(steps.Length != 0, () => $$"""
                  const [currentStep, setCurrentStep] = useState({{steps.First()}})
                  const allSteps = useMemo(() => [{{steps.Select(s => s.ToString()).Join(", ")}}], [])
                  const toPreviousStep = useEvent(() => {
                {{steps.Skip(1).SelectTextTemplate((step, i) => $$"""
                    {{(i == 0 ? "if" : "} else if")}} (currentStep === {{step}}) {
                      setCurrentStep({{steps.ElementAt(i)}})
                """)}}
                    }
                  })
                  const toNextStep = useEvent(() => {
                {{steps.SkipLast(1).SelectTextTemplate((step, i) => $$"""
                    {{(i == 0 ? "if" : "} else if")}} (currentStep === {{step}}) {
                      setCurrentStep({{steps.ElementAt(i + 1)}})
                """)}}
                    }
                  })
                """)}}
                  const launch = useEvent(async (param: Types.{{param.TsTypeName}}) => {
                    return await executeCommandApi({
                      url: `{{Url}}`,
                      param,
                      defaultSuccessMessage: '{{_rootAggregate.Item.DisplayName}}処理が成功しました。',
                    })
                  })

                  return {
                {{If(steps.Length != 0, () => $$"""
                    /** 現在のステップの番号 */
                    currentStep,
                    /** このコマンドにあるステップ番号の一覧 */
                    allSteps,
                    /** 前のステップに遷移します。現在が先頭のステップである場合は何も起きません。 */
                    toPreviousStep,
                    /** 次のステップに遷移します。現在が最後尾のステップである場合は何も起きません。 */
                    toNextStep,
                """)}}
                    /** コマンドを実行します。 */
                    launch,
                    /** 画面上に表示させるべき処理結果の詳細データが帰ってきた場合はこのオブジェクトに格納されます。 */
                    resultDetail,
                  }
                }
                """;
        }
        internal static string RenderCommonHook(CodeRenderingContext context) {
            return $$"""
                /** コマンドを呼び出し、その処理結果を解釈して画面遷移したりファイルダウンロードを開始したりする */
                export const {{USE_COMMAND_RESULT_PARSER}} = <T extends object>(setError?: ReactHookForm.UseFormSetError<T>) => {
                  const navigate = ReactRouter.useNavigate()
                  const { postWithHandler } = Util.useHttpRequest()
                  const [, dispatchToast] = Util.useToastContext()
                  const [, dispatchMsg] = Util.useMsgContext()
                  const [resultDetail, setResultDetail] = React.useState<unknown>()

                  const executeCommandApi = useEvent(async ({ url, param, defaultSuccessMessage, defaultFileName }: {
                    url: string
                    param: T
                    defaultSuccessMessage?: string
                    /** 処理結果のファイルダウンロード時の既定の名前 */
                    defaultFileName?: string
                  }): Promise<boolean> => {
                    return await postWithHandler(url, param, async response => {
                      if (response.status === 202 /* Accepted. このリクエストにおいては「～してもよいですか？」の確認メッセージ表示を意味する */) {
                        // 「～してもよいですか？」の確認メッセージ表示
                        const data = (await response.json()) as { {{CommandResult.HTTP_CONFIRM_DETAIL}}: string[] }
                        for (const msg of data.{{CommandResult.HTTP_CONFIRM_DETAIL}}) {
                          if (!window.confirm(msg)) {
                            return { success: false } // "OK"が選択されなかった場合は処理実行APIを呼ばずに処理中断
                          }
                        }
                        // すべての確認メッセージで"OK"が選ばれた場合は再度処理実行APIを呼ぶ。確認メッセージを表示しない旨のオプションをつけたうえで呼ぶ。
                        const success = await executeCommandApi({
                          url: `${url}?{{HTTP_PARAM_IGNORECONFIRM}}=true`,
                          param,
                          defaultSuccessMessage,
                        })
                        return { success }

                      } else if (response.ok) {
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
                            // トーストで処理成功の旨を表示
                            dispatchToast(msg => msg.info(data.text ?? defaultSuccessMessage ?? '処理が成功しました。'))

                            if (data.detail) {
                              // 処理結果の詳細情報がある場合、画面上で結果を表示。
                              // 具体的な表示方法はUIを伴うのでこのフックを呼ぶ側に任せる。
                              setResultDetail(data.detail)
                              return { success: false } // ここの値がtrueだと呼び元のダイアログが閉じてしまうのでfalseにしている
                            } else {
                              return { success: true } // トーストでの表示のみの場合はダイアログを閉じる
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

                      } else if (response.status === 422 /* Unprocessable Content. エラー */) {
                        // 入力内容エラー
                        const data = (await response.json()) as { {{CommandResult.HTTP_ERROR_DETAIL}}: [ReactHookForm.FieldPath<T>, { types: { [key: string]: string } }][] }
                        if (setError) {
                          for (const [name, error] of data.{{CommandResult.HTTP_ERROR_DETAIL}}) {
                            setError(name, error)
                          }
                        } else {
                          dispatchMsg(msg => msg.error(`入力内容が不正です: ${JSON.stringify(data.{{CommandResult.HTTP_ERROR_DETAIL}})}`))
                        }
                        return { success: false }

                      } else if (response.status === 500 /* 想定外のエラー */) {
                        const data = (await response.json()) as { detail: string }
                        if (typeof data?.detail === 'string') {
                          dispatchMsg(msg => msg.error(data.detail))
                          return { success: false }
                        }
                      }
                    })
                  })
                  return { executeCommandApi, resultDetail }
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
                    var result = new {{CommandResult.GENERATOR_WEB_CLASS_NAME}}(this, _applicationService);
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
