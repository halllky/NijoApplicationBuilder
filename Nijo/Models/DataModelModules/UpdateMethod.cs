using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Parts.CSharp;
using Nijo.Util.DotnetEx;
using Nijo.ValueMemberTypes;
using Nijo.Parts.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nijo.Models.DataModelModules {
    /// <summary>
    /// 更新処理
    /// </summary>
    internal class UpdateMethod {
        internal UpdateMethod(RootAggregate rootAggregate) {
            _rootAggregate = rootAggregate;
        }
        private readonly RootAggregate _rootAggregate;

        internal string MethodName => $"Update{_rootAggregate.PhysicalName}Async";
        internal string OnBeforeMethodName => $"OnBeforeUpdate{_rootAggregate.PhysicalName}";
        internal string OnAfterMethodName => $"OnAfterUpdate{_rootAggregate.PhysicalName}Async";

        internal string Render(CodeRenderingContext ctx) {
            var command = new SaveCommand(_rootAggregate, SaveCommand.E_Type.Update);
            var dbEntity = new EFCoreEntity(_rootAggregate);
            var messages = new SaveCommandMessageContainer(_rootAggregate);

            var keys = _rootAggregate
                .GetKeyVMs()
                .Select((vm, i) => {
                    var fullpath = vm.GetPathFromEntry().ToArray();
                    return new {
                        TempVarName = $"searchKey{i + 1}",
                        vm.PhysicalName,
                        vm.DisplayName,
                        VmType = vm.Type,
                        LogTemplate = $"{vm.DisplayName.Replace("\"", "\\\"")}: {{key{i}}}",
                        SaveCommandFullPath = fullpath.AsSaveCommand().ToArray(),
                        SaveCommandMessageFullPath = fullpath.AsSaveCommandMessage().ToArray(),
                        DbEntityFullPath = fullpath.AsDbEntity().ToArray(),
                    };
                })
                .ToArray();

            var hasSequence = _rootAggregate
                .EnumerateThisAndDescendants()
                .SelectMany(agg => agg.GetMembers())
                .Any(member => member is ValueMember vm && vm.Type is SequenceMember);

            return $$"""
                #region 更新処理
                /// <summary>
                /// {{_rootAggregate.DisplayName}} の更新を実行します。
                /// </summary>
                public virtual async Task {{MethodName}}({{command.CsClassNameUpdate}} command, {{messages.InterfaceName}} messages, {{PresentationContext.INTERFACE}} context) {

                    // 更新に必要な項目が空の場合は処理中断
                    var keyIsEmpty = false;
                {{keys.SelectTextTemplate(vm => $$"""
                    if (command.{{vm.SaveCommandFullPath.Join("?.")}} == null) {
                        keyIsEmpty = true;
                        messages.{{vm.SaveCommandMessageFullPath.Join(".")}}.AddError({{MsgFactory.MSG}}.{{ERR_KEY_IS_EMPTY}}("{{vm.DisplayName.Replace("\"", "\\\"")}}"));
                    }
                """)}}
                    if (keyIsEmpty) {
                        Log.Debug("{{_rootAggregate.DisplayName.Replace("\"", "\\\"")}}更新で主キー空エラーが発生したデータ: {0}", command.ToJson());
                        return;
                    }

                    // 更新前データ取得
                {{keys.SelectTextTemplate(vm => $$"""
                    var {{vm.TempVarName}} = {{vm.VmType.RenderCastToPrimitiveType()}}command.{{vm.SaveCommandFullPath.Join("!.")}};
                """)}}

                    var beforeDbEntity = DbContext.{{dbEntity.DbSetName}}
                        .AsNoTracking()
                {{dbEntity.RenderInclude().SelectTextTemplate(source => $$"""
                        {{source}}
                """)}}
                        .SingleOrDefault(e {{WithIndent(keys.SelectTextTemplate((vm, i) => $$"""
                                           {{(i == 0 ? "=>" : "&&")}} e.{{vm.DbEntityFullPath.Join("!.")}} == {{vm.TempVarName}}
                                           """), "                           ")}});

                    if (beforeDbEntity == null) {
                        messages.AddError({{MsgFactory.MSG}}.{{ERR_DATA_NOT_FOUND}}());
                        Log.Debug("{{_rootAggregate.DisplayName.Replace("\"", "\\\"")}}更新で更新対象が見つからないエラーが発生したデータ: {0}", command.ToJson());
                        return;
                    }

                    var afterDbEntity = command.{{SaveCommand.TO_DBENTITY}}();

                    // 自動的に登録される項目
                    afterDbEntity.{{EFCoreEntity.VERSION}}++;
                    afterDbEntity.{{EFCoreEntity.CREATED_AT}} = beforeDbEntity.{{EFCoreEntity.CREATED_AT}};
                    afterDbEntity.{{EFCoreEntity.UPDATED_AT}} = {{ApplicationService.CURRENT_TIME}};
                    afterDbEntity.{{EFCoreEntity.CREATE_USER}} = beforeDbEntity.{{EFCoreEntity.CREATE_USER}};
                    afterDbEntity.{{EFCoreEntity.UPDATE_USER}} = {{ApplicationService.CURRENT_USER}};

                    // 更新前処理。入力検証や自動補完項目の設定を行なう。
                    {{CheckRequired.METHOD_NAME}}(afterDbEntity, messages);
                    {{CheckMaxLength.METHOD_NAME}}(afterDbEntity, messages);
                    {{CheckCharacterType.METHOD_NAME}}(afterDbEntity, messages);
                    {{CheckDigitsAndScales.METHOD_NAME}}(afterDbEntity, messages);
                    {{DynamicEnum.METHOD_NAME}}(afterDbEntity, messages);
                    {{OnBeforeMethodName}}(command, beforeDbEntity, messages, context);

                    // エラーがある場合は処理中断
                    if (messages.HasError()) {
                        // 単なる必須入力漏れなどでもエラーログが出過ぎてしまうのを防ぐため、
                        // IgnoreConfirmがtrueのとき（==更新を確定するつもりのとき）のみ内容をログ出力する
                        if (context.Options.IgnoreConfirm) {
                            Log.Debug("{{_rootAggregate.DisplayName.Replace("\"", "\\\"")}}更新で入力エラーが発生した登録内容(JSON): {0}", command.ToJson());
                        }
                        return;
                    }

                    // 「更新しますか？」の確認メッセージが承認される前の1巡目はエラーチェックのみで処理中断
                    if (!context.Options.IgnoreConfirm) return;

                    if (DbContext.Database.CurrentTransaction == null) throw new InvalidOperationException("トランザクションが開始されていません。");

                {{If(hasSequence, () => $$"""
                    // シーケンス項目
                    {{SequenceMember.SET_METHOD}}(afterDbEntity);

                """)}}
                    // 更新実行
                    const string SAVE_POINT = "SAVE_POINT"; // 更新後処理でエラーが発生した場合はこのデータの更新のみロールバックする
                    try {
                        var entry = DbContext.Entry(afterDbEntity);
                        entry.State = EntityState.Modified;
                        entry.Property(e => e.{{EFCoreEntity.VERSION}}).OriginalValue = command.{{SaveCommand.VERSION}};

                {{RenderDescendantAttaching(_rootAggregate).SelectTextTemplate(source => $$"""
                        {{WithIndent(source, "        ")}}

                """)}}
                        await DbContext.Database.CurrentTransaction.CreateSavepointAsync(SAVE_POINT);
                        await DbContext.SaveChangesAsync();

                    } catch (DbUpdateException ex) {
                        await DbContext.Database.CurrentTransaction.RollbackToSavepointAsync(SAVE_POINT);

                        // 後続処理に影響が出るのを防ぐためエンティティを解放
                        DbContext.Entry(afterDbEntity).State = EntityState.Detached;
                {{RenderDescendantDetaching(_rootAggregate, "afterDbEntity").SelectTextTemplate(source => $$"""
                        {{WithIndent(source, "        ")}}
                """)}}

                        if (ex is DbUpdateConcurrencyException) {
                            messages.AddError({{MsgFactory.MSG}}.{{ERR_CONCURRENCY}}());
                            Log.Warn("{{_rootAggregate.DisplayName.Replace("\"", "\\\"")}}更新で楽観排他エラー: {0}", command.ToJson());

                        } else {
                            messages.AddError({{MsgFactory.MSG}}.{{ERR_ID_UNKNOWN}}(ex.Message));
                            Log.Error(ex);
                            Log.Debug("{{_rootAggregate.DisplayName.Replace("\"", "\\\"")}}更新でSQL発行時エラーが発生した登録内容(JSON): {0}", command.ToJson());
                        }
                        return;
                    }

                    // 更新後処理
                    try {
                        await {{OnAfterMethodName}}(afterDbEntity, beforeDbEntity, messages, context);

                        // 後続処理に影響が出るのを防ぐためエンティティを解放
                        DbContext.Entry(afterDbEntity).State = EntityState.Detached;
                {{RenderDescendantDetaching(_rootAggregate, "afterDbEntity").SelectTextTemplate(source => $$"""
                        {{WithIndent(source, "        ")}}
                """)}}

                        // セーブポイント解放
                        await DbContext.Database.CurrentTransaction.ReleaseSavepointAsync(SAVE_POINT);

                    } catch (Exception ex) {
                        messages.AddError({{MsgFactory.MSG}}.{{ERR_ID_UNKNOWN}}(ex.Message));
                        Log.Error(ex);
                        Log.Debug("{{_rootAggregate.DisplayName.Replace("\"", "\\\"")}}更新後エラーが発生した登録内容(JSON): {0}", command.ToJson());
                        await DbContext.Database.CurrentTransaction.RollbackToSavepointAsync(SAVE_POINT);
                        return;
                    }

                    Log.Info("{{_rootAggregate.DisplayName.Replace("\"", "\\\"")}}データを更新しました。（{{keys.Select(x => x.LogTemplate).Join(", ")}}）", {{keys.Select(x => $"afterDbEntity.{x.DbEntityFullPath.Join("?.")}").Join(", ")}});
                    Log.Debug("{{_rootAggregate.DisplayName.Replace("\"", "\\\"")}} 更新パラメータ: {0}", command.ToJson());
                }
                /// <summary>
                /// {{_rootAggregate.DisplayName}} の更新の確定前に実行される処理。
                /// 自動生成されないエラーチェックはここで実装する。
                /// エラーがあった場合、第3引数のメッセージにエラー内容を格納する。
                /// </summary>
                public virtual void {{OnBeforeMethodName}}({{command.CsClassNameUpdate}} command, {{dbEntity.CsClassName}} oldValue, {{messages.InterfaceName}} messages, {{PresentationContext.INTERFACE}} context) {
                    // このメソッドをオーバーライドして処理を実装してください。
                }
                /// <summary>
                /// {{_rootAggregate.DisplayName}} の更新のSQL発行後、コミット前に実行される処理。
                /// このメソッドの中で例外が送出された場合、{{_rootAggregate.DisplayName}} の更新はロールバックされる。
                /// このメソッドで実装される想定としているものの例は以下。
                /// <list>
                /// <item>{{_rootAggregate.DisplayName}}と常に同期していなければならないリードレプリカの更新</item>
                /// <item>{{_rootAggregate.DisplayName}}と常に同期していなければならない外部リソースの更新やメッセージング</item>
                /// </list>
                /// </summary>
                public virtual Task {{OnAfterMethodName}}({{dbEntity.CsClassName}} newValue, {{dbEntity.CsClassName}} oldValue, {{messages.InterfaceName}} messages, {{PresentationContext.INTERFACE}} context) {
                    // このメソッドをオーバーライドして処理を実装してください。
                    return Task.CompletedTask;
                }
                #endregion 更新処理
                """;
        }


        #region Create/Update/Deleleで共通
        internal const string ERR_ID_UNKNOWN = "UnknownUpdateError";
        internal const string ERR_KEY_IS_EMPTY = "KeyEmptyError";
        internal const string ERR_DATA_NOT_FOUND = "DataNotFoundError";
        internal const string ERR_CONCURRENCY = "ConcurrencyError";

        internal static void RegisterCommonParts(CodeRenderingContext ctx) {
            ctx.Use<MsgFactory>()
                .AddMessage(ERR_ID_UNKNOWN,
                            "登録/更新/削除のタイミングでRDBMS上で何らかのエラーが生じた場合のメッセージ",
                            "登録処理でエラーが発生しました: {0}")
                .AddMessage(ERR_KEY_IS_EMPTY,
                            "更新または削除で対象の主キーが指定されていない場合のメッセージ",
                            "{0}が空です。")
                .AddMessage(ERR_DATA_NOT_FOUND,
                            "更新対象・削除対象のデータがデータベース上で見つからなかったときのメッセージ",
                            "更新対象のデータが見つかりません。")
                .AddMessage(ERR_CONCURRENCY,
                            "楽観排他制御に引っかかったときのメッセージ",
                            "ほかのユーザーが更新しました。");
        }

        /// <summary>
        /// 子孫要素をDbContextにアタッチするソースをレンダリングする。
        /// </summary>
        internal static IEnumerable<string> RenderDescendantAttaching(RootAggregate rootAggregate) {
            var descendantDbEntities = rootAggregate.EnumerateDescendants().ToArray();

            for (int i = 0; i < descendantDbEntities.Length; i++) {
                var builder = new StringBuilder();
                var paths = descendantDbEntities[i].GetPathFromEntry().ToArray();
                var before = $"before{descendantDbEntities[i].PhysicalName}_{i}";
                var after_ = $"after{descendantDbEntities[i].PhysicalName}_{i}";

                // before, after それぞれの子孫インスタンスを一次配列に格納する
                void RenderEntityArray(bool renderBefore) {
                    var tempVar = renderBefore ? before : after_;

                    if (paths.Any(node => node is ChildrenAggregate)) {
                        // 子集約までの経路の途中に配列が含まれる場合
                        builder.Append(renderBefore
                            ? $"var {tempVar} = beforeDbEntity"
                            : $"var {tempVar} = afterDbEntity");

                        var select = false;
                        foreach (var node in paths) {
                            // Children
                            if (node is ChildrenAggregate children) {
                                var nav = new EFCoreEntity.NavigationOfParentChild((AggregateBase?)node.PreviousNode ?? throw new InvalidOperationException("ありえない"), children);
                                builder.Append(select
                                    ? $".SelectMany(x => x.{nav.Principal.OtherSidePhysicalName})"
                                    : $".{nav.Principal.OtherSidePhysicalName}");
                                select = true;
                                continue;
                            }
                            // Child
                            if (node is ChildAggregate child) {
                                var nav = new EFCoreEntity.NavigationOfParentChild((AggregateBase?)node.PreviousNode ?? throw new InvalidOperationException("ありえない"), child);
                                builder.Append(select
                                    ? $".Select(x => x.{nav.Principal.OtherSidePhysicalName})"
                                    : $".{nav.Principal.OtherSidePhysicalName}");
                                continue;
                            }
                        }

                        var dbEntity = new EFCoreEntity(descendantDbEntities[i]);
                        builder.AppendLine($$"""
                            .OfType<{{dbEntity.CsClassName}}>() ?? Enumerable.Empty<{{dbEntity.CsClassName}}>();
                            """);

                    } else {
                        // 子集約までの経路の途中に配列が含まれない場合
                        var source = renderBefore ? "beforeDbEntity" : "afterDbEntity";
                        var dbEntity = new EFCoreEntity(descendantDbEntities[i]);
                        builder.AppendLine($$"""
                            var {{tempVar}} = new {{dbEntity.CsClassName}}?[] {
                                {{source}}.{{paths.AsDbEntity().Join(".")}},
                            }.OfType<{{dbEntity.CsClassName}}>().ToArray();
                            """);
                    }
                }
                RenderEntityArray(true);
                RenderEntityArray(false);

                // ChangeState変更
                builder.AppendLine($$"""
                    foreach (var a in {{after_}}) {
                        var b = {{before}}.SingleOrDefault(b => b.{{EFCoreEntity.KEYEQUALS}}(a));
                        if (b == null) {
                            DbContext.Entry(a).State = EntityState.Added;
                        } else {
                            DbContext.Entry(a).State = EntityState.Modified;
                        }
                    }
                    foreach (var b in {{before}}) {
                        var a = {{after_}}.SingleOrDefault(a => a.{{EFCoreEntity.KEYEQUALS}}(b));
                        if (a == null) {
                            DbContext.Entry(b).State = EntityState.Deleted;
                        }
                    }
                    """);

                yield return builder.ToString();
            }
        }

        /// <summary>
        /// 子孫要素の EntityState を全てDetachにしていくソースをレンダリングする。
        /// </summary>
        internal static IEnumerable<string> RenderDescendantDetaching(RootAggregate rootAggregate, string rootEntityName) {
            var descendantDbEntities = rootAggregate.EnumerateDescendants().ToArray();

            for (int i = 0; i < descendantDbEntities.Length; i++) {
                var builder = new StringBuilder();
                var paths = descendantDbEntities[i].GetPathFromEntry().Skip(1).ToArray();
                var after_ = $"after{descendantDbEntities[i].PhysicalName}_{i}";

                // before, after それぞれの子孫インスタンスを一次配列に格納する
                void RenderEntityArray() {
                    var tempVar = after_;

                    if (paths.Any(path => path is ChildrenAggregate)) {
                        // 子集約までの経路の途中に配列が含まれる場合
                        builder.Append($"var {tempVar} = {rootEntityName}");

                        var select = false;
                        foreach (var node in paths) {
                            // Children
                            if (node is ChildrenAggregate children) {
                                var nav = new EFCoreEntity.NavigationOfParentChild((AggregateBase?)node.PreviousNode ?? throw new InvalidOperationException("ありえない"), children);
                                builder.Append(select
                                    ? $".SelectMany(x => x.{nav.Principal.OtherSidePhysicalName})"
                                    : $".{nav.Principal.OtherSidePhysicalName}");
                                select = true;
                                continue;
                            }
                            // Child
                            if (node is ChildAggregate child) {
                                var nav = new EFCoreEntity.NavigationOfParentChild((AggregateBase?)node.PreviousNode ?? throw new InvalidOperationException("ありえない"), child);
                                builder.Append(select
                                    ? $".Select(x => x.{nav.Principal.OtherSidePhysicalName})"
                                    : $".{nav.Principal.OtherSidePhysicalName}");
                                continue;
                            }

                            throw new InvalidOperationException("子孫列挙なのでChildかChildrenしかありえない");
                        }

                        var efCoreEntity = new EFCoreEntity(descendantDbEntities[i]);
                        builder.AppendLine($".OfType<{efCoreEntity.CsClassName}>() ?? Enumerable.Empty<{efCoreEntity.CsClassName}>();");

                    } else {
                        // 子集約までの経路の途中に配列が含まれない場合
                        var efCoreEntity = new EFCoreEntity(descendantDbEntities[i]);
                        var childPath = paths.Select(node => new EFCoreEntity.NavigationOfParentChild(
                            (AggregateBase?)node.PreviousNode ?? throw new InvalidOperationException("ありえない"),
                            (AggregateBase?)node ?? throw new InvalidOperationException("ありえない")));
                        builder.AppendLine($$"""
                            var {{tempVar}} = new {{efCoreEntity.CsClassName}}?[] {
                                {{rootEntityName}}.{{childPath.Select(p => p.Principal.OtherSidePhysicalName).Join("?.")}},
                            }.OfType<{{efCoreEntity.CsClassName}}>().ToArray();
                            """);
                    }
                }
                RenderEntityArray();

                // ChangeState変更
                builder.AppendLine($$"""
                    foreach (var a in {{after_}}) {
                        DbContext.Entry(a).State = EntityState.Detached;
                    }
                    """);

                yield return builder.ToString();
            }
        }
        #endregion Create/Update/Deleleで共通
    }
}
