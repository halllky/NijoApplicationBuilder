using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.EFCore {
    partial class Find : ITemplate {
        internal Find(CodeRenderingContext ctx) {
            _ctx = ctx;
        }
        private readonly CodeRenderingContext _ctx;

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
                _instance = dbEntity.GetUiInstance().Item;
                _ctx = ctx;
            }

            private readonly GraphNode<EFCoreEntity> _dbEntity;
            private readonly AggregateInstance _instance;
            private readonly CodeRenderingContext _ctx;

            internal string ReturnType => _instance.ClassName;
            internal string MethodName => $"Find{_dbEntity.GetCorrespondingAggregate().Item.DisplayName.ToCSharpSafe()}";
            internal string DbSetName => _dbEntity.Item.DbSetName;
            internal string AggregateInstanceTypeFullName => $"{_ctx.Config.RootNamespace}.{_instance.ClassName}";

            internal IEnumerable<string> Include() {
                return IncludeRecursively(_dbEntity);
            }
            private static IEnumerable<string> IncludeRecursively(GraphNode<EFCoreEntity> entity) {
                var includes = entity.GetChildMembers()
                    .Concat(entity.GetChildrenMembers())
                    .Concat(entity.GetVariationMembers())
                    .Concat(entity.GetRefMembers());
                foreach (var edge in includes) {
                    var path = edge.Source
                        .PathFromEntry()
                        .Select(edge => $".{edge.RelationName}")
                        .Join("");
                    yield return $".Include(x => x{path}.{edge.RelationName})";

                    // 再帰処理
                    if (!edge.IsRef()) {
                        foreach (var descendant in IncludeRecursively(edge.Terminal.As<EFCoreEntity>())) {
                            yield return descendant;
                        }
                    }
                }
            }
        }
    }
}
