using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.WebClient {
    partial class SingleView : ITemplate {
        internal SingleView(GraphNode<Aggregate> aggregate, CodeRenderingContext ctx) {
            _ctx = ctx;
            _aggregate = aggregate;
            _dbEntity = aggregate.GetDbEntity().AsEntry();
            _instance = aggregate.GetInstanceClass().AsEntry();

            PropNameWidth = CreateView.GetPropNameFlexBasis(_instance.GetProperties(ctx.Config));
        }

        private readonly CodeRenderingContext _ctx;
        private readonly GraphNode<Aggregate> _aggregate;
        private readonly GraphNode<EFCoreEntity> _dbEntity;
        private readonly GraphNode<AggregateInstance> _instance;

        public string FileName => "detail.tsx";
        internal string Url => $"/{_aggregate.Item.UniqueId}/detail";
        internal string Route => $"/{_aggregate.Item.UniqueId}/detail/:instanceKey";

        private string GetMultiViewUrl() => new MultiView(_aggregate, _ctx).Url;
        private string GetFindCommandApi() => new AggFile.Controller(_aggregate).FindCommandApi;
        private string GetUpdateCommandApi() => new AggFile.Controller(_aggregate).UpdateCommandApi;

        private string PropNameWidth { get; }

        private IEnumerable<string> RenderForm(AggregateInstance.SchalarProperty prop) {
            var renderer = new CreateView.FormRenderer(prop, _dbEntity);
            foreach (var line in prop.CorrespondingDbColumn.MemberType.RenderUI(renderer)) {
                yield return line;
            }
        }
    }
}
