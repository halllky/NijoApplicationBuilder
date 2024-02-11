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
            return aggregate.Item.ClassName;
        }
        private static IEnumerable<GraphNode<Aggregate>> GetAvailableAggregates(CodeRenderingContext context) {
            return context.Schema
                .RootAggregates()
                .Where(a => a.Item.Options.Handler == NijoCodeGenerator.Models.WriteModel.Key);
        }

        public void BuildSchema(AppSchemaBuilder builder) {
        }

        public void GenerateCode(CodeRenderingContext context) {

            context.EditWebApiDirectory(dir => {
                dir.Directory(App.ASP_UTIL_DIR, utilDir => {
                    utilDir.Generate(RenderTaskDefinition(context));
                    utilDir.Generate(RenderParamBuilder(context));
                });
            });

            context.EditReactDirectory(dir => {
                dir.Generate(UseLocalRepositoryCommitHandling(context));

                dir.Directory(App.REACT_UTIL_DIR, utilDir => {
                    utilDir.Generate(TsHelper(context));
                });
            });
        }
    }
}
