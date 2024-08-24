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

            return $$"""
                //#region {{_rootAggregate.Item.DisplayName}}ダイアログ
                /** {{_rootAggregate.Item.DisplayName}}処理実行パラメータ入力ダイアログを開く関数を返します。 */
                export const {{HookName}} = () => {
                  const [, dispatchDialog] = Layout.useDialogContext()

                  return React.useCallback((initialParam?: Types.{{param.TsTypeName}}) => {
                    dispatchDialog(state => state.pushDialog('{{_rootAggregate.Item.DisplayName}}', ({ closeDialog }) => {
                      const rhfMethods = Util.useFormEx<Types.{{param.TsTypeName}}>({ defaultValues: initialParam ?? Types.{{param.TsNewObjectFunction}}() })
                      const { getValues } = rhfMethods
                      const execute = Hooks.{{executor.HookName}}()
                      const handleClickExec = useEvent(() => {
                        execute(getValues())
                        closeDialog()
                      })

                      return (
                        <FormProvider {...rhfMethods}>
                          <div className="h-full flex flex-col">
                            <div className="flex-1 overflow-y-auto">
                              {{form.RenderCaller()}}
                            </div>
                            <div className="flex justify-end">
                              <Input.IconButton fill onClick={handleClickExec}>実行</Input.IconButton>
                            </div>
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
