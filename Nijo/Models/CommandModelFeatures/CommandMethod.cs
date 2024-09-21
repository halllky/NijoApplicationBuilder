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

        private string AppSrvOnStepChangingMethod => $"On{_rootAggregate.Item.PhysicalName}StepChanging";
        private string AppSrvExecuteMethod => $"Execute{_rootAggregate.Item.PhysicalName}";

        /// <summary>C#側でステップ属性のどの要素かを表す列挙体の名前</summary>
        private string StepEnumName => $"E_{_rootAggregate.Item.PhysicalName}Steps";

        private const string USE_COMMAND_RESULT_PARSER = "useCommandResultParser";
        private const string HTTP_PARAM_IGNORECONFIRM = "ignoreConfirm";

        private const string USE_STEP_CHAGNE_HOOK = "useCommandStepChanges";
        private const string CONTROLLER_CHAGNE_STEPS = "change-step";

        // ステップ切り替え時イベント引数
        private const string STEP_CHANGING_EVENT_ARGS = "CommandStepChangingEventArgs";
        private const string DATA_TS = "data";
        private const string BEFORE_STEP_TS = "beforeStep";
        private const string AFTER_STEP_TS = "afterStep";
        private const string VISITED_STEPS_TS = "visitedSteps";
        private const string IGNORE_CONFIRM_TS = "ignoreConfirm";

        internal string RenderHook(CodeRenderingContext context) {
            var param = new CommandParameter(_rootAggregate);
            var steps = _rootAggregate
                .GetMembers()
                .OfType<AggregateMember.RelationMember>()
                .Where(m => m.MemberAggregate.Item.Options.Step != null)
                .Select(m => m.MemberAggregate.Item.Options.Step!.Value)
                .OrderBy(step => step)
                .ToArray();

            var args = new List<string>();
            if (steps.Length != 0) {
                args.Add($"getValues: ReactHookForm.UseFormGetValues<Types.{param.TsTypeName}>");
                args.Add($"resetData: ReactHookForm.UseFormReset<Types.{param.TsTypeName}>");
            }
            args.Add($"setError: ReactHookForm.UseFormSetError<Types.{param.TsTypeName}>");

            return $$"""
                /** {{_rootAggregate.Item.DisplayName}}処理を呼び出す関数を返します。 */
                export const {{HookName}} = ({{args.Join(", ")}}) => {
                  const { executeCommandApi, resultDetail } = {{USE_COMMAND_RESULT_PARSER}}(setError)
                {{If(steps.Length != 0, () => $$"""
                  const [currentStep, setCurrentStep] = React.useState({{steps.First()}}) // 現在表示中のステップの番号
                  const [visitedSteps, setVisitedSteps] = React.useState<number[]>([]) // 一度以上表示したことがあるステップの番号
                  const callStepChangeEvent = {{USE_STEP_CHAGNE_HOOK}}(resetData, setError, setVisitedSteps)
                  const allSteps = React.useMemo(() => {
                    return [{{steps.Select(s => s.ToString()).Join(", ")}}]
                  }, [])
                  const toPreviousStep = useEvent(async () => {
                    const beforeStep = currentStep
                    let afterStep: number
                {{steps.Skip(1).SelectTextTemplate((step, i) => $$"""
                    {{(i == 0 ? "if" : "} else if")}} (currentStep === {{step}}) {
                      afterStep = {{steps.ElementAt(i)}}
                """)}}
                    } else {
                      return
                    }
                    const success = await callStepChangeEvent(`/{{Controller.SUBDOMAIN}}/{{CommandController.SUBDOMAIN}}/{{(_rootAggregate.Item.Options.LatinName ?? _rootAggregate.Item.UniqueId).ToKebabCase()}}/{{CONTROLLER_CHAGNE_STEPS}}`, getValues(), beforeStep, afterStep, visitedSteps)
                    if (success) setCurrentStep(afterStep)
                  })
                  const toNextStep = useEvent(async () => {
                    const beforeStep = currentStep
                    let afterStep: number
                {{steps.SkipLast(1).SelectTextTemplate((step, i) => $$"""
                    {{(i == 0 ? "if" : "} else if")}} (currentStep === {{step}}) {
                      afterStep = {{steps.ElementAt(i + 1)}}
                """)}}
                    } else {
                      return
                    }
                    const success = await callStepChangeEvent(`/{{Controller.SUBDOMAIN}}/{{CommandController.SUBDOMAIN}}/{{(_rootAggregate.Item.Options.LatinName ?? _rootAggregate.Item.UniqueId).ToKebabCase()}}/{{CONTROLLER_CHAGNE_STEPS}}`, getValues(), beforeStep, afterStep, visitedSteps)
                    if (success) setCurrentStep(afterStep)
                  })
                """)}}
                  const launch = useEvent(async (param: Types.{{param.TsTypeName}}) => {
                    return await executeCommandApi({
                      url: `/{{Controller.SUBDOMAIN}}/{{CommandController.SUBDOMAIN}}/{{(_rootAggregate.Item.Options.LatinName ?? _rootAggregate.Item.UniqueId).ToKebabCase()}}`,
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
                /** 複数のステップから成るコマンドにおいてステップの切り替え時のサーバー側処理を呼んでその結果を解釈する */
                export const {{USE_STEP_CHAGNE_HOOK}} = <T extends object>(
                  resetData: ReactHookForm.UseFormReset<T>,
                  setError: ReactHookForm.UseFormSetError<T>,
                  setVisitedSteps: ((v: number[]) => void)) => {
                  const { postWithHandler } = Util.useHttpRequest()
                  const [, dispatchMsg] = Util.useMsgContext()

                  const callStepChangeEvent = useEvent(async (url: string, {{DATA_TS}}: T, {{BEFORE_STEP_TS}}: number | undefined, {{AFTER_STEP_TS}}: number, {{VISITED_STEPS_TS}}: number[], {{IGNORE_CONFIRM_TS}}?: boolean): Promise<boolean> => {
                    return await postWithHandler(url, { {{DATA_TS}}, {{BEFORE_STEP_TS}}, {{AFTER_STEP_TS}}, {{VISITED_STEPS_TS}}, {{IGNORE_CONFIRM_TS}} }, async response => {
                      if (response.status === 200 /* 成功 */) {
                        // サーバー側で編集された値を画面に反映する
                        const resData = (await response.json()) as { {{DATA_TS}}: T, {{VISITED_STEPS_TS}}: number[] }
                        resetData(resData.{{DATA_TS}}, { keepDefaultValues: true })
                        setVisitedSteps(resData.{{VISITED_STEPS_TS}})
                        return { success: true }

                      } else if (response.status === 202 /* Accepted. このリクエストにおいては「～してもよいですか？」の確認メッセージ表示を意味する */) {
                        // 「～してもよいですか？」の確認メッセージ表示
                        const resData = (await response.json()) as { {{CommandResult.HTTP_CONFIRM_DETAIL}}: string[] }
                        for (const msg of resData.{{CommandResult.HTTP_CONFIRM_DETAIL}}) {
                          if (!window.confirm(msg)) {
                            return { success: false } // "OK"が選択されなかった場合は処理実行APIを呼ばずに処理中断
                          }
                        }
                        // すべての確認メッセージで"OK"が選ばれた場合は再度処理実行APIを呼ぶ。確認メッセージを表示しない旨のオプションをつけたうえで呼ぶ。
                        const success = await callStepChangeEvent(url, {{DATA_TS}}, {{BEFORE_STEP_TS}}, {{AFTER_STEP_TS}}, {{VISITED_STEPS_TS}}, true)
                        return { success }

                      } else if (response.status === 422 /* Unprocessable Content. エラー */) {
                        // 入力内容エラー
                        const resData = (await response.json()) as { {{CommandResult.HTTP_ERROR_DETAIL}}: [ReactHookForm.FieldPath<T>, { types: { [key: string]: string } }][] }
                        if (setError) {
                          for (const [name, error] of resData.{{CommandResult.HTTP_ERROR_DETAIL}}) {
                            setError(name, error)
                          }
                        } else {
                          dispatchMsg(msg => msg.error(`入力内容が不正です: ${JSON.stringify(resData.{{CommandResult.HTTP_ERROR_DETAIL}})}`))
                        }
                        return { success: false }
                      }
                    })
                  })
                  return callStepChangeEvent
                }
                /** コマンドを呼び出し、その処理結果を解釈して画面遷移したりファイルダウンロードを開始したりする */
                export const {{USE_COMMAND_RESULT_PARSER}} = <T extends object>(setError: ReactHookForm.UseFormSetError<T>) => {
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
                        for (const [name, error] of data.{{CommandResult.HTTP_ERROR_DETAIL}}) {
                          setError(name, error)
                        }
                        return { success: false }
                      }
                    })
                  })
                  return { executeCommandApi, resultDetail }
                }
                """;
        }

        internal string RenderController(CodeRenderingContext context) {
            var param = new CommandParameter(_rootAggregate);
            var steps = _rootAggregate
                .GetMembers()
                .OfType<AggregateMember.RelationMember>()
                .Any(m => m.MemberAggregate.Item.Options.Step != null);

            return $$"""
                {{If(steps, () => $$"""
                /// <summary>
                /// {{_rootAggregate.Item.DisplayName}}のステップ変更時イベント
                /// </summary>
                [HttpPost("{{(_rootAggregate.Item.Options.LatinName ?? _rootAggregate.Item.UniqueId).ToKebabCase()}}/{{CONTROLLER_CHAGNE_STEPS}}")]
                public virtual IActionResult {{_rootAggregate.Item.PhysicalName}}ChgngeStep([FromBody] {{STEP_CHANGING_EVENT_ARGS}}<{{param.CsClassName}}, {{StepEnumName}}, {{param.MessageDataCsClassName}}> e) {
                    _applicationService.{{AppSrvOnStepChangingMethod}}(e);
                    if (e.Errors.HasError()) {
                        return UnprocessableEntity(new { {{CommandResult.HTTP_ERROR_DETAIL}} = e.Errors.ToJsonNodes(null) });
                    } else if (!e.IgnoreConfirm && e.HasConfirm()) {
                        return Accepted(new { {{CommandResult.HTTP_CONFIRM_DETAIL}} = e.GetConfirms().ToArray() });
                    } else {
                        return Ok(e);
                    }
                }
                """)}}
                /// <summary>
                /// {{_rootAggregate.Item.DisplayName}}処理をWebAPI経由で実行するためのエンドポイント
                /// </summary>
                [HttpPost("{{(_rootAggregate.Item.Options.LatinName ?? _rootAggregate.Item.UniqueId).ToKebabCase()}}")]
                public virtual IActionResult {{_rootAggregate.Item.PhysicalName}}([FromBody] {{param.CsClassName}} param, [FromQuery] bool {{HTTP_PARAM_IGNORECONFIRM}}) {
                    var resultGenerator = new {{CommandResult.GENERATOR_WEB_CLASS_NAME}}<{{param.MessageDataCsClassName}}>(this, _applicationService);
                    var commandResult = _applicationService.{{AppSrvExecuteMethod}}(param, resultGenerator, {{HTTP_PARAM_IGNORECONFIRM}});
                    return (({{CommandResult.GENERATOR_WEB_CLASS_NAME}}<{{param.MessageDataCsClassName}}>.{{CommandResult.ACTION_RESULT_CONTAINER}})commandResult).ActionResult;
                }
                """;
        }

        internal string RenderAbstractMethod(CodeRenderingContext context) {
            var param = new CommandParameter(_rootAggregate);
            var steps = _rootAggregate
                .GetMembers()
                .OfType<AggregateMember.RelationMember>()
                .Where(m => m.MemberAggregate.Item.Options.Step != null)
                .Select(m => m.MemberAggregate.Item.Options.Step!.Value)
                .OrderBy(step => step)
                .ToArray();

            return $$"""
                {{If(steps.Length > 0, () => $$"""
                /// <summary>
                /// {{_rootAggregate.Item.DisplayName}}のステップ切り替え時やコマンド実行前のタイミングで実行される処理
                /// </summary>
                /// <param name="param">処理パラメータ</param>
                /// <param name="e">エラー情報などがある場合はこのオブジェクトに設定してください。</param>
                public virtual void {{AppSrvOnStepChangingMethod}}({{STEP_CHANGING_EVENT_ARGS}}<{{param.CsClassName}}, {{StepEnumName}}, {{param.MessageDataCsClassName}}> e) {
                    // ステップの切り替え時に何らかの処理が必要な場合、このメソッドをオーバーライドして処理を記述してください。
                }
                """)}}
                /// <summary>
                /// {{_rootAggregate.Item.DisplayName}}処理実行
                /// </summary>
                /// <param name="param">処理パラメータ</param>
                /// <param name="result">処理結果。return result.XXXXX(); のような形で記述してください。</param>
                /// <param name="ignoreConfirm">「～しますがよいですか？」のメッセージを無視する場合（画面側で「はい」が選択された後）はtrue</param>
                public virtual {{CommandResult.RESULT_INTERFACE_NAME}} {{AppSrvExecuteMethod}}({{param.CsClassName}} param, {{CommandResult.GENERATOR_INTERFACE_NAME}}<{{param.MessageDataCsClassName}}> result, bool ignoreConfirm) {
                    throw new NotImplementedException("{{_rootAggregate.Item.DisplayName}}処理は実装されていません。");
                }
                """;
        }

        internal string RenderStepEnum() {
            var steps = _rootAggregate
                .GetMembers()
                .OfType<AggregateMember.RelationMember>()
                .Where(m => m.MemberAggregate.Item.Options.Step != null)
                .Select(m => new {
                    m.MemberAggregate.Item.PhysicalName,
                    m.MemberAggregate.Item.Options.Step!.Value,
                })
                .OrderBy(step => step.Value)
                .ToArray();
            if (steps.Length == 0) return SKIP_MARKER;

            return $$"""
                /// <summary>{{_rootAggregate.Item.DisplayName}}コマンドのUIのうち第何ステップかを表す列挙体</summary>
                public enum {{StepEnumName}} {
                {{steps.SelectTextTemplate(x => $$"""
                    {{x.PhysicalName}} = {{x.Value}},
                """)}}
                }
                """;
        }

        internal static SourceFile RenderStepChangeEventArgs() => new() {
            FileName = "CommandEventArgs.cs",
            RenderContent = ctx => {
                return $$"""
                    using System.Text.Json.Serialization;

                    namespace {{ctx.Config.RootNamespace}};

                    /// <summary>
                    /// 複数のステップから成るコマンドにおいて、ステップの切り替え時に実行されるイベントの引数。
                    /// </summary>
                    public partial class {{STEP_CHANGING_EVENT_ARGS}}<TData, TStep, TErrors>
                        where TStep : struct, Enum
                        where TErrors : new() {

                    #pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
                        /// <summary>
                        /// データ。このオブジェクトの内容を編集するとクライアント側に反映されます。
                        /// </summary>
                        [JsonPropertyName("{{DATA_TS}}")]
                        public TData Data { get; set; }
                        /// <summary>
                        /// 切り替え前のステップ。nullの場合はコマンド画面初期表示時を表します。
                        /// </summary>
                        [JsonPropertyName("{{BEFORE_STEP_TS}}")]
                        public TStep? BeforeStep { get; init; }
                        /// <summary>
                        /// 切り替え後のステップ
                        /// </summary>
                        [JsonPropertyName("{{AFTER_STEP_TS}}")]
                        public TStep AfterStep { get; init; }
                        /// <summary>
                        /// 開いたことのあるステップの一覧
                        /// </summary>
                        [JsonPropertyName("{{VISITED_STEPS_TS}}")]
                        public List<TStep> VisitedSteps { get; set; }
                        /// <summary>
                        /// 「～ですが処理実行してよいですか？」を無視するかどうか
                        /// </summary>
                        [JsonPropertyName("{{IGNORE_CONFIRM_TS}}")]
                        public bool IgnoreConfirm { get; init; }
                    #pragma warning restore CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。

                        /// <summary>
                        /// エラーメッセージ。1件以上ある場合はステップの切り替えが中断されます。
                        /// </summary>
                        [JsonIgnore]
                        public TErrors Errors { get; } = new();

                        /// <summary>
                        /// 「～ですが処理実行してよいですか？」等の確認メッセージを追加します。
                        /// </summary>
                        public void AddConfirm(string message) => _confirms.Add(message);
                        public bool HasConfirm() => _confirms.Count > 0;
                        public IEnumerable<string> GetConfirms() => _confirms;
                        private readonly List<string> _confirms = new();
                    }
                    """;
            },
        };
    }
}
