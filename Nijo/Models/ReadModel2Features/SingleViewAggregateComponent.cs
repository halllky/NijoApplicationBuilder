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
            _depth = 0;
        }
        protected SingleViewAggregateComponent(GraphNode<Aggregate> aggregate, int depth) {
            _aggregate = aggregate;
            _depth = depth;
        }

        protected readonly GraphNode<Aggregate> _aggregate;
        private readonly int _depth;

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
        /// コンポーネントのルート要素
        /// </summary>
        protected virtual VerticalFormBuilder GetComponentRoot(bool isReadOnly) {
            return new VerticalFormBuilder();
        }

        /// <summary>
        /// このコンポーネント、参照先のコンポーネント、子孫コンポーネントを再帰的に列挙します。
        /// </summary>
        internal virtual IEnumerable<SingleViewAggregateComponent> EnumerateThisAndDescendantsRecursively() {
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
                .SelectMany(component => component.EnumerateThisAndDescendantsRecursively());
            foreach (var component in descendants) {
                yield return component;
            }
        }

        /// <summary>
        /// <see cref="VerticalFormBuilder"/> のインスタンスを組み立てます。
        /// </summary>
        protected VerticalFormBuilder BuildVerticalForm(CodeRenderingContext context, bool isReadOnly) {
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
            var formBuilder = GetComponentRoot(isReadOnly);
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
            var vForm = BuildVerticalForm(context, isReadOnly);
            var maxDepth = EnumerateThisAndDescendantsRecursively().Max(component => component._depth);

            return $$"""
                const {{ComponentName}} = () => {
                  const { register, registerEx, getValues, setValue, formState: { errors }, control } = Util.useFormContextEx<{{UseFormType}}>()

                  return (
                    <div className="p-px">
                      <Input.ErrorMessage name="root" errors={errors} />
                      {{WithIndent(vForm.RenderAsRoot(context, maxDepth), "      ")}}
                    </div>
                  )
                }
                """;
        }


        /* ------------------------- 子孫要素のコンポーネント ここから ------------------------------ */

        /// <summary>
        /// 子孫要素のコンポーネントをその種類に応じて返します。
        /// </summary>
        private SingleViewAggregateComponent GetDescendantComponent(AggregateMember.RelationMember member) {
            if (member is AggregateMember.Parent parent) {
                return new ParentOrChildOrRefComponent(parent, _depth + 1);

            } else if (member is AggregateMember.Ref @ref) {
                return new ParentOrChildOrRefComponent(@ref, _depth + 1);

            } else if (member is AggregateMember.Child child) {
                return new ParentOrChildOrRefComponent(child, _depth + 1);

            } else if (member is AggregateMember.VariationItem variation) {
                return new VariationItemComponent(variation, _depth + 1);

            } else if (member is AggregateMember.Children children1 && !children1.ChildrenAggregate.CanDisplayAllMembersAs2DGrid()) {
                return new ChildrenFormComponent(children1, _depth + 1);

            } else if (member is AggregateMember.Children children2 && children2.ChildrenAggregate.CanDisplayAllMembersAs2DGrid()) {
                return new ChildrenGridComponent(children2, _depth + 1);

            } else {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// 子孫コンポーネント
        /// </summary>
        private abstract class DescendantComponent : SingleViewAggregateComponent {
            internal DescendantComponent(AggregateMember.RelationMember member, int depth) : base(member.MemberAggregate, depth) {
                _member = member;
            }

            private readonly AggregateMember.RelationMember _member;

            protected override VerticalFormBuilder GetComponentRoot(bool isReadOnly) {
                var fullpath = _member.GetFullPathAsReactHookFormRegisterName(E_PathType.Value, GetArgumentsAndLoopVar());
                var sectionName = _member is AggregateMember.Parent parent
                    ? parent.ParentAggregate.Item.DisplayName
                    : _member.MemberName;
                var label = $$"""
                    <>
                      <VForm2.LabelText>{{sectionName}}</VForm2.LabelText>
                      <Input.ErrorMessage name={`{{fullpath.Join(".")}}`} errors={errors} />
                    </>
                    """;
                return new VerticalFormBuilder(label, E_VForm2LabelType.JsxElement);
            }
        }


        /// <summary>
        /// 単純な1個の隣接要素のコンポーネント
        /// </summary>
        private class ParentOrChildOrRefComponent : DescendantComponent {
            internal ParentOrChildOrRefComponent(AggregateMember.Parent parent, int depth) : base(parent, depth) { }
            internal ParentOrChildOrRefComponent(AggregateMember.Child child, int depth) : base(child, depth) { }
            internal ParentOrChildOrRefComponent(AggregateMember.Ref @ref, int depth) : base(@ref, depth) { }

            internal override string RenderDeclaring(CodeRenderingContext context, bool isReadOnly) {
                var args = GetArguments().ToArray();
                var vForm = BuildVerticalForm(context, isReadOnly);

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
        private class VariationItemComponent : DescendantComponent {
            internal VariationItemComponent(AggregateMember.VariationItem variation, int depth) : base(variation, depth) {
                _variation = variation;
            }

            private readonly AggregateMember.VariationItem _variation;

            internal override string RenderDeclaring(CodeRenderingContext context, bool isReadOnly) {
                var args = GetArguments().ToArray();
                var vForm = BuildVerticalForm(context, isReadOnly);
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
        /// <see cref="AggregateMember.Children"/> のコンポーネント（グリッド形式・フォーム形式共通）
        /// </summary>
        private abstract class ChildrenComponent : DescendantComponent {
            internal ChildrenComponent(AggregateMember.Children children, int depth) : base(children, depth) {
                _children = children;
            }
            protected readonly AggregateMember.Children _children;

            protected string GetLoopVar() {
                var args = GetArguments().ToArray();
                return args.Length == 0 ? "x" : $"x{args.Length}";
            }

            protected override IEnumerable<string> GetArgumentsAndLoopVar() {
                foreach (var arg in GetArguments()) {
                    yield return arg;
                }
                yield return GetLoopVar();
            }
        }


        /// <summary>
        /// <see cref="AggregateMember.Children"/> のコンポーネント（子配列をもつなど表であらわせない場合）
        /// </summary>
        private class ChildrenFormComponent : ChildrenComponent {
            internal ChildrenFormComponent(AggregateMember.Children children, int depth) : base(children, depth) { }

            protected override VerticalFormBuilder GetComponentRoot(bool isReadOnly) {
                // ここでビルドされるフォームは配列全体ではなくループの中身の方
                var loopVar = GetLoopVar();
                var creatable = !isReadOnly && !_aggregate.IsOutOfEntryTree(); // 参照先の集約の子孫要素は追加削除不可
                var fullpath = _children.GetFullPathAsReactHookFormRegisterName(E_PathType.Value, GetArgumentsAndLoopVar());
                var label = $$"""
                    <>
                      <div className="inline-flex gap-2 py-px justify-start items-center">
                        <VForm2.LabelText>{{{loopVar}}}</VForm2.LabelText>
                    {{If(creatable, () => $$"""
                        <Input.IconButton outline mini icon={XMarkIcon} onClick={onRemove({{loopVar}})}>削除</Input.IconButton>
                    """)}}
                      </div>
                      <Input.ErrorMessage name={`{{fullpath.Join(".")}}.${{{loopVar}}}`} errors={errors} />
                    </>
                    """;
                return new VerticalFormBuilder(label, E_VForm2LabelType.JsxElement, loopVar);
            }

            internal override string RenderDeclaring(CodeRenderingContext context, bool isReadOnly) {
                var args = GetArguments().ToArray();
                var vForm = BuildVerticalForm(context, isReadOnly);
                var loopVar = GetLoopVar();

                var registerNameArray = _children
                    .GetFullPathAsReactHookFormRegisterName(E_PathType.Value, args)
                    .ToArray();

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
                      const { fields, append, remove } = useFieldArray({ control, name: `{{registerNameArray.Join(".")}}` })
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
                        <VForm2.Indent label={<>
                          <div className="inline-flex gap-2 py-px justify-start items-center">
                            <VForm2.LabelText>{{_aggregate.GetParent()?.RelationName}}</VForm2.LabelText>
                    {{If(creatable, () => $$"""
                            <Input.IconButton outline mini icon={PlusIcon} onClick={onCreate}>追加</Input.IconButton>
                    """)}}
                          </div>
                          <Input.ErrorMessage name={`{{registerNameArray.Join(".")}}`} errors={errors} />
                        </>}>
                          {fields.map((item, {{loopVar}}) => (
                            {{WithIndent(vForm.Render(context), "        ")}}
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
        private class ChildrenGridComponent : ChildrenComponent {
            internal ChildrenGridComponent(AggregateMember.Children children, int depth) : base(children, depth) { }

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
                var registerNameArray = _children
                    .GetFullPathAsReactHookFormRegisterName(E_PathType.Value, args)
                    .ToArray();
                var editable = !isReadOnly && !_aggregate.IsOutOfEntryTree();

                // 何らかの状態変更があったときに更新フラグを立てる集約
                // （自身または祖先集約の中でライフサイクルをもっているもののうち直近の集約）
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
                    var asChildren = (AggregateMember.Children)nearestHavingLifeCycleAggregate.AsChildRelationMember();
                    var path = asChildren.GetFullPathAsReactHookFormRegisterName(E_PathType.Value, GetArgumentsAndLoopVar());
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
                      const { register, registerEx, setValue, formState: { errors }, control } = Util.useFormContextEx<{{UseFormType}}>()
                      const { fields, append, remove, update } = useFieldArray({ control, name: `{{registerNameArray.Join(".")}}` })
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
                          label={<>
                            <div className="flex items-center gap-2">
                              <VForm2.LabelText>{{_aggregate.GetParent()?.RelationName}}</VForm2.LabelText>
                    {{If(editable, () => $$"""
                              <Input.IconButton outline mini icon={PlusIcon} onClick={onAdd}>追加</Input.IconButton>
                              <Input.IconButton outline mini icon={XMarkIcon} onClick={onRemove}>削除</Input.IconButton>
                    """)}}
                            </div>
                            <Input.ErrorMessage name={`{{registerNameArray.Join(".")}}`} errors={errors} />
                          </>}
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

            internal override IEnumerable<SingleViewAggregateComponent> EnumerateThisAndDescendantsRecursively() {
                yield return this;

                // グリッドなので、このコンポーネントから呼ばれる他の子コンポーネントや参照先コンポーネントは無い
            }
        }
    }
}
