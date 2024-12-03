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
    internal class EnumList : IAggregateMemberType {
        public string GetUiDisplayName() => "列挙体";
        public string GetHelpText() => $"列挙体。";

        public EnumList(EnumDefinition definition) {
            Definition = definition;
        }
        public EnumDefinition Definition { get; }

        public string GetCSharpTypeName() => Definition.Name;
        public string GetTypeScriptTypeName() {
            return Definition.Items.Select(x => $"'{x.PhysicalName}'").Join(" | ");
        }

        private string SearchConditionEnum => $"{Definition.Name}SearchCondition";
        private const string ANY_CHECKED = "AnyChecked";

        public string GetSearchConditionCSharpType(AggregateMember.ValueMember vm) {
            return SearchConditionEnum;
        }
        public string GetSearchConditionTypeScriptType(AggregateMember.ValueMember vm) {
            return $"{{ {Definition.Items.Select(i => $"{i.PhysicalName}?: boolean").Join(", ")} }}";
        }

        void IAggregateMemberType.GenerateCode(CodeRenderingContext context) {
            context.CoreLibrary.Enums.Add($$"""
                /// <summary>{{Definition.Name}}の検索条件クラス</summary>
                public class {{SearchConditionEnum}} {
                {{Definition.Items.SelectTextTemplate(item => $$"""
                    public bool {{item.PhysicalName}} { get; set; }
                """)}}

                    /// <summary>いずれかの値が選択されているかを返します。</summary>
                    public bool {{ANY_CHECKED}}() {
                {{Definition.Items.SelectTextTemplate(item => $$"""
                        if ({{item.PhysicalName}}) return true;
                """)}}
                        return false;
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
            string paramType;
            string cast;
            if (string.IsNullOrWhiteSpace(member.Options.EnumSqlParamType)) {
                paramType = enumType;
                cast = string.Empty;
            } else {
                paramType = member.Options.EnumSqlParamType;
                cast = $"({member.Options.EnumSqlParamType}?)";
            }

            var whereFullpath = searchQueryObject == E_SearchQueryObject.SearchResult
                ? member.GetFullPathAsSearchResult(E_CsTs.CSharp, out var isArray)
                : member.GetFullPathAsDbEntity(E_CsTs.CSharp, out isArray);

            return $$"""
                if ({{fullpathNullable}} != null && {{fullpathNotNull}}.{{ANY_CHECKED}}()) {
                    var array = new List<{{paramType}}?>();
                {{Definition.Items.SelectTextTemplate(item => $$"""
                    if ({{fullpathNotNull}}.{{item.PhysicalName}}) array.Add({{cast}}{{enumType}}.{{item.PhysicalName}});
                """)}}

                {{If(isArray, () => $$"""
                    {{query}} = {{query}}.Where(x => x.{{whereFullpath.SkipLast(1).Join(".")}}.Any(y => array.Contains({{cast}}y.{{member.MemberName}})));
                """).Else(() => $$"""
                    {{query}} = {{query}}.Where(x => array.Contains({{cast}}x.{{whereFullpath.Join(".")}}));
                """)}}
                }
                """;
        }

        string IAggregateMemberType.RenderSearchConditionVFormBody(AggregateMember.ValueMember vm, FormUIRenderingContext ctx) {
            var fullpath = ctx.GetReactHookFormFieldPath(vm.Declared).Join(".");
            return $$"""
                <div className="flex flex-wrap gap-x-2 gap-y-1">
                {{Definition.Items.SelectTextTemplate(item => $$"""
                  <Input.CheckBox label="{{item.DisplayName.Replace("\"", "&quot;")}}" {...{{ctx.Register}}(`{{fullpath}}.{{item.PhysicalName}}`)} />
                """)}}
                </div>
                """;
        }

        string IAggregateMemberType.RenderSingleViewVFormBody(AggregateMember.ValueMember vm, FormUIRenderingContext ctx) {
            var fullpath = ctx.GetReactHookFormFieldPath(vm.Declared).Join(".");

            var attrs = new List<string> {
                $"options={{[{Definition.Items.Select(x => $"'{x.PhysicalName}' as const").Join(", ")}]}}",
                $"textSelector={{item => item}}",
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

        public string DataTableColumnDefHelperName => Definition.Name;
        Parts.WebClient.DataTable.CellType.Helper IAggregateMemberType.RenderDataTableColumnDefHelper(CodeRenderingContext ctx) {
            var returnType = $"{Parts.WebClient.DataTable.CellType.RETURNS_ONE_COLUMN}<TRow, {GetTypeScriptTypeName()} | undefined>";
            var body = $$"""
                /** {{Definition.Name}} */
                const {{Definition.Name}}: {{returnType}} = (header, getValue, setValue, opt) => {
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
                {{Definition.Items.SelectTextTemplate((x, i) => $$"""
                      {{(i == 0 ? "if" : "} else if")}} (trimmed === '{{x.PhysicalName}}') {
                        setValue(row, '{{x.PhysicalName}}', rowIndex)
                """)}}
                      } else {
                        setValue(row, undefined, rowIndex)
                      }
                    },
                    comboProps: {
                      onFilter: async keyword => {
                        const array = [{{Definition.Items.Select(x => $"'{x.PhysicalName}' as const").Join(", ")}}]
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
