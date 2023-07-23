using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.ReactAndWebApi {
    partial class ComboBox : ITemplate {
        internal static IEnumerable<ComboBox> All(CodeRenderingContext ctx) {
            return ctx.Schema
                .ToEFCoreGraph()
                .RootEntities()
                .Select(dbEntity => new ComboBox(dbEntity, ctx));
        }

        internal ComboBox(GraphNode<EFCoreEntity> dbEntity, CodeRenderingContext ctx) {
            _ctx = ctx;
            Aggregate = dbEntity.GetCorrespondingAggregate();
            _dbEntity = dbEntity;
            _searchCondition = new SearchCondition(dbEntity);
            _searchResult = new SearchResult(dbEntity);
            _controller = new AggFile.Controller(dbEntity.GetCorrespondingAggregate());
        }

        private readonly CodeRenderingContext _ctx;
        private readonly GraphNode<EFCoreEntity> _dbEntity;
        private readonly SearchCondition _searchCondition;
        private readonly SearchResult _searchResult;
        private readonly AggFile.Controller _controller;

        internal GraphNode<Aggregate> Aggregate { get; }

        public string FileName => $"{Aggregate.Item.DisplayName.ToFileNameSafe()}_ComboBox.tsx";
        internal string ImportName => Path.GetFileNameWithoutExtension(FileName);

        internal string ComponentName => $"ComboBox{Aggregate.Item.DisplayName.ToCSharpSafe()}";

        internal string UseQueryKey => $"combo-{Aggregate.Item.UniqueId}";
        internal string Api => new AggFile.Controller(_dbEntity.GetCorrespondingAggregate()).KeywordSearchCommandApi;
    }
}
