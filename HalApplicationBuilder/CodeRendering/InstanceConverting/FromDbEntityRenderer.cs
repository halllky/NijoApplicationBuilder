using HalApplicationBuilder.CodeRendering.Presentation;
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
    internal class FromDbEntityRenderer {
        internal FromDbEntityRenderer(GraphNode<Aggregate> aggregate, CodeRenderingContext ctx) {
            _aggregate = aggregate;
            _ctx = ctx;
        }

        private readonly GraphNode<Aggregate> _aggregate;
        private readonly CodeRenderingContext _ctx;

        private const string METHODNAME = AggregateMember.FROM_DB_ENTITY_METHOD_NAME;

        internal string Render() {
            return $$"""
                /// <summary>
                /// {{_aggregate.Item.DisplayName}}のデータベースから取得した内容を画面に表示する形に変換します。
                /// </summary>
                public static {{_aggregate.Item.ClassName}} {{METHODNAME}}({{_ctx.Config.EntityNamespace}}.{{_aggregate.Item.EFCoreEntityClassName}} entity) {
                    var instance = new {{_aggregate.Item.ClassName}} {
                        {{WithIndent(RenderBody(_aggregate, _aggregate, "entity", 0), "        ")}}
                    };
                    instance.{{AggregateInstanceBase.INSTANCE_KEY}} = instance.{{AggregateRenderer.GETINSTANCEKEY_METHOD_NAME}}().ToString();
                    instance.{{AggregateInstanceBase.INSTANCE_NAME}} = instance.{{AggregateRenderer.GETINSTANCENAME_METHOD_NAME}}();
                    return instance;
                }
                """;
        }

        private IEnumerable<string> RenderBody(GraphNode<Aggregate> instance, GraphNode<Aggregate> rootInstance, string rootInstanceName, int depth) {
            foreach (var prop in instance.GetMembers()) {
                if (prop is AggregateMember.ValueMember valueMember) {
                    yield return $$"""
                        {{valueMember.PropertyName}} = {{rootInstanceName}}.{{valueMember.GetFullPath(rootInstance).Join(".")}},
                        """;

                } else if (prop is AggregateMember.Ref refProp) {
                    var nav = refProp.GetNavigationProperty();
                    var refTarget = refProp.Owner == nav.Principal.Owner
                        ? nav.Principal
                        : nav.Relevant;
                    var names = refTarget.Owner
                        .GetInstanceNameMembers()
                        .Select(member => $"{rootInstanceName}.{member.GetFullPath(rootInstance).Join(".")}");
                    var foreignKeys = refTarget.ForeignKeys
                        .Select(fk => $"{rootInstanceName}.{fk.GetFullPath(rootInstance.As<IEFCoreEntity>()).Join(".")}");

                    yield return $$"""
                        {{refProp.PropertyName}} = new() {
                            {{AggregateInstanceKeyNamePair.KEY}} = {{Utility.CLASSNAME}}.{{Utility.TO_JSON}}(new object?[] { {{foreignKeys.Join(", ")}} }),
                            {{AggregateInstanceKeyNamePair.NAME}} = {{names.Join(" + ")}},
                        },
                        """;

                } else if (prop is AggregateMember.Children children) {
                    var item = depth == 0 ? "item" : $"item{depth}";
                    var childClass = children.MemberAggregate.Item.ClassName;
                    var childInstance = children.MemberAggregate;
                    var childFullPath = children.GetFullPath(rootInstance).Join(".");

                    yield return $$"""
                        {{children.PropertyName}} = {{rootInstanceName}}.{{childFullPath}}.Select({{item}} => new {{childClass}} {
                            {{WithIndent(RenderBody(childInstance, childInstance, item, depth + 1), "    ")}}
                        }).ToList(),
                        """;

                } else if (prop is AggregateMember.RelationMember child) {
                    var childClass = child.MemberAggregate.Item.ClassName;
                    var childInstance = child.MemberAggregate;

                    yield return $$"""
                        {{child.PropertyName}} = new {{childClass}} {
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
