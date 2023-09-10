using HalApplicationBuilder.CodeRendering.Util;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HalApplicationBuilder.CodeRendering.TemplateTextHelper;

namespace HalApplicationBuilder.CodeRendering.InstanceConverting {
    internal class ToDbEntityRenderer {
        internal ToDbEntityRenderer(GraphNode<Aggregate> aggregate, CodeRenderingContext ctx) {
            _aggregate = aggregate;
            _ctx = ctx;
        }

        private readonly GraphNode<Aggregate> _aggregate;
        private readonly CodeRenderingContext _ctx;

        internal const string METHODNAME = "ToDbEntity";

        internal string Render() {
            return $$"""
                /// <summary>
                /// {{_aggregate.Item.DisplayName}}のオブジェクトをデータベースに保存する形に変換します。
                /// </summary>
                public {{_ctx.Config.EntityNamespace}}.{{_aggregate.Item.EFCoreEntityClassName}} {{METHODNAME}}() {
                    return new {{_ctx.Config.EntityNamespace}}.{{_aggregate.Item.EFCoreEntityClassName}} {
                        {{WithIndent(RenderBody(_aggregate, _aggregate, "this", 0), "        ")}}
                    };
                }
                """;
        }

        private IEnumerable<string> RenderBody(GraphNode<Aggregate> instance, GraphNode<Aggregate> rootInstance, string rootInstanceName, int depth) {
            foreach (var prop in instance.GetMembers()) {
                if (prop is AggregateMember.ParentPK) {
                    continue; // Children等の分岐でレンダリングするので

                } else if (prop is AggregateMember.RefTargetMember) {
                    continue; // Refの分岐でレンダリングするので

                } else if (prop is AggregateMember.ValueMember valueMember) {
                    yield return $$"""
                        {{valueMember.GetDbColumn().PropertyName}} = {{rootInstanceName}}.{{valueMember.GetFullPath(rootInstance).Join(".")}},
                        """;

                } else if (prop is AggregateMember.Ref refProp) {
                    var refTargetKeys = refProp.GetForeignKeys().ToArray();
                    var refPropFullpath = $"{rootInstanceName}.{refProp.GetFullPath(rootInstance).Join(".")}.{AggregateInstanceKeyNamePair.KEY}";
                    for (int i = 0; i < refTargetKeys.Length; i++) {
                        var col = refTargetKeys[i];
                        var memberType = col.MemberType.GetCSharpTypeName();

                        yield return $$"""
                            {{col.PropertyName}} = ({{memberType}}){{AggregateInstanceKeyNamePair.RenderKeyJsonRestoring(refPropFullpath)}}[{{i}}]!,
                            """;
                    }

                } else if (prop is AggregateMember.Children children) {
                    var item = depth == 0 ? "item" : $"item{depth}";
                    var nav = children.GetNavigationProperty();
                    var childProp = nav.Principal.PropertyName;
                    var childInstance = children.MemberAggregate;
                    var childDbEntityClass = $"{_ctx.Config.EntityNamespace}.{nav.Relevant.Owner.Item.EFCoreEntityClassName}";

                    yield return $$"""
                        {{children.PropertyName}} = this.{{childProp}}.Select({{item}} => new {{childDbEntityClass}} {
                        {{childInstance.GetColumns().OfType<DbColumn.ParentTablePKColumn>().SelectTextTemplate(parentPk => $$"""
                            {{parentPk.PropertyName}} = {{rootInstanceName}}.{{parentPk.Original.GetFullPath(rootInstance.As<IEFCoreEntity>()).Join(".")}},
                        """)}}
                            {{WithIndent(RenderBody(childInstance, childInstance, item, depth + 1), "    ")}}
                        }).ToList(),
                        """;

                } else if (prop is AggregateMember.RelationMember child) {
                    var nav = child.GetNavigationProperty();
                    var childProp = nav.Principal.PropertyName;
                    var childInstance = child.MemberAggregate;
                    var childDbEntityClass = $"{_ctx.Config.EntityNamespace}.{nav.Relevant.Owner.Item.EFCoreEntityClassName}";

                    yield return $$"""
                        {{child.PropertyName}} = new {{childDbEntityClass}} {
                        {{childInstance.GetColumns().OfType<DbColumn.ParentTablePKColumn>().SelectTextTemplate(parentPk => $$"""
                            {{parentPk.PropertyName}} = {{rootInstanceName}}.{{parentPk.Original.GetFullPath(rootInstance.As<IEFCoreEntity>()).Join(".")}},
                        """)}}
                            {{WithIndent(RenderBody(childInstance, rootInstance, rootInstanceName, depth + 1), "    ")}}
                        },
                        """;

                } else {
                    throw new NotImplementedException();
                }
            }
        }
    }
}
