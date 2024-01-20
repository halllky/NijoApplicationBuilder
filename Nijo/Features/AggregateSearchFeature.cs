using Nijo.Core;
using Nijo.Util.DotnetEx;
using static Nijo.Util.CodeGenerating.TemplateTextHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nijo.Parts.Utility;
using Nijo.Util.CodeGenerating;
using Nijo.Parts;
using Nijo.Parts.WebClient;
using Nijo.Parts.WebServer;

namespace Nijo.Features {
    public class AggregateSearchFeature {
        internal static MultiView GetMultiView(GraphNode<Aggregate> rootAggregate) {
            var fields = GetFields(rootAggregate);

            var createView = new SingleView(rootAggregate, SingleView.E_Type.Create);
            var singleView = new SingleView(rootAggregate, SingleView.E_Type.View);

            var keys = rootAggregate
                .GetKeys()
                .OfType<AggregateMember.ValueMember>()
                .Select(m => m.GetDbColumn().GetFullPath().Join("_"));

            return new MultiView {
                DisplayName = rootAggregate.Item.DisplayName,
                Fields = fields.Values.ToArray(),
                AppSrvMethodName = AppServiceSearchMethodName(rootAggregate),
                CreateViewUrl = rootAggregate.IsStored()
                    ? createView.GetUrlStringForReact()
                    : null,
                SingleViewUrlFunctionBody = rootAggregate.IsStored()
                    ? (data => singleView.GetUrlStringForReact(keys.Select(field => $"{data}.{field}")))
                    : null,
            };
        }
        private static string AppServiceSearchMethodName(GraphNode<Aggregate> rootAggregate) {
            return $"Search{rootAggregate.Item.DisplayName.ToCSharpSafe()}";
        }
        private static IReadOnlyDictionary<DbColumn, MultiViewField> GetFields(GraphNode<Aggregate> rootAggregate) {

            var rootAndDescendants = rootAggregate
                .EnumerateThisAndDescendants()
                .Where(x => x.EnumerateAncestorsAndThis()
                             .All(ancestor => !ancestor.IsChildrenMember()
                                           && !ancestor.IsVariationMember()));
            var descendantColumns = rootAndDescendants
                .SelectMany(entity => entity.GetColumns());

            // 参照先のキーはdescendantColumnsの中に入っているが、名前は入っていないので、別途取得の必要あり
            var refTargets = rootAndDescendants
                .SelectMany(entity => entity.GetMembers())
                .OfType<AggregateMember.Ref>();
            var refTargetKeys = refTargets.SelectMany(@ref => @ref.MemberAggregate.GetKeys());
            var refTargetNames = refTargets.SelectMany(@ref => @ref.MemberAggregate.GetNames());
            var refTargetColumns = refTargetNames
               .Except(refTargetKeys)
               .OfType<AggregateMember.ValueMember>()
               .Select(member => member.GetDbColumn());

            // 名称重複の解決
            var nameCount = descendantColumns
                .Concat(refTargetColumns)
                .GroupBy(col => col.Options.MemberName)
                .ToDictionary(g => g.Key, _ => 0);

            var fields = descendantColumns
                .Concat(refTargetColumns)
                .ToDictionary(
                    col => col,
                    col => {
                        // 名称重複の解決
                        var ix = nameCount[col.Options.MemberName];
                        var suffix = ix == 0 ? string.Empty : ix.ToString();
                        nameCount[col.Options.MemberName] = ix + 1;

                        return new MultiViewField {
                            MemberType = col.Options.MemberType,
                            VisibleInGui = !col.Options.InvisibleInGui,
                            PhysicalName = col.Options.MemberName + suffix,
                        };
                    });
            return fields;
        }

        public bool? UseDefaultSearchLogic { get; set; }

        public void GenerateCode(CodeRenderingContext context, GraphNode<Aggregate> rootAggregate) {
            var multiView = GetMultiView(rootAggregate);

            context.AddPage(multiView);

            context.UseAggregateFile(rootAggregate, builder => {
                builder.DataClassDeclaring.Add($$"""
                    /// <summary>
                    /// {{multiView.DisplayName}}の一覧検索処理の検索条件を表すクラスです。
                    /// </summary>
                    public partial class {{multiView.SearchConditionClassName}} : {{MultiView.SEARCHCONDITION_BASE_CLASS_NAME}} {
                    {{multiView.Fields.SelectTextTemplate(member => If(member.MemberType.SearchBehavior == SearchBehavior.Range, () => $$"""
                        public {{FromTo.CLASSNAME}}<{{member.MemberType.GetCSharpTypeName()}}?> {{member.PhysicalName}} { get; set; } = new();
                    """).Else(() => $$"""
                        public {{member.MemberType.GetCSharpTypeName()}}? {{member.PhysicalName}} { get; set; }
                    """))}}
                    }

                    /// <summary>
                    /// {{multiView.DisplayName}}の一覧検索処理の検索結果1件を表すクラスです。
                    /// </summary>
                    public partial class {{multiView.SearchResultClassName}} {
                    {{multiView.Fields.SelectTextTemplate(member => $$"""
                        public {{member.MemberType.GetCSharpTypeName()}}? {{member.PhysicalName}} { get; set; }
                    """)}}
                    }
                    """);

                builder.ControllerActions.Add($$"""
                    [HttpGet("{{Parts.WebClient.Controller.SEARCH_ACTION_NAME}}")]
                    public virtual IActionResult Search([FromQuery] string param) {
                        var json = System.Web.HttpUtility.UrlDecode(param);
                        var condition = string.IsNullOrWhiteSpace(json)
                            ? new {{multiView.SearchConditionClassName}}()
                            : {{UtilityClass.CLASSNAME}}.{{UtilityClass.PARSE_JSON}}<{{multiView.SearchConditionClassName}}>(json);
                        var searchResult = _applicationService
                            .{{multiView.AppSrvMethodName}}(condition)
                            .AsEnumerable();
                        return this.JsonContent(searchResult);
                    }
                    """);

                if (UseDefaultSearchLogic == true || rootAggregate.IsStored()) {
                    builder.AppServiceMethods.Add(RenderSearchMethod(context, rootAggregate));
                } else {
                    builder.AppServiceMethods.Add(RenderNotImplementedSearchMethod(context, rootAggregate));
                }

                builder.TypeScriptDataTypes.Add(multiView.RenderTypeScriptTypeDef(context));
            });
        }
        /// <summary>
        /// Viewなどオーバーライド前提のものの検索処理
        /// </summary>
        private string RenderNotImplementedSearchMethod(CodeRenderingContext ctx, GraphNode<Aggregate> rootAggregate) {
            var appSrv = new ApplicationService();
            var multiView = GetMultiView(rootAggregate);

            return $$"""
                public virtual IEnumerable<{{multiView.SearchResultClassName}}> {{AppServiceSearchMethodName(rootAggregate)}}({{multiView.SearchConditionClassName}} conditions) {
                    // このメソッドは自動生成の対象外です。
                    // {{appSrv.ConcreteClass}}クラスでこのメソッドをオーバーライドして実装してください。
                    return Enumerable.Empty<{{multiView.SearchResultClassName}}>();
                }
                """;
        }
        /// <summary>
        /// 標準の検索処理
        /// </summary>
        private string RenderSearchMethod(CodeRenderingContext ctx, GraphNode<Aggregate> rootAggregate) {
            var appSrv = new ApplicationService();
            var fields = GetFields(rootAggregate);
            var selectClause = fields.Select(field => new {
                resultMemberName = field.Value.PhysicalName,
                dbColumnPath = field.Key.GetFullPath().Join("."),
            });
            var multiView = GetMultiView(rootAggregate);

            return $$"""
                /// <summary>
                /// {{rootAggregate.Item.DisplayName}}の一覧検索を行います。
                /// </summary>
                public virtual IEnumerable<{{multiView.SearchResultClassName}}> {{AppServiceSearchMethodName(rootAggregate)}}({{multiView.SearchConditionClassName}} param) {
                    var query = {{appSrv.DbContext}}.{{rootAggregate.Item.DbSetName}}.Select(e => new {{multiView.SearchResultClassName}} {
                {{selectClause.SelectTextTemplate(x => $$"""
                        {{x.resultMemberName}} = e.{{x.dbColumnPath}},
                """)}}
                    });

                {{fields.Values.SelectTextTemplate(field =>
                If(field.MemberType.SearchBehavior == SearchBehavior.Ambiguous, () => $$"""
                    if (!string.IsNullOrWhiteSpace(param.{{field.PhysicalName}})) {
                        var trimmed = param.{{field.PhysicalName}}.Trim();
                        query = query.Where(x => x.{{field.PhysicalName}}.Contains(trimmed));
                    }
                """).ElseIf(field.MemberType.SearchBehavior == SearchBehavior.Range, () => $$"""
                    if (param.{{field.PhysicalName}}.{{FromTo.FROM}} != default) {
                        query = query.Where(x => x.{{field.PhysicalName}} >= param.{{field.PhysicalName}}.{{FromTo.FROM}});
                    }
                    if (param.{{field.PhysicalName}}.{{FromTo.TO}} != default) {
                        query = query.Where(x => x.{{field.PhysicalName}} <= param.{{field.PhysicalName}}.{{FromTo.TO}});
                    }
                """).ElseIf(field.MemberType.SearchBehavior == SearchBehavior.Strict && new[] { "string", "string?" }.Contains(field.MemberType.GetCSharpTypeName()), () => $$"""
                    if (!string.IsNullOrWhiteSpace(param.{{field.PhysicalName}})) {
                        query = query.Where(x => x.{{field.PhysicalName}} == param.{{field.PhysicalName}});
                    }
                """).ElseIf(field.MemberType.SearchBehavior == SearchBehavior.Strict, () => $$"""
                    if (param.{{field.PhysicalName}} != default) {
                        query = query.Where(x => x.{{field.PhysicalName}} == param.{{field.PhysicalName}});
                    }
                """))}}
                    if (param.{{MultiView.SEARCHCONDITION_PAGE_PROP_NAME}} != null) {
                        const int PAGE_SIZE = 20;
                        var skip = param.{{MultiView.SEARCHCONDITION_PAGE_PROP_NAME}}.Value * PAGE_SIZE;
                        query = query.Skip(skip).Take(PAGE_SIZE);
                    }

                    return query.AsEnumerable();
                }
                """;
        }
    }
}
