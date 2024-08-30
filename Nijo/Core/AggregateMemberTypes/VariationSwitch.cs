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
        internal VariationSwitch(VariationGroup<Aggregate> variationGroup) {
            _variationGroup = variationGroup;
        }
        private readonly VariationGroup<Aggregate> _variationGroup;

        public SearchBehavior SearchBehavior => SearchBehavior.Strict;

        private string CsEnumTypeName => _variationGroup.CsEnumType;
        public string GetCSharpTypeName() => CsEnumTypeName;

        public string GetTypeScriptTypeName() {
            return _variationGroup
                .VariationAggregates
                .Select(kv => $"'{kv.Value.RelationName}'")
                .Join(" | ");
        }

        public ReactInputComponent GetReactComponent() {
            var props = new Dictionary<string, string> {
                { "options", $"[{_variationGroup.VariationAggregates.Select(kv => $"'{kv.Value.RelationName}' as const").Join(", ")}]" },
                { "textSelector", "item => item" },
            };

            return new ReactInputComponent {
                Name = "Input.Selection",
                Props = props,
            };
        }

        public IGridColumnSetting GetGridColumnEditSetting() {
            return new ComboboxColumnSetting {
                OptionItemTypeName = _variationGroup.VariationAggregates.Select(kv => $"'{kv.Value.RelationName}'").Join(" | "),
                Options = $"[{_variationGroup.VariationAggregates.Select(kv => $"'{kv.Value.RelationName}' as const").Join(", ")}]",
                EmitValueSelector = $"opt => opt",
                MatchingKeySelectorFromEmitValue = $"value => value",
                MatchingKeySelectorFromOption = $"opt => opt",
                TextSelector = $"opt => opt",
                OnClipboardCopy = (value, formatted) => $$"""
                    const {{formatted}} = {{value}} ?? ''
                    """,
                OnClipboardPaste = (value, formatted) => $$"""
                    let {{formatted}}: {{_variationGroup.VariationAggregates.Select(kv => $"'{kv.Value.RelationName}'").Join(" | ")}} | undefined
                    {{_variationGroup.VariationAggregates.SelectTextTemplate((kv, i) => $$"""
                    {{(i == 0 ? "if" : "} else if")}} ({{value}} === '{{kv.Value.RelationName}}') {
                      {{formatted}} = '{{kv.Value.RelationName}}'
                    """)}}
                    } else {
                      {{formatted}} = undefined
                    }
                    """,
            };
        }


        private string SearchConditionClass => $"{_variationGroup.GroupName}SearchCondition";
        private const string ANY_CHECKED = "AnyChecked";
        private const string ALL_CHECKED = "AllChecked";

        public string GetSearchConditionCSharpType() {
            return SearchConditionClass;
        }
        public string GetSearchConditionTypeScriptType() {
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
            var component = GetReactComponent();
            var fullpath = ctx.GetReactHookFormFieldPath(vm.Declared).Join(".");
            return $$"""
                <div className="flex flex-wrap gap-x-2 gap-y-1">
                {{_variationGroup.VariationAggregates.SelectTextTemplate(kv => $$"""
                  <Input.CheckBox label="{{kv.Value.RelationName}}" {...{{ctx.Register}}(`{{fullpath}}.{{kv.Value.RelationName}}`)} />
                """)}}
                </div>
                """;
        }

        string IAggregateMemberType.RenderSingleViewVFormBody(AggregateMember.ValueMember vm, FormUIRenderingContext ctx) {
            var component = GetReactComponent();
            var fullpath = ctx.GetReactHookFormFieldPath(vm.Declared).Join(".");
            var readOnly = ctx.RenderReadOnlyStatement(vm.Declared);
            return $$"""
                <{{component.Name}} {...{{ctx.Register}}(`{{fullpath}}`)}{{component.GetPropsStatement().Join("")}} {{readOnly}}/>
                {{ctx.RenderErrorMessage(vm)}}
                """;
        }
    }
}
