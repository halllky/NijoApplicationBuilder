using Nijo.Core;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.CommandModelFeatures {
    /// <summary>
    /// <see cref="CommandModel"/> 呼び出し用のモーダルダイアログのUI
    /// </summary>
    internal class CommandDialog {
        internal CommandDialog(GraphNode<Aggregate> rootAggregate) {
            _rootAggregate = rootAggregate;
        }
        private readonly GraphNode<Aggregate> _rootAggregate;

        internal string HookName => $"use{_rootAggregate.Item.PhysicalName}Dialog";

        internal string RenderHook(CodeRenderingContext context) {
            var param = new CommandParameter(_rootAggregate);
            var form = new CommandDialogAggregateComponent(_rootAggregate);
            var executor = new CommandMethod(_rootAggregate);

            var steps = _rootAggregate
                .GetMembers()
                .OfType<AggregateMember.RelationMember>()
                .Where(m => m.MemberAggregate.Item.Options.Step != null)
                .Select(m => m.MemberAggregate.Item.Options.Step!.Value)
                .OrderBy(step => step)
                .ToArray();

            return $$"""
                //#region {{_rootAggregate.Item.DisplayName}}ダイアログ
                /** {{_rootAggregate.Item.DisplayName}}処理実行パラメータ入力ダイアログを開く関数を返します。 */
                export const {{HookName}} = () => {
                  const [, dispatchDialog] = Layout.useDialogContext()

                  return React.useCallback((initialParam?: Types.{{param.TsTypeName}}) => {
                    dispatchDialog(state => state.pushDialog('{{_rootAggregate.Item.DisplayName}}', ({ closeDialog }) => {
                      const rhfMethods = Util.useFormEx<Types.{{param.TsTypeName}}>({ defaultValues: initialParam ?? Types.{{param.TsNewObjectFunction}}() })
                      const { getValues, setError, clearErrors } = rhfMethods
                      const { launch, resultDetail } = Hooks.{{executor.HookName}}(setError)
                {{If(steps.Length != 0, () => $$"""
                      const [currentStep, setCurrentStep] = useState({{steps.First()}})
                      const allSteps = useMemo(() => [{{steps.Select(s => s.ToString()).Join(", ")}}], [])
                      const handlePreviousStep = useEvent(() => {
                {{steps.Skip(1).SelectTextTemplate((step, i) => $$"""
                        {{(i == 0 ? "if" : "} else if")}} (currentStep === {{step}}) {
                          setCurrentStep({{steps.ElementAt(i)}})
                """)}}
                        }
                      })
                      const handleNextStep = useEvent(() => {
                {{steps.SkipLast(1).SelectTextTemplate((step, i) => $$"""
                        {{(i == 0 ? "if" : "} else if")}} (currentStep === {{step}}) {
                          setCurrentStep({{steps.ElementAt(i + 1)}})
                """)}}
                        }
                      })
                """)}}
                      const handleClickExec = useEvent(async () => {
                        clearErrors()
                        const finish = await launch(getValues())
                        if (finish) closeDialog()
                      })

                      return (
                        <FormProvider {...rhfMethods}>
                          <div className="h-full flex flex-col gap-1">
                            <div className="flex-1 overflow-y-auto">
                              {resultDetail === undefined ? (
                                {{form.RenderCaller(null, steps.Length != 0 ? [$"{CommandDialogAggregateComponent.CURRENT_STEP}={{currentStep}}"] : null)}}
                              ) : (
                                <Layout.UnknownObjectViewer label="処理結果" value={resultDetail} />
                              )}
                            </div>
                            {resultDetail === undefined && (
                              <div className="flex justify-end">
                {{If(steps.Length == 0, () => $$"""
                                <Input.IconButton fill onClick={handleClickExec}>実行</Input.IconButton>
                """).Else(() => $$"""
                                <Input.IconButton outline onClick={handlePreviousStep} className={(currentStep === {{steps.First()}} ? 'invisible' : '')}>戻る</Input.IconButton>
                                <div className="flex-1 flex justify-center items-center">
                                  <Input.WizardStepIndicator currentStep={currentStep} allSteps={allSteps} />
                                </div>
                                {currentStep === {{steps.Last()}} ? (
                                  <Input.IconButton fill onClick={handleClickExec}>実行</Input.IconButton>
                                ) : (
                                  <Input.IconButton outline onClick={handleNextStep}>次へ</Input.IconButton>
                                )}
                """)}}
                              </div>
                            )}
                          </div>
                        </FormProvider>
                      )
                    }))
                  }, [dispatchDialog])
                }
                {{form.EnumerateThisAndDescendantsRecursively().SelectTextTemplate(component => $$"""

                {{component.RenderDeclaring(context, false)}}
                """)}}
                //#endregion {{_rootAggregate.Item.DisplayName}}ダイアログ
                """;
        }
    }
}