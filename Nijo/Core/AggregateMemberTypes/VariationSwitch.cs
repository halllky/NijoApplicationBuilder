using Nijo.Models.ReadModel2Features;
using Nijo.Models.RefTo;
using Nijo.Models.WriteModel2Features;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    internal class VariationSwitch : IAggregateMemberType {
        public string GetUiDisplayName() => "VariationSwitch";
        public string GetHelpText() {
            return $"ある親集約がその子要素にバリエーションを持つとき、親集約側に定義される属性。" +
                $"そのデータがどのバリエーションなのかを表す整数属性。" +
                $"例えば「1:正規雇用」「2:非正規雇用」のようなバリエーションがあるとき、" +
                $"データベースの親集約のテーブルには'1'または'2'の数値が登録されますが、これのことです。";
        }

        internal VariationSwitch(VariationGroup<Aggregate> variationGroup) {
            _variationGroup = variationGroup;
        }
        private readonly VariationGroup<Aggregate> _variationGroup;

        private string CsEnumTypeName => _variationGroup.CsEnumType;
        public string GetCSharpTypeName() => CsEnumTypeName;

        public string GetTypeScriptTypeName() {
            return _variationGroup
                .VariationAggregates
                .Select(kv => $"'{kv.Value.RelationName}'")
                .Join(" | ");
        }

        public string UiConstraintType => "MemberConstraintBase";
        public virtual IEnumerable<string> RenderUiConstraintValue(AggregateMember.ValueMember vm) {
            yield break;
        }

        private string SearchConditionClass => $"{_variationGroup.GroupName}SearchCondition";
        private const string ANY_CHECKED = "AnyChecked";
        private const string ALL_CHECKED = "AllChecked";

        public string GetSearchConditionCSharpType(AggregateMember.ValueMember vm) {
            return SearchConditionClass;
        }
        public string GetSearchConditionTypeScriptType(AggregateMember.ValueMember vm) {
            return $"{{ {_variationGroup.VariationAggregates.Values.Select(edge => $"{edge.Terminal.Item.PhysicalName}?: boolean").Join(", ")} }}";
        }

        void IAggregateMemberType.GenerateCode(CodeRenderingContext context) {
            context.CoreLibrary.Enums.Add($$"""
                /// <summary>{{_variationGroup.GroupName}}の検索条件クラス</summary>
                public class {{SearchConditionClass}} {
                {{_variationGroup.VariationAggregates.Values.SelectTextTemplate(edge => $$"""
                    public bool {{edge.Terminal.Item.PhysicalName}} { get; set; }
                """)}}

                    /// <summary>いずれかの値が選択されているかを返します。</summary>
                    public bool {{ANY_CHECKED}}() {
                {{_variationGroup.VariationAggregates.Values.SelectTextTemplate(edge => $$"""
                        if ({{edge.Terminal.Item.PhysicalName}}) return true;
                """)}}
                        return false;
                    }
                    /// <summary>すべての値が選択されているかを返します。</summary>
                    public bool {{ALL_CHECKED}}() {
                {{_variationGroup.VariationAggregates.Values.SelectTextTemplate(edge => $$"""
                        if (!{{edge.Terminal.Item.PhysicalName}}) return false;
                """)}}
                        return true;
                    }
                }
                """);
        }

        string IAggregateMemberType.RenderFilteringStatement(AggregateMember.ValueMember member, string query, string searchCondition, E_SearchConditionObject searchConditionObject, E_SearchQueryObject searchQueryObject) {
            var pathFromSearchCondition = searchConditionObject == E_SearchConditionObject.SearchCondition
                ? member.Declared.GetFullPathAsSearchConditionFilter(E_CsTs.CSharp)
                : member.Declared.GetFullPathAsRefSearchConditionFilter(E_CsTs.CSharp);
            var fullpathNullable = $"{searchCondition}.{pathFromSearchCondition.Join("?.")}";
            var fullpathNotNull = $"{searchCondition}.{pathFromSearchCondition.Join(".")}";
            var enumType = GetCSharpTypeName();
            var whereFullpath = searchQueryObject == E_SearchQueryObject.SearchResult
                ? member.GetFullPathAsSearchResult(E_CsTs.CSharp, out var isArray)
                : member.GetFullPathAsDbEntity(E_CsTs.CSharp, out isArray);

            return $$"""
                if ({{fullpathNullable}} != null
                    && {{fullpathNotNull}}.{{ANY_CHECKED}}()
                    && !{{fullpathNotNull}}.{{ALL_CHECKED}}()) {
                    var array = new List<{{enumType}}?>();
                {{_variationGroup.VariationAggregates.Values.SelectTextTemplate(edge => $$"""
                    if ({{fullpathNotNull}}.{{edge.Terminal.Item.PhysicalName}}) array.Add({{enumType}}.{{edge.Terminal.Item.PhysicalName}});
                """)}}

                {{If(isArray, () => $$"""
                    {{query}} = {{query}}.Where(x => x.{{whereFullpath.SkipLast(1).Join(".")}}.Any(y => array.Contains(y.{{member.MemberName}})));
                """).Else(() => $$"""
                    {{query}} = {{query}}.Where(x => array.Contains(x.{{whereFullpath.Join(".")}}));
                """)}}
                }
                """;
        }

        string IAggregateMemberType.RenderSearchConditionVFormBody(AggregateMember.ValueMember vm, FormUIRenderingContext ctx) {
            var fullpath = ctx.GetReactHookFormFieldPath(vm.Declared).Join(".");
            return $$"""
                <div className="flex flex-wrap gap-x-2 gap-y-1">
                {{_variationGroup.VariationAggregates.SelectTextTemplate(kv => $$"""
                  <Input.CheckBox label="{{GetLabel(kv.Value)}}" {...{{ctx.Register}}(`{{fullpath}}.{{kv.Value.RelationName}}`)} />
                """)}}
                </div>
                """;

            string GetLabel(GraphEdge edge) {
                var displayName = edge.GetDisplayName() ?? edge.RelationName;
                return displayName.Replace("\"", "&quot;");
            }
        }

        string IAggregateMemberType.RenderSingleViewVFormBody(AggregateMember.ValueMember vm, FormUIRenderingContext ctx) {
            var fullpath = ctx.GetReactHookFormFieldPath(vm.Declared).Join(".");

            var attrs = new List<string> {
                $"options={{[{_variationGroup.VariationAggregates.Select(kv => $"'{kv.Value.RelationName}' as const").Join(", ")}]}}",
                $"textSelector={{item => item}} ",
                $"{ctx.RenderReadOnlyStatement(vm.Declared)}", // readonly
            };

            // ラジオボタンまたはコンボボックスどちらか決め打ちの場合
            if (vm.Options.IsCombo) {
                attrs.Add("combo");
            } else if (vm.Options.IsRadio) {
                attrs.Add("radio");
            }

            ctx.EditComponentAttributes?.Invoke(vm, attrs);

            return $$"""
                <Input.Selection {...{{ctx.Register}}(`{{fullpath}}`)} {{attrs.Join(" ")}}/>
                {{ctx.RenderErrorMessage(vm)}}
                """;
        }

        public string DataTableColumnDefHelperName => _variationGroup.GroupName;
        Parts.WebClient.DataTable.CellType.Helper IAggregateMemberType.RenderDataTableColumnDefHelper(CodeRenderingContext ctx) {
            var returnType = $"{Parts.WebClient.DataTable.CellType.RETURNS_ONE_COLUMN}<TRow, {GetTypeScriptTypeName()} | undefined>";
            var body = $$"""
                /** {{_variationGroup.GroupName}} */
                const {{_variationGroup.GroupName}}: {{returnType}} = (header, getValue, setValue, opt) => {
                  const editSetting: ColumnEditSetting<TRow, {{GetTypeScriptTypeName()}}> = {
                    type: 'combo',
                    readOnly: typeof opt?.readOnly === 'function'
                      ? opt.readOnly
                      : undefined,
                    onStartEditing: row => getValue(row),
                    onEndEditing: (row, value, rowIndex) => {
                      setValue(row, value, rowIndex)
                    },
                    onClipboardPaste: (row, value, rowIndex) => {
                      const trimmed = value.trim()
                {{_variationGroup.VariationAggregates.SelectTextTemplate((x, i) => $$"""
                      {{(i == 0 ? "if" : "} else if")}} (trimmed === '{{x.Value.RelationName}}') {
                        setValue(row, '{{x.Value.RelationName}}', rowIndex)
                """)}}
                      } else {
                        setValue(row, undefined, rowIndex)
                      }
                    },
                    comboProps: {
                      onFilter: async keyword => {
                        const array = [{{_variationGroup.VariationAggregates.Select(x => $"'{x.Value.RelationName}' as const").Join(", ")}}]
                        if (!keyword) return array // 絞り込みワード未指定の場合は全件表示
                        const normalized = Util.normalize(keyword)
                        return array.filter(opt => Util.normalize(opt).includes(normalized))
                      },
                      getOptionText: opt => opt,
                      getValueFromOption: opt => opt,
                      getValueText: value => value,
                    }
                  }
                  return {
                    ...opt,
                    id: `${opt?.headerGroupName}::${header}`,
                    header,
                    render: row => <PlainCell>{getValue(row)}</PlainCell>,
                    onClipboardCopy: row => getValue(row) ?? '',
                    editSetting: opt?.readOnly === true
                      ? undefined
                      : (editSetting as ColumnEditSetting<TRow, unknown>),
                  }
                }
                """;
            return new() {
                Body = body,
                FunctionName = DataTableColumnDefHelperName,
                ReturnType = returnType,
            };
        }
    }
}
