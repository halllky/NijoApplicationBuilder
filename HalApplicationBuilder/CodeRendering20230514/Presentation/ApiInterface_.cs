using HalApplicationBuilder.Core20230514;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering20230514.Presentation {
    partial class ApiInterface : ITemplate {

        internal ApiInterface(CodeRenderingContext ctx) {
            _ctx = ctx;
        }
        private readonly CodeRenderingContext _ctx;

        public string FileName => "Api.cs";
        private const string E = "e";

        private void ToDbEntity(GraphNode<EFCoreEntity> dbEntity) {
            var instance = new AggregateInstance(dbEntity);
            if (dbEntity.Source == null) {
                WriteLine($"return new {_ctx.Config.EntityNamespace}.{dbEntity.Item.ClassName} {{");
            } else {
                WriteLine($"{dbEntity.Source.RelationName.ToCSharpSafe()} = new {instance.ClassName} {{");
            }

            // 自身のメンバー
            var path = new[] { E }.Concat(dbEntity.PathFromEntry().Select(x => x.RelationName.ToCSharpSafe())).Join(".");
            foreach (var member in instance.GetMembers()) {
                WriteLine($"    {member.PropertyName} = {path}.{member.CorrespondingDbColumn.PropertyName},");
            }

            // 子集約
            var children = dbEntity.GetChildMembers()
                .Concat(dbEntity.GetChildrenMembers())
                .Concat(dbEntity.GetVariationMembers());
            foreach (var edge in children) {
                PushIndent("    ");
                FromDbEntity(edge.Terminal);
                PopIndent();
            }

            // 参照
            // TODO

            if (dbEntity.Source == null)
                WriteLine($"}};");
            else
                WriteLine($"}},");
        }

        private void FromDbEntity(GraphNode<EFCoreEntity> dbEntity) {
            var instance = new AggregateInstance(dbEntity);
            if (dbEntity.Source == null) {
                WriteLine($"return new {instance.ClassName} {{");
            } else {
                WriteLine($"{dbEntity.Source.RelationName.ToCSharpSafe()} = new {instance.ClassName} {{");
            }

            // 自身のメンバー
            var path = new[] { E }.Concat(dbEntity.PathFromEntry().Select(x => x.RelationName.ToCSharpSafe())).Join(".");
            foreach (var member in instance.GetMembers()) {
                WriteLine($"    {member.PropertyName} = {path}.{member.CorrespondingDbColumn.PropertyName},");
            }

            // 子集約
            var children = dbEntity.GetChildMembers()
                .Concat(dbEntity.GetChildrenMembers())
                .Concat(dbEntity.GetVariationMembers());
            foreach (var edge in children) {
                PushIndent("    ");
                FromDbEntity(edge.Terminal);
                PopIndent();
            }

            // 参照
            // TODO

            if (dbEntity.Source == null)
                WriteLine($"}};");
            else
                WriteLine($"}},");
        }
    }
}
