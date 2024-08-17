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

        protected string ComponentName {
            get {
                if (_dataClass.Aggregate.IsOutOfEntryTree()) {
                    var refEntry = _dataClass.Aggregate.GetRefEntryEdge().RelationName;
                    return $"{_dataClass.Aggregate.Item.PhysicalName}ViewOf{refEntry}Reference";

                } else {
                    return $"{_dataClass.Aggregate.Item.PhysicalName}View";
                }
            }
        }
        protected string UseFormType => $"AggregateType.{new DataClassForDisplay(_dataClass.Aggregate.GetRoot()).TsTypeName}";

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
                .Select((_, i) => $"index{i}")
                .ToArray();
        }

        /// <summary>
        /// このコンポーネントと子孫コンポーネントを再帰的に列挙します。
        /// </summary>
        internal IEnumerable<SingleViewAggregateComponent> EnumerateThisAndDescendants() {
            yield return this;

            var descendants = _dataClass
                .GetChildMembers()
                .Select(GetDescendantComponent)
                .SelectMany(component => component.EnumerateThisAndDescendants());
            foreach (var component in descendants) {
                yield return component;
            }
        }

        /// <summary>
        /// <see cref="VerticalFormBuilder"/> のインスタンスを組み立てます。
        /// </summary>
        protected VerticalFormBuilder BuildVerticalForm(CodeRenderingContext context) {
            var formBuilder = new VerticalFormBuilder();
            var formContext = new ReactPageRenderingContext {
                CodeRenderingContext = context,
                Register = "registerEx",
                RenderingObjectType = E_ReactPageRenderingObjectType.DataClassForDisplay,
                RenderErrorMessage = vm => {
                    var fullpath = vm.Declared.GetFullPathAsReactHookFormRegisterName(E_CsTs.TypeScript, E_PathType.Value, GetArguments());

                    // 参照先のValueMemberにはエラーメッセージが無い。
                    // エラーメッセージは参照先のValueMemberではなくRefにつく
                    var render = vm.DeclaringAggregate == _dataClass.Aggregate;

                    return $$"""
                        {{If(render, () => $$"""
                        <Input.ErrorMessage name={`{{fullpath.Join(".")}}`} errors={errors} />
                        """)}}
                        """;
                },
            };

            foreach (var member in _dataClass.GetOwnMembers()) {
                if (member is AggregateMember.ValueMember vm) {
                    if (vm.DeclaringAggregate != _dataClass.Aggregate) continue; // 参照先の項目
                    if (vm.Options.InvisibleInGui) continue; // 非表示項目

                    formBuilder.AddItem(
                        vm.Options.MemberType is Core.AggregateMemberTypes.Sentence,
                        member.MemberName,
                        E_VForm2LabelType.String,
                        vm.Options.MemberType.RenderSingleViewVFormBody(vm, formContext));

                } else if (member is AggregateMember.Ref @ref) {
                    var refTarget = new RefTo.RefDisplayData(@ref.RefTo, @ref.RefTo);
                    var fullpath = @ref.GetFullPathAsReactHookFormRegisterName(E_CsTs.TypeScript, E_PathType.Value, GetArguments());
                    var section = formBuilder.AddSection($$"""
                        (<>
                          <VForm2.LabelText>{{@ref.MemberName}}</VForm2.LabelText>
                          <Input.ErrorMessage name={`{{fullpath.Join(".")}}`} errors={errors} />
                        </>)
                        """,
                        E_VForm2LabelType.JsxElement);
                    BuildRecursively(refTarget, section);

                    void BuildRecursively(RefTo.RefDisplayData refTarget, VerticalFormSection section) {
                        foreach (var refMember in refTarget.GetOwnMembers()) {
                            if (refMember is AggregateMember.ValueMember vm2) {
                                section.AddItem(
                                    vm2.Options.MemberType is Core.AggregateMemberTypes.Sentence,
                                    vm2.MemberName,
                                    E_VForm2LabelType.String,
                                    vm2.Options.MemberType.RenderSingleViewVFormBody(vm2, formContext));

                            } else if (refMember is AggregateMember.RelationMember rel) {
                                var relRefTarget = new RefTo.RefDisplayData(rel.MemberAggregate, @ref.RefTo);
                                var relSection = section.AddSection(rel.MemberName, E_VForm2LabelType.String);
                                BuildRecursively(relRefTarget, relSection);
                            }
                        }
                    }
                }
            }
            foreach (var childDataClass in _dataClass.GetChildMembers()) {
                var args = GetArguments().ToArray();
                var descendant = GetDescendantComponent(childDataClass);
                formBuilder.AddUnknownParts(descendant.RenderCaller(args));
            }

            return formBuilder;
        }

        /// <summary>
        /// このコンポーネントの呼び出し処理をレンダリングします。
        /// </summary>
        internal string RenderCaller(params string[] args) {
            var parameters = GetArguments()
                .Select((a, i) => $"{a}={{{args.ElementAtOrDefault(i)}}} ");
            return $$"""
                <{{ComponentName}} {{parameters.Join("")}}/>
                """;
        }

        internal virtual string RenderDeclaring(CodeRenderingContext context, bool isReadOnly) {
            // ルート要素のコンポーネントのレンダリング
            var vForm = BuildVerticalForm(context);

            return $$"""
                const {{ComponentName}} = () => {
                  const { register, registerEx, getValues, setValue, formState: { errors }, control } = Util.useFormContextEx<{{UseFormType}}>()

                  return (
                    <div className="p-px">
                      <Input.ErrorMessage name="root" errors={errors} />
                      {{WithIndent(vForm.RenderAsRoot(context), "      ")}}
                    </div>
                  )
                }
                """;
        }


        /* ------------------------- 子孫要素のコンポーネント ここから ------------------------------ */

        /// <summary>
        /// 子孫要素のコンポーネントをその種類に応じて返します。
        /// </summary>
        private static SingleViewAggregateComponent GetDescendantComponent(DataClassForDisplayDescendant displayData) {
            if (displayData.MemberInfo is AggregateMember.Child child) {
                return new ChildComponent(child);

            } else if (displayData.MemberInfo is AggregateMember.VariationItem variation) {
                return new VariationItemComponent(variation);

            } else if (displayData.MemberInfo is AggregateMember.Children children1 && !displayData.Aggregate.CanDisplayAllMembersAs2DGrid()) {
                return new ChildrenFormComponent(children1);

            } else if (displayData.MemberInfo is AggregateMember.Children children2 && displayData.Aggregate.CanDisplayAllMembersAs2DGrid()) {
                return new ChildrenGridComponent(children2);

            } else {
                throw new NotImplementedException();
            }
        }


        /// <summary>
        /// <see cref="AggregateMember.Child"/> のコンポーネント
        /// </summary>
        private class ChildComponent : SingleViewAggregateComponent {
            internal ChildComponent(AggregateMember.Child child) : base(child.ChildAggregate) {
                _child = child;
            }

            private readonly AggregateMember.Child _child;

            internal override string RenderDeclaring(CodeRenderingContext context, bool isReadOnly) {
                var args = GetArguments().ToArray();
                var vForm = BuildVerticalForm(context);

                return $$"""
                    const {{ComponentName}} = ({{{args.Join(", ")}} }: {
                    {{args.SelectTextTemplate(arg => $$"""
                      {{arg}}: number
                    """)}}
                    }) => {
                      const { register, registerEx, getValues, setValue } = Util.useFormContextEx<{{UseFormType}}>()

                      return (
                        {{WithIndent(vForm.Render(context), "    ")}}
                      )
                    }
                    """;
            }
        }


        /// <summary>
        /// <see cref="AggregateMember.VariationItem"/> のコンポーネント
        /// </summary>
        private class VariationItemComponent : SingleViewAggregateComponent {
            internal VariationItemComponent(AggregateMember.VariationItem variation) : base(variation.VariationAggregate) {
                _variation = variation;
            }

            private readonly AggregateMember.VariationItem _variation;

            internal override string RenderDeclaring(CodeRenderingContext context, bool isReadOnly) {
                var args = GetArguments().ToArray();
                var vForm = BuildVerticalForm(context);
                var switchProp = $"`{_variation.Group.GetFullPathAsReactHookFormRegisterName(E_CsTs.TypeScript, E_PathType.Value, args).Join(".")}`";

                return $$"""
                    const {{ComponentName}} = ({{{args.Join(", ")}} }: {
                    {{args.SelectTextTemplate(arg => $$"""
                      {{arg}}: number
                    """)}}
                    }) => {
                      const { register, registerEx, getValues, setValue, control } = Util.useFormContextEx<{{UseFormType}}>()
                      const switchProp = useWatch({ name: `{{switchProp}}`, control })

                      const body = (
                        {{WithIndent(vForm.Render(context), "    ")}}
                      )

                      return switchProp === '{{_variation.TsValue}}'
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
        }


        /// <summary>
        /// <see cref="AggregateMember.Children"/> のコンポーネント（子配列をもつなど表であらわせない場合）
        /// </summary>
        private class ChildrenFormComponent : SingleViewAggregateComponent {
            internal ChildrenFormComponent(AggregateMember.Children children) : base(children.ChildrenAggregate) {
                _children = children;
            }

            private readonly AggregateMember.Children _children;

            internal override string RenderDeclaring(CodeRenderingContext context, bool isReadOnly) {
                var args = GetArguments().ToArray();
                var vForm = BuildVerticalForm(context);
                var loopVar = args.Length == 0 ? "x" : $"x{args.Length}";

                var registerNameArray = _dataClass.Aggregate
                        .GetFullPathAsReactHookFormRegisterName(E_CsTs.TypeScript, E_PathType.Value, args)
                        .ToArray();
                var registerName = registerNameArray.Length > 0
                    ? $"`{registerNameArray.Join(".")}`"
                    : string.Empty;

                return $$"""
                    const {{ComponentName}} = ({{{args.Join(", ")}} }: {
                    {{args.SelectTextTemplate(arg => $$"""
                      {{arg}}: number
                    """)}}
                    }) => {
                      const { register, registerEx, control } = Util.useFormContextEx<{{UseFormType}}>()
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
                              {{WithIndent(vForm.Render(context), "          ")}}
                            </VForm2.Indent>
                          ))}
                        </VForm2.Indent>
                      )
                    }
                    """;
            }
        }


        /// <summary>
        /// <see cref="AggregateMember.Children"/> のコンポーネント（子配列などをもたず表で表せる場合）
        /// </summary>
        private class ChildrenGridComponent : SingleViewAggregateComponent {
            internal ChildrenGridComponent(AggregateMember.Children children) : base(children.ChildrenAggregate) {
                _children = children;
            }

            private readonly AggregateMember.Children _children;

            internal override string RenderDeclaring(CodeRenderingContext context, bool isReadOnly) {
                var args = GetArguments().ToArray();
                var vForm = BuildVerticalForm(context);
                var loopVar = args.Length == 0 ? "x" : $"x{args.Length}";

                var registerNameArray = _dataClass.Aggregate
                        .GetFullPathAsReactHookFormRegisterName(E_CsTs.TypeScript, E_PathType.Value, args)
                        .ToArray();
                var registerName = registerNameArray.Length > 0
                    ? $"`{registerNameArray.Join(".")}`"
                    : string.Empty;

                var tableBuilder = new Parts.WebClient.DataTable.DataTableBuilder(_dataClass.Aggregate, $"AggregateType.{_dataClass.TsTypeName}");
                tableBuilder.AddMembers(_dataClass);

                return $$"""
                    const {{ComponentName}} = ({{{args.Join(", ")}} }: {
                    {{args.SelectTextTemplate(arg => $$"""
                      {{arg}}: number
                    """)}}
                    }) => {
                      const { get } = Util.useHttpRequest()
                      const { register, registerEx, control } = Util.useFormContextEx<{{UseFormType}}>()
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
                          {{WithIndent(tableBuilder.RenderColumnDef(context), "      ")}}
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
                            className="h-64 resize-y w-full border-none"
                          />
                        </VForm2.Item>
                      )
                    }
                    """;
            }
        }
    }
}
