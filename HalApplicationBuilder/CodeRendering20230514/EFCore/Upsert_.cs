using HalApplicationBuilder.Core20230514;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering20230514.EFCore {
    partial class Upsert {

        internal Upsert(CodeRenderingContext ctx) {
            _ctx = ctx;
        }
        private readonly CodeRenderingContext _ctx;

        internal const string METHOD_NAME = "Upsert";
        private const string ITEM = "item";

        public string FileName => $"{_ctx.Config.DbContextName.ToFileNameSafe()}.Find.cs";

        private IEnumerable<MethodInfo> BuildMethods() {
            return _ctx.Schema
                .ToEFCoreGraph()
                .Where(dbEntity => dbEntity.IsRoot())
                .Select(dbEntity => new MethodInfo(dbEntity, _ctx));
        }

        internal class MethodInfo {
            internal MethodInfo(GraphNode<EFCoreEntity> dbEntity, CodeRenderingContext ctx) {
                _dbEntity = dbEntity;
                _instance = new AggregateInstance(dbEntity);
                _ctx = ctx;
            }

            private readonly GraphNode<EFCoreEntity> _dbEntity;
            private readonly AggregateInstance _instance;
            private readonly CodeRenderingContext _ctx;

            internal string ArgType => AggregateInstanceTypeFullName;
            internal string MethodName => $"Find{_dbEntity.Item.Aggregate.Item.DisplayName.ToCSharpSafe()}";
            internal string DbSetName => _dbEntity.Item.DbSetName;
            internal string AggregateInstanceTypeFullName => $"{_ctx.Config.RootNamespace}.{_instance.ClassName}";
        }
    }
}
