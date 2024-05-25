using Nijo.Core;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nijo.Util.CodeGenerating;
using Nijo.Parts.WebClient;

namespace Nijo.Features.Storing {
    /// <summary>
    /// <see cref="SingleView"/> を構成する部分となる、集約1つごとに生成されるReactコンポーネント
    /// </summary>
    internal class AggregateComponent {
        internal AggregateComponent(GraphNode<Aggregate> aggregate, SingleView.E_Type type, bool asSingleRefKeyAggregate) {
            if (!aggregate.IsRoot()) throw new ArgumentException("ルート集約でない場合はもう片方のコンストラクタを使用");

            _aggregate = aggregate;
            _relationToParent = null;
            _mode = type;
            _asSingleRefKeyAggregate = asSingleRefKeyAggregate;
        }
        internal AggregateComponent(AggregateMember.RelationMember relationMember, SingleView.E_Type type) {
            _aggregate = relationMember.MemberAggregate;
            _relationToParent = relationMember;
            _mode = type;
            _asSingleRefKeyAggregate = false;
        }

        private readonly GraphNode<Aggregate> _aggregate;
        private readonly AggregateMember.RelationMember? _relationToParent;
        private readonly SingleView.E_Type _mode;
        /// <summary>参照先の集約のSingleViewの下部に表示される集約かどうか</summary>
        private readonly bool _asSingleRefKeyAggregate;

        internal string RenderCaller() {
            var componentName = GetComponentName();
            var args = GetArguments()
                .Select(arg => $" {arg}={{{arg}}}")
                .Join(string.Empty);

            return $"<{componentName}{args} />";
        }

