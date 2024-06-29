using Nijo.Core;
using Nijo.Parts;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Features.BatchUpdate {
    internal partial class BatchUpdateFeature : IFeature {

        private static string GetKey(GraphNode<Aggregate> aggregate) {
            return aggregate.Item.PhysicalName;
        }
        private static IEnumerable<GraphNode<Aggregate>> GetAvailableAggregatesOrderByDataFlow(CodeRenderingContext context) {
            return context.Schema
                .RootAggregatesOrderByDataFlow()
                .Where(a => a.Item.Options.Handler == NijoCodeGenerator.Models.WriteModel.Key);
        }

        public void BuildSchema(AppSchemaBuilder builder) {
        }

        public void GenerateCode(CodeRenderingContext context) {

            context.WebApiProject.UtilDir(utilDir => {
                utilDir.Generate(RenderMainClass());
                utilDir.Generate(RenderTaskDefinition());
            });

            context.WebApiProject.ControllerDir(controllerDir => {
                controllerDir.Generate(RenderController());
            });

            context.ReactProject.UtilDir(utilDir => {
                utilDir.Generate(RenderReactHook(context));
            });
        }
    }
}
