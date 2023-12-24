using Nijo.Core;
using Nijo.DotnetEx;
using static Nijo.Util.CodeGenerating.TemplateTextHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nijo.Features.Searching;
using Nijo.Util.CodeGenerating;

namespace Nijo.Features.InstanceHandling {
    internal class CreateFeature {
        internal CreateFeature(GraphNode<Aggregate> aggregate) {
            _aggregate = aggregate;
        }

        private readonly GraphNode<Aggregate> _aggregate;

        internal string ArgType => _aggregate.Item.ClassName;
        internal string MethodName => $"Create{_aggregate.Item.DisplayName.ToCSharpSafe()}";

        internal string RenderController() {
            var controller = new WebClient.Controller(_aggregate.Item);
            var param = new AggregateCreateCommand(_aggregate);

            return $$"""
                [HttpPost("{{WebClient.Controller.CREATE_ACTION_NAME}}")]
                public virtual IActionResult Create([FromBody] {{param.ClassName}} param) {
                    if (_applicationService.{{MethodName}}(param, out var created, out var errors)) {
                        return this.JsonContent(created);
                    } else {
                        return BadRequest(this.JsonContent(errors));
                    }
                }
                """;
        }

        internal string RenderAppSrvMethod() {
            var appSrv = new ApplicationService();
            var controller = new WebClient.Controller(_aggregate.Item);
            var param = new AggregateCreateCommand(_aggregate);
            var find = new FindFeature(_aggregate);

            var searchKeys = _aggregate
                .GetKeys()
                .Where(m => m is AggregateMember.ValueMember)
                .Select(m => $"dbEntity.{m.GetFullPath().Join(".")}");

            return $$"""
                public virtual bool {{MethodName}}({{param.ClassName}} command, out {{_aggregate.Item.ClassName}} created, out ICollection<string> errors) {
                    var dbEntity = command.{{AggregateDetail.TO_DBENTITY}}();
                    {{appSrv.DbContext}}.Add(dbEntity);

                    try {
                        {{appSrv.DbContext}}.SaveChanges();
                    } catch (DbUpdateException ex) {
                        created = new {{_aggregate.Item.ClassName}}();
                        errors = ex.GetMessagesRecursively("  ").ToList();
                        return false;
                    }

                    var afterUpdate = this.{{find.FindMethodName}}({{searchKeys.Join(", ")}});
                    if (afterUpdate == null) {
                        created = new {{_aggregate.Item.ClassName}}();
                        errors = new[] { "更新後のデータの再読み込みに失敗しました。" };
                        return false;
                    }

                    created = afterUpdate;
                    errors = new List<string>();
                    return true;
                }
                """;
        }
    }
}
