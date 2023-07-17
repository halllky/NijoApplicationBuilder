using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.Presentation {
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
                        .Concat(instance.PathFromEntry().Select(x => x.RelationName.ToCSharpSafe()))
                        .Concat(new[] { prop.CorrespondingDbColumn.PropertyName })
                        .Join(".");
                    WriteLine($"{prop.PropertyName} = {path},");
                }

                foreach (var child in instance.GetChildAggregateProperties(_ctx.Config)) {
                    var childProp = child.CorrespondingNavigationProperty.Principal.PropertyName;
                    var childDbEntity = $"{_ctx.Config.EntityNamespace}.{child.CorrespondingNavigationProperty.Relevant.Owner.Item.ClassName}";
                    if (child.Multiple) {
                        WriteLine($"{child.PropertyName} = this.{childProp}.Select(x => new {childDbEntity} {{");
                        PushIndent("    ");
                        WriteBody(child.ChildAggregateInstance.AsEntry(), "x");
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

            WriteLine($"return new {_ctx.Config.EntityNamespace}.{aggregateInstance.GetDbEntity().Item.ClassName} {{");
            PushIndent("    ");
            WriteBody(aggregateInstance, "this");
            PopIndent();
            WriteLine($"}};");
        }

        private void FromDbEntity(GraphNode<AggregateInstance> aggregateInstance) {

            void WriteBody(GraphNode<AggregateInstance> instance, string right) {
                foreach (var prop in instance.GetSchalarProperties(_ctx.Config)) {
                    var path = new[] { right }
                        .Concat(instance.PathFromEntry().Select(x => x.RelationName.ToCSharpSafe()))
                        .Concat(new[] { prop.CorrespondingDbColumn.PropertyName })
                        .Join(".");
                    WriteLine($"{prop.PropertyName} = {path},");
                }

                foreach (var child in instance.GetChildAggregateProperties(_ctx.Config)) {
                    if (child.Multiple) {
                        WriteLine($"{child.PropertyName} = {right}.{child.PropertyName}.Select(x => new {child.ChildAggregateInstance.Item.ClassName} {{");
                        PushIndent("    ");
                        WriteBody(child.ChildAggregateInstance.AsEntry(), "x");
                        PopIndent();
                        WriteLine($"}}).ToList(),");

                    } else {
                        WriteLine($"{child.PropertyName} = new {child.ChildAggregateInstance.Item.ClassName} {{");
                        PushIndent("    ");
                        WriteBody(child.ChildAggregateInstance, E);
                        PopIndent();
                        WriteLine($"}},");
                    }
                }
            }

            WriteLine($"return new {aggregateInstance.Item.ClassName} {{");
            PushIndent("    ");
            WriteBody(aggregateInstance, E);
            PopIndent();
            WriteLine($"}};");
        }
    }
}
