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
        }

        private readonly CodeRenderingContext _ctx;
        private readonly GraphNode<Aggregate> _aggregate;
        private readonly GraphNode<EFCoreEntity> _dbEntity;
        private readonly GraphNode<AggregateInstance> _instance;

        public string FileName => "detail.tsx";
        internal string Url => $"/{_aggregate.Item.UniqueId}/detail";

        private string GetMultiViewUrl() => new MultiView(_aggregate, _ctx).Url;
        private string GetUpdateCommandApi() => new AggFile.Controller(_aggregate).UpdateCommandApi;

        private void RenderForm() {
            foreach (var prop in _instance.GetSchalarProperties(_ctx.Config)) {
                var renderer = new CreateView.FormRenderer(prop, _dbEntity);
                foreach (var line in prop.CorrespondingDbColumn.MemberType.RenderUI(renderer)) {
                    WriteLine(line);
                }
            }
        }
    }
}
