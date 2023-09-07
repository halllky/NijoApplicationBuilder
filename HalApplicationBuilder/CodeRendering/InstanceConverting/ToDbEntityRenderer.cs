using HalApplicationBuilder.CodeRendering.Util;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.InstanceConverting {
    internal class ToDbEntityRenderer {
        internal ToDbEntityRenderer(GraphNode<Aggregate> aggregate, CodeRenderingContext ctx) {
            _aggregate = aggregate;
            _aggregateInstance = aggregate.GetInstanceClass().AsEntry();
            _dbEntity = aggregate.GetDbEntity().AsEntry();
            _ctx = ctx;
        }

        private readonly GraphNode<Aggregate> _aggregate;
        private readonly GraphNode<AggregateInstance> _aggregateInstance;
        private readonly GraphNode<EFCoreEntity> _dbEntity;
        private readonly CodeRenderingContext _ctx;

        private const string METHODNAME = AggregateInstance.TO_DB_ENTITY_METHOD_NAME;

        internal string Render() {
            var builder = new StringBuilder();
            var indent = "";

            void WriteBody(GraphNode<AggregateInstance> instance, string parentPath, string instancePath, int depth) {
                // 親のPK
                var parent = instance.GetParent()?.Initial;
                if (parent != null) {
                    var parentPkColumns = instance
                        .GetDbEntity()
                        .GetColumns()
                        .Where(col => col is EFCoreEntity.ParentTablePrimaryKey)
                        .Cast<EFCoreEntity.ParentTablePrimaryKey>();
                    foreach (var col in parentPkColumns) {
                        builder.AppendLine($"{indent}{col.PropertyName} = {parentPath}.{col.CorrespondingParentColumn.PropertyName},");
                    }
                }
                // 自身のメンバー
                foreach (var prop in instance.GetSchalarProperties()) {
                    builder.AppendLine($"{indent}{prop.CorrespondingDbColumn.PropertyName} = {instancePath}.{prop.PropertyName},");
                }
                foreach (var prop in instance.GetVariationSwitchProperties(_ctx.Config)) {
                    builder.AppendLine($"{indent}{prop.CorrespondingDbColumn.PropertyName} = {instancePath}.{prop.PropertyName},");
                }
                // Ref
                foreach (var prop in instance.GetRefProperties(_ctx.Config)) {
                    for (int i = 0; i < prop.CorrespondingDbColumns.Length; i++) {
                        var col = prop.CorrespondingDbColumns[i];
                        builder.AppendLine($"{indent}{col.PropertyName} = ({col.MemberType.GetCSharpTypeName()}){InstanceKey.CLASS_NAME}.{InstanceKey.PARSE}({instancePath}.{prop.PropertyName}.{AggregateInstanceKeyNamePair.KEY}).{InstanceKey.OBJECT_ARRAY}[{i}],");
                    }
                }
                // 子要素
                foreach (var child in instance.GetChildrenProperties(_ctx.Config)) {
                    var childProp = child.CorrespondingNavigationProperty.Principal.PropertyName;
                    var childDbEntity = $"{_ctx.Config.EntityNamespace}.{child.CorrespondingNavigationProperty.Relevant.Owner.Item.ClassName}";

                    builder.AppendLine($"{indent}{child.PropertyName} = this.{childProp}.Select(x{depth} => new {childDbEntity} {{");
                    indent += "    ";
                    WriteBody(child.ChildAggregateInstance.AsEntry(), instancePath, $"x{depth}", depth + 1);
                    indent = indent.Substring(indent.Length - 4, 4);
                    builder.AppendLine($"{indent}}}).ToList(),");
                }
                foreach (var child in instance.GetChildProperties(_ctx.Config)) {
                    var childProp = child.CorrespondingNavigationProperty.Principal.PropertyName;
                    var childDbEntity = $"{_ctx.Config.EntityNamespace}.{child.CorrespondingNavigationProperty.Relevant.Owner.Item.ClassName}";

                    builder.AppendLine($"{indent}{child.PropertyName} = new {childDbEntity} {{");
                    indent += "    ";
                    WriteBody(child.ChildAggregateInstance, instancePath, $"{instancePath}.{child.ChildAggregateInstance.Source!.RelationName}", depth + 1);
                    indent = indent.Substring(indent.Length - 4, 4);
                    builder.AppendLine($"{indent}}},");
                }
                foreach (var child in instance.GetVariationProperties(_ctx.Config)) {
                    var childProp = child.CorrespondingNavigationProperty.Principal.PropertyName;
                    var childDbEntity = $"{_ctx.Config.EntityNamespace}.{child.CorrespondingNavigationProperty.Relevant.Owner.Item.ClassName}";

                    builder.AppendLine($"{indent}{child.PropertyName} = new {childDbEntity} {{");
                    indent += "    ";
                    WriteBody(child.ChildAggregateInstance, instancePath, $"{instancePath}.{child.ChildAggregateInstance.Source!.RelationName}", depth + 1);
                    indent = indent.Substring(indent.Length - 4, 4);
                    builder.AppendLine($"{indent}}},");
                }
            }

            builder.AppendLine($"{indent}return new {_ctx.Config.EntityNamespace}.{_dbEntity.Item.ClassName} {{");
            indent += "    ";
            WriteBody(_aggregateInstance, "", "this", 0);
            indent = indent.Substring(indent.Length - 4, 4);
            builder.AppendLine($"{indent}}};");

            return builder.ToString();
        }
    }
}
