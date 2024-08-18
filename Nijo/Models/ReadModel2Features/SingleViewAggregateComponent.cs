using Nijo.Core;
using Nijo.Models.RefTo;
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
            _aggregate = aggregate;
        }

        protected readonly GraphNode<Aggregate> _aggregate;

        protected string ComponentName {
            get {
                if (_componentName == null) {
                    if (_aggregate.IsOutOfEntryTree()) {
                        var relationHistory = new List<string>();
                        foreach (var edge in _aggregate.PathFromEntry()) {
                            if (edge.IsParentChild() && edge.Source == edge.Terminal) {
                                relationHistory.Add(edge.Initial.As<Aggregate>().Item.PhysicalName);
                            } else {
                                relationHistory.Add(edge.RelationName.ToCSharpSafe());
                            }
                        }
                        _componentName = $"{_aggregate.Item.PhysicalName}View_{relationHistory.Join("の")}";

                    } else {
                        _componentName = $"{_aggregate.Item.PhysicalName}View";
                    }
                }
                return _componentName;
            }
        }
        private string? _componentName;

        protected string UseFormType => $"AggregateType.{new DataClassForDisplay(_aggregate.GetEntry().As<Aggregate>()).TsTypeName}";

        /// <summary>
        /// このコンポーネントが受け取る引数の名前のリスト。
        /// 祖先コンポーネントの中に含まれるChildrenの数だけ、
        /// このコンポーネントのその配列中でのインデックスが特定されている必要があるので、それを引数で受け取る。
        /// </summary>
        protected IEnumerable<string> GetArguments() {
            return _aggregate
                .PathFromEntry()
                .Where(edge => edge.Terminal != _aggregate
                            && edge.Source == edge.Initial
                            && edge.IsParentChild()
                            && edge.Terminal.As<Aggregate>().IsChildrenMember())
                .Select((_, i) => $"index{i}");
        }
        /// <summary>
        /// コンポーネント引数 + Children用のループ変数
        /// </summary>
        protected virtual IEnumerable<string> GetArgumentsAndLoopVar() {
            return GetArguments();
        }

        /// <summary>
        /// このコンポーネント、参照先のコンポーネント、子孫コンポーネントを再帰的に列挙します。
        /// </summary>
        internal virtual IEnumerable<SingleViewAggregateComponent> EnumerateThisAndDescendants() {
            yield return this;

            // 子要素の列挙
            IEnumerable<AggregateMember.RelationMember> members;
            if (_aggregate.IsOutOfEntryTree()) {
                var refEntry = _aggregate.GetRefEntryEdge().Terminal;
                var displayData = new RefDisplayData(_aggregate, refEntry);
                members = displayData
                    .GetOwnMembers()
                    .OfType<AggregateMember.RelationMember>();
            } else {
                var displayData = new DataClassForDisplay(_aggregate);
                var refs = displayData
                    .GetOwnMembers()
                    .OfType<AggregateMember.Ref>();
                var child = displayData
                    .GetChildMembers()
                    .Select(x => x.MemberInfo);
                members = refs.Concat(child);
            }

            // 子要素のコンポーネントへの変換
            var descendants = members
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
            // レンダリング対象メンバーを列挙
            IEnumerable<AggregateMember.AggregateMemberBase> members;
            if (_aggregate.IsOutOfEntryTree()) {
                var refEntry = _aggregate.GetRefEntryEdge().Terminal;
                var displayData = new RefDisplayData(_aggregate, refEntry);
                members = displayData
                    .GetOwnMembers();
            } else {
                var displayData = new DataClassForDisplay(_aggregate);
                members = displayData
                    .GetOwnMembers()
                    .Concat(displayData.GetChildMembers().Select(x => x.MemberInfo));
            }

            // フォーム組み立て
            var formBuilder = new VerticalFormBuilder();
            var formContext = new ReactPageRenderingContext {
                CodeRenderingContext = context,
                Register = "registerEx",
                AncestorsIndexes = GetArgumentsAndLoopVar(),
                RenderErrorMessage = vm => {
                    var fullpath = vm.Declared.GetFullPathAsReactHookFormRegisterName(E_PathType.Value, GetArgumentsAndLoopVar());
                    var render = vm.DeclaringAggregate == _aggregate; // 参照先のValueMemberにはエラーメッセージが無い。エラーメッセージは参照先のValueMemberではなくRefにつく
                    return $$"""
                        {{If(render, () => $$"""
                        <Input.ErrorMessage name={`{{fullpath.Join(".")}}`} errors={errors} />
                        """)}}
                        """;
                },
            };
            foreach (var member in members.OrderBy(m => m.Order)) {
                if (member is AggregateMember.ValueMember vm) {
                    formBuilder.AddItem(
                        vm.Options.MemberType is Core.AggregateMemberTypes.Sentence,
                        member.MemberName,
                        E_VForm2LabelType.String,
                        vm.Options.MemberType.RenderSingleViewVFormBody(vm, formContext));

                } else if (member is AggregateMember.RelationMember rel) {
                    var descendant = GetDescendantComponent(rel);
                    formBuilder.AddUnknownParts(descendant.RenderCaller(GetArgumentsAndLoopVar()));

                } else {
                    throw new NotImplementedException();
                }
            }

            return formBuilder;
        }

        /// <summary>
        /// このコンポーネントの呼び出し処理をレンダリングします。
        /// </summary>
        internal string RenderCaller(IEnumerable<string>? args = null) {
            var parameters = GetArguments()
                .Select((a, i) => $"{a}={{{args?.ElementAtOrDefault(i)}}} ");
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
        private static SingleViewAggregateComponent GetDescendantComponent(AggregateMember.RelationMember member) {
            if (member is AggregateMember.Parent parent) {
                return new ParentOrChildOrRefComponent(parent);

            } else if (member is AggregateMember.Ref @ref) {
                return new ParentOrChildOrRefComponent(@ref);

            } else if (member is AggregateMember.Child child) {
                return new ParentOrChildOrRefComponent(child);

            } else if (member is AggregateMember.VariationItem variation) {
                return new VariationItemComponent(variation);

            } else if (member is AggregateMember.Children children1 && !children1.ChildrenAggregate.CanDisplayAllMembersAs2DGrid()) {
                return new ChildrenFormComponent(children1);

            } else if (member is AggregateMember.Children children2 && children2.ChildrenAggregate.CanDisplayAllMembersAs2DGrid()) {
                return new ChildrenGridComponent(children2);

            } else {
                throw new NotImplementedException();
            }
        }


        /// <summary>
        /// 単純な1個の隣接要素のコンポーネント
        /// </summary>
        private class ParentOrChildOrRefComponent : SingleViewAggregateComponent {
            internal ParentOrChildOrRefComponent(AggregateMember.Parent parent) : base(parent.ParentAggregate) { }
            internal ParentOrChildOrRefComponent(AggregateMember.Child child) : base(child.ChildAggregate) { }
            internal ParentOrChildOrRefComponent(AggregateMember.Ref @ref) : base(@ref.RefTo) { }

            internal override string RenderDeclaring(CodeRenderingContext context, bool isReadOnly) {
                var args = GetArguments().ToArray();
                var vForm = BuildVerticalForm(context);

                return $$"""
                    const {{ComponentName}} = ({{{(args.Length == 0 ? " " : $" {args.Join(", ")} ")}}}: {
                    {{args.SelectTextTemplate(arg => $$"""
                      {{arg}}: number
                    """)}}
                    }) => {
                      const { register, registerEx, getValues, setValue, formState: { errors } } = Util.useFormContextEx<{{UseFormType}}>()

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
                var switchProp = _variation.Group.GetFullPathAsReactHookFormRegisterName(E_PathType.Value, args).Join(".");

                return $$"""
                    const {{ComponentName}} = ({{{(args.Length == 0 ? " " : $" {args.Join(", ")} ")}}}: {
                    {{args.SelectTextTemplate(arg => $$"""
                      {{arg}}: number
                    """)}}
                    }) => {
                      const { register, registerEx, getValues, setValue, formState: { errors }, control } = Util.useFormContextEx<{{UseFormType}}>()
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

            private string GetLoopVar() {
                var args = GetArguments().ToArray();
                return args.Length == 0 ? "x" : $"x{args.Length}";
            }

            protected override IEnumerable<string> GetArgumentsAndLoopVar() {
                foreach (var arg in GetArguments()) {
                    yield return arg;
                }
                yield return GetLoopVar();
            }

            internal override string RenderDeclaring(CodeRenderingContext context, bool isReadOnly) {
                var args = GetArguments().ToArray();
                var vForm = BuildVerticalForm(context);
                var loopVar = GetLoopVar();

                var registerNameArray = _aggregate
                    .GetFullPathAsReactHookFormRegisterName(E_PathType.Value, args)
                    .ToArray();
                var registerName = registerNameArray.Length > 0
                    ? $"`{registerNameArray.Join(".")}`"
                    : string.Empty;

                var creatable = !isReadOnly
                    && !_aggregate.IsOutOfEntryTree(); // 参照先の集約の子孫要素は追加削除不可
                var createNewItem = _aggregate.IsOutOfEntryTree()
                    ? string.Empty
                    : $"AggregateType.{new DataClassForDisplay(_aggregate).TsNewObjectFunction}()";

                return $$"""
                    const {{ComponentName}} = ({{{(args.Length == 0 ? " " : $" {args.Join(", ")} ")}}}: {
                    {{args.SelectTextTemplate(arg => $$"""
                      {{arg}}: number
                    """)}}
                    }) => {
                      const { register, registerEx, formState: { errors }, control } = Util.useFormContextEx<{{UseFormType}}>()
                      const { fields, append, remove } = useFieldArray({ control, name: {{registerName}} })
                    {{If(creatable, () => $$"""
                      const onCreate = useCallback(() => {
                        append({{createNewItem}})
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
                            <VForm2.LabelText>{{_aggregate.GetParent()?.RelationName}}</VForm2.LabelText>
                    {{If(creatable, () => $$"""
                            <Input.Button onClick={onCreate}>追加</Input.Button>
                    """)}}
                          </div>
                        )}>
                          {fields.map((item, {{loopVar}}) => (
                            <VForm2.Indent key={{{loopVar}}} label={(
                    {{If(creatable, () => $$"""
                              <div className="flex flex-col gap-1">
                                <VForm2.LabelText>{{{loopVar}}}</VForm2.LabelText>
                                <Input.IconButton underline icon={XMarkIcon} onClick={onRemove({{loopVar}})}>削除</Input.IconButton>
                              </div>
                    """).Else(() => $$"""
                              <VForm2.LabelText>{{{loopVar}}}</VForm2.LabelText>
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

            private string GetLoopVar() {
                var args = GetArguments().ToArray();
                return args.Length == 0 ? "x" : $"x{args.Length}";
            }
            protected override IEnumerable<string> GetArgumentsAndLoopVar() {
                foreach (var arg in GetArguments()) {
                    yield return arg;
                }
                yield return GetLoopVar();
            }

            internal override string RenderDeclaring(CodeRenderingContext context, bool isReadOnly) {
                Parts.WebClient.DataTable.DataTableBuilder tableBuilder;
                string rowType;
                string createNewItem;
                if (_aggregate.IsOutOfEntryTree()) {
                    var refEntry = _aggregate.GetRefEntryEdge().Terminal;
                    var displayData = new RefDisplayData(_aggregate, refEntry);
                    rowType = $"AggregateType.{displayData.TsTypeName}";
                    createNewItem = string.Empty;
                    tableBuilder = new Parts.WebClient.DataTable.DataTableBuilder(_aggregate, rowType, false);
                    tableBuilder.AddMembers(displayData);
                } else {
                    var displayData = new DataClassForDisplay(_aggregate);
                    rowType = $"AggregateType.{displayData.TsTypeName}";
                    createNewItem = $"AggregateType.{displayData.TsNewObjectFunction}()";
                    tableBuilder = new Parts.WebClient.DataTable.DataTableBuilder(_aggregate, rowType, !isReadOnly);
                    tableBuilder.AddMembers(displayData);
                }

                var args = GetArguments().ToArray();
                var vForm = BuildVerticalForm(context);
                var loopVar = GetLoopVar();

                var registerNameArray = _aggregate
                    .GetFullPathAsReactHookFormRegisterName(E_PathType.Value, args)
                    .ToArray();
                var registerName = registerNameArray.Length > 0
                    ? $"`{registerNameArray.Join(".")}`"
                    : string.Empty;

                var editable = !isReadOnly && !_aggregate.IsOutOfEntryTree();

                // 何らかの状態変更があったときに更新フラグを立てる集約
                var nearestHavingLifeCycleAggregate = _aggregate
                    .EnumerateAncestorsAndThis()
                    .Reverse()
                    .FirstOrDefault(agg => new DataClassForDisplay(agg).HasLifeCycle);
                string willBeChanged;
                if (nearestHavingLifeCycleAggregate == null) {
                    willBeChanged = $"/*エラー！更新フラグを立てるべきオブジェクトを特定できません。*/.{DataClassForDisplay.WILL_BE_CHANGED_TS}";
                } else if (nearestHavingLifeCycleAggregate.IsRoot()) {
                    willBeChanged = DataClassForDisplay.WILL_BE_CHANGED_TS;
                } else {
                    var path = nearestHavingLifeCycleAggregate.GetFullPathAsReactHookFormRegisterName(E_PathType.Value, GetArgumentsAndLoopVar());
                    willBeChanged = nearestHavingLifeCycleAggregate == _aggregate
                        ? $"{path.Join(".")}.${{rowIndex}}.{DataClassForDisplay.WILL_BE_CHANGED_TS}"
                        : $"{path.Join(".")}.{DataClassForDisplay.WILL_BE_CHANGED_TS}";
                }

                return $$"""
                    const {{ComponentName}} = ({{{(args.Length == 0 ? " " : $" {args.Join(", ")} ")}}}: {
                    {{args.SelectTextTemplate(arg => $$"""
                      {{arg}}: number
                    """)}}
                    }) => {
                      const { get } = Util.useHttpRequest()
                      const { register, registerEx, setValue, control } = Util.useFormContextEx<{{UseFormType}}>()
                      const { fields, append, remove, update } = useFieldArray({ control, name: {{registerName}} })
                      const dtRef = useRef<Layout.DataTableRef<{{rowType}}>>(null)

                    {{If(editable, () => $$"""
                      const onAdd = useCallback((e: React.MouseEvent) => {
                        append({{createNewItem}})
                        e.preventDefault()
                      }, [append])
                      const onRemove = useCallback((e: React.MouseEvent) => {
                        const selectedRowIndexes = dtRef.current?.getSelectedRows().map(({ rowIndex }) => rowIndex) ?? []
                        for (const index of selectedRowIndexes.sort((a, b) => b - a)) remove(index)
                        e.preventDefault()
                      }, [dtRef, remove])

                    """)}}
                      const options = useMemo<Layout.DataTableProps<{{rowType}}>>(() => ({
                    {{If(editable, () => $$"""
                        onChangeRow: (rowIndex, row) => {
                          setValue(`{{willBeChanged}}`, true)
                          update(rowIndex, row)
                        },
                    """)}}
                        columns: [
                          {{WithIndent(tableBuilder.RenderColumnDef(context), "      ")}}
                        ],
                      }), [get, update, setValue{{args.Select(a => $", {a}").Join("")}}])

                      return (
                        <VForm2.Item wide
                    {{If(editable, () => $$"""
                          label={(
                            <div className="flex items-center gap-2">
                              <VForm2.LabelText>{{_aggregate.GetParent()?.RelationName}}</VForm2.LabelText>
                              <Input.Button icon={PlusIcon} onClick={onAdd}>追加</Input.Button>
                              <Input.Button icon={XMarkIcon} onClick={onRemove}>削除</Input.Button>
                            </div>
                          )}
                    """).Else(() => $$"""
                          label="{{_aggregate.GetParent()?.RelationName}}"
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

            internal override IEnumerable<SingleViewAggregateComponent> EnumerateThisAndDescendants() {
                yield return this;

                // グリッドなので、このコンポーネントから呼ばれる他の子コンポーネントや参照先コンポーネントは無い
            }
        }
    }
}
