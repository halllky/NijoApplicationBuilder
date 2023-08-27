using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.WebClient {
    partial class SingleView : ITemplate {
        internal SingleView(GraphNode<Aggregate> aggregate, CodeRenderingContext ctx) {
            _ctx = ctx;
            _aggregate = aggregate;
            _instance = aggregate.GetInstanceClass().AsEntry();
        }

        private readonly CodeRenderingContext _ctx;
        private readonly GraphNode<Aggregate> _aggregate;
        private readonly GraphNode<AggregateInstance> _instance;

        public string FileName => "detail.tsx";
        internal string Url => $"/{_aggregate.Item.UniqueId}/detail";
        internal string Route => $"/{_aggregate.Item.UniqueId}/detail/:instanceKey";

        private string GetMultiViewUrl() => new MultiView(_aggregate, _ctx).Url;
        private string GetFindCommandApi() => new AggFile.Controller(_aggregate).FindCommandApi;
        private string GetUpdateCommandApi() => new AggFile.Controller(_aggregate).UpdateCommandApi;
    }
}
