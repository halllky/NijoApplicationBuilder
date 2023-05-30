using HalApplicationBuilder.Core20230514;
using HalApplicationBuilder.DotnetEx;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering20230514.ReactAndWebApi {
    partial class ComboBox : ITemplate {
        internal static IEnumerable<ComboBox> All(CodeRenderingContext ctx) {
            return ctx.Schema
                .ToEFCoreGraph()
                .RootEntities()
                .Select(dbEntity => new ComboBox(dbEntity, ctx));
        }

        internal ComboBox(GraphNode<EFCoreEntity> dbEntity, CodeRenderingContext ctx) {
            _ctx = ctx;
            Aggregate = dbEntity.Item.Aggregate;
            _dbEntity = dbEntity;
            _searchCondition = new SearchCondition(dbEntity);
            _searchResult = new SearchResult(dbEntity);
            _controller = new Controller(dbEntity, ctx);
        }

        private readonly CodeRenderingContext _ctx;
        private readonly GraphNode<EFCoreEntity> _dbEntity;
        private readonly SearchCondition _searchCondition;
        private readonly SearchResult _searchResult;
        private readonly Controller _controller;

        internal GraphNode<Aggregate> Aggregate { get; }

        public string FileName => $"{Aggregate.Item.DisplayName.ToFileNameSafe()}_ComboBox.tsx";
        internal string ImportName => Path.GetFileNameWithoutExtension(FileName);

        internal string ComponentName => $"ComboBox{Aggregate.Item.DisplayName.ToCSharpSafe()}";

        internal string UseQueryKey => $"combo-{Aggregate.Item.UniqueId}";
        internal string Api => new Controller(_dbEntity, _ctx).KeywordSearchCommandApi;
    }
}