        internal string RenderDeclaration() {
            var dataClass = new DataClassForDisplay(_aggregate);
            var localRepos = new LocalRepository(_aggregate);
            var componentName = GetComponentName();
            var args = GetArguments().ToArray();

            // useFormの型。Refの参照元のコンポーネントのレンダリングの可能性があるためGetRootではなくGetEntry
            var entryDataClass = new DataClassForDisplay(_aggregate.GetEntry().As<Aggregate>());
            var useFormType = $"AggregateType.{entryDataClass.TsTypeName}";
            var registerNameArray = _aggregate.GetRHFRegisterName(args).ToArray();
            var registerName = registerNameArray.Length > 0 ? $"`{registerNameArray.Join(".")}`" : string.Empty;

            // この集約を参照する隣接集約 ※DataTableの列定義は当該箇所で定義している
            var relevantAggregatesCalling = _aggregate
                .GetReferedEdgesAsSingleKey()
                .SelectTextTemplate(edge => new AggregateComponent(edge.Initial, _mode, true).RenderCaller());

            if (_relationToParent == null && !_asSingleRefKeyAggregate) {
                // ルート集約のレンダリング（画面の中の主集約）
                return $$"""
                    const {{componentName}} = ({{{args.Join(", ")}} }: {
                    {{args.SelectTextTemplate(arg => $$"""
                      {{arg}}: number
                    """)}}
                    }) => {
                      const { register, registerEx, watch, getValues } = Util.useFormContextEx<{{useFormType}}>()
                      const item = getValues({{registerName}})

                      return (
                        <>
                          <VForm.Container leftColumnMinWidth="{{GetLeftColumnWidth()}}">
                            {{WithIndent(RenderMembers(), "        ")}}
                          </VForm.Container>
                          {{WithIndent(relevantAggregatesCalling, "      ")}}
                        </>
                      )
                    }
                    """;

            } else if (_relationToParent == null && _asSingleRefKeyAggregate) {
                // ルート集約のレンダリング（画面の中の主集約を参照する別のルート集約）

                // 参照先のitemKeyと対応するプロパティを初期化する
                var refTo = _aggregate.GetSingleRefKeyAggregate()!;
                string? RefKeyInitializer(AggregateMember.AggregateMemberBase member) {
                    if (member is AggregateMember.Ref r && r == refTo) {
                        var refToRegisterNameArray = refTo.RefTo.GetRHFRegisterName(args).ToArray();
                        var refToRegisterName = refToRegisterNameArray.Length > 0 ? $"`{refToRegisterNameArray.Join(".")}`" : string.Empty;

                        return $"getValues({refToRegisterName})?.{DataClassForDisplay.LOCAL_REPOS_ITEMKEY}";

                    } else {
                        return null;
                    }
                };

                return $$"""
                    const {{componentName}} = ({{{args.Join(", ")}} }: {
                    {{args.SelectTextTemplate(arg => $$"""
                      {{arg}}: number
                    """)}}
                    }) => {
                      const { register, registerEx, watch, getValues, setValue } = Util.useFormContextEx<{{useFormType}}>()
                      const item = watch({{registerName}})
                      const state = item ? Util.getLocalRepositoryState(item) : undefined

                      const handleCreate = useCallback(() => {
                        setValue({{registerName}}, {{WithIndent(dataClass.RenderNewObjectLiteral(RefKeyInitializer), "    ")}})
                      }, [getValues, setValue])
                      const handleDelete = useCallback(() => {
                        const current = getValues({{registerName}})
                        if (current) setValue({{registerName}}, { ...current, {{DataClassForDisplay.WILL_BE_DELETED}}: true })
                      }, [setValue, getValues])
                      const handleRedo = useCallback(() => {
                        const current = getValues({{registerName}})
                        if (current) setValue({{registerName}}, { ...current, {{DataClassForDisplay.WILL_BE_DELETED}}: false })
                      }, [setValue, getValues])

                      return (
                        <>
                          <VForm.Container
                            leftColumnMinWidth="{{GetLeftColumnWidth()}}"
                            label="{{_aggregate.Item.DisplayName}}"
                            labelSide={(state === '' || state === '+' || state === '*') && (
                              <Input.Button icon={XMarkIcon} onClick={handleDelete}>削除</Input.Button>
                            )}
                            className="pt-4"
                          >
                            {state === undefined && (
                              <VForm.Item wide>
                                <Input.Button icon={PlusIcon} onClick={handleCreate}>作成</Input.Button>
                              </VForm.Item>
                            )}
                            {state === '-' && (
                              <VForm.Item wide>
                                <Input.Button icon={ArrowUturnLeftIcon} onClick={handleRedo}>元に戻す</Input.Button>
                              </VForm.Item>
                            )}
                            {(state === '' || state === '+' || state === '*') && (
                              <>
                                {{WithIndent(RenderMembers(), "            ")}}
                              </>
                            )}
                          </VForm.Container>
                          {{WithIndent(relevantAggregatesCalling, "      ")}}
                        </>
                      )
                    }
                    """;

            } else if (_relationToParent is AggregateMember.Child) {
                // Childのレンダリング
                return $$"""
                    const {{componentName}} = ({{{args.Join(", ")}} }: {
                    {{args.SelectTextTemplate(arg => $$"""
                      {{arg}}: number
                    """)}}
                    }) => {
                      const { register, registerEx, watch, getValues } = Util.useFormContextEx<{{useFormType}}>()
                      const item = getValues({{registerName}})

                      return (
                        <>
                          {{WithIndent(RenderMembers(), "      ")}}
                          {{WithIndent(relevantAggregatesCalling, "      ")}}
                        </>
                      )
                    }
                    """;

            } else if (_relationToParent is AggregateMember.VariationItem variation) {
                // Variationメンバーのレンダリング
                var switchProp = $"`{variation.Group.GetRHFRegisterName(args).Join(".")}`";

                return $$"""
                    const {{componentName}} = ({{{args.Join(", ")}} }: {
                    {{args.SelectTextTemplate(arg => $$"""
                      {{arg}}: number
                    """)}}
                    }) => {
                      const { registerEx, watch, getValues } = Util.useFormContextEx<{{useFormType}}>()
                      const item = getValues({{registerName}})

                      const body = (
                        <>
                          {{WithIndent(RenderMembers(), "      ")}}
                          {{WithIndent(relevantAggregatesCalling, "      ")}}
                        </>
                      )

                      return watch({{switchProp}}) === '{{variation.Key}}'
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

            } else if (!_aggregate.CanDisplayAllMembersAs2DGrid()) {
                // Childrenのレンダリング（子集約をもつなど表であらわせない場合）
                var loopVar = GetLoopVarName();

                return $$"""
                    const {{componentName}} = ({{{args.Join(", ")}} }: {
                    {{args.SelectTextTemplate(arg => $$"""
                      {{arg}}: number
                    """)}}
                    }) => {
                      const { registerEx, watch, control } = Util.useFormContextEx<{{useFormType}}>()
                      const { fields, append, remove } = useFieldArray({
                        control,
                        name: {{registerName}},
                      })
                    {{If(_mode != SingleView.E_Type.View, () => $$"""
                      const onCreate = useCallback(() => {
                        append({{WithIndent(dataClass.RenderNewObjectLiteral(), "    ")}})
                      }, [append])
                      const onRemove = useCallback((index: number) => {
                        return (e: React.MouseEvent) => {
                          remove(index)
                          e.preventDefault()
                        }
                      }, [remove])
                    """)}}

                      return (
                        <VForm.Container labelSide={(
                          <div className="flex gap-2 justify-start">
                            <h1 className="text-base font-semibold select-none py-1">
                              {{_aggregate.GetParent()?.RelationName}}
                            </h1>
                    {{If(_mode != SingleView.E_Type.View, () => $$"""
                            <Input.Button onClick={onCreate}>追加</Input.Button>
                    """)}}
                            <div className="flex-1"></div>
                          </div>
                        )}>
                          {fields.map((item, {{loopVar}}) => (
                    {{If(_mode == SingleView.E_Type.View, () => $$"""
                            <VForm.Container key={{{loopVar}}}>
                    """).Else(() => $$"""
                            <VForm.Container key={{{loopVar}}} labelSide={(
                              <Input.IconButton
                                underline
                                icon={XMarkIcon}
                                onClick={onRemove({{loopVar}})}>
                                削除
                              </Input.IconButton>
                            )}>
                    """)}}
                              {{WithIndent(RenderMembers(), "          ")}}
                            </VForm.Container>
                          ))}
                          {{WithIndent(relevantAggregatesCalling, "      ")}}
                        </VForm.Container>
                      )
                    }
                    """;

            } else {
                // Childrenのレンダリング（子集約をもたない場合）
                var loopVar = GetLoopVarName();
                var editable = _mode == SingleView.E_Type.View ? "false" : "true";

                var colDefs = DataTableColumn.FromMembers(
                        "item",
                        _aggregate,
                        _mode == SingleView.E_Type.View,
                        useFormContextType: $"AggregateType.{new DataClassForDisplay(_aggregate.GetEntry().As<Aggregate>()).TsTypeName}",
                        registerPathModifier: null,
                        arrayIndexVarNamesFromFormRootToDataTableOwner: args);

                return $$"""
                    const {{componentName}} = ({{{args.Join(", ")}} }: {
                    {{args.SelectTextTemplate(arg => $$"""
                      {{arg}}: number
                    """)}}
                    }) => {
                      const { registerEx, watch, control } = Util.useFormContextEx<{{useFormType}}>()
                      const { fields, append, remove, update } = useFieldArray({
                        control,
                        name: {{registerName}},
                      })
                      const dtRef = useRef<Layout.DataTableRef<AggregateType.{{dataClass.TsTypeName}}>>(null)

                    {{If(_mode != SingleView.E_Type.View, () => $$"""
                      const onAdd = useCallback((e: React.MouseEvent) => {
                        append({{WithIndent(dataClass.RenderNewObjectLiteral(), "    ")}})
                        e.preventDefault()
                      }, [append])
                      const onRemove = useCallback((e: React.MouseEvent) => {
                        const selectedRowIndexes = dtRef.current?.getSelectedIndexes() ?? []
                        for (const index of selectedRowIndexes.sort((a, b) => b - a)) remove(index)
                        e.preventDefault()
                      }, [remove])
                    """)}}

                      const options = useMemo<Layout.DataTableProps<AggregateType.{{dataClass.TsTypeName}}>>(() => ({
                    {{If(_mode != SingleView.E_Type.View, () => $$"""
                        onChangeRow: update,
                    """)}}
                        columns: [
                          {{WithIndent(colDefs.SelectTextTemplate(def => def.Render()), "      ")}}
                        ],
                      }), [{{args.Select(a => $"{a}, ").Join("")}}update])

                      return (
                        <VForm.Item wide
                          label="{{_aggregate.GetParent()?.RelationName}}"
                    {{If(_mode != SingleView.E_Type.View, () => $$"""
                          labelSide={<>
                            <Input.Button
                              icon={PlusIcon}
                              onClick={onAdd}>
                              追加
                            </Input.Button>
                            <Input.Button
                              icon={XMarkIcon}
                              onClick={onRemove}>
                              削除
                            </Input.Button>
                          </>}
                    """)}}
                          >
                          <Layout.DataTable
                            ref={dtRef}
                            data={fields}
                            {...options}
                            className="h-64 w-full"
                          />
                        </VForm.Item>
                      )
                    }
                    """;
            }
        }

        private IEnumerable<string> RenderMembers() {
            foreach (var member in GetMembers()) {

                if (member is AggregateMember.Schalar schalar) {
                    if (schalar.Options.InvisibleInGui) {
                        yield return $$"""
                            <input type="hidden" {...register(`{{schalar.GetRHFRegisterName(GetArguments().Concat([GetLoopVarName()])).Join(".")}}`)} />
                            """;

                    } else {
                        var reactComponent = schalar.Options.MemberType.GetReactComponent(new GetReactComponentArgs {
                            Type = GetReactComponentArgs.E_Type.InDetailView,
                        });

                        // read only
                        if (_mode == SingleView.E_Type.View) {
                            reactComponent.Props.Add("readOnly", string.Empty);

                        } else if (_mode == SingleView.E_Type.Edit
                                   && schalar is AggregateMember.ValueMember vm
                                   && vm.IsKey) {
                            if (_aggregate.IsRoot() || _aggregate.IsChildrenMember()) {
                                reactComponent.Props.Add("readOnly", $"item?.{DataClassForDisplay.EXISTS_IN_REMOTE_REPOS}");
                            }
                        }

                        yield return $$"""
                            <VForm.Item label="{{schalar.MemberName}}">
                              <{{reactComponent.Name}} {...registerEx(`{{schalar.GetRHFRegisterName(GetArguments().Concat([GetLoopVarName()])).Join(".")}}`)}{{string.Concat(reactComponent.GetPropsStatement())}} />
                            </VForm.Item>
                            """;
                    }

                } else if (member is AggregateMember.Ref refProperty) {
                    if (_aggregate != _aggregate.GetEntry().As<Aggregate>()) {
                        // このコンポーネントが参照先集約のSingleViewの一部としてレンダリングされている場合、
                        // キーがどの参照先データかは自明のため、非表示にする。
                        yield return $$"""
                            <input type="hidden" {...register(`{{refProperty.GetRHFRegisterName(GetArguments().Concat([GetLoopVarName()])).Join(".")}}`)} />
                            """;

                    } else if (_mode == SingleView.E_Type.View) {
                        // リンク
                        var navigation = new NavigationWrapper(refProperty.RefTo);

                        yield return $$"""
                             <VForm.Item label="{{refProperty.MemberName}}">
                               <Link className="text-link" to={Util.{{navigation.GetSingleViewUrlHookName}}(getValues('{{refProperty.RefTo.GetFullPathAsSingleViewDataClass().Join(".")}}'), 'view')}>
                                 {getValues('{{refProperty.RefTo.GetFullPathAsSingleViewDataClass().Join(".")}}')}
                               </Link>
                             </VForm.Item>
                             """;

                    } else {
                        // コンボボックス
                        var registerName = $"`{refProperty.GetRHFRegisterName(GetArguments().Concat([GetLoopVarName()])).Join(".")}`";
                        var combobox = new ComboBox(refProperty.RefTo);
                        var component = _mode switch {
                            SingleView.E_Type.Create => combobox.RenderCaller(registerName, "className='w-full'"),
                            SingleView.E_Type.Edit => combobox.RenderCaller(registerName, "className='w-full'", IfReadOnly("readOnly", refProperty)),
                            _ => throw new NotImplementedException(),
                        };
                        yield return $$"""
                            <VForm.Item label="{{refProperty.MemberName}}">
                              {{WithIndent(component, "  ")}}
                            </VForm.Item>
                            """;
                    }

                } else if (member is AggregateMember.Children children) {
                    yield return $$"""
                        {{new AggregateComponent(children, _mode).RenderCaller()}}
                        """;

                } else if (member is AggregateMember.Child child) {
                    yield return $$"""
                        <VForm.Container label="{{child.MemberName}}">
                          {{new AggregateComponent(child, _mode).RenderCaller()}}
                        </VForm.Container>
                        """;

                } else if (member is AggregateMember.Variation variationSwitch) {
                    var switchProp = $"`{variationSwitch.GetRHFRegisterName(GetArguments().Concat([GetLoopVarName()])).Join(".")}`";
                    var disabled = IfReadOnly("disabled", variationSwitch);

                    yield return $$"""
                        <VForm.Container
                          labelSide={<>
                            {{variationSwitch.MemberName}}
                            <Input.Selection
                              {...registerEx({{switchProp}})}
                              options={[
                        {{variationSwitch.GetGroupItems().SelectTextTemplate(variation => $$"""
                                { value: '{{variation.Key}}', text: '{{variation.MemberName}}' },
                        """)}}
                              ]}
                              keySelector={item => item.value}
                              textSelector={item => item.text}
                            />
                          </>}
                        >
                        {{variationSwitch.GetGroupItems().SelectTextTemplate(variation => $$"""
                          {{WithIndent(new AggregateComponent(variation, _mode).RenderCaller(), "  ")}}
                        """)}}
                        </VForm.Container>
                        """;
                }
            }
        }

        #region 部品
        private string GetComponentName() {
            var entry = _aggregate.GetEntry().As<Aggregate>();
            if (_aggregate.IsInTreeOf(entry)) {
                return $"{_aggregate.Item.TypeScriptTypeName}View";

            } else {
                var path = _aggregate
                    .PathFromEntry()
                    .Select(edge => edge.RelationName.ToCSharpSafe())
                    .Join("_");
                return $"{path}_{_aggregate.Item.TypeScriptTypeName}View";
            }
        }

        private IReadOnlyList<string> GetArguments() {
            // 祖先コンポーネントの中に含まれるChildrenの数だけ、
            // このコンポーネントのその配列中でのインデックスが特定されている必要があるので、引数で渡す
            return _aggregate
                .PathFromEntry()
                .Where(edge => edge.Terminal != _aggregate
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

        private IEnumerable<AggregateMember.AggregateMemberBase> GetMembers() {
            return new TransactionScopeDataClass(_aggregate).GetOwnMembers();
        }

        private string IfReadOnly(string readOnly, AggregateMember.AggregateMemberBase prop) {
            if (_mode == SingleView.E_Type.View) {
                return readOnly;
            }

            if (_mode == SingleView.E_Type.Edit) {
                if (prop is AggregateMember.ValueMember vm && vm.IsKey
                    || prop is AggregateMember.Ref @ref && @ref.Relation.IsPrimary()) {

                    if (_aggregate.IsRoot() || _aggregate.IsChildrenMember()) {
                        return $"{readOnly}={{item?.{DataClassForDisplay.EXISTS_IN_REMOTE_REPOS}}}";
                    }
                }
            }

            return "";
        }

        /// <summary>
        /// 左列の横幅
        /// </summary>
        private string GetLeftColumnWidth() {
            const decimal INDENT_WIDTH = 1.5m;

            var maxIndent = _aggregate
                .EnumerateDescendants()
                .Select(a => a.EnumerateAncestors().Count())
                .DefaultIfEmpty()
                .Max();

            var headersWidthRem = _aggregate
                .EnumerateThisAndDescendants()
                .SelectMany(
                    a => new TransactionScopeDataClass(a)
                        .GetOwnMembers()
                        .Where(m => {
                            // 同じ行に値を表示せず、名前が長くても行の横幅いっぱい占有できるため、除外
                            if (m is AggregateMember.Child) return false;
                            if (m is AggregateMember.Children) return false;
                            if (m is AggregateMember.Variation) return false;

                            // 画面上にメンバー名が表示されないため除外
                            if (m is AggregateMember.VariationItem) return false;
                            if (m is AggregateMember.ValueMember vm && vm.Options.InvisibleInGui) return false;

                            return true;
                        }),
                    (a, m) => new {
                        m.MemberName,
                        IndentWidth = a.EnumerateAncestors().Count() * INDENT_WIDTH, // インデント1個の幅をだいたい1.5remとして計算
                        NameWidthRem = m.MemberName.CalculateCharacterWidth() / 2 * 1.2m, // tailwindの1.2remがだいたい全角文字1文字分
                    });
            // インデント込みで最も横幅が長いメンバーの横幅を計算
            var longestHeaderWidthRem = headersWidthRem
                .Select(x => Math.Ceiling((x.IndentWidth + x.NameWidthRem) * 10m) / 10m)
                .DefaultIfEmpty()
                .Max();
            // - longestHeaderWidthRemにはインデントの横幅も含まれているのでインデントの横幅を引く
            // - ヘッダ列の横幅にちょっと余裕をもたせるために+8
            var indentWidth = maxIndent * INDENT_WIDTH;
            var headerWidth = Math.Max(indentWidth, longestHeaderWidthRem - indentWidth) + 8m;

            return $"{headerWidth}rem";
        }
        #endregion 部品
    }
}
