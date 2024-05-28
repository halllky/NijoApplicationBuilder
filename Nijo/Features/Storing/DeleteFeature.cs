using Nijo.Core;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nijo.Parts.WebServer;
using Nijo.Util.CodeGenerating;
using Nijo.Models;

namespace Nijo.Features.Storing {
    internal class DeleteFeature {
        internal DeleteFeature(GraphNode<Aggregate> aggregate) {
            _aggregate = aggregate;
        }

        private readonly GraphNode<Aggregate> _aggregate;

        internal string ArgType => new DataClassForSave(_aggregate).CsClassName;
        internal string MethodName => $"Delete{_aggregate.Item.PhysicalName}";

        internal string RenderController() {
            var dataClass = new DataClassForSave(_aggregate);

            return $$"""
                [HttpDelete("{{Parts.WebClient.Controller.DELETE_ACTION_NAME}}")]
                public virtual IActionResult Delete({{dataClass.CsClassName}} param) {
                    if (_applicationService.{{MethodName}}(param, out var errors)) {
                        return Ok();
                    } else {
                        return BadRequest(this.JsonContent(errors));
                    }
                }
                """;
        }

        internal string RenderAppSrvMethod() {
            var appSrv = new ApplicationService();
            var controller = new Parts.WebClient.Controller(_aggregate.Item);
            var dataClass = new DataClassForSave(_aggregate);
            var searchKeys = _aggregate
                .GetKeys()
                .OfType<AggregateMember.ValueMember>()
                .Select(vm => $"data.{vm.Declared.GetFullPath().Join(".")}")
                .ToArray();

            return $$"""
                public virtual bool {{MethodName}}({{dataClass.CsClassName}} data, out ICollection<string> errors) {

                    {{WithIndent(FindFeature.RenderDbEntityLoading(
                        _aggregate,
                        appSrv.DbContext,
                        "entity",
                        searchKeys,
                        tracks: true,
                        includeRefs: true), "    ")}}

                    if (entity == null) {
                        errors = new[] { "削除対象のデータが見つかりません。" };
                        return false;
                    }

                    var deleted = {{dataClass.CsClassName}}.{{DataClassForSave.FROM_DBENTITY}}(entity);

                    {{appSrv.DbContext}}.Remove(entity);

                    try {
                        {{appSrv.DbContext}}.SaveChanges();
                    } catch (DbUpdateException ex) {
                        errors = ex.GetMessagesRecursively().ToArray();
                        return false;
                    }

                    // // {{_aggregate.Item.DisplayName}}の更新をトリガーとする処理を実行します。
                    // var updateEvent = new AggregateUpdateEvent<{{dataClass.CsClassName}}> {
                    //     Deleted = new[] { deleted },
                    // };

                    errors = Array.Empty<string>();
                    return true;
                }
                """;
        }
    }
}
