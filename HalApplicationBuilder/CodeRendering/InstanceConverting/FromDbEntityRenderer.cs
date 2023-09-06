using HalApplicationBuilder.CodeRendering.Presentation;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.InstanceConverting {
    internal class FromDbEntityRenderer {
        internal FromDbEntityRenderer(GraphNode<Aggregate> aggregate, CodeRenderingContext ctx) {
            _aggregate = aggregate;
            _aggregateInstance = aggregate.GetInstanceClass().AsEntry();
            _dbEntity = aggregate.GetDbEntity().AsEntry();
            _ctx = ctx;
        }

        private readonly GraphNode<Aggregate> _aggregate;
        private readonly GraphNode<AggregateInstance> _aggregateInstance;
        private readonly GraphNode<EFCoreEntity> _dbEntity;
        private readonly CodeRenderingContext _ctx;

        internal const string METHODNAME = "FromDbEntity";

        internal string Render() {
            var builder = new StringBuilder();
            var indent = "";

            void WriteBody(GraphNode<AggregateInstance> instance, string parentPath, string instancePath, int depth) {
                // 自身のメンバー
                foreach (var prop in instance.GetSchalarProperties()) {
                    builder.AppendLine($"{indent}{prop.PropertyName} = {instancePath}.{prop.CorrespondingDbColumn.PropertyName},");
                }
                foreach (var prop in instance.GetVariationSwitchProperties(_ctx.Config)) {
                    builder.AppendLine($"{indent}{prop.PropertyName} = {instancePath}.{prop.CorrespondingDbColumn.PropertyName},");
                }
                // Ref
                foreach (var prop in instance.GetRefProperties(_ctx.Config)) {
                    var navigation = prop.Owner.GetDbEntity() == prop.CorrespondingNavigationProperty.Principal.Owner
                        ? prop.CorrespondingNavigationProperty.Principal
                        : prop.CorrespondingNavigationProperty.Relevant;
                    builder.AppendLine($"{indent}{prop.PropertyName} = {prop.RefTarget.Item.ClassName}.{AggregateInstance.FROM_DB_ENTITY_METHOD_NAME}({instancePath}.{navigation.PropertyName}).{AggregateRenderer.TOKEYNAMEPAIR_METHOD_NAME}(),");
                }
                // 子要素
                foreach (var child in instance.GetChildrenProperties(_ctx.Config)) {
                    builder.AppendLine($"{indent}{child.PropertyName} = {instancePath}.{child.PropertyName}.Select(x{depth} => new {child.ChildAggregateInstance.Item.ClassName} {{");
                    indent += "    ";
                    WriteBody(child.ChildAggregateInstance.AsEntry(), instancePath, $"x{depth}", depth + 1);
                    indent = indent.Substring(indent.Length - 4, 4);
                    builder.AppendLine($"{indent}}}).ToList(),");
                }
                foreach (var child in instance.GetChildProperties(_ctx.Config)) {
                    builder.AppendLine($"{indent}{child.PropertyName} = new {child.ChildAggregateInstance.Item.ClassName} {{");
                    indent += "    ";
                    WriteBody(child.ChildAggregateInstance, instancePath, $"{instancePath}.{child.CorrespondingNavigationProperty.Principal.PropertyName}", depth + 1);
                    indent = indent.Substring(indent.Length - 4, 4);
                    builder.AppendLine($"{indent}}},");
                }
                foreach (var child in instance.GetVariationProperties(_ctx.Config)) {
                    builder.AppendLine($"{indent}{child.PropertyName} = new {child.ChildAggregateInstance.Item.ClassName} {{");
                    indent += "    ";
                    WriteBody(child.ChildAggregateInstance, instancePath, $"{instancePath}.{child.CorrespondingNavigationProperty.Principal.PropertyName}", depth + 1);
                    indent = indent.Substring(indent.Length - 4, 4);
                    builder.AppendLine($"{indent}}},");
                }
            }

            builder.AppendLine($$"""
                /// <summary>
                /// {{_aggregate.Item.DisplayName}}のデータベースから取得した内容を画面に表示する形に変換します。
                /// </summary>
                public static {{_aggregateInstance.Item.ClassName}} {{AggregateInstance.FROM_DB_ENTITY_METHOD_NAME}}({{_ctx.Config.EntityNamespace}}.{{_dbEntity.Item.ClassName}} e) {
                """);
            indent += "    ";

            builder.AppendLine($"{indent}var instance = new {_aggregateInstance.Item.ClassName} {{");
            indent += "    ";
            WriteBody(_aggregateInstance, "", "e", 0);
            indent = indent.Substring(indent.Length - 4, 4);
            builder.AppendLine($"{indent}}};");
            builder.AppendLine($"{indent}instance.{AggregateInstanceBase.INSTANCE_KEY} = instance.{AggregateRenderer.GETINSTANCEKEY_METHOD_NAME}().ToString();");
            builder.AppendLine($"{indent}instance.{AggregateInstanceBase.INSTANCE_NAME} = instance.{AggregateRenderer.GETINSTANCENAME_METHOD_NAME}();");
            builder.AppendLine($"{indent}return instance;");

            indent = indent.Substring(indent.Length - 4, 4);
            builder.AppendLine("}");

            return builder.ToString();
        }
    }
}
