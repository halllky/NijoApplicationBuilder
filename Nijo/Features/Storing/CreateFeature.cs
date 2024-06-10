using Nijo.Core;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nijo.Parts;
using Nijo.Parts.WebServer;
using Nijo.Util.CodeGenerating;
using Nijo.Models;

namespace Nijo.Features.Storing {
    internal class CreateFeature {
        internal CreateFeature(GraphNode<Aggregate> aggregate) {
            _aggregate = aggregate;
        }

        private readonly GraphNode<Aggregate> _aggregate;

        internal string ArgType => new AggregateCreateCommand(_aggregate).CsClassName;
        internal string MethodName => $"Create{_aggregate.Item.DisplayName.ToCSharpSafe()}";

        internal string RenderController() {
            var controller = new Parts.WebClient.Controller(_aggregate.Item);
            var param = new AggregateCreateCommand(_aggregate);

            return $$"""
                [HttpPost("{{Parts.WebClient.Controller.CREATE_ACTION_NAME}}")]
                public virtual IActionResult Create([FromBody] {{param.CsClassName}} param) {
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
            var controller = new Parts.WebClient.Controller(_aggregate.Item);
            var forSave = new DataClassForSave(_aggregate);
            var forDisplay = new DataClassForDisplay(_aggregate);
            var param = new AggregateCreateCommand(_aggregate);
            var find = new FindFeature(_aggregate);

            var searchKeys = _aggregate
                .GetKeys()
                .Where(m => m is AggregateMember.ValueMember)
                .Select(m => $"dbEntity.{m.GetFullPath().Join(".")}");

            var customize = new Customize(_aggregate);

            return $$"""
                public virtual bool {{MethodName}}({{param.CsClassName}} command, out {{forDisplay.CsClassName}} created, out ICollection<string> errors) {

                    var beforeSaveEventArg = new BeforeCreateEventArgs<{{param.CsClassName}}> {
                        Data = command,
                        IgnoreConfirm = false, // TODO: ワーニングの仕組みを作る
                    };
                    {{customize.CreatingMethodName}}(beforeSaveEventArg);
                    if (beforeSaveEventArg.Errors.Count > 0) {
                        created = new();
                        errors = beforeSaveEventArg.Errors.Select(err => err.Message).ToArray();
                        return false;
                    }
                    if (beforeSaveEventArg.Confirms.Count > 0) {
                        created = new();
                        errors = beforeSaveEventArg.Errors.Select(err => err.Message).ToArray(); // TODO: ワーニングの仕組みを作る
                        return false;
                    }

                    var dbEntity = command.{{DataClassForSave.TO_DBENTITY}}();
                    {{appSrv.DbContext}}.Add(dbEntity);

                    try {
                        {{appSrv.DbContext}}.SaveChanges();
                    } catch (DbUpdateException ex) {
                        created = new {{forDisplay.CsClassName}}();
                        errors = ex.GetMessagesRecursively("  ").ToList();
                        return false;
                    }

                    var afterUpdate = this.{{find.FindMethodName}}({{searchKeys.Join(", ")}});
                    if (afterUpdate == null) {
                        created = new {{forDisplay.CsClassName}}();
                        errors = new[] { "更新後のデータの再読み込みに失敗しました。" };
                        return false;
                    }

                    var afterSaveEventArg = new AfterCreateEventArgs<{{param.CsClassName}}>  {
                        Created = command,
                    };
                    {{customize.CreatedMethodName}}(afterSaveEventArg);

                    created = afterUpdate;
                    errors = new List<string>();

                    return true;
                }

                /// <summary>
                /// {{_aggregate.Item.DisplayName}}の新規登録前に実行されます。
                /// エラーチェック、ワーニング、自動算出項目の設定などを行います。
                /// </summary>
                protected virtual void {{customize.CreatingMethodName}}({{Customize.BEFORE_CREATE_EVENT_ARGS}}<{{param.CsClassName}}> arg) { }
                /// <summary>
                /// {{_aggregate.Item.DisplayName}}の新規登録SQL発効後、コミット前に実行されます。
                /// </summary>
                protected virtual void {{customize.CreatedMethodName}}({{Customize.AFTER_CREATE_EVENT_ARGS}}<{{param.CsClassName}}> arg) { }

                """;
        }
    }
}
