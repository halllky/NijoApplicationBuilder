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

        internal string Render(CodeRenderingContext ctx, IReadOnlyDictionary<SchemaNodeIdentity, InstancePropertyPath> variablePathInfo) {
            var command = new SaveCommand(_rootAggregate, SaveCommand.E_Type.Update);
            var keyCommand = new SaveCommand(_rootAggregate, SaveCommand.E_Type.UpdOrDelKey);
            var dbEntity = new EFCoreEntity(_rootAggregate);
            var messages = new SaveCommandMessageContainer(_rootAggregate);

            var selectKeysLeft = new Variable("e", dbEntity)
                .CreateProperties()
                .ToArray();
            var pkValueCandidates = new Variable("afterDbEntity", dbEntity)
                .CreateProperties()
                .ToArray();
            var keys = _rootAggregate
                .GetKeyVMs()
                .Select((vm, i) => {
                    var fullpath = vm.GetPathFromEntry().ToArray();
                    return new {
                        TempVarName = $"searchKey{i + 1}",
                        vm.DisplayName,
                        VmType = vm.Type,
                        LogTemplate = $"{vm.DisplayName.Replace("\"", "\\\"")}: {{key{i}}}",
                        SaveCommandFullPath = fullpath.AsSaveCommand().ToArray(),
                        SaveCommandMessageFullPath = fullpath.AsSaveCommandMessage().ToArray(),
                        SingleOrDefaultLeft = selectKeysLeft
                            .Single(x => x.Metadata.SchemaPathNode.ToMappingKey() == vm.ToMappingKey())
                            .GetJoinedPathFromInstance(E_CsTs.CSharp, "!."),
                        DbEntityFullPath = pkValueCandidates
                            .Single(x => x.Metadata.SchemaPathNode.ToMappingKey() == vm.ToMappingKey())
                            .GetJoinedPathFromInstance(E_CsTs.CSharp, "?."),
                    };
                })
                .ToArray();

            var xmlComment = $$"""
                /// <summary>
                /// {{_rootAggregate.DisplayName}} の更新を実行します。
                /// </summary>
                /// <param name="key">更新対象の主キーと楽観排他制御用のバージョン。</param>
                /// <param name="updater">更新関数。引数は更新前の値。この関数の中で更新したいプロパティを書き換えてください。非同期処理がある場合は async / await を使用できます。</param>
                /// <param name="context">コンテキスト</param>
                /// <param name="messageOwner">
                /// エラーメッセージを特定の位置に付加したい場合は指定する。
                /// nullの場合はコンテキストのルートに付加される。
                /// </param>
                /// <returns>エラーがあった場合やエラーチェックのみの場合はfalseを、正常終了した場合はtrueと更新後のデータを返す。</returns>
                """;

            return $$"""
                #region 更新処理
                {{xmlComment}}
                public virtual async Task<DataModelSaveResult<{{dbEntity.CsClassName}}>> {{MethodName}}(
                    {{keyCommand.CsClassNameDelete}} key,
                    Func<{{command.CsClassNameUpdate}}, Task> updater,
                    {{PresentationContext.INTERFACE}} context,
                    {{MessageContainer.SETTER_INTERFACE}}? messageOwner = null) {

                    var messages = messageOwner?.As<{{messages.InterfaceName}}>() ?? context.As<{{messages.InterfaceName}}>().Messages;

                    // 更新に必要な項目が空の場合は処理中断
                    var keyIsEmpty = false;
                {{keys.SelectTextTemplate(vm => $$"""
                    if (key.{{vm.SaveCommandFullPath.Join("?.")}} == null) {
                        keyIsEmpty = true;
                        messages.{{vm.SaveCommandMessageFullPath.Join(".")}}.AddError({{MsgFactory.MSG}}.{{ERR_KEY_IS_EMPTY}}("{{vm.DisplayName.Replace("\"", "\\\"")}}"));
                    }
                """)}}
                    if (keyIsEmpty) {
                        Log.LogDebug("{{_rootAggregate.DisplayName.Replace("\"", "\\\"")}}更新で主キー空エラーが発生したデータ: {data}", {{ApplicationService.SERIALIZE_FOR_LOG}}(key));
                        return new(DataModelSaveErrorReason.ValidationError);
                    }

                    // 更新前データ取得
                {{keys.SelectTextTemplate(vm => $$"""
                    var {{vm.TempVarName}} = {{vm.VmType.RenderCastToPrimitiveType()}}key.{{vm.SaveCommandFullPath.Join("!.")}};
                """)}}

                    var beforeDbEntity = await DbContext.{{dbEntity.DbSetName}}
                        .AsNoTracking()
                {{dbEntity.RenderInclude().SelectTextTemplate(source => $$"""
                        {{source}}
                """)}}
                        .SingleOrDefaultAsync(e {{WithIndent(keys.SelectTextTemplate((vm, i) => $$"""
                                                {{(i == 0 ? "=>" : "&&")}} {{vm.SingleOrDefaultLeft}} == {{vm.TempVarName}}
                                                """), "                                ")}})
                        .ConfigureAwait(false);

                    if (beforeDbEntity == null) {
                        messages.AddError({{MsgFactory.MSG}}.{{ERR_DATA_NOT_FOUND}}());
                        Log.LogDebug("{{_rootAggregate.DisplayName.Replace("\"", "\\\"")}}更新で更新対象が見つからないエラーが発生したデータ: {data}", {{ApplicationService.SERIALIZE_FOR_LOG}}(key));
                        return new(DataModelSaveErrorReason.ValidationError);
                    }

                    // 値の書き換え
                    var command = {{command.CsClassNameUpdate}}.{{SaveCommand.FROM_DBENTITY}}(beforeDbEntity);
                    await updater(command);
                    var afterDbEntity = command.{{SaveCommand.TO_DBENTITY}}();

                    // 自動的に登録される項目
                    afterDbEntity.{{EFCoreEntity.VERSION}} = (key.{{SaveCommand.VERSION}} ?? beforeDbEntity.{{EFCoreEntity.VERSION}}) + 1;
                    afterDbEntity.{{EFCoreEntity.CREATED_AT}} = beforeDbEntity.{{EFCoreEntity.CREATED_AT}};
                    afterDbEntity.{{EFCoreEntity.UPDATED_AT}} = {{ApplicationService.CURRENT_TIME}};
                    afterDbEntity.{{EFCoreEntity.CREATE_USER}} = beforeDbEntity.{{EFCoreEntity.CREATE_USER}};
                    afterDbEntity.{{EFCoreEntity.UPDATE_USER}} = {{ApplicationService.CURRENT_USER}};

                    // 更新前処理。入力検証や自動補完項目の設定を行なう。
                {{DataModel.GetValidators(ctx).SelectTextTemplate(validator => $$"""
                    {{validator.RenderCaller(this, _rootAggregate, "afterDbEntity", "messages")}};
                """)}}
                    {{OnBeforeMethodName}}(command, beforeDbEntity, messages, context);

                    // エラーがある場合は処理中断
                    if (messages.GetState()?.DescendantsAndSelf().Any(c => c.Errors.Count > 0) == true) {
                        // 単なる必須入力漏れなどでもエラーログが出過ぎてしまうのを防ぐため、
                        // 更新を確定するつもりのときのみ内容をログ出力する
                        if (!context.ValidationOnly) {
                            Log.LogDebug("{{_rootAggregate.DisplayName.Replace("\"", "\\\"")}}更新で入力エラーが発生した登録内容(JSON): {data}", {{ApplicationService.SERIALIZE_FOR_LOG}}(command));
                        }
                        return new(DataModelSaveErrorReason.ValidationError);
                    }

                    // 「更新しますか？」の確認メッセージが承認される前の1巡目はエラーチェックのみで処理中断
                    if (context.ValidationOnly) return new(true);
                    if (DbContext.Database.CurrentTransaction == null) throw new InvalidOperationException("トランザクションが開始されていません。");

                    // 更新実行
                    const string SAVE_POINT = "SAVE_POINT"; // 更新後処理でエラーが発生した場合はこのデータの更新のみロールバックする
                    try {
                        var entry = DbContext.Entry(afterDbEntity);
                        entry.State = EntityState.Modified;
                        entry.Property(e => e.{{EFCoreEntity.VERSION}}).OriginalValue = key.{{SaveCommand.VERSION}} ?? beforeDbEntity.{{EFCoreEntity.VERSION}};

                {{RenderDescendantAttaching(_rootAggregate, variablePathInfo).SelectTextTemplate(source => $$"""
                        {{WithIndent(source)}}

                """)}}
                        await DbContext.Database.CurrentTransaction.CreateSavepointAsync(SAVE_POINT).ConfigureAwait(false);
                        await DbContext.SaveChangesAsync().ConfigureAwait(false);

                    } catch (DbUpdateException ex) {
                        await DbContext.Database.CurrentTransaction.RollbackToSavepointAsync(SAVE_POINT).ConfigureAwait(false);

                        // 後続処理に影響が出るのを防ぐためエンティティを解放
                        DbContext.Entry(afterDbEntity).State = EntityState.Detached;
                {{RenderDescendantDetaching(_rootAggregate, variablePathInfo, "afterDbEntity").SelectTextTemplate(source => $$"""
                        {{WithIndent(source)}}
                """)}}

                        if (ex is DbUpdateConcurrencyException) {
                            messages.AddError({{MsgFactory.MSG}}.{{ERR_CONCURRENCY}}());
                            Log.LogWarning("{{_rootAggregate.DisplayName.Replace("\"", "\\\"")}}更新で楽観排他エラー: {data}", {{ApplicationService.SERIALIZE_FOR_LOG}}(command));
                            return new(DataModelSaveErrorReason.ConcurrencyError);

                        } else {
                            messages.AddError({{MsgFactory.MSG}}.{{ERR_ID_UNKNOWN}}(ex.Message));
                            Log.LogError(ex, "更新処理中にエラーが発生しました。");
                            Log.LogDebug("{{_rootAggregate.DisplayName.Replace("\"", "\\\"")}}更新でSQL発行時エラーが発生した登録内容(JSON): {data}", {{ApplicationService.SERIALIZE_FOR_LOG}}(command));
                            return new(DataModelSaveErrorReason.ValidationError);
                        }
                    }

                    // 更新後処理
                    try {
                        await {{OnAfterMethodName}}(afterDbEntity, beforeDbEntity, messages, context);

                        // 後続処理に影響が出るのを防ぐためエンティティを解放
                        DbContext.Entry(afterDbEntity).State = EntityState.Detached;
                {{RenderDescendantDetaching(_rootAggregate, variablePathInfo, "afterDbEntity").SelectTextTemplate(source => $$"""
                        {{WithIndent(source)}}
                """)}}

                        // セーブポイント解放
                        await DbContext.Database.CurrentTransaction.ReleaseSavepointAsync(SAVE_POINT).ConfigureAwait(false);

                    } catch (Exception ex) {
                        messages.AddError({{MsgFactory.MSG}}.{{ERR_ID_UNKNOWN}}(ex.Message));
                        Log.LogError(ex, "更新後の処理中にエラーが発生しました。");
                        Log.LogDebug("{{_rootAggregate.DisplayName.Replace("\"", "\\\"")}}更新後エラーが発生した登録内容(JSON): {data}", {{ApplicationService.SERIALIZE_FOR_LOG}}(command));
                        await DbContext.Database.CurrentTransaction.RollbackToSavepointAsync(SAVE_POINT).ConfigureAwait(false);
                        return new(DataModelSaveErrorReason.AfterSaveError);
                    }

                    Log.LogInformation("{{_rootAggregate.DisplayName.Replace("\"", "\\\"")}}データを更新しました。（{{keys.Select(x => x.LogTemplate).Join(", ")}}）", {{keys.Select(x => x.DbEntityFullPath).Join(", ")}});
                    Log.LogDebug("{{_rootAggregate.DisplayName.Replace("\"", "\\\"")}} 更新パラメータ: {data}", {{ApplicationService.SERIALIZE_FOR_LOG}}(command));

                    return new(afterDbEntity);
                }
                {{xmlComment}}
                public virtual Task<DataModelSaveResult<{{dbEntity.CsClassName}}>> {{MethodName}}(
                    {{keyCommand.CsClassNameDelete}} key,
                    Action<{{command.CsClassNameUpdate}}> updater,
                    {{PresentationContext.INTERFACE}} context,
                    {{MessageContainer.SETTER_INTERFACE}}? messageOwner = null) {

                    // 非同期版のオーバーロードに委譲
                    return {{MethodName}}(
                        key,
                        command => {
                            updater(command);
                            return Task.CompletedTask;
                        },
                        context,
                        messageOwner);
                }

                /// <summary>
                /// {{_rootAggregate.DisplayName}} の更新の確定前に実行される処理。
                /// このメソッドの中でエラーが追加された場合、{{_rootAggregate.DisplayName}} の更新は中断される。
                /// どの画面・バッチから更新された場合であっても必ず {{_rootAggregate.DisplayName}} が満たしていなければならない整合性はここで実装する。
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
        internal static IEnumerable<string> RenderDescendantAttaching(RootAggregate rootAggregate, IReadOnlyDictionary<SchemaNodeIdentity, InstancePropertyPath> variablePathInfo) {
            var descendantDbEntities = rootAggregate.EnumerateDescendants().ToArray();

            for (int i = 0; i < descendantDbEntities.Length; i++) {
                var descAggregate = descendantDbEntities[i];
                var descDbEntity = new EFCoreEntity(descAggregate);
                var tempBefore = $"before{descAggregate.PhysicalName}_{i}";
                var tempAfter = $"after{descAggregate.PhysicalName}_{i}";

                // ChangeState変更。Child型の場合は、オプショナル（親テーブル1に対しChildは0または1）の考慮を行う。
                // 1. 子テーブルが元々存在し、更新後も存在する場合 → UPDATE
                // 2. 子テーブルが元々存在せず、更新後に存在する場合 → INSERT
                // 3. 子テーブルが元々存在するが、更新後に存在しない場合 → DELETE
                var arrayPath = variablePathInfo[descAggregate.ToMappingKey()].GetFlattenArrayPath(E_CsTs.CSharp, out var isMany);
                if (isMany) {
                    yield return $$"""
                        var {{tempBefore}} = beforeDbEntity.{{arrayPath.Join("?.")}}?.OfType<{{descDbEntity.CsClassName}}>() ?? [];
                        var {{tempAfter}} = afterDbEntity.{{arrayPath.Join("?.")}}?.OfType<{{descDbEntity.CsClassName}}>() ?? [];
                        foreach (var a in {{tempAfter}}) {
                            var b = {{tempBefore}}.SingleOrDefault(b => b.{{EFCoreEntity.KEYEQUALS}}(a));
                            if (b == null) {
                                DbContext.Entry(a).State = EntityState.Added; // 子テーブルが元々存在せず、更新後に存在する場合 → INSERT
                            } else {
                                DbContext.Entry(a).State = EntityState.Modified; // 子テーブルが元々存在し、更新後も存在する場合 → UPDATE
                            }
                        }
                        foreach (var b in {{tempBefore}}) {
                            var a = {{tempAfter}}.SingleOrDefault(a => a.{{EFCoreEntity.KEYEQUALS}}(b));
                            if (a == null) {
                                DbContext.Entry(b).State = EntityState.Deleted; // 子テーブルが元々存在するが、更新後に存在しない場合 → DELETE
                            }
                        }
                        """;

                } else {
                    yield return $$"""
                        var {{tempBefore}} = beforeDbEntity.{{arrayPath.Join("?.")}};
                        var {{tempAfter}} = afterDbEntity.{{arrayPath.Join("?.")}};
                        if ({{tempAfter}} != null) {
                            if ({{tempBefore}} != null) {
                                DbContext.Entry({{tempAfter}}).State = EntityState.Modified; // 子テーブルが元々存在し、更新後も存在する場合 → UPDATE
                            } else {
                                DbContext.Entry({{tempAfter}}).State = EntityState.Added; // 子テーブルが元々存在せず、更新後に存在する場合 → INSERT
                            }
                        } else if ({{tempBefore}} != null) {
                            DbContext.Entry({{tempBefore}}).State = EntityState.Deleted; // 子テーブルが元々存在するが、更新後に存在しない場合 → DELETE
                        }
                        """;
                }
            }
        }

        /// <summary>
        /// 子孫要素の EntityState を全てDetachにしていくソースをレンダリングする。
        /// </summary>
        internal static IEnumerable<string> RenderDescendantDetaching(RootAggregate rootAggregate, IReadOnlyDictionary<SchemaNodeIdentity, InstancePropertyPath> variablePathInfo, string rootEntityName) {
            var descendantDbEntities = rootAggregate.EnumerateDescendants().ToArray();

            for (int i = 0; i < descendantDbEntities.Length; i++) {
                var descAggregate = descendantDbEntities[i];
                var descDbEntity = new EFCoreEntity(descAggregate);
                var temp = $"after{descAggregate.PhysicalName}_{i}";
                var arrayPath = variablePathInfo[descAggregate.ToMappingKey()].GetFlattenArrayPath(E_CsTs.CSharp, out var isMany);
                if (isMany) {
                    yield return $$"""
                        var {{temp}} = {{rootEntityName}}.{{arrayPath.Join("?.")}}?.OfType<{{descDbEntity.CsClassName}}>() ?? [];
                        foreach (var a in {{temp}}) {
                            DbContext.Entry(a).State = EntityState.Detached;
                        }
                        """;
                } else {
                    yield return $$"""
                        var {{temp}} = {{rootEntityName}}.{{arrayPath.Join("?.")}};
                        if ({{temp}} != null) {
                            DbContext.Entry({{temp}}).State = EntityState.Detached;
                        }
                        """;
                }
            }
        }
        #endregion Create/Update/Deleleで共通
    }
}
