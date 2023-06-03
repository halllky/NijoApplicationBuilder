using HalApplicationBuilder.CodeRendering20230514.EFCore;
using HalApplicationBuilder.Core20230514;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering20230514.ReactAndWebApi {
    partial class Controller : ITemplate {
        internal static IEnumerable<Controller> All(CodeRenderingContext ctx) {
            return ctx.Schema
                .ToEFCoreGraph()
                .RootEntities()
                .Select(dbEntity => new Controller(dbEntity, ctx));
        }

        internal Controller(GraphNode<EFCoreEntity> dbEntity, CodeRenderingContext ctx) {
            _ctx = ctx;
            _agg = dbEntity.Item.Aggregate;
            _dbEntity = dbEntity;
            _searchResult = new SearchResult(dbEntity);

            _searchMethod = new Search.MethodInfo(dbEntity, ctx);
            _aggregateInstance = new AggregateInstance(dbEntity);
        }

        private readonly CodeRenderingContext _ctx;
        private readonly GraphNode<Aggregate> _agg;
        private readonly GraphNode<EFCoreEntity> _dbEntity;
        private readonly SearchResult _searchResult;
        private readonly Search.MethodInfo _searchMethod;
        private readonly AggregateInstance _aggregateInstance;

        public string FileName => $"{_agg.Item.DisplayName.ToFileNameSafe()}Controller.cs";
        internal string ClassName => $"{_agg.Item.DisplayName.ToCSharpSafe()}Controller";

        private const string SEARCH_ACTION_NAME = "list";
        private const string CREATE_ACTION_NAME = "create";
        private const string UPDATE_ACTION_NAME = "update";
        private const string FIND_ACTION_NAME = "detail";
        private const string KEYWORDSEARCH_ACTION_NAME = "list-by-keyword";

        private const string SUBDOMAIN = "api";

        internal string SubDomain => $"{SUBDOMAIN}/{_agg.Item.DisplayName.ToCSharpSafe()}";
        internal string SearchCommandApi => $"/{SubDomain}/{SEARCH_ACTION_NAME}";
        internal string CreateCommandApi => $"/{SubDomain}/{CREATE_ACTION_NAME}";
        internal string UpdateCommandApi => $"/{SubDomain}/{UPDATE_ACTION_NAME}";
        internal string KeywordSearchCommandApi => $"/{SubDomain}/{KEYWORDSEARCH_ACTION_NAME}";
    }
}
