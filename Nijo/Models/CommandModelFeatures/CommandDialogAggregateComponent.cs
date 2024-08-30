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

namespace Nijo.Models.CommandModelFeatures {
    /// <summary>
    /// コマンド起動ダイアログの一部を構成する、集約1個と対応するReactコンポーネント。
    /// </summary>
    internal class CommandDialogAggregateComponent {

        internal CommandDialogAggregateComponent(GraphNode<Aggregate> aggregate) {
            _aggregate = aggregate;
            _depth = 0;
        }
        protected CommandDialogAggregateComponent(GraphNode<Aggregate> aggregate, int depth) {
            _aggregate = aggregate;
            _depth = depth;
        }

        /// <summary>
        /// ルート集約がステップ属性をもつ子集約のコンポーネントを呼び出すときに
        /// 現在表示中のステップ数と一致する子要素のみ表示させるための状態
        /// </summary>
        internal const string CURRENT_STEP = "currentStep";

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

        protected string UseFormType => $"Types.{new CommandParameter(_aggregate.GetEntry().As<Aggregate>()).TsTypeName}";

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
        internal virtual IEnumerable<CommandDialogAggregateComponent> EnumerateThisAndDescendantsRecursively() {
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
                var displayData = new CommandParameter(_aggregate);
                members = displayData
                    .GetOwnMembers()
                    .Select(m => m.MemberInfo)
                    .OfType<AggregateMember.RelationMember>();
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
            var formBuilder = GetComponentRoot(isReadOnly);
            var formContext = new FormUIRenderingContext {
                CodeRenderingContext = context,
                Register = "registerEx",
                GetReactHookFormFieldPath = vm => vm.GetFullPathAsCommandParameterRHFRegisterName(GetArgumentsAndLoopVar()),
                RenderReadOnlyStatement = vm => {
                    if (isReadOnly) {
                        return FormUIRenderingContext.READONLY;
                    } else if (vm.Owner.IsOutOfEntryTree() && !vm.IsKey) {
                        return FormUIRenderingContext.READONLY; // 参照先かつキーでない項目は読み取り専用
                    } else {
                        return string.Empty; // コマンドのツリー内部の項目や参照先のキーは常に編集可能
                    }
                },
                RenderErrorMessage = vm => {
                    var fullpath = vm.Declared.GetFullPathAsCommandParameterRHFRegisterName(GetArgumentsAndLoopVar());
                    var render = vm.DeclaringAggregate == _aggregate; // 参照先のValueMemberにはエラーメッセージが無い。エラーメッセージは参照先のValueMemberではなくRefにつく
                    return $$"""
                        {{If(render, () => $$"""
                        <Input.ErrorMessage name={`{{fullpath.Join(".")}}`} errors={errors} />
                        """)}}
                        """;
                },
            };
            foreach (var member in EnumerateRenderedMemberes().OrderBy(m => m.Order)) {
                if (member is AggregateMember.ValueMember vm) {
                    formBuilder.AddItem(
                        vm.Options.MemberType is Core.AggregateMemberTypes.Sentence,
                        member.MemberName,
                        E_VForm2LabelType.String,
                        vm.Options.MemberType.RenderSingleViewVFormBody(vm, formContext));

                } else if (member is AggregateMember.RelationMember rel) {
                    var descendant = GetDescendantComponent(rel);

                    // ルート集約がステップ属性をもつ子集約のコンポーネントを呼び出すときは
                    // 現在表示中のステップ数と一致する子要素のみ表示させる
                    var step = rel.MemberAggregate.Item.Options.Step;
                    var hidden = step == null
                        ? Enumerable.Empty<string>()
                        : [$"hidden={{{CURRENT_STEP} !== {step}}}"];

                    formBuilder.AddUnknownParts(descendant.RenderCaller(GetArgumentsAndLoopVar(), hidden));

                } else {
                    throw new NotImplementedException();
                }
            }

            return formBuilder;
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
                var displayData = new CommandParameter(_aggregate);
                return displayData
                    .GetOwnMembers()
                    .Select(m => m.MemberInfo)
                    .Where(m => m is not AggregateMember.ValueMember vm
                             || !vm.Options.InvisibleInGui);
            }
        }

        /// <summary>
        /// このコンポーネントの呼び出し処理をレンダリングします。
        /// </summary>
        internal string RenderCaller(IEnumerable<string>? args = null, IEnumerable<string>? additionalAttributes = null) {
            var parameters = GetArguments()
                .Select((a, i) => $"{a}={{{args?.ElementAtOrDefault(i)}}} ")
                .Concat(additionalAttributes?.Select(x => $"{x} ") ?? Enumerable.Empty<string>());
            return $$"""
                <{{ComponentName}} {{parameters.Join("")}}/>
                """;
        }

