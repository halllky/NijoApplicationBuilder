using HalApplicationBuilder.Core20230514;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering20230514.Presentation {
    partial class ApiInterface : ITemplate {

        internal ApiInterface(CodeRenderingContext ctx) {
            _ctx = ctx;
        }
        private readonly CodeRenderingContext _ctx;

        public string FileName => "Api.cs";
        private const string E = "e";

        private void ToDbEntity(GraphNode<AggregateInstance> aggregateInstance) {

            void WriteBody(GraphNode<AggregateInstance> instance, string right) {
                foreach (var prop in instance.GetSchalarProperties(_ctx.Config)) {
                    var path = new[] { right }
                        .Concat(instance.Item.CorrespondingDbEntity.PathFromEntry().Select(x => x.RelationName.ToCSharpSafe()))
                        .Concat(new[] { prop.CorrespondingDbColumn.PropertyName })
                        .Join(".");
                    WriteLine($"{prop.PropertyName} = {path},");
                }

                foreach (var child in instance.GetChildAggregateProperties(_ctx.Config)) {
                    var childProp = child.CorrespondingNavigationProperty.Principal.PropertyName;
                    var childDbEntity = child.CorrespondingNavigationProperty.Relevant.CSharpTypeName;
                    if (child.Multiple) {
                        WriteLine($"{child.PropertyName} = this.{childProp}.Select(x => new {childDbEntity} {{");
                        PushIndent("    ");
                        WriteBody(child.ChildAggregateInstance, "x");
                        PopIndent();
                        WriteLine($"}}).ToList(),");

                    } else {
                        WriteLine($"{child.PropertyName} = new {childDbEntity} {{");
                        PushIndent("    ");
                        WriteBody(child.ChildAggregateInstance, "this");
                        PopIndent();
                        WriteLine($"}},");
                    }
                }
            }

            WriteLine($"return new {_ctx.Config.EntityNamespace}.{aggregateInstance.Item.CorrespondingDbEntity.Item.ClassName} {{");
            PushIndent("    ");
            WriteBody(aggregateInstance, "this");
            PopIndent();
            WriteLine($"}};");
        }

        private void FromDbEntity(GraphNode<AggregateInstance> instance) {
            if (instance.Source == null) {
                WriteLine($"return new {instance.Item.ClassName} {{");
            } else {
                WriteLine($"{instance.Source.RelationName.ToCSharpSafe()} = new {instance.Item.ClassName} {{");
            }

            // 自身のプロパティ
            foreach (var prop in instance.GetSchalarProperties(_ctx.Config)) {
                var path = new[] { E }
                    .Concat(instance.PathFromEntry().Select(x => x.RelationName.ToCSharpSafe()))
                    .Concat(new[] { prop.CorrespondingDbColumn.PropertyName })
                    .Join(".");
                WriteLine($"    {prop.PropertyName} = {path},");
            }

            // 子集約
            foreach (var edge in instance.GetChildAggregateProperties(_ctx.Config)) {
                PushIndent("    ");
                FromDbEntity(edge.ChildAggregateInstance);
                PopIndent();
            }

            // 参照
            // TODO

            if (instance.Source == null)
                WriteLine($"}};");
            else
                WriteLine($"}},");
        }
    }
}
