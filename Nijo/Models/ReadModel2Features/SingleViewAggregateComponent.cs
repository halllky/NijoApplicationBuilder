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

        internal string ComponentName {
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
        protected virtual VForm2 GetComponentRoot() {
            var maxDepth = _aggregate.Item.Options.FormDepth ?? EnumerateThisAndDescendantsRecursively().Max(component => component._depth);

            return new VForm2.RootNode(
                new VForm2.StringLabel(_aggregate.Item.DisplayName),
                maxDepth,
                _aggregate.Item.Options.EstimatedLabelWidth);
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
                members = displayData
                    .GetChildMembers()
                    .Select(x => x.MemberInfo);
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
        protected VForm2 BuildVerticalForm(CodeRenderingContext context) {
            var formBuilder = GetComponentRoot();
            var formContext = new FormUIRenderingContext {
                CodeRenderingContext = context,
                Register = "registerEx",
                GetReactHookFormFieldPath = vm => vm.GetFullPathAsReactHookFormRegisterName(E_PathType.Value, GetArgumentsAndLoopVar()),
                RenderReadOnlyStatement = vm => GetReadOnlyStatement(vm),
                RenderErrorMessage = vm => {
                    var fullpath = vm.Declared.GetFullPathAsReactHookFormRegisterName(E_PathType.Value, GetArgumentsAndLoopVar());
                    var render = vm.DeclaringAggregate == _aggregate; // 参照先のValueMemberにはエラーメッセージが無い。エラーメッセージは参照先のValueMemberではなくRefにつく
                    return $$"""
                        {{If(render, () => $$"""
                        <Input.FormItemMessage name={`{{fullpath.Join(".")}}`} errors={errors} />
                        """)}}
                        """;
                },
            };
            foreach (var member in EnumerateRenderedMemberes().OrderBy(m => m.Order)) {
                if (member is AggregateMember.ValueMember vm) {
                    var readOnlyStatement = GetReadOnlyStatement(vm);
                    var fullpath = formContext.GetReactHookFormFieldPath(vm);

                    VForm2.Label label = vm.IsRequired && vm.Owner.IsInEntryTree()
                        ? new VForm2.JSXElementLabel($$"""
                            <VForm2.LabelText>
                              {{member.DisplayName}}
                              <Input.RequiredChip />
                            </VForm2.LabelText>
                            """)
                        : new VForm2.StringLabel(member.DisplayName);

                    var body = vm.Options.MemberType.RenderSingleViewVFormBody(vm, formContext);

                    formBuilder.Append(new VForm2.ItemNode(label, vm.Options.WideInVForm ?? false, body));

                } else if (member is AggregateMember.Ref @ref) {
                    var refDisplayData = new RefDisplayData(@ref.RefTo, @ref.RefTo);
                    var valueFullpath = @ref.GetFullPathAsReactHookFormRegisterName(E_PathType.Value, GetArgumentsAndLoopVar());
                    var readonlyFullpath = @ref.GetFullPathAsReactHookFormRegisterName(E_PathType.ReadOnly, GetArgumentsAndLoopVar());

                    formBuilder.Append(new VForm2.UnknownNode($$"""
                        <Components.{{refDisplayData.UiComponentName}}
                          {...registerEx(`{{valueFullpath.Join(".")}}`)}
                          control={control}
                          displayName="{{@ref.DisplayName.Replace("\"", "&quot;")}}"
                          readOnly={Util.isReadOnlyField(`{{readonlyFullpath.Join(".")}}`, getValues)}
                          required={{{(@ref.Relation.IsRequired() ? "true" : "false")}}}
                        />
                        """, @ref.Relation.IsWide() ?? true));

                } else if (member is AggregateMember.RelationMember rel) {
                    var descendant = GetDescendantComponent(rel);
                    formBuilder.Append(new VForm2.UnknownNode(descendant.RenderCaller(GetArgumentsAndLoopVar()), true));

                } else {
                    throw new NotImplementedException();
                }
            }

            return formBuilder;
        }

        /// <summary>
        /// そのValueMemberの項目が読み取り専用か否かを指定するReactコンポーネントのプロパティ渡し部分のソースをレンダリングします。
        /// 常に読み取り専用なら "readOnly" , サーバーから返ってきた読み取り専用設定に依存する場合は "readOnly={getValues(`aaa.bbb.ccc`)}" のような文字列になります。
        /// </summary>
        private string GetReadOnlyStatement(AggregateMember.AggregateMemberBase member) {
            // エントリーのツリー内の項目の読み取り専用は実行時の画面表示用データに付随しているreadonlyオブジェクトの値に従う
            if (member.Owner.IsInEntryTree()) {
                var fullpath = member.GetFullPathAsReactHookFormRegisterName(E_PathType.ReadOnly, GetArgumentsAndLoopVar());
                return $"{FormUIRenderingContext.READONLY}={{Util.isReadOnlyField(`{fullpath.Join(".")}`, getValues)}}";
            }

            // 参照先の参照先の項目は常に読み取り専用
            var refEntry = member.Owner.GetRefEntryEdge();
            if (refEntry.Terminal != member.Owner) {
                return FormUIRenderingContext.READONLY;
            }

            // 参照先の項目であってもキーでないものは常に読み取り専用
            if (member is AggregateMember.ValueMember vm && !vm.IsKey
                || member is AggregateMember.Ref @ref && !@ref.Relation.IsPrimary()) {
                return FormUIRenderingContext.READONLY;
            }

            var fullpath2 = member.Owner
                .GetRefEntryEdge()
                .AsRefMember()
                .GetFullPathAsReactHookFormRegisterName(E_PathType.ReadOnly, GetArgumentsAndLoopVar());
            return $"{FormUIRenderingContext.READONLY}={{Util.isReadOnlyField(`{fullpath2.Join(".")}`, getValues)}}";
        }

        /// <summary>
        /// レンダリング対象メンバーを列挙
        /// </summary>
        private IEnumerable<AggregateMember.AggregateMemberBase> EnumerateRenderedMemberes() {
            if (_aggregate.IsOutOfEntryTree()) {
                var refEntry = _aggregate.GetRefEntryEdge().Terminal;
                var displayData = new RefDisplayData(_aggregate, refEntry);
                return displayData
                    .GetOwnMembers()
                    .Where(m => m is not AggregateMember.ValueMember vm
                             || !vm.Options.InvisibleInGui);
            } else {
                var displayData = new DataClassForDisplay(_aggregate);
                return displayData
                    .GetOwnMembers()
                    .Where(m => m is not AggregateMember.ValueMember vm
                             || !vm.Options.InvisibleInGui)
                    .Concat(displayData.GetChildMembers().Select(x => x.MemberInfo));
            }
        }

        /// <summary>
        /// このコンポーネントの呼び出し処理をレンダリングします。
        /// </summary>
        internal string RenderCaller(IEnumerable<string>? args = null) {
            var parameters = GetArguments()
                .Select((a, i) => $"{a}={{{args?.ElementAtOrDefault(i)}}} ");
            return $$"""
                <UI.{{ComponentName}} {{parameters.Join("")}}/>
                """;
        }

        internal virtual string RenderDeclaring(CodeRenderingContext context) {
            // ルート要素のコンポーネントのレンダリング
            var vForm = BuildVerticalForm(context);

            return $$"""
                export const {{ComponentName}} = () => {
                  const { mode } = useContext({{SingleView.PAGE_CONTEXT}})
                  const { register, registerEx, getValues, setValue, formState: { errors }, control } = Util.useFormContextEx<{{UseFormType}}>()
                  const { {{UiContextSectionName}}: UI, {{Parts.WebClient.DataTable.CellType.USE_HELPER}}, ...Components } = React.useContext({{UiContext.CONTEXT_NAME}})

                  return (
                    <div className="p-px">
                      <Input.FormItemMessage name="root" errors={errors} />
                      {{WithIndent(vForm.Render(context), "      ")}}
                    </div>
                  )
                }
                """;
        }

        /* ------------------------- UIコンテキスト用 ここから ------------------------------ */
        protected string UiContextSectionName => new SingleView(_aggregate.GetEntry().As<Aggregate>()).UiContextSectionName;
        internal string RenderUiContextType() {
            return $$"""
                /** 画面中の{{_aggregate.Item.DisplayName.Replace("*/", "")}}を表す部分 */
                {{ComponentName}}: (props: {
                {{GetArguments().SelectTextTemplate(arg => $$"""
                  {{arg}}: number
                """)}}
                }) => React.ReactNode
                """;
        }

        /* ------------------------- 子孫要素のコンポーネント ここから ------------------------------ */

        /// <summary>
        /// 子孫要素のコンポーネントをその種類に応じて返します。
        /// </summary>
        private SingleViewAggregateComponent GetDescendantComponent(AggregateMember.RelationMember member) {
            if (member is AggregateMember.Parent parent) {
                return new ParentOrChildComponent(parent, _depth + 1);

            } else if (member is AggregateMember.Child child) {
                return new ParentOrChildComponent(child, _depth + 1);

            } else if (member is AggregateMember.VariationItem variation) {
                return new VariationItemComponent(variation, _depth + 1);

            } else if (member is AggregateMember.Ref @ref) {
                throw new InvalidOperationException($"参照先のコンポーネントは{nameof(UiContext)}経由で呼ぶのでこの分岐に来るのはおかしい");

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

            protected override VForm2 GetComponentRoot() {
                var fullpath = _member.GetFullPathAsReactHookFormRegisterName(E_PathType.Value, GetArgumentsAndLoopVar());
                var sectionName = _member is AggregateMember.Parent parent
                    ? parent.ParentAggregate.Item.DisplayName
                    : _member.DisplayName;
                var label = new VForm2.JSXElementLabel($$"""
                    <>
                      <VForm2.LabelText>{{sectionName}}</VForm2.LabelText>
                      <Input.FormItemMessage name={`{{fullpath.Join(".")}}`} errors={errors} />
                    </>
                    """);
                return new VForm2.IndentNode(label);
            }
        }


        /// <summary>
        /// 単純な1個の隣接要素のコンポーネント
        /// </summary>
        private class ParentOrChildComponent : DescendantComponent {
            internal ParentOrChildComponent(AggregateMember.Parent parent, int depth) : base(parent, depth) { }
            internal ParentOrChildComponent(AggregateMember.Child child, int depth) : base(child, depth) { }

            internal override string RenderDeclaring(CodeRenderingContext context) {
                var args = GetArguments().ToArray();
                var vForm = BuildVerticalForm(context);

                return $$"""
                    export const {{ComponentName}} = ({{{(args.Length == 0 ? " " : $" {args.Join(", ")} ")}}}: {
                    {{args.SelectTextTemplate(arg => $$"""
                      {{arg}}: number
                    """)}}
                    }) => {
                      const { mode } = useContext({{SingleView.PAGE_CONTEXT}})
                      const { register, registerEx, getValues, setValue, formState: { errors } } = Util.useFormContextEx<{{UseFormType}}>()
                      const { {{UiContextSectionName}}: UI, {{Parts.WebClient.DataTable.CellType.USE_HELPER}}, ...Components } = React.useContext({{UiContext.CONTEXT_NAME}})

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

            internal override string RenderDeclaring(CodeRenderingContext context) {
                var args = GetArguments().ToArray();
                var vForm = BuildVerticalForm(context);
                var switchProp = _variation.Group.GetFullPathAsReactHookFormRegisterName(E_PathType.Value, args).Join(".");

                return $$"""
                    export const {{ComponentName}} = ({{{(args.Length == 0 ? " " : $" {args.Join(", ")} ")}}}: {
                    {{args.SelectTextTemplate(arg => $$"""
                      {{arg}}: number
                    """)}}
                    }) => {
                      const { mode } = useContext({{SingleView.PAGE_CONTEXT}})
                      const { register, registerEx, getValues, setValue, formState: { errors }, control } = Util.useFormContextEx<{{UseFormType}}>()
                      const switchProp = useWatch({ name: `{{switchProp}}`, control })
                      const { {{UiContextSectionName}}: UI, {{Parts.WebClient.DataTable.CellType.USE_HELPER}}, ...Components } = React.useContext({{UiContext.CONTEXT_NAME}})

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

            protected override VForm2 GetComponentRoot() {
                // ここでビルドされるフォームは配列全体ではなくループの中身の方
                var loopVar = GetLoopVar();
                var creatable = !_aggregate.IsOutOfEntryTree(); // 参照先の集約の子孫要素は追加削除不可
                var fullpath = _children.GetFullPathAsReactHookFormRegisterName(E_PathType.Value, GetArgumentsAndLoopVar());
                var label = new VForm2.JSXElementLabel($$"""
                    <>
                      <div className="inline-flex gap-2 py-px justify-start items-center">
                        <VForm2.LabelText>{{{loopVar}}}</VForm2.LabelText>
                    {{If(creatable, () => $$"""
                        {mode !== 'detail' && (
                          <Input.IconButton outline mini icon={Icon.XMarkIcon} onClick={onRemove({{loopVar}})}>削除</Input.IconButton>
                        )}
                    """)}}
                      </div>
                      <Input.FormItemMessage name={`{{fullpath.Join(".")}}.${{{loopVar}}}`} errors={errors} />
                    </>
                    """);
                return new VForm2.IndentNode(label, $"key={{{loopVar}}}");
            }

            internal override string RenderDeclaring(CodeRenderingContext context) {
                var args = GetArguments().ToArray();
                var vForm = BuildVerticalForm(context);
                var loopVar = GetLoopVar();

                var registerNameArray = _children
                    .GetFullPathAsReactHookFormRegisterName(E_PathType.Value, args)
                    .ToArray();

                var creatable = !_aggregate.IsOutOfEntryTree(); // 参照先の集約の子孫要素は追加削除不可
                var createNewItem = _aggregate.IsOutOfEntryTree()
                    ? string.Empty
                    : $"AggregateType.{new DataClassForDisplay(_aggregate).TsNewObjectFunction}()";

                return $$"""
                    export const {{ComponentName}} = ({{{(args.Length == 0 ? " " : $" {args.Join(", ")} ")}}}: {
                    {{args.SelectTextTemplate(arg => $$"""
                      {{arg}}: number
                    """)}}
                    }) => {
                      const { mode } = useContext({{SingleView.PAGE_CONTEXT}})
                      const { register, registerEx, getValues, formState: { errors }, control } = Util.useFormContextEx<{{UseFormType}}>()
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
                      const { {{UiContextSectionName}}: UI, {{Parts.WebClient.DataTable.CellType.USE_HELPER}}, ...Components } = React.useContext({{UiContext.CONTEXT_NAME}})

                      return (
                        <VForm2.Indent label={<>
                          <div className="inline-flex gap-2 py-px justify-start items-center">
                            <VForm2.LabelText>
                              {{_aggregate.GetParent()?.GetDisplayName() ?? _aggregate.GetParent()?.RelationName}}
                    {{If(_children.Relation.IsRequired() && _children.Owner.IsInEntryTree(), () => $$"""
                              <Input.RequiredChip />
                    """)}}
                            </VForm2.LabelText>
                    {{If(creatable, () => $$"""
                            {mode !== 'detail' && (
                              <Input.IconButton outline mini icon={Icon.PlusIcon} onClick={onCreate}>追加</Input.IconButton>
                            )}
                    """)}}
                          </div>
                          <Input.FormItemMessage name={`{{registerNameArray.Join(".")}}`} errors={errors} />
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

            internal override string RenderDeclaring(CodeRenderingContext context) {
                var displayData = new DataClassForDisplay(_aggregate);
                Parts.WebClient.DataTable.DataTableBuilder tableBuilder;
                string rowType;
                string createNewItem;
                if (_aggregate.IsOutOfEntryTree()) {
                    var refEntry = _aggregate.GetRefEntryEdge().Terminal;
                    var refDisplayData = new RefDisplayData(_aggregate, refEntry);
                    rowType = $"AggregateType.{refDisplayData.TsTypeName}";
                    createNewItem = string.Empty;
                    tableBuilder = Parts.WebClient.DataTable.DataTableBuilder.ReadOnlyGrid(_aggregate, rowType);
                    tableBuilder.AddMembers(refDisplayData);
                } else {
                    rowType = $"AggregateType.{displayData.TsTypeName}";
                    createNewItem = $"AggregateType.{displayData.TsNewObjectFunction}()";
                    tableBuilder = Parts.WebClient.DataTable.DataTableBuilder.EditableGrid(_aggregate, rowType, OnValueChange, ReadOnlyDynamic);
                    tableBuilder.AddMembers(displayData);

                    string OnValueChange(AggregateMember.AggregateMemberBase m) {
                        return $$"""
                            (row, value, rowIndex) => {
                            {{If(m.Owner != _aggregate, () => $$"""
                              if (row.{{m.GetFullPathAsDataClassForDisplay(E_CsTs.TypeScript, _aggregate).SkipLast(1).Join("?.")}} === undefined) return
                            """)}}
                              row.{{m.GetFullPathAsDataClassForDisplay(E_CsTs.TypeScript, _aggregate).Join(".")}} = value
                              update(rowIndex, row)
                            }
                            """;
                    }
                    string ReadOnlyDynamic(AggregateMember.AggregateMemberBase member) {
                        return $"(row, rowIndex) => Util.isReadOnlyField(`{member.GetFullPathAsReactHookFormRegisterName(E_PathType.ReadOnly, [.. GetArguments(), "rowIndex"]).Join(".")}`, getValues)";
                    }
                }

                var args = GetArguments().ToArray();
                var registerNameArray = _children
                    .GetFullPathAsReactHookFormRegisterName(E_PathType.Value, args)
                    .ToArray();
                var editable = !_aggregate.IsOutOfEntryTree();

                return $$"""
                    export const {{ComponentName}} = ({{{(args.Length == 0 ? " " : $" {args.Join(", ")} ")}}}: {
                    {{args.SelectTextTemplate(arg => $$"""
                      {{arg}}: number
                    """)}}
                    }) => {
                      const { complexPost } = Util.useHttpRequest()
                      const { mode } = useContext({{SingleView.PAGE_CONTEXT}})
                      const { register, registerEx, getValues, setValue, formState: { errors }, control } = Util.useFormContextEx<{{UseFormType}}>()
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
                      const { {{Parts.WebClient.DataTable.CellType.USE_HELPER}}, ...Components } = React.useContext({{UiContext.CONTEXT_NAME}})
                      const cellType = {{Parts.WebClient.DataTable.CellType.USE_HELPER}}<{{rowType}}>()
                      const columns = useMemo((): Layout.DataTableColumn<{{rowType}}>[] => [
                        {{WithIndent(tableBuilder.RenderColumnDef(context), "    ")}}
                      ], [mode, complexPost, update, setValue{{args.Select(a => $", {a}").Join("")}}, cellType])

                      return (
                        <VForm2.Item wideLabelValue
                          label={<>
                            <div className="flex items-center gap-2">
                              <VForm2.LabelText>{{_aggregate.GetParent()?.GetDisplayName() ?? _aggregate.GetParent()?.RelationName}}</VForm2.LabelText>
                    {{If(_children.Relation.IsRequired() && _children.Owner.IsInEntryTree(), () => $$"""
                              <Input.RequiredChip />
                    """)}}
                    {{If(editable, () => $$"""
                              {mode !== 'detail' && (<>
                                <Input.IconButton outline mini icon={Icon.PlusIcon} onClick={onAdd}>追加</Input.IconButton>
                                <Input.IconButton outline mini icon={Icon.XMarkIcon} onClick={onRemove}>削除</Input.IconButton>
                              </>)}
                    """)}}
                            </div>
                            <Input.FormItemMessage name={`{{registerNameArray.Join(".")}}`} errors={errors} />
                          </>}
                        >
                          <Layout.DataTable
                            ref={dtRef}
                            data={fields}
                            columns={columns}
                    {{If(editable, () => $$"""
                            onChangeRow={(mode === 'detail' ? undefined : update)} // undefinedの場合はセル編集不可
                    """)}}
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
