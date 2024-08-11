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

        public SearchBehavior SearchBehavior => SearchBehavior.Strict;
        public string GetCSharpTypeName() => Definition.Name;
        public string GetTypeScriptTypeName() {
            return Definition.Items.Select(x => $"'{x.PhysicalName}'").Join(" | ");
        }

        public ReactInputComponent GetReactComponent() {
            var props = new Dictionary<string, string> {
                { "options", $"[{Definition.Items.Select(x => $"'{x.PhysicalName}' as const").Join(", ")}]" },
                { "textSelector", "item => item" },
            };

            return new ReactInputComponent {
                Name = "Input.Selection",
                Props = props,
            };
        }

        public IGridColumnSetting GetGridColumnEditSetting() {
            return new ComboboxColumnSetting {
                OptionItemTypeName = GetTypeScriptTypeName(),
                Options = $"[{Definition.Items.Select(x => $"'{x.PhysicalName}' as const").Join(", ")}]",
                EmitValueSelector = $"opt => opt",
                MatchingKeySelectorFromEmitValue = $"value => value",
                MatchingKeySelectorFromOption = $"opt => opt",
                TextSelector = $"opt => opt",
                OnClipboardCopy = (value, formatted) => $$"""
                    const {{formatted}} = {{value}} ?? ''
                    """,
                OnClipboardPaste = (value, formatted) => $$"""
                    let {{formatted}}: {{Definition.Items.Select(x => $"'{x.PhysicalName}'").Join(" | ")}} | undefined
                    {{Definition.Items.SelectTextTemplate((x, i) => $$"""
                    {{(i == 0 ? "if" : "} else if")}} ({{value}} === '{{x.PhysicalName}}') {
                      {{formatted}} = '{{x.PhysicalName}}'
                    """)}}
                    } else {
                      {{formatted}} = undefined
                    }
                    """,
            };
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
            var isArray = member.Owner.EnumerateAncestorsAndThis().Any(a => a.IsChildrenMember());
            var fullpathNullable = $"{searchCondition}.{pathFromSearchCondition.Join("?.")}";
            var fullpathNotNull = $"{searchCondition}.{pathFromSearchCondition.Join(".")}";
            var entityOwnerPath = member.Owner.GetFullPathAsDbEntity().Join(".");
            var entityMemberPath = member.GetFullPathAsDbEntity().Join(".");
            var enumType = GetCSharpTypeName();

            return $$"""
                if ({{fullpathNullable}} != null && {{fullpathNotNull}}.{{ANY_CHECKED}}()) {
                    var array = new List<{{enumType}}?>();
                {{Definition.Items.SelectTextTemplate(item => $$"""
                    if ({{fullpathNotNull}}.{{item.PhysicalName}}) array.Add({{enumType}}.{{item.PhysicalName}});
                """)}}

                {{If(isArray, () => $$"""
                    {{query}} = {{query}}.Where(x => x.{{entityOwnerPath}}.Any(y => array.Contains(y.{{member.MemberName}})));
                """).Else(() => $$"""
                    {{query}} = {{query}}.Where(x => array.Contains(x.{{entityMemberPath}}));
                """)}}
                }
                """;
        }

        string IAggregateMemberType.RenderSearchConditionVFormBody(AggregateMember.ValueMember vm, ReactPageRenderingContext ctx) {
            var component = GetReactComponent();
            var fullpath = ctx.RenderingObjectType switch {
                E_ReactPageRenderingObjectType.SearchCondition => vm.Declared.GetFullPathAsSearchConditionFilter(E_CsTs.TypeScript).Join("."),
                E_ReactPageRenderingObjectType.RefTarget => vm.Declared.GetFullPathAsDataClassForRefTarget().Join("."),
                E_ReactPageRenderingObjectType.DataClassForDisplay => vm.Declared.GetFullPathAsDataClassForDisplay(E_CsTs.TypeScript).Join("."),
                _ => throw new NotImplementedException(),
            };
            return $$"""
                {{Definition.Items.SelectTextTemplate(item => $$"""
                <Input.CheckBox {...{{ctx.Register}}(`{{fullpath}}.{{item.PhysicalName}}`)} />
                """)}}
                """;
        }

        string IAggregateMemberType.RenderSingleViewVFormBody(AggregateMember.ValueMember vm, ReactPageRenderingContext ctx) {
            var component = GetReactComponent();
            var fullpath = ctx.RenderingObjectType switch {
                E_ReactPageRenderingObjectType.SearchCondition => vm.Declared.GetFullPathAsSearchConditionFilter(E_CsTs.TypeScript).Join("."),
                E_ReactPageRenderingObjectType.RefTarget => vm.Declared.GetFullPathAsDataClassForRefTarget().Join("."),
                E_ReactPageRenderingObjectType.DataClassForDisplay => vm.Declared.GetFullPathAsDataClassForDisplay(E_CsTs.TypeScript).Join("."),
                _ => throw new NotImplementedException(),
            };
            return $$"""
                <{{component.Name}} {...{{ctx.Register}}(`{{fullpath}}`)}{{component.GetPropsStatement().Join("")}} />
                """;
        }
    }
}