        internal virtual string RenderDeclaring(CodeRenderingContext context, bool isReadOnly) {
            // ルート要素のコンポーネントのレンダリング
            var vForm = BuildVerticalForm(context, isReadOnly);
            var maxDepth = EnumerateThisAndDescendantsRecursively().Max(component => component._depth);

            var existsStep = _aggregate
                .GetMembers()
                .Any(m => m is AggregateMember.RelationMember rm && rm.MemberAggregate.Item.Options.Step != null);
            var args = existsStep
                ? $$"""
                    { {{CURRENT_STEP}} }: {
                      {{CURRENT_STEP}}: number
                    }
                    """
                : string.Empty;

            return $$"""
                const {{ComponentName}} = ({{args}}) => {
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
        private CommandDialogAggregateComponent GetDescendantComponent(AggregateMember.RelationMember member) {
            if (member is AggregateMember.Parent parent) {
                return new ParentComponent(parent, _depth + 1);

            } else if (member is AggregateMember.Child child) {
                return new ChildComponent(child, _depth + 1);

            } else if (member is AggregateMember.VariationItem variation) {
                return new VariationItemComponent(variation, _depth + 1);

            } else if (member is AggregateMember.Ref @ref) {
                return new RefComponent(@ref, _depth + 1);

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
        private abstract class DescendantComponent : CommandDialogAggregateComponent {
            internal DescendantComponent(AggregateMember.RelationMember member, int depth) : base(member.MemberAggregate, depth) {
                _member = member;
            }

            private readonly AggregateMember.RelationMember _member;

            protected override VerticalFormBuilder GetComponentRoot(bool isReadOnly) {
                var fullpath = _member.GetFullPathAsCommandParameterRHFRegisterName(GetArgumentsAndLoopVar());
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
        private class ParentComponent : DescendantComponent {
            internal ParentComponent(AggregateMember.Parent parent, int depth) : base(parent, depth) { }

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
        /// 単純な1個の隣接要素のコンポーネント
        /// </summary>
        private class ChildComponent : DescendantComponent {
            internal ChildComponent(AggregateMember.Child child, int depth) : base(child, depth) { }

            private bool HasStep => _aggregate.Item.Options.Step != null;

            protected override VerticalFormBuilder GetComponentRoot(bool isReadOnly) {
                var attributes = HasStep
                    ? new[] { $"className={{(hidden ? 'hidden' : '')}}" }
                    : [];
                return new VerticalFormBuilder(_aggregate.Item.DisplayName, E_VForm2LabelType.String, attributes);
            }

            internal override string RenderDeclaring(CodeRenderingContext context, bool isReadOnly) {
                var vForm = BuildVerticalForm(context, isReadOnly);

                var args = GetArguments().ToDictionary(x => x, _ => "number");
                if (HasStep) args.Add("hidden", "boolean");

                return $$"""
                    const {{ComponentName}} = ({{{(args.Count == 0 ? " " : $" {args.Keys.Join(", ")} ")}}}: {
                    {{args.SelectTextTemplate(arg => $$"""
                      {{arg.Key}}: {{arg.Value}}
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
        /// <see cref="AggregateMember.Ref"/> のコンポーネント
        /// </summary>
        private class RefComponent : DescendantComponent {
            internal RefComponent(AggregateMember.Ref @ref, int depth) : base(@ref, depth) {
                _ref = @ref;
            }
            private readonly AggregateMember.Ref _ref;
            private const string OPEN = "openSearchDialog";

            internal override string RenderDeclaring(CodeRenderingContext context, bool isReadOnly) {
                var args = GetArguments().ToArray();
                var vForm = BuildVerticalForm(context, isReadOnly);
                var dialog = new SearchDialog(_ref.RefTo, _ref.RefTo);
                var fullpath = _ref.GetFullPathAsCommandParameterRHFRegisterName(GetArgumentsAndLoopVar());

                return $$"""
                    const {{ComponentName}} = ({{{(args.Length == 0 ? " " : $" {args.Join(", ")} ")}}}: {
                    {{args.SelectTextTemplate(arg => $$"""
                      {{arg}}: number
                    """)}}
                    }) => {
                      const { register, registerEx, getValues, setValue, formState: { errors } } = Util.useFormContextEx<{{UseFormType}}>()
                    {{If(!isReadOnly, () => $$"""
                      const {{OPEN}} = {{dialog.HookName}}()
                      const handleClickSearch = useEvent(() => {
                        {{OPEN}}({
                          onSelect: item => setValue(`{{fullpath.Join(".")}}`, item)
                        })
                      })
                    """)}}

                      return (
                        {{WithIndent(vForm.Render(context), "    ")}}
                      )
                    }
                    """;
            }

            protected override VerticalFormBuilder GetComponentRoot(bool isReadOnly) {
                var fullpath = _ref.GetFullPathAsCommandParameterRHFRegisterName(GetArgumentsAndLoopVar());
                var label = $$"""
                    <>
                      <div className="inline-flex items-center py-1 gap-2">
                        <VForm2.LabelText>{{_ref.MemberName}}</VForm2.LabelText>
                    {{If(!isReadOnly, () => $$"""
                        <Input.IconButton underline mini icon={Icon.MagnifyingGlassIcon} onClick={handleClickSearch}>検索</Input.IconButton>
                    """)}}
                      </div>
                      <Input.ErrorMessage name={`{{fullpath.Join(".")}}`} errors={errors} />
                    </>
                    """;
                return new VerticalFormBuilder(label, E_VForm2LabelType.JsxElement);
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
                var switchProp = _variation.Group.GetFullPathAsCommandParameterRHFRegisterName(args).Join(".");

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
                var fullpath = _children.GetFullPathAsCommandParameterRHFRegisterName(GetArgumentsAndLoopVar());
                var label = $$"""
                    <>
                      <div className="inline-flex gap-2 py-px justify-start items-center">
                        <VForm2.LabelText>{{{loopVar}}}</VForm2.LabelText>
                    {{If(creatable, () => $$"""
                        <Input.IconButton outline mini icon={Icon.XMarkIcon} onClick={onRemove({{loopVar}})}>削除</Input.IconButton>
                    """)}}
                      </div>
                      <Input.ErrorMessage name={`{{fullpath.Join(".")}}.${{{loopVar}}}`} errors={errors} />
                    </>
                    """;
                return new VerticalFormBuilder(label, E_VForm2LabelType.JsxElement, $"key={{{loopVar}}}");
            }

            internal override string RenderDeclaring(CodeRenderingContext context, bool isReadOnly) {
                var args = GetArguments().ToArray();
                var vForm = BuildVerticalForm(context, isReadOnly);
                var loopVar = GetLoopVar();

                var registerNameArray = _children
                    .GetFullPathAsCommandParameterRHFRegisterName(args)
                    .ToArray();

                var creatable = !isReadOnly
                    && !_aggregate.IsOutOfEntryTree(); // 参照先の集約の子孫要素は追加削除不可
                var createNewItem = _aggregate.IsOutOfEntryTree()
                    ? string.Empty
                    : $"Types.{new CommandParameter(_aggregate).TsNewObjectFunction}()";

                return $$"""
                    const {{ComponentName}} = ({{{(args.Length == 0 ? " " : $" {args.Join(", ")} ")}}}: {
                    {{args.SelectTextTemplate(arg => $$"""
                      {{arg}}: number
                    """)}}
                    }) => {
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

                      return (
                        <VForm2.Indent label={<>
                          <div className="inline-flex gap-2 py-px justify-start items-center">
                            <VForm2.LabelText>{{_aggregate.GetParent()?.RelationName}}</VForm2.LabelText>
                    {{If(creatable, () => $$"""
                            <Input.IconButton outline mini icon={Icon.PlusIcon} onClick={onCreate}>追加</Input.IconButton>
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
                    rowType = $"Types.{displayData.TsTypeName}";
                    createNewItem = string.Empty;
                    tableBuilder = new Parts.WebClient.DataTable.DataTableBuilder(_aggregate, rowType, false);
                    tableBuilder.AddMembers(displayData);
                } else {
                    var displayData = new CommandParameter(_aggregate);
                    rowType = $"Types.{displayData.TsTypeName}";
                    createNewItem = $"Types.{displayData.TsNewObjectFunction}()";
                    tableBuilder = new Parts.WebClient.DataTable.DataTableBuilder(_aggregate, rowType, !isReadOnly);
                    tableBuilder.AddMembers(displayData);
                }

                var args = GetArguments().ToArray();
                var registerNameArray = _children
                    .GetFullPathAsCommandParameterRHFRegisterName(args)
                    .ToArray();
                var editable = !isReadOnly && !_aggregate.IsOutOfEntryTree();

                return $$"""
                    const {{ComponentName}} = ({{{(args.Length == 0 ? " " : $" {args.Join(", ")} ")}}}: {
                    {{args.SelectTextTemplate(arg => $$"""
                      {{arg}}: number
                    """)}}
                    }) => {
                      const { get } = Util.useHttpRequest()
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
                      const options = useMemo<Layout.DataTableProps<{{rowType}}>>(() => ({
                    {{If(editable, () => $$"""
                        onChangeRow: update,
                    """)}}
                        columns: [
                          {{WithIndent(tableBuilder.RenderColumnDef(context), "      ")}}
                        ],
                      }), [get, update, setValue{{args.Select(a => $", {a}").Join("")}}])

                      return (
                        <VForm2.Item wideLabelValue
                          label={<>
                            <div className="flex items-center gap-2">
                              <VForm2.LabelText>{{_aggregate.GetParent()?.RelationName}}</VForm2.LabelText>
                    {{If(editable, () => $$"""
                              <Input.IconButton outline mini icon={Icon.PlusIcon} onClick={onAdd}>追加</Input.IconButton>
                              <Input.IconButton outline mini icon={Icon.XMarkIcon} onClick={onRemove}>削除</Input.IconButton>
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

            internal override IEnumerable<CommandDialogAggregateComponent> EnumerateThisAndDescendantsRecursively() {
                yield return this;

                // グリッドなので、このコンポーネントから呼ばれる他の子コンポーネントや参照先コンポーネントは無い
            }
        }
    }
}
