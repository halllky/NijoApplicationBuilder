using Nijo.Core;
using Nijo.Features.KeywordSearching;
using Nijo.Features.WebClient;
using Nijo.DotnetEx;
using static Nijo.Util.CodeGenerating.TemplateTextHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nijo.Util.CodeGenerating;

namespace Nijo.Features.InstanceHandling {

    internal class AggregateComponent {
        internal AggregateComponent(GraphNode<Aggregate> aggregate, SingleView.E_Type type) {
            if (!aggregate.IsRoot()) throw new ArgumentException("ルート集約でない場合はもう片方のコンストラクタを使用");

            _aggregate = aggregate;
            _relationToParent = null;
            _mode = type;
        }
        internal AggregateComponent(AggregateMember.RelationMember relationMember, SingleView.E_Type type) {
            _aggregate = relationMember.MemberAggregate;
            _relationToParent = relationMember;
            _mode = type;
        }

        private readonly GraphNode<Aggregate> _aggregate;
        private readonly AggregateMember.RelationMember? _relationToParent;
        private readonly SingleView.E_Type _mode;

        private string ComponentName => $"{_aggregate.Item.TypeScriptTypeName}View";

        private IReadOnlyList<string>? _arguments;
        private IReadOnlyList<string> Arguments {
            get {
                // 祖先コンポーネントの中に含まれるChildrenの数だけ、
                // このコンポーネントのその配列中でのインデックスが特定されている必要があるので、引数で渡す
                return _arguments ??= _aggregate
                    .PathFromEntry()
                    .Where(edge => edge.Terminal != _aggregate
                                && edge.Terminal.As<Aggregate>().IsChildrenMember())
                    .Select((_, i) => $"index_{i}")
                    .ToArray();
            }
        }

        private IEnumerable<AggregateMember.AggregateMemberBase>? _members;
        private IEnumerable<AggregateMember.AggregateMemberBase> Members => _members ??= new AggregateDetail(_aggregate).GetOwnMembers();

        internal string RenderCaller() {
            var args = Arguments
                .Select(arg => $" {arg}={{{arg}}}")
                .Join(string.Empty);
            return $"<{ComponentName}{args} />";
        }

