using Nijo.Core;
using Nijo.DotnetEx;
using Nijo.Features.InstanceHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Features {
    internal class MasterDataFeature : NijoFeatureBaseByAggregate {
        public override string KeywordInAppSchema => "master-data";

        public override void GenerateCode(ICodeRenderingContext context, GraphNode<Aggregate> rootAggregate) {
            var createCommand = new Command.CommandFeature();
            createCommand.CommandName = root => $"{root.Item.ClassName}新規作成";
            createCommand.ActionName = root => $"作成";
            createCommand.GenerateCode(context, rootAggregate);

            context.WebApiProject.EditDirectory(genDir => {
                genDir.Generate(new AggregateRenderer(rootAggregate).Render());
            });

            context.ReactProject.EditDirectory(reactDir => {
                reactDir.Directory("pages", pageDir => {
                    pageDir.Directory(rootAggregate.Item.DisplayName.ToFileNameSafe(), aggregateDir => {
                        aggregateDir.Generate(new Searching.AggregateSearchFeature(rootAggregate).GetMultiView().RenderMultiView());
                        aggregateDir.Generate(new SingleView(rootAggregate, SingleView.E_Type.View).Render());
                        aggregateDir.Generate(new SingleView(rootAggregate, SingleView.E_Type.Edit).Render());
                    });
                });
            });
        }
    }
}
