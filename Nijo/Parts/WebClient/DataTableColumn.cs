using Nijo.Core;
using Nijo.Features.Storing;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts.WebClient {
    /// <summary>
    /// Reactテンプレート側で宣言されているコンポーネント DataTable の列定義
    /// </summary>
    internal class DataTableColumn {

        /// <summary>
        /// 集約のメンバーを列挙して列定義を表すオブジェクトを返します。
        /// useFieldArray の update 関数を使用しているので、その関数を参照できる場所にレンダリングされる必要があります。
        /// </summary>
        /// <param name="dataTableRowTypeName">DataTableの行の型の名前</param>
        /// <param name="dataTableOwner">このDataTableにはこの集約のメンバーの列が表示されます。</param>
        /// <param name="readOnly">このDataTableが読み取り専用か否か</param>
        /// <param name="useFormContextType">useFormContextのジェネリック型</param>
        /// <param name="registerPathModifier">ReactHookFormの登録パスの編集関数</param>
        /// <param name="arrayIndexVarNamesFromFormRootToDataTableOwner">React Hook Forms の記法において、フォームのルートからdataTableOwnerまでのパスに含まれる配列インデックスを表す変数名</param>
        internal static IEnumerable<DataTableColumn> FromMembers(
            string dataTableRowTypeName,
            GraphNode<Aggregate> dataTableOwner,
            bool readOnly,
            string? useFormContextType = null,
            Func<string, string>? registerPathModifier = null,
            IReadOnlyList<string>? arrayIndexVarNamesFromFormRootToDataTableOwner = null) {

            // ----------------------------------------------------
            // AggregateMember列
            var colIndex = 0;
            DataTableColumn ToDataTableColumn(AggregateMember.AggregateMemberBase member) {
                var vm = member as AggregateMember.ValueMember;
                var refMember = member as AggregateMember.Ref;

                var memberPath = member.GetFullPathAsSingleViewDataClass(since: dataTableOwner);

                // 非編集時のセル表示文字列
                string? formatted = null;
                if (vm != null) {
                    var editSettings = vm.Options.MemberType.GetGridColumnEditSetting();
                    if (editSettings is ComboboxColumnSetting ccs) {
                        formatted = ccs.GetDisplayText?.Invoke("value", "formatted");
                    }

                } else if (refMember != null) {
                    var names = refMember.RefTo
                        .AsEntry()
                        .GetNames()
                        .OfType<AggregateMember.ValueMember>();
                    formatted = $$"""
                        const formatted = `{{names.Select(n => $"${{value?.{n.Declared.GetFullPath().Join("?.")} ?? ''}}").Join("")}}`
                        """;
                }

                var cell = $$"""
                    cellProps => {
                      const value = cellProps.row.original.{{memberPath.Join("?.")}}
                      {{If(formatted != null, () => WithIndent(formatted!, "  "))}}
                      return (
                        <span className="block w-full px-1 overflow-hidden whitespace-nowrap">
                          {{{(formatted == null ? "value" : "formatted")}}}
                          &nbsp; {/* <= すべての値が空の行がつぶれるのを防ぐ */}
                        </span>
                      )
                    }
                    """;

                var hidden = vm?.Options.InvisibleInGui == true
                    ? true
                    : (bool?)null;

                var headerGroupName = member.Owner == dataTableOwner
                    ? null
                    : member.Owner.Item.DisplayName;

                IGridColumnSetting? editSetting = null;
                if (vm != null) {
                    editSetting = vm.Options.MemberType.GetGridColumnEditSetting();

                } else if (refMember != null) {
                    var refInfo = new DataClassForDisplayRefTarget(refMember.RefTo);
                    var api = new KeywordSearchingFeature(refMember.RefTo);
                    var combo = new ComboBox(refMember.RefTo);
                    var keys = refMember.RefTo
                        .AsEntry()
                        .GetKeys()
                        .OfType<AggregateMember.ValueMember>();
                    var names = refMember.RefTo
                        .AsEntry()
                        .GetNames()
                        .OfType<AggregateMember.ValueMember>();

                    editSetting = new AsyncComboboxColumnSetting {
                        OptionItemTypeName = $"AggregateType.{refInfo.TsTypeName}",
                        QueryKey = combo.RenderReactQueryKeyString(),
                        Query = $$"""
                            async keyword => {
                              const response = await get<AggregateType.{{refInfo.TsTypeName}}[]>(`{{api.GetUri()}}`, { keyword })
                              if (!response.ok) return []
                              return response.data
                            }
                            """,
                        EmitValueSelector = $"item => item",
                        MatchingKeySelectorFromEmitValue = $"item => item.{DataClassForDisplayRefTarget.INSTANCE_KEY}",
                        MatchingKeySelectorFromOption = $"item => item.{DataClassForDisplayRefTarget.INSTANCE_KEY}",
                        TextSelector = $"item => `{names.Select(n => $"${{item.{n.Declared.GetFullPathAsDisplayRefTargetClass().Join("?.")} ?? ''}}").Join("")}`",
                        OnClipboardCopy = (value, formatted) => $$"""
                            const {{formatted}} = {{value}} ? JSON.stringify({{value}}) : ''
                            """,
                        OnClipboardPaste = (value, formatted) => $$"""
                            let {{formatted}}: AggregateType.{{refInfo.TsTypeName}} | undefined
                            if ({{value}}) {
                              try {
                                const obj: AggregateType.{{refInfo.TsTypeName}} = JSON.parse({{value}})
                                // 登録にはインスタンスキーが使われるのでキーの型だけは細かくチェックする
                                if (obj.{{DataClassForDisplayRefTarget.INSTANCE_KEY}} === undefined) throw new Error
                                const arrInstanceKey: [{{keys.Select(k => k.Options.MemberType.GetTypeScriptTypeName()).Join(", ")}}] = JSON.parse(obj.{{DataClassForDisplayRefTarget.INSTANCE_KEY}})
                                if (!Array.isArray(arrInstanceKey)) throw new Error
                            {{keys.SelectTextTemplate((k, i) => $$"""
                                if (typeof arrInstanceKey[{{i}}] !== '{{k.Options.MemberType.GetTypeScriptTypeName()}}') throw new Error
                            """)}}
                                {{formatted}} = obj
                              } catch {
                                {{formatted}} = undefined
                              }
                            } else {
                              {{formatted}} = undefined
                            }
                            """,
                    };
                }

                // GET VALUE // TODO: 値取得関数がいろいろあるので整理する
                var editSettingGetValueFromRow = editSetting?.GetValueFromRow == null ? $$"""
                    row => row.{{memberPath.Join("?.")}}
                    """ : $$"""
                    row => {
                      const value = row.{{memberPath.Join("?.")}}
                      {{WithIndent(editSetting.GetValueFromRow("value", "formatted"), "  ")}}
                      return formatted
                    }
                    """;

                // SET VALUE
                /// TODO: <see cref="IGridColumnSetting.SetValueToRow"/> と役割重複しているので整理する
                string editSettingSetValueToRow;
                if (member.DeclaringAggregate == dataTableOwner) {
                    editSettingSetValueToRow = $$"""
                        (row, value) => {
                        {{If(editSetting?.SetValueToRow == null, () => $$"""
                          row.{{memberPath.Join(".")}} = value
                        """).Else(() => $$"""
                          {{WithIndent(editSetting!.SetValueToRow!("value", "formatted"), "  ")}}
                          row.{{memberPath.Join(".")}} = formatted
                        """)}}
                        }
                        """;
                } else {
                    var ownerPath = member.Owner.GetFullPathAsSingleViewDataClass(since: dataTableOwner);
                    var rootAggPath = member.Owner.GetRoot().GetFullPathAsSingleViewDataClass(since: dataTableOwner);
                    editSettingSetValueToRow = $$"""
                        (row, value) => {
                          if (row.{{ownerPath.Join("?.")}}) {
                        {{If(editSetting?.SetValueToRow == null, () => $$"""
                            row.{{memberPath.Join(".")}} = value
                        """).Else(() => $$"""
                            {{WithIndent(editSetting!.SetValueToRow!("value", "formatted"), "    ")}}
                            row.{{memberPath.Join(".")}} = formatted
                        """)}}
                          }
                        }
                        """;
                }

                colIndex++;

                return new DataTableColumn {
                    DataTableRowTypeName = dataTableRowTypeName,
                    Id = $"col{colIndex}",
                    Header = member.MemberName,
                    Cell = cell,
                    AccessorFn = editSettingGetValueFromRow,
                    Hidden = hidden,
                    HeaderGroupName = headerGroupName,
                    EditSetting = editSetting,
                    EditSettingGetValueFromRow = editSettingGetValueFromRow,
                    EditSettingSetValueToRow = editSettingSetValueToRow,
                    MemberPath = memberPath,
                };
            }

            // ----------------------------------------------------
            // テーブル中の被参照集約の列のインスタンスを追加または削除するボタン
            DataTableColumn RefFromButtonColumn(DataClassForDisplay.RelationProp refFrom) {
                var tableArrayRegisterName = dataTableOwner.GetRHFRegisterName();
                var refFromDisplayData = new DataClassForDisplay(refFrom.MainAggregate);
                var value = refFrom.MainAggregate.Item.PhysicalName;

                // ページのルート集約から被参照集約までのパス
                var ownerPath = refFrom.MainAggregate.GetFullPathAsSingleViewDataClass(since: dataTableOwner);
                var arrayIndexes = new List<string>();
                if (arrayIndexVarNamesFromFormRootToDataTableOwner != null) arrayIndexes.AddRange(arrayIndexVarNamesFromFormRootToDataTableOwner);
                arrayIndexes.Add("row.index");
                var registerName = refFrom.MainAggregate.GetRHFRegisterName(arrayIndexes).Join(".");
                if (registerPathModifier != null) registerName = registerPathModifier(registerName);

                // 参照先のitemKeyと対応するプロパティを初期化する
                string? RefKeyInitializer(AggregateMember.AggregateMemberBase member) {
                    if (member is AggregateMember.Ref r && r.RefTo == dataTableOwner) {
                        return $$"""
                            {
                              {{DataClassForDisplayRefTarget.INSTANCE_KEY}}: row.original.{{DataClassForDisplay.LOCAL_REPOS_ITEMKEY}},
                            }
                            """;

                    } else {
                        return null;
                    }
                };

                return new DataTableColumn {
                    DataTableRowTypeName = dataTableRowTypeName,
                    Id = $"ref-from-{refFrom.PropName}",
                    Header = string.Empty,
                    HeaderGroupName = refFrom.MainAggregate.Item.DisplayName,
                    Cell = $$"""
                        ({ row }) => {

                          const create{{refFrom.MainAggregate.Item.PhysicalName}} = useCallback(() => {
                            if (row.original{{ownerPath.SkipLast(1).Select(x => $"?.{x}").Join("")}}) {
                              row.original.{{ownerPath.Join(".")}} = {{WithIndent(refFromDisplayData.RenderNewObjectLiteral(RefKeyInitializer), "      ")}}
                              update(row.index, { ...row.original })
                            }
                          }, [row.index])

                          const delete{{refFrom.MainAggregate.Item.PhysicalName}} = useCallback(() => {
                            if (row.original.{{ownerPath.Join("?.")}}) {
                              row.original.{{ownerPath.Join(".")}}.{{DataClassForDisplay.WILL_BE_DELETED}} = true
                              update(row.index, { ...row.original })
                            }
                          }, [row.index])

                          const {{value}} = row.original.{{ownerPath.Join("?.")}}

                          return <>
                            {({{value}} === undefined || {{value}}.{{DataClassForDisplay.WILL_BE_DELETED}}) && (
                              <Input.Button icon={PlusIcon} onClick={create{{refFrom.MainAggregate.Item.PhysicalName}}}>作成</Input.Button>
                            )}
                            {({{value}} !== undefined && !{{value}}.{{DataClassForDisplay.WILL_BE_DELETED}}) && (
                              <Input.Button icon={XMarkIcon} onClick={delete{{refFrom.MainAggregate.Item.PhysicalName}}}>削除</Input.Button>
                            )}
                          </>
                        }
                        """,
                };
            }

            // ----------------------------------------------------

            // グリッドに表示するメンバーを列挙
            IEnumerable<DataTableColumn> Collect(DataClassForDisplay dataClass) {
                foreach (var prop in dataClass.GetOwnProps()) {
                    if (prop.Member is AggregateMember.ValueMember vm) {
                        if (vm.DeclaringAggregate != dataClass.MainAggregate) continue;
                        if (vm.Options.InvisibleInGui) continue;
                        yield return ToDataTableColumn(prop.Member);

                    } else if (prop.Member is AggregateMember.Ref @ref) {
                        if (@ref.RefTo.IsSingleRefKeyOf(@ref.Owner)) continue;
                        yield return ToDataTableColumn(prop.Member);
                    }
                }

                foreach (var prop in dataClass.GetChildProps()) {

                    // ChildrenやVariationのメンバーを列挙していないのはグリッド上で表現できないため
                    if (prop.MemberInfo is AggregateMember.Children) continue;
                    if (prop.MemberInfo is AggregateMember.VariationItem) continue;

                    foreach (var reucusive in Collect(new DataClassForDisplay(prop.MainAggregate))) {
                        yield return reucusive;
                    }
                }

                foreach (var prop in dataClass.GetRefFromProps()) {
                    yield return RefFromButtonColumn(prop);
                    foreach (var recursive in Collect(new DataClassForDisplay(prop.MainAggregate))) {
                        yield return recursive;
                    }
                }
            }

            var root = new DataClassForDisplay(dataTableOwner);

            foreach (var column in Collect(root)) {
                yield return column;
            }
        }

        internal required string DataTableRowTypeName { get; init; }

        // react table のAPI
        internal required string Id { get; init; }
        internal required string Header { get; init; }
        internal required string Cell { get; init; }
        internal int? Size { get; init; }
        internal bool? EnableResizing { get; init; }
        internal string? AccessorFn { get; init; }

        // 独自定義
        internal bool? Hidden { get; init; }
        internal string? HeaderGroupName { get; init; }
        internal IGridColumnSetting? EditSetting { get; init; }
        internal string? EditSettingGetValueFromRow { get; init; }
        internal string? EditSettingSetValueToRow { get; init; }

        /// <summary>TODO: 要リファクタリング</summary>
        internal IEnumerable<string>? MemberPath { get; init; }

        internal string Render() {
            var textboxEditSetting = EditSetting as TextColumnSetting;
            var comboboxEditSetting = EditSetting as ComboboxColumnSetting;
            var asyncComboEditSetting = EditSetting as AsyncComboboxColumnSetting;

            return $$"""
                {
                  id: '{{Id}}',
                  header: '{{Header}}',
                  cell: {{WithIndent(Cell, "  ")}},
                {{If(Size != null, () => $$"""
                  size: {{Size}},
                """)}}
                {{If(EnableResizing != null, () => $$"""
                  enableResizing: {{(EnableResizing!.Value ? "true" : "false")}},
                """)}}
                {{If(AccessorFn != null, () => $$"""
                  accessorFn: {{WithIndent(AccessorFn!, "  ")}},
                """)}}
                {{If(Hidden != null, () => $$"""
                  hidden: {{(Hidden!.Value ? "true" : "false")}},
                """)}}
                {{If(HeaderGroupName != null, () => $$"""
                  headerGroupName: '{{HeaderGroupName}}',
                """)}}
                {{If(textboxEditSetting != null, () => $$"""
                  editSetting: {
                    type: 'text',
                {{If(EditSettingGetValueFromRow != null, () => $$"""
                    getTextValue: {{WithIndent(EditSettingGetValueFromRow!, "    ")}},
                """)}}
                {{If(EditSettingSetValueToRow != null, () => $$"""
                    setTextValue: {{WithIndent(EditSettingSetValueToRow!, "    ")}},
                """)}}
                  },
                """)}}
                {{If(comboboxEditSetting != null, () => $$"""
                  editSetting: (() => {
                    const comboSetting: Layout.ColumnEditSetting<{{DataTableRowTypeName}}, {{comboboxEditSetting!.OptionItemTypeName}}> = {
                      type: 'combo',
                {{If(EditSettingGetValueFromRow != null, () => $$"""
                      getValueFromRow: {{WithIndent(EditSettingGetValueFromRow!, "      ")}},
                """)}}
                {{If(EditSettingSetValueToRow != null, () => $$"""
                      setValueToRow: {{WithIndent(EditSettingSetValueToRow!, "      ")}},
                """)}}
                {{If(comboboxEditSetting.OnClipboardCopy != null, () => $$"""
                      onClipboardCopy: row => {
                        {{WithIndent(comboboxEditSetting.OnClipboardCopy!($"row.{MemberPath?.Join("?.")}", "formatted"), "        ")}}
                        return formatted
                      },
                """)}}
                {{If(comboboxEditSetting.OnClipboardPaste != null, () => $$"""
                      onClipboardPaste: (row, value) => {
                {{If(MemberPath != null && MemberPath.Count() >= 2, () => $$"""
                        if (row.{{MemberPath?.SkipLast(1).Join("?.")}} === undefined) return
                """)}}
                        {{WithIndent(comboboxEditSetting.OnClipboardPaste!("value", "formatted"), "        ")}}
                        row.{{MemberPath?.Join(".")}} = formatted
                      },
                """)}}
                      comboProps: {
                        options: {{comboboxEditSetting!.Options}},
                        emitValueSelector: {{comboboxEditSetting!.EmitValueSelector}},
                        matchingKeySelectorFromEmitValue: {{comboboxEditSetting!.MatchingKeySelectorFromEmitValue}},
                        matchingKeySelectorFromOption: {{comboboxEditSetting!.MatchingKeySelectorFromOption}},
                        textSelector: {{comboboxEditSetting!.TextSelector}},
                      },
                    }
                    return comboSetting as Layout.ColumnEditSetting<{{DataTableRowTypeName}}, unknown>
                  })(),
                """)}}
                {{If(asyncComboEditSetting != null, () => $$"""
                  editSetting: (() => {
                    const asyncComboSetting: Layout.ColumnEditSetting<{{DataTableRowTypeName}}, {{asyncComboEditSetting!.OptionItemTypeName}}> = {
                      type: 'async-combo',
                {{If(EditSettingGetValueFromRow != null, () => $$"""
                      getValueFromRow: {{WithIndent(EditSettingGetValueFromRow!, "      ")}},
                """)}}
                {{If(EditSettingSetValueToRow != null, () => $$"""
                      setValueToRow: {{WithIndent(EditSettingSetValueToRow!, "      ")}},
                """)}}
                {{If(asyncComboEditSetting.OnClipboardCopy != null, () => $$"""
                      onClipboardCopy: row => {
                        {{WithIndent(asyncComboEditSetting.OnClipboardCopy!($"row.{MemberPath?.Join("?.")}", "formatted"), "        ")}}
                        return formatted
                      },
                """)}}
                {{If(asyncComboEditSetting.OnClipboardPaste != null, () => $$"""
                      onClipboardPaste: (row, value) => {
                {{If(MemberPath != null && MemberPath.Count() >= 2, () => $$"""
                        if (row.{{MemberPath?.SkipLast(1).Join("?.")}} === undefined) return
                """)}}
                        {{WithIndent(asyncComboEditSetting.OnClipboardPaste!("value", "formatted"), "        ")}}
                        row.{{MemberPath?.Join(".")}} = formatted
                      },
                """)}}
                      comboProps: {
                        queryKey: {{asyncComboEditSetting!.QueryKey}},
                        query: {{WithIndent(asyncComboEditSetting!.Query, "        ")}},
                        emitValueSelector: {{asyncComboEditSetting!.EmitValueSelector}},
                        matchingKeySelectorFromEmitValue: {{asyncComboEditSetting!.MatchingKeySelectorFromEmitValue}},
                        matchingKeySelectorFromOption: {{asyncComboEditSetting!.MatchingKeySelectorFromOption}},
                        textSelector: {{asyncComboEditSetting!.TextSelector}},
                      },
                    }
                    return asyncComboSetting as Layout.ColumnEditSetting<{{DataTableRowTypeName}}, unknown>
                  })(),
                """)}}
                },
                """;
        }
    }
}
