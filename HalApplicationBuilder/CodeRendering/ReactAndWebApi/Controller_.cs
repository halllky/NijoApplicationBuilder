using HalApplicationBuilder.CodeRendering.EFCore;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.ReactAndWebApi {
    partial class Controller : ITemplate {
        internal static IEnumerable<Controller> All(CodeRenderingContext ctx) {
            return ctx.Schema
                .ToEFCoreGraph()
                .RootEntities()
                .Select(dbEntity => new Controller(dbEntity, ctx));
        }

        internal Controller(GraphNode<EFCoreEntity> dbEntity, CodeRenderingContext ctx) {
            _ctx = ctx;
            _agg = dbEntity.GetCorrespondingAggregate();
            _dbEntity = dbEntity;
        }

        private readonly CodeRenderingContext _ctx;
        private readonly GraphNode<Aggregate> _agg;
        private readonly GraphNode<EFCoreEntity> _dbEntity;

        private string AggregateInstance => _dbEntity.GetUiInstance().Item.ClassName;

        public string FileName => $"{_agg.Item.DisplayName.ToFileNameSafe()}Controller.cs";
        internal string ClassName => $"{_agg.Item.DisplayName.ToCSharpSafe()}Controller";

        private string Search => new Search.MethodInfo(_dbEntity, _ctx).MethodName;
        private string SearchArgType => new Search.MethodInfo(_dbEntity, _ctx).ArgType;

        private string Find => new Find.MethodInfo(_dbEntity, _ctx).MethodName;

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
