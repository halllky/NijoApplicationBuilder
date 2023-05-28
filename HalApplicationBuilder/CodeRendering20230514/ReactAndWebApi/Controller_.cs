using HalApplicationBuilder.Core20230514;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering20230514.ReactAndWebApi {
    partial class Controller {
        internal Controller(GraphNode<EFCoreEntity> dbEntity, CodeRenderingContext ctx) {
            _ctx = ctx;
            _agg = dbEntity.Item.Aggregate;
            _dbEntity = dbEntity;
            _searchResult = new SearchResult(dbEntity);
        }

        private readonly CodeRenderingContext _ctx;
        private readonly GraphNode<Aggregate> _agg;
        private readonly GraphNode<EFCoreEntity> _dbEntity;
        private readonly SearchResult _searchResult;

        internal string SubDomain => $"api/{_agg.Item.DisplayName.ToCSharpSafe()}";
        internal string SearchCommandApi => $"/{SubDomain}/list";
        internal string CreateCommandApi => $"/{SubDomain}/create";
        internal string UpdateCommandApi => $"/{SubDomain}/update";
    }
}
