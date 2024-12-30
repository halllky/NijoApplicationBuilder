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
    internal class Boolean : IAggregateMemberType {
        public string GetUiDisplayName() => "真偽値";
        public string GetHelpText() => $"真偽値。";

        public string GetCSharpTypeName() => "bool";
        public string GetTypeScriptTypeName() => "boolean";

        public string UiConstraintType => "MemberConstraintBase";
        public virtual IEnumerable<string> RenderUiConstraintValue(AggregateMember.ValueMember vm) {
            yield break;
        }

        private const string BOOL_SEARCH_CONDITION_ENUM = "E_BoolSearchCondition";
        private const string NO_FILTER = "指定なし";
        private const string ONLY_TRUE = "Trueのみ";
        private const string ONLY_FALSE = "Falseのみ";

        public string GetSearchConditionCSharpType(AggregateMember.ValueMember vm) => BOOL_SEARCH_CONDITION_ENUM;
        public string GetSearchConditionTypeScriptType(AggregateMember.ValueMember vm) => $"'{NO_FILTER}' | '{ONLY_TRUE}' | '{ONLY_FALSE}'";

        void IAggregateMemberType.GenerateCode(CodeRenderingContext context) {
            context.CoreLibrary.Enums.Add($$"""
                public enum {{BOOL_SEARCH_CONDITION_ENUM}} {
                    {{NO_FILTER}},
                    {{ONLY_TRUE}},
                    {{ONLY_FALSE}},
                }
                """);
        }

        string IAggregateMemberType.RenderFilteringStatement(AggregateMember.ValueMember member, string query, string searchCondition, E_SearchConditionObject searchConditionObject, E_SearchQueryObject searchQueryObject) {
            var pathFromSearchCondition = searchConditionObject == E_SearchConditionObject.SearchCondition
                ? member.Declared.GetFullPathAsSearchConditionFilter(E_CsTs.CSharp)
                : member.Declared.GetFullPathAsRefSearchConditionFilter(E_CsTs.CSharp);
            var fullpathNullable = $"{searchCondition}.{pathFromSearchCondition.Join("?.")}";
            var whereFullpath = searchQueryObject == E_SearchQueryObject.SearchResult
                ? member.GetFullPathAsSearchResult(E_CsTs.CSharp, out var isArray)
                : member.GetFullPathAsDbEntity(E_CsTs.CSharp, out isArray);

            return $$"""
                if ({{fullpathNullable}} == {{BOOL_SEARCH_CONDITION_ENUM}}.{{ONLY_TRUE}}) {
                {{If(isArray, () => $$"""
                    {{query}} = {{query}}.Where(x => x.{{whereFullpath.SkipLast(1).Join(".")}}.Any(y => y.{{member.MemberName}} == true));
                """).Else(() => $$"""
                    {{query}} = {{query}}.Where(x => x.{{whereFullpath.Join(".")}} == true);
                """)}}
                } else if ({{fullpathNullable}} == {{BOOL_SEARCH_CONDITION_ENUM}}.{{ONLY_FALSE}}) {
                {{If(isArray, () => $$"""
                    {{query}} = {{query}}.Where(x => x.{{whereFullpath.SkipLast(1).Join(".")}}.Any(y => y.{{member.MemberName}} == false));
                """).Else(() => $$"""
                    {{query}} = {{query}}.Where(x => x.{{whereFullpath.Join(".")}} == false);
                """)}}
                }
                """;
        }

        string IAggregateMemberType.RenderSearchConditionVFormBody(AggregateMember.ValueMember vm, FormUIRenderingContext ctx) {
            var fullpath = ctx.GetReactHookFormFieldPath(vm.Declared).Join(".");
            return $$"""
                <Input.Selection {...{{ctx.Register}}(`{{fullpath}}`)} options={['Trueのみ', 'Falseのみ', '指定なし']} textSelector={item => item} />
                """;
        }

        string IAggregateMemberType.RenderSingleViewVFormBody(AggregateMember.ValueMember vm, FormUIRenderingContext ctx) {
            var fullpath = ctx.GetReactHookFormFieldPath(vm.Declared).Join(".");

            var attrs = new List<string>();
            attrs.Add(ctx.RenderReadOnlyStatement(vm.Declared));
            ctx.EditComponentAttributes?.Invoke(vm, attrs);

            return $$"""
                <Input.CheckBox {...{{ctx.Register}}(`{{fullpath}}`)} {{attrs.Join(" ")}}/>
                {{ctx.RenderErrorMessage(vm)}}
                """;
        }

        public string DataTableColumnDefHelperName => "bool";
        Parts.WebClient.DataTable.CellType.Helper IAggregateMemberType.RenderDataTableColumnDefHelper(CodeRenderingContext ctx) {
            var returnType = $"{Parts.WebClient.DataTable.CellType.RETURNS_ONE_COLUMN}<TRow, {GetTypeScriptTypeName()} | undefined>";
            var body = $$"""
                /** 真偽値 */
                const bool: {{returnType}} = (header, getValue, setValue, opt) => {
                  const editSetting: ColumnEditSetting<TRow, { key: 'T' | 'F', text: string }> = {
                    type: 'combo',
                    readOnly: typeof opt?.readOnly === 'function'
                      ? opt.readOnly
                      : undefined,
                    onStartEditing: row => getValue(row) ? { key: 'T', text: '✓' } : { key: 'F', text: '' },
                    onEndEditing: (row, value, rowIndex) => {
                      setValue(row, value?.key === 'T', rowIndex)
                    },
                    onClipboardPaste: (row, value, rowIndex) => {
                      const normalized = Util.normalize(value).toLowerCase()
                      const blnValue =
                        normalized === 't'
                        || normalized === 'true'
                        || normalized === '1'
                        || normalized === '✓'
                      setValue(row, blnValue, rowIndex)
                    },
                    comboProps: {
                      onFilter: async () => [{ key: 'T', text: '✓' }, { key: 'F', text: '' }],
                      getOptionText: opt => opt.text,
                      getValueFromOption: opt => opt,
                      getValueText: value => value.text,
                    }
                  }
                  return {
                    ...opt,
                    id: `${opt?.headerGroupName}::${header}`,
                    header,
                    render: row => <PlainCell>{(getValue(row) ? '✓' : '')}</PlainCell>,
                    onClipboardCopy: row => getValue(row) ? 'TRUE' : 'FALSE',
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