        internal string Render() {
            if (_relationToParent == null || _relationToParent is AggregateMember.Child) {
                // ルート集約またはChildのレンダリング
                return $$"""
                    const {{ComponentName}} = ({ {{Arguments.Join(", ")}} }: {
                    {{Arguments.SelectTextTemplate(arg => $$"""
                      {{arg}}: number
                    """)}}
                    }) => {
                      const { register, registerEx, watch, getValues } = Input.useFormContextEx<AggregateType.{{_aggregate.GetRoot().Item.TypeScriptTypeName}}>()
                      const item = getValues({{GetRegisterName()}})

                      return (
                        <>
                          {{WithIndent(RenderMembers(), "      ")}}
                        </>
                      )
                    }
                    """;

            } else if (_relationToParent is AggregateMember.VariationItem variation) {
                // Variationメンバーのレンダリング
                var switchProp = GetRegisterName(_aggregate.GetParent()!.Initial, variation.Group);

                return $$"""
                    const {{ComponentName}} = ({ {{Arguments.Join(", ")}} }: {
                    {{Arguments.SelectTextTemplate(arg => $$"""
                      {{arg}}: number
                    """)}}
                    }) => {
                      const { registerEx, watch, getValues } = Input.useFormContextEx<AggregateType.{{_aggregate.GetRoot().Item.TypeScriptTypeName}}>()
                      const item = getValues({{GetRegisterName()}})

                      const body = (
                        <>
                          {{WithIndent(RenderMembers(), "      ")}}
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

            } else if (_aggregate.GetMembers().Any(m => m is AggregateMember.Child
                                                     || m is AggregateMember.Children
                                                     || m is AggregateMember.VariationItem)) {
                // Childrenのレンダリング（子集約をもつ場合）
                var loopVar = $"index_{Arguments.Count}";
                var createNewChildrenItem = new TSInitializerFunction(_aggregate).FunctionName;

                return $$"""
                    const {{ComponentName}} = ({ {{Arguments.Join(", ")}} }: {
                    {{Arguments.SelectTextTemplate(arg => $$"""
                      {{arg}}: number
                    """)}}
                    }) => {
                      const { registerEx, watch, control } = Input.useFormContextEx<AggregateType.{{_aggregate.GetRoot().Item.TypeScriptTypeName}}>()
                      const { fields, append, remove } = useFieldArray({
                        control,
                        name: {{GetRegisterName()}},
                      })
                      const onAdd = useCallback((e: React.MouseEvent) => {
                        append(AggregateType.{{createNewChildrenItem}}())
                        e.preventDefault()
                      }, [append])
                      const onCreate = useCallback(() => {
                        append(AggregateType.{{createNewChildrenItem}}())
                      }, [append])
                      const onRemove = useCallback((index: number) => {
                        return (e: React.MouseEvent) => {
                          remove(index)
                          e.preventDefault()
                        }
                      }, [remove])
                    
                      return (
                        <VForm.Section table label="{{_aggregate.GetParent()!.RelationName}}">
                          <VForm.Row fullWidth>
                            <Layout.TabGroup
                              items={fields}
                              keySelector={item => item.{{AggregateDetail.OBJECT_ID}} ?? ''}
                    {{If(_mode != SingleView.E_Type.View, () => $$"""
                              onCreate={onCreate}
                    """)}}
                            >
                              {({ item, index: {{loopVar}} }) => (
                                <VForm.Root>
                                  <VForm.Section table>
                                    {{WithIndent(RenderMembers(), "                ")}}

                    {{If(_mode != SingleView.E_Type.View, () => $$"""
                                    <VForm.Row fullWidth>
                                      <Input.IconButton
                                        underline
                                        icon={XMarkIcon}
                                        onClick={onRemove({{loopVar}})}
                                        className="absolute top-full right-0">
                                        削除
                                      </Input.IconButton>
                                    </VForm.Row>
                    """)}}
                                  </VForm.Section>
                                </VForm.Root>
                              )}
                            </Layout.TabGroup>
                          </VForm.Row>
                        </VForm.Section>
                      )
                    }
                    """;

            } else {
                // Childrenのレンダリング（子集約をもたない場合）
                var loopVar = $"index_{Arguments.Count}";
                var createNewChildrenItem = new TSInitializerFunction(_aggregate).FunctionName;
                var editable = _mode == SingleView.E_Type.View ? "false" : "true";
                var colDefs = Members.Select(m => m switch {
                    AggregateMember.ValueMember vm => new {
                        field = m.MemberName,
                        editable,
                        cellEditor = vm.Options.MemberType.GetGridCellEditorName(),
                        cellEditorParams = vm.Options.MemberType.GetGridCellEditorParams(),
                        valueFormatter = vm.Options.MemberType.GetGridCellValueFormatter(),
                    },
                    AggregateMember.Ref rm => new {
                        field = m.MemberName,
                        editable,
                        cellEditor = "Input." + new ComboBox(rm.MemberAggregate).ComponentName,
                        cellEditorParams = (IReadOnlyDictionary<string, string>)new Dictionary<string, string> {
                            { "raectHookFormId", $"(rowIndex: number) => `{GetRegisterName().Replace("`", "")}.${{rowIndex}}.{rm.MemberName}`" },
                        },
                        valueFormatter = $"({{ value }}) => ({new RefTargetKeyName(rm.MemberAggregate).GetNameMembers().Select(m => $"value?.{m.MemberName}").Join(" + ")}) || ''",
                    },
                    _ => throw new NotImplementedException(),
                });

                return $$"""
                    const {{ComponentName}} = ({ {{Arguments.Join(", ")}} }: {
                    {{Arguments.SelectTextTemplate(arg => $$"""
                      {{arg}}: number
                    """)}}
                    }) => {
                      const [{ darkMode }] = useAppContext()
                      const { registerEx, watch, control } = Input.useFormContextEx<AggregateType.{{_aggregate.GetRoot().Item.TypeScriptTypeName}}>()
                      const { fields, append, remove } = useFieldArray({
                        control,
                        name: {{GetRegisterName()}},
                      })

                      const gridApi = useRef<AgGridReact<typeof fields[0]> | null>(null)

                      const onAdd = useCallback((e: React.MouseEvent) => {
                        append(AggregateType.{{createNewChildrenItem}}())
                        e.preventDefault()
                      }, [append])
                      const onRemove = useCallback((e: React.MouseEvent) => {
                        const selectedRows = gridApi.current?.api.getSelectedRows() ?? []
                        for (const row of selectedRows) {
                          const index = fields.indexOf(row)
                          remove(index)
                        }
                        e.preventDefault()
                      }, [remove, fields])

                      const columnDefs = useMemo<ColDef<typeof fields[0]>[]>(() => [
                    {{colDefs.SelectTextTemplate(def => $$"""
                        {
                          field: '{{def.field}}',
                          cellDataType: false, // セル型の自動推論を無効にする
                          resizable: true,
                          sortable: false,
                          editable: {{def.editable}},
                          cellEditor: Input.generateCellEditor({{GetRegisterName()}}, {{def.cellEditor}}, {
                    {{def.cellEditorParams.SelectTextTemplate(p => $$"""
                            {{p.Key}}: {{p.Value}},
                    """)}}
                          }),
                          cellEditorPopup: true,
                    {{If(def.valueFormatter != string.Empty, () => $$"""
                          valueFormatter: {{def.valueFormatter}},
                    """)}}
                        },
                    """)}}
                      ], [])

                      return (
                        <VForm.Section
                          table
                          label={<>
                            {{_aggregate.GetParent()!.RelationName}}
                    {{If(_mode != SingleView.E_Type.View, () => $$"""
                            <Input.IconButton
                              underline
                              inline
                              icon={PlusIcon}
                              onClick={onAdd}>
                              追加
                            </Input.IconButton>
                            <Input.IconButton
                              underline
                              inline
                              icon={XMarkIcon}
                              onClick={onRemove}>
                              削除
                            </Input.IconButton>
                    """)}}
                          </>}
                        >
                          <VForm.Row fullWidth>
                            <Input.AgGridWrapper
                              ref={gridApi}
                              rowData={fields}
                              columnDefs={columnDefs}
                              className="h-64 w-full"
                            />
                          </VForm.Row>
                        </VForm.Section>
                      )
                    }
                    """;
            }
        }

        private string RenderMembers() {
            return Members.SelectTextTemplate(prop => prop switch {
                AggregateMember.Schalar x => RenderProperty(x),
                AggregateMember.Ref x => RenderProperty(x),
                AggregateMember.Child x => RenderProperty(x),
                AggregateMember.VariationItem x => string.Empty, // Variationの分岐の中でレンダリングされるので // RenderProperty(x),
                AggregateMember.Variation x => RenderProperty(x),
                AggregateMember.Children x => RenderProperty(x),
                _ => throw new NotImplementedException(),
            });
        }

        private string RenderProperty(AggregateMember.Children children) {
            var childrenComponent = new AggregateComponent(children, _mode);

            return $$"""
                {{If(children.Owner.IsRoot(), () => $$"""
                <VForm.Spacer />
                """)}}
                {{childrenComponent.RenderCaller()}}
                """;
        }

        private string RenderProperty(AggregateMember.Child child) {
            var childComponent = new AggregateComponent(child, _mode);

            return $$"""
                {{If(child.Owner.IsRoot(), () => $$"""
                <VForm.Spacer />
                """)}}
                <VForm.Section label="{{child.MemberName}}" table>
                  {{childComponent.RenderCaller()}}
                </VForm.Section>
                """;
        }

        private string RenderProperty(AggregateMember.VariationItem variation) {
            var childComponent = new AggregateComponent(variation, _mode);
            return $$"""
                {{WithIndent(childComponent.RenderCaller(), "")}}
                """;
        }

        private string RenderProperty(AggregateMember.Variation variationSwitch) {
            var switchProp = GetRegisterName(variationSwitch);
            var disabled = IfReadOnly("disabled", variationSwitch);

            return $$"""
                {{If(variationSwitch.Owner.IsRoot(), () => $$"""
                <VForm.Spacer />
                """)}}
                <VForm.Section
                  table
                  label={<>
                    {{variationSwitch.MemberName}}
                    <Input.SelectionEmitsKey
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
                {{variationSwitch.GetGroupItems().SelectTextTemplate(item => $$"""
                  {{WithIndent(RenderProperty(item), "  ")}}
                """)}}
                </VForm.Section>
                """;
        }

        private string RenderProperty(AggregateMember.Ref refProperty) {
            var combobox = new KeywordSearching.ComboBox(refProperty.MemberAggregate);
            var registerName = GetRegisterName(refProperty);
            var callCombobox = _mode switch {
                SingleView.E_Type.Create => combobox.RenderCaller(registerName, "className='w-full'"),
                SingleView.E_Type.View => combobox.RenderCaller(registerName, "className='w-full'", "readOnly"),
                SingleView.E_Type.Edit => combobox.RenderCaller(registerName, "className='w-full'", IfReadOnly("readOnly", refProperty)),
                _ => throw new NotImplementedException(),
            };

            return $$"""
                <VForm.Row label="{{refProperty.MemberName}}">
                  {{WithIndent(callCombobox, "  ")}}
                </VForm.Row>
                """;
        }

        #region SCHALAR PROPERTY
        private string RenderProperty(AggregateMember.Schalar schalar) {
            if (schalar.Options.InvisibleInGui) {
                return $$"""
                    <VForm.Row hidden>
                      <input type="hidden" {...register({{GetRegisterName(schalar)}})} />
                    </VForm.Row>
                    """;

            } else {
                var renderer = new ReactForm(this, schalar, _mode);
                return $$"""
                    <VForm.Row label="{{schalar.MemberName}}">
                      {{WithIndent(schalar.Options.MemberType.RenderUI(renderer), "  ")}}
                    </VForm.Row>
                    """;
            }
        }
        private class ReactForm : IGuiFormRenderer {
            internal ReactForm(AggregateComponent component, AggregateMember.Schalar prop, SingleView.E_Type mode) {
                _component = component;
                _prop = prop;
                _mode = mode;
            }
            private readonly AggregateComponent _component;
            private readonly AggregateMember.Schalar _prop;
            private readonly SingleView.E_Type _mode;

            /// <summary>
            /// Createビュー兼シングルビュー: テキストボックス
            /// </summary>
            public string TextBox(bool multiline = false) {
                var name = _component.GetRegisterName(_prop);
                var readOnly = _component.IfReadOnly("readOnly", _prop);

                if (multiline) {
                    return $$"""
                        <Input.Description {...registerEx({{name}})} className="{{INPUT_WIDTH}}" {{readOnly}} />
                        """;
                } else {
                    return $$"""
                        <Input.Word {...registerEx({{name}})} className="{{INPUT_WIDTH}}" {{readOnly}} />
                        """;
                }
            }
            public string Number() {
                var name = _component.GetRegisterName(_prop);
                var readOnly = _component.IfReadOnly("readOnly", _prop);
                return $$"""
                    <Input.Num {...registerEx({{name}})} className="{{INPUT_WIDTH}}" {{readOnly}} />
                    """;
            }
            public string DateTime(IGuiFormRenderer.E_DateType dateType) {
                var name = _component.GetRegisterName(_prop);
                var readOnly = _component.IfReadOnly("readOnly", _prop);
                var componentName = dateType switch {
                    IGuiFormRenderer.E_DateType.Year => "Input.Num",
                    IGuiFormRenderer.E_DateType.YearMonth => "Input.YearMonth",
                    _ => "Input.Date",
                };
                return $$"""
                    <{{componentName}} {...registerEx({{name}})} className="{{INPUT_WIDTH}}" {{readOnly}} />
                    """;
            }

            /// <summary>
            /// Createビュー兼シングルビュー: トグル
            /// </summary>
            public string Toggle() {
                // checked属性はregisterに含まれないので自力で渡す必要がある
                var registerName = _component.GetRegisterName(_prop);
                var readOnly = _component.IfReadOnly("readOnly", _prop);
                return $$"""
                    <Input.CheckBox {...registerEx({{registerName}})} {{readOnly}} />
                    """;
            }

            /// <summary>
            /// Createビュー兼シングルビュー: 選択肢（コード自動生成時に要素が確定しているもの）
            /// </summary>
            public string Selection(IEnumerable<KeyValuePair<string, string>> options) {
                var name = _component.GetRegisterName(_prop);
                return $$"""
                    <Input.SelectionEmitsKey
                      {...registerEx({{name}})}
                      options={[
                    {{options.SelectTextTemplate(option => $$"""
                        '{{option.Value}}' as const,
                    """)}}
                      ]}
                      keySelector={item => item}
                      textSelector={item => item}
                      {{_mode switch {
                        SingleView.E_Type.View => $"readOnly",
                        SingleView.E_Type.Edit => _prop.Options.IsKey
                            ? $"readOnly={{(item?.{AggregateDetail.IS_LOADED}}}"
                            : string.Empty,
                        _ => string.Empty,
                      }}}
                    />
                    """;
            }

            public string HiddenField() {
                var registerName = _component.GetRegisterName(_prop);
                return $$"""
                    <input type="hidden" {...registerEx({{registerName}})} />
                    """;
            }
        }
        #endregion SCHALAR PROPERTY


        private int TableIndent => _aggregate.EnumerateAncestors().Count();

        #region ラベル列の横幅
        internal const string INPUT_WIDTH = "w-80";
        internal static string GetPropNameFlexBasis(IEnumerable<string> propNames) {
            var maxCharWidth = propNames
                .Select(prop => prop.CalculateCharacterWidth())
                .DefaultIfEmpty()
                .Max();

            var a = (maxCharWidth + 1) / 2; // tailwindのbasisはrem基準（全角文字n文字）のため偶数にそろえる
            var b = a + 2; // ちょっと横幅に余裕をもたせるための +2
            var c = Math.Min(96, b * 4); // tailwindでは basis-96 が最大なので

            return $"basis-{c}";
        }
        #endregion ラベル列の横幅

        private string GetRegisterName(AggregateMember.AggregateMemberBase? prop = null) {
            return GetRegisterName(_aggregate, prop);
        }
        private static string GetRegisterName(GraphNode<Aggregate> aggregate, AggregateMember.AggregateMemberBase? prop = null) {
            var path = new List<string>();
            var i = 0;
            foreach (var edge in aggregate.PathFromEntry()) {
                path.Add(edge.RelationName);

                if (edge.Terminal.As<Aggregate>().IsChildrenMember()) {
                    if (edge.Terminal != aggregate) {
                        // 祖先の中にChildrenがあるので配列番号を加える
                        path.Add("${index_" + i.ToString() + "}");
                        i++;
                    } else if (edge.Terminal == aggregate && prop != null) {
                        // このコンポーネント自身がChildrenのとき
                        // - propがnull: useArrayFieldの登録名の作成なので配列番号を加えない
                        // - propがnullでない: mapの中のプロパティのレンダリングなので配列番号を加える
                        path.Add("${index_" + i.ToString() + "}");
                        i++;
                    }
                }
            }
            if (prop != null) {
                path.Add(prop.MemberName);
            }
            var name = path.Join(".");
            return string.IsNullOrEmpty(name) ? string.Empty : $"`{name}`";
        }

        private string IfReadOnly(string readOnly, AggregateMember.AggregateMemberBase prop) {
            return _mode switch {
                SingleView.E_Type.Create => "",
                SingleView.E_Type.View => readOnly,
                SingleView.E_Type.Edit
                    => prop is AggregateMember.ValueMember vm && vm.IsKey
                    || prop is AggregateMember.Ref @ref && @ref.Relation.IsPrimary()
                        ? $"{readOnly}={{item?.{AggregateDetail.IS_LOADED}}}"
                        : $"",
                _ => throw new NotImplementedException(),
            };
        }
    }
}
