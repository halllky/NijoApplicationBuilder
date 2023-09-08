using HalApplicationBuilder.CodeRendering.Searching;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.WebClient {
    partial class CreateView : ITemplate {
        internal CreateView(GraphNode<Aggregate> aggregate, CodeRenderingContext ctx) {
            _ctx = ctx;
            _aggregate = aggregate;
        }

        private readonly CodeRenderingContext _ctx;
        private readonly GraphNode<Aggregate> _aggregate;

        public string FileName => "new.tsx";
        internal string Url => $"/{_aggregate.Item.UniqueId}/new";
        internal string Route => $"/{_aggregate.Item.UniqueId}/new";

        private string GetMultiViewUrl() => new SearchFeature(_aggregate.As<IEFCoreEntity>(), _ctx).ReactPageUrl;
        private string GetSingleViewUrl() => new SingleView(_aggregate, _ctx, asEditView: false).Url;
        private string GetCreateCommandApi() => new Controller(_aggregate.Item, _ctx).CreateCommandApi;
    }
}
