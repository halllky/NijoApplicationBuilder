using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.WebClient
{
    partial class ComboBox : ITemplate
    {
        internal ComboBox(GraphNode<Aggregate> aggregate, CodeRenderingContext ctx)
        {
            _ctx = ctx;
            _aggregate = aggregate;
            _dbEntity = aggregate.GetDbEntity().AsEntry();
        }

        private readonly CodeRenderingContext _ctx;
        private readonly GraphNode<Aggregate> _aggregate;
        private readonly GraphNode<EFCoreEntity> _dbEntity;

        public string FileName => $"ComboBox{_aggregate.Item.DisplayName.ToFileNameSafe()}.tsx";

        internal string ComponentName => $"ComboBox{_aggregate.Item.DisplayName.ToCSharpSafe()}";

        internal string UseQueryKey => $"combo-{_aggregate.Item.UniqueId}";
        internal string Api => new AggFile.Controller(_dbEntity.GetCorrespondingAggregate()).KeywordSearchCommandApi;
    }
}
