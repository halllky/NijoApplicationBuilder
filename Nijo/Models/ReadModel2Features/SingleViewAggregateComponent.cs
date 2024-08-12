using Nijo.Core;
using Nijo.Parts.WebClient;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.ReadModel2Features {
    /// <summary>
    /// 詳細画面の一部を構成する、集約1個と対応するReactコンポーネント。
    /// </summary>
    internal class SingleViewAggregateComponent {

        internal SingleViewAggregateComponent(GraphNode<Aggregate> aggregate) {
            _dataClass = new DataClassForDisplay(aggregate);
        }

        protected readonly DataClassForDisplay _dataClass;

        protected string GetComponentName() {
            if (_dataClass.Aggregate.IsOutOfEntryTree()) {
                var refEntry = _dataClass.Aggregate.GetRefEntryEdge().RelationName;
                return $"{_dataClass.Aggregate.Item.PhysicalName}ViewOf{refEntry}Reference";

            } else {
                return $"{_dataClass.Aggregate.Item.PhysicalName}View";
            }
        }

        /// <summary>
        /// 子孫コンポーネントを再帰的に列挙します。
        /// </summary>
        internal IEnumerable<SingleViewDescendantAggregateComponent> EnumerateDescendantsRecursively() {
            foreach (var child in _dataClass.GetChildMembers()) {
                var childComponent = new SingleViewDescendantAggregateComponent(child.MemberInfo);
                yield return childComponent;

                foreach (var grandChildComponent in childComponent.EnumerateDescendantsRecursively()) {
                    yield return grandChildComponent;
                }
            }
        }

        /// <summary>
        /// <see cref="VerticalFormBuilder"/> のインスタンスを組み立てます。
        /// </summary>
        internal VerticalFormBuilder BuildVerticalForm(ReactPageRenderingContext context) {
            var formBuilder = new VerticalFormBuilder();

            foreach (var member in _dataClass.GetOwnMembers()) {
                if (member is AggregateMember.ValueMember vm) {
                    formBuilder.AddItem(
                        vm.Options.MemberType is Core.AggregateMemberTypes.Sentence,
                        member.MemberName,
                        E_VForm2LabelType.String,
                        vm.Options.MemberType.RenderSingleViewVFormBody(vm, context));

                } else if (member is AggregateMember.Ref @ref) {
                    void BuildRecursively(RefTo.RefDisplayData refTarget, VerticalFormSection section) {
                        foreach (var refMember in refTarget.GetOwnMembers()) {
                            if (refMember is AggregateMember.ValueMember vm2) {
                                formBuilder.AddItem(
                                    vm2.Options.MemberType is Core.AggregateMemberTypes.Sentence,
                                    member.MemberName,
                                    E_VForm2LabelType.String,
                                    vm2.Options.MemberType.RenderSingleViewVFormBody(vm2, context));

                            } else if (refMember is AggregateMember.RelationMember rel) {
                                var relRefTarget = new RefTo.RefDisplayData(rel.MemberAggregate, @ref.RefTo);
                                var relSection = formBuilder.AddSection(rel.MemberName, E_VForm2LabelType.String);
                                BuildRecursively(relRefTarget, relSection);
                            }
                        }
                    }
                    var refTarget = new RefTo.RefDisplayData(@ref.RefTo, @ref.RefTo);
                    var section = new VerticalFormSection(@ref.MemberName, E_VForm2LabelType.String);
                    BuildRecursively(refTarget, section);
                }
            }
            foreach (var childDataClass in _dataClass.GetChildMembers()) {
                var childComponent = new SingleViewDescendantAggregateComponent(childDataClass.MemberInfo);
                formBuilder.AddUnknownParts(childComponent.RenderCaller());
            }

            return formBuilder;
        }
    }

    /// <summary>
    /// 子孫要素の <see cref="SingleViewAggregateComponent"/>
    /// </summary>
    internal class SingleViewDescendantAggregateComponent : SingleViewAggregateComponent {
        internal SingleViewDescendantAggregateComponent(AggregateMember.RelationMember relationMember) : base(relationMember.MemberAggregate) {
            _relationMember = relationMember;
        }

        private readonly AggregateMember.RelationMember _relationMember;

        /// <summary>
        /// このコンポーネントが受け取る引数の名前のリスト。
        /// 祖先コンポーネントの中に含まれるChildrenの数だけ、
        /// このコンポーネントのその配列中でのインデックスが特定されている必要があるので、それを引数で受け取る。
        /// </summary>
        protected IReadOnlyList<string> GetArguments() {
            return _dataClass.Aggregate
                .PathFromEntry()
                .Where(edge => edge.Terminal != _dataClass.Aggregate
                            && edge.Terminal.As<Aggregate>().IsChildrenMember())
                .Select((_, i) => $"index_{i}")
                .ToArray();
        }
        /// <summary>
        /// Childrenのレンダリングのために用いられるループ変数の名前
        /// </summary>
        private string GetLoopVarName() {
            return $"index_{GetArguments().Count}";
        }
        /// <summary>
        /// このコンポーネントの呼び出し処理をレンダリングします。
        /// </summary>
        internal string RenderCaller() {
            return $$"""
                <{{GetComponentName()}} />
                """;
        }

        /// <summary>
        /// 子孫要素のコンポーネント定義をレンダリングします。
        /// </summary>
        internal string RenderDeclaring(ReactPageRenderingContext context, bool isReadOnly) {
            var componentName = GetComponentName();
            var args = GetArguments().ToArray();
            var useFormType = $"AggregateType.{new DataClassForDisplay(_dataClass.Aggregate.GetRoot()).TsTypeName}";
            var vForm = BuildVerticalForm(context);

            var registerNameArray = _dataClass.Aggregate.GetFullPathAsReactHookFormRegisterName(E_CsTs.TypeScript, args).ToArray();
            var registerName = registerNameArray.Length > 0 ? $"`{registerNameArray.Join(".")}`" : string.Empty;

            // Childのレンダリング
            if (_relationMember is AggregateMember.Child) {
                return $$"""
                    const {{componentName}} = ({{{args.Join(", ")}} }: {
                    {{args.SelectTextTemplate(arg => $$"""
                      {{arg}}: number
                    """)}}
                    }) => {
                      const { register, registerEx, getValues, setValue } = Util.useFormContextEx<{{useFormType}}>()

                      return (
                        {{WithIndent(vForm.Render(context.CodeRenderingContext), "    ")}}
                      )
                    }
                    """;
            }

            // Variationメンバーのレンダリング
            if (_relationMember is AggregateMember.VariationItem variation) {
                var switchProp = $"`{variation.Group.GetFullPathAsReactHookFormRegisterName(E_CsTs.TypeScript, args).Join(".")}`";

                return $$"""
                    const {{componentName}} = ({{{args.Join(", ")}} }: {
                    {{args.SelectTextTemplate(arg => $$"""
                      {{arg}}: number
                    """)}}
                    }) => {
                      const { register, registerEx, getValues, setValue } = Util.useFormContextEx<{{useFormType}}>()
                      const switchProp = useWatch({{switchProp}})

                      const body = (
                        {{WithIndent(vForm.Render(context.CodeRenderingContext), "    ")}}
                      )

                      return switchProp === '{{variation.TsValue}}'
                        ? (
                          <>
                            {body}
                          </>
                        ) : (
                          <div className="hidden">
                            {body}
                          </div>
                        )
                    }
                    """;

            }

            // Childrenのレンダリング（子集約をもつなど表であらわせない場合）
            if (_relationMember is AggregateMember.Children && !_dataClass.Aggregate.CanDisplayAllMembersAs2DGrid()) {
                var loopVar = GetLoopVarName();

                return $$"""
                    const {{componentName}} = ({{{args.Join(", ")}} }: {
                    {{args.SelectTextTemplate(arg => $$"""
                      {{arg}}: number
                    """)}}
                    }) => {
                      const { register, registerEx, control } = Util.useFormContextEx<{{useFormType}}>()
                      const { fields, append, remove } = useFieldArray({ control, name: {{registerName}} })
                    {{If(!isReadOnly, () => $$"""
                      const onCreate = useCallback(() => {
                        append(AggregateType.{{_dataClass.TsNewObjectFunction}}())
                      }, [append])
                      const onRemove = useCallback((index: number) => {
                        return (e: React.MouseEvent) => {
                          remove(index)
                          e.preventDefault()
                        }
                      }, [remove])
                    """)}}

                      return (
                        <VForm2.Indent label={(
                          <div className="flex gap-2 justify-start">
                            <VForm2.LabelText>{{_dataClass.Aggregate.GetParent()?.RelationName}}</VForm2.LabelText>
                    {{If(!isReadOnly, () => $$"""
                            <Input.Button onClick={onCreate}>追加</Input.Button>
                    """)}}
                          </div>
                        )}>
                          {fields.map((item, {{loopVar}}) => (
                            <VForm2.Indent key={{{loopVar}}} labelPosition="left" label={(
                    {{If(isReadOnly, () => $$"""
                              <VForm2.LabelText>{{{loopVar}}}</VForm2.LabelText>
                    """).Else(() => $$"""
                              <div className="flex flex-col gap-1">
                                <VForm2.LabelText>{{{loopVar}}}</VForm2.LabelText>
                                <Input.IconButton underline icon={XMarkIcon} onClick={onRemove({{loopVar}})}>削除</Input.IconButton>
                              </div>
                    """)}}
                            )}>
                              {{WithIndent(vForm.Render(context.CodeRenderingContext), "          ")}}
                            </VForm2.Indent>
                          ))}
                        </VForm2.Indent>
                      )
                    }
                    """;
            }

            // Childrenのレンダリング（子集約をもたず表で表せる場合）
            if (_relationMember is AggregateMember.Children && _dataClass.Aggregate.CanDisplayAllMembersAs2DGrid()) {
                var loopVar = GetLoopVarName();
                var tableBuilder = new Parts.WebClient.DataTable.DataTableBuilder(_dataClass.Aggregate, $"AggregateType.{_dataClass.TsTypeName}");
                tableBuilder.AddMembers(_dataClass);

                return $$"""
                    const {{componentName}} = ({{{args.Join(", ")}} }: {
                    {{args.SelectTextTemplate(arg => $$"""
                      {{arg}}: number
                    """)}}
                    }) => {
                      const { get } = Util.useHttpRequest()
                      const { register, registerEx, control } = Util.useFormContextEx<{{useFormType}}>()
                      const { fields, append, remove, update } = useFieldArray({ control, name: {{registerName}} })
                      const dtRef = useRef<Layout.DataTableRef<AggregateType.{{_dataClass.TsTypeName}}>>(null)

                    {{If(!isReadOnly, () => $$"""
                      const onAdd = useCallback((e: React.MouseEvent) => {
                        append(AggregateType.{{_dataClass.TsNewObjectFunction}}())
                        e.preventDefault()
                      }, [append])
                      const onRemove = useCallback((e: React.MouseEvent) => {
                        const selectedRowIndexes = dtRef.current?.getSelectedRows().map(({ rowIndex }) => rowIndex) ?? []
                        for (const index of selectedRowIndexes.sort((a, b) => b - a)) remove(index)
                        e.preventDefault()
                      }, [dtRef, remove])

                    """)}}
                      const options = useMemo<Layout.DataTableProps<AggregateType.{{_dataClass.TsTypeName}}>>(() => ({
                    {{If(!isReadOnly, () => $$"""
                        onChangeRow: update,
                    """)}}
                        columns: [
                          {{WithIndent(tableBuilder.RenderColumnDef(context.CodeRenderingContext), "      ")}}
                        ],
                      }), [get, {{args.Select(a => $"{a}, ").Join("")}}update])

                      return (
                        <VForm2.Item wide
                    {{If(isReadOnly, () => $$"""
                          label="{{_dataClass.Aggregate.GetParent()?.RelationName}}"
                    """).Else(() => $$"""
                          label={(
                            <div className="flex items-center gap-2">
                              <VForm2.LabelText>{{_dataClass.Aggregate.GetParent()?.RelationName}}</VForm2.LabelText>
                              <Input.Button icon={PlusIcon} onClick={onAdd}>追加</Input.Button>
                              <Input.Button icon={XMarkIcon} onClick={onRemove}>削除</Input.Button>
                            </div>
                          )}
                    """)}}
                          >
                          <Layout.DataTable
                            ref={dtRef}
                            data={fields}
                            {...options}
                            className="h-64 w-full border-t border-color-3"
                          />
                        </VForm2.Item>
                      )
                    }
                    """;
            }

            throw new NotImplementedException();
        }
    }
}
