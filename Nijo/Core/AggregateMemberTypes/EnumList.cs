using Nijo.Models.ReadModel2Features;
using Nijo.Models.RefTo;
using Nijo.Models.WriteModel2Features;
using Nijo.Parts.Utility;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    public class EnumList : IAggregateMemberType {
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

        public string GetSearchConditionCSharpType() {
            return SearchConditionEnum;
        }
        public string GetSearchConditionTypeScriptType() {
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
            var whereFullpath = searchQueryObject == E_SearchQueryObject.SearchResult
                ? member.GetFullPathAsSearchResult(E_CsTs.CSharp, out var isArray)
                : member.GetFullPathAsDbEntity(E_CsTs.CSharp, out isArray);

            return $$"""
                if ({{fullpathNullable}} != null && {{fullpathNotNull}}.{{ANY_CHECKED}}()) {
                    var array = new List<{{enumType}}?>();
                {{Definition.Items.SelectTextTemplate(item => $$"""
                    if ({{fullpathNotNull}}.{{item.PhysicalName}}) array.Add({{enumType}}.{{item.PhysicalName}});
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
                {{Definition.Items.SelectTextTemplate(item => $$"""
                  <Input.CheckBox label="{{item.DisplayName.Replace("\"", "&quot;")}}" {...{{ctx.Register}}(`{{fullpath}}.{{item.PhysicalName}}`)} />
                """)}}
                </div>
                """;
        }

        string IAggregateMemberType.RenderSingleViewVFormBody(AggregateMember.ValueMember vm, FormUIRenderingContext ctx) {
            var fullpath = ctx.GetReactHookFormFieldPath(vm.Declared).Join(".");

            var attrs = new List<string> {
                $"options={{[{Definition.Items.Select(x => $"'{x.PhysicalName}' as const").Join(", ")}]}} ",
                $"textSelector={{item => item}}",
                $"{ctx.RenderReadOnlyStatement(vm.Declared)} ", // readonly
            };

            return $$"""
                <Input.Selection {...{{ctx.Register}}(`{{fullpath}}`)} {{attrs.Join("")}}/>
                {{ctx.RenderErrorMessage(vm)}}
                """;
        }

        public string DataTableColumnDefHelperName => Definition.Name;
        Parts.WebClient.DataTable.CellType.Helper IAggregateMemberType.RenderDataTableColumnDefHelper() {
            var body = $$"""
                /** {{Definition.Name}} */
                const {{Definition.Name}}: {{Parts.WebClient.DataTable.CellType.RETURNS_ONE_COLUMN}}<TRow, {{GetTypeScriptTypeName()}} | undefined> = (header, getValue, setValue, opt) => {
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
                      options: [{{Definition.Items.Select(x => $"'{x.PhysicalName}'").Join(", ")}}],
                      emitValueSelector: x => x,
                      matchingKeySelectorFromEmitValue: x => x,
                      matchingKeySelectorFromOption: x => x,
                      textSelector: x => x,
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
            };
        }
    }
}
