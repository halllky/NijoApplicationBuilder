using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nijo.Parts;
using Nijo.Parts.Utility;
using Nijo.Parts.WebServer;
using Nijo.Util.CodeGenerating;

namespace Nijo.Features.BackgroundService {
    partial class BgTaskFeature {

        private static SourceFile Launcher(CodeRenderingContext ctx) => new SourceFile {
            FileName = "BackgroundTaskLauncher.cs",
            RenderContent = context => {
                var dbContextFullName = $"{ctx.Config.DbContextNamespace}.{ctx.Config.DbContextName}";
                var dbSetName = ctx.Schema.GetAggregate(GraphNodeId).Item.DbSetName;

                return $$"""
                    using Microsoft.EntityFrameworkCore;
                    using System;
                    using System.Collections.Generic;
                    using System.Linq;
                    using System.Reflection;
                    using System.Text;
                    using System.Text.Json;
                    using System.Text.Json.Serialization;
                    using System.Threading.Tasks;

                    namespace {{ctx.Config.RootNamespace}} {
                        public sealed class {{LAUNCHER_CLASSNAME}} : Microsoft.Extensions.Hosting.BackgroundService {

                            protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
                                var serviceCollection = new ServiceCollection();
                                {{Configure.GetClassFullname(ctx.Config)}}.{{Configure.CONFIGURE_SERVICES}}(serviceCollection);
                                var services = serviceCollection.BuildServiceProvider();

                                var logger = services.GetRequiredService<ILogger>();
                                var settings = services.GetRequiredService<RuntimeSettings.Server>();
                                var runningTasks = new Dictionary<string, Task>();

                                var directory = Path.Combine(Directory.GetCurrentDirectory(), settings.JobDirectory ?? "job");
                                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

                                stoppingToken.Register(() => {
                                    logger.LogInformation($"バッチ起動監視処理の中止が要請されました。");
                                });

                                try {

                                    logger.LogInformation($"バッチ起動監視 開始");

                                    while (!stoppingToken.IsCancellationRequested) {
                                        // 待機
                                        try {
                                            await Task.Delay(settings.BackgroundTask.PollingSpanMilliSeconds, stoppingToken);
                                        } catch (TaskCanceledException) {
                                            continue;
                                        }

                                        // 終了しているバッチがないか調べる
                                        using var pollingScope = services.CreateScope();
                                        var dbContext = pollingScope.ServiceProvider.GetRequiredService<{{dbContextFullName}}>();
                                        DetectFinishing(runningTasks, dbContext, logger);

                                        // 起動対象バッチがあるかどうか検索
                                        var queued = dbContext
                                            .{{dbSetName}}
                                            .Where(task => task.State == {{ENUM_BGTASKSTATE}}.{{ENUM_BGTASKSTATE_WAITTOSTART}})
                                            .OrderBy(task => task.RequestTime)
                                            .Take(5)
                                            .ToArray();
                                        if (!queued.Any()) continue;

                                        // バッチ起動
                                        var now = DateTime.Now;
                                        var contextFactory = new BackgroundTaskContextFactory(now, services, directory);
                                        foreach (var entity in queued) {
                                            try {
                                                var backgroundTask = BackgroundTask.FindTaskByID(entity.{{COL_BATCHTYPE}});
                                                var executeArgument = CreateExecuteArgument(backgroundTask, entity, contextFactory, stoppingToken);

                                                var task = Task.Run(() => {
                                                    logger.LogInformation("バッチ実行開始 {Id} ({Type} {Name})", entity.{{COL_ID}}, entity.{{COL_BATCHTYPE}}, entity.{{COL_NAME}});
                                                    backgroundTask.Execute(executeArgument);
                                                    logger.LogInformation("バッチ実行終了 {Id} ({Type} {Name})", entity.{{COL_ID}}, entity.{{COL_BATCHTYPE}}, entity.{{COL_NAME}});
                                                }, CancellationToken.None);
                                                runningTasks.Add(entity.{{COL_ID}}, task);

                                                entity.{{COL_STARTTIME}} = now;
                                                entity.{{COL_STATE}} = {{ENUM_BGTASKSTATE}}.{{ENUM_BGTASKSTATE_RUNNING}};
                                                dbContext.SaveChanges();

                                            } catch (Exception ex) {
                                                logger.LogError(ex, "バッチの起動に失敗しました({Id} {Name}): {Message}", entity.{{COL_ID}}, entity.{{COL_NAME}}, ex.Message);
                                            }
                                        }
                                    }
                                } catch (Exception ex) {
                                    logger.LogCritical(ex, "バッチ起動監視処理でエラーが発生しました: {Message}", ex.Message);
                                }

                                // 起動中ジョブの終了を待機
                                try {
                                    logger.LogInformation("起動中ジョブの終了を待機します。");
                                    using var disposingScope = services.CreateScope();
                                    Task.WaitAll(runningTasks.Values.ToArray(), CancellationToken.None);
                                    var dbContext = disposingScope.ServiceProvider.GetRequiredService<{{dbContextFullName}}>();
                                    DetectFinishing(runningTasks, dbContext, logger);
                                } catch (Exception ex) {
                                    logger.LogCritical(ex, "バッチ起動監視処理(起動中ジョブの終了待機)でエラーが発生しました: {Message}", ex.Message);
                                }

                                logger.LogInformation($"バッチ起動監視 終了");
                            }

                            /// <summary>
                            /// ジョブの起動指示をもとに実行用のオブジェクトを作成して返します。
                            /// </summary>
                            private static JobChain CreateExecuteArgument(BackgroundTask backgroundTask, {{ctx.Config.EntityNamespace}}.BackgroundTaskEntity entity, BackgroundTaskContextFactory contextFactory, CancellationToken stoppingToken) {
                                var type = backgroundTask.GetType();

                                while (type != null && type != typeof(object)) {
                                    // パラメータなしの場合
                                    if (type == typeof(BackgroundTask)) {
                                        return new JobChain(entity.{{COL_ID}}, new(), contextFactory, stoppingToken);
                                    }
                                    // パラメータありの場合
                                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(BackgroundTask<>)) {
                                        var genericType = type.GetGenericArguments()[0];

                                        object parsed;
                                        try {
                                            parsed = {{UtilityClass.CLASSNAME}}.{{UtilityClass.ENSURE_OBJECT_TYPE}}(entity.ParameterJson, genericType);
                                        } catch (JsonException ex) {
                                            throw new InvalidOperationException($"バッチID {entity.{{COL_ID}}} のパラメータを {genericType.Name} 型のJSONとして解釈できません。", ex);
                                        }

                                        var jobChainType = typeof(JobChainWithParameter<>).MakeGenericType(genericType);
                                        return (JobChain)Activator.CreateInstance(jobChainType, new object[] {
                                            entity.{{COL_ID}},
                                            parsed,
                                            new Stack<string>(),
                                            contextFactory,
                                            stoppingToken,
                                        })!;
                                    }
                                    type = type.BaseType;
                                }

                                throw new InvalidOperationException();
                            }

                            /// <summary>
                            /// 終了したタスクを検知して完了情報を記録します。
                            /// </summary>
                            private void DetectFinishing(Dictionary<string, Task> runningTasks, {{dbContextFullName}} dbContext, ILogger logger) {
                                // 終了したバッチを列挙
                                var completedTasks = runningTasks
                                    .Where(kv => kv.Value.IsCompleted)
                                    .ToDictionary(kv => kv.Key, kv => kv.Value);
                                if (!completedTasks.Any()) return;

                                // バッチと対応するデータをDBから検索
                                var ids = completedTasks.Keys.ToArray();
                                var entities = dbContext
                                    .{{dbSetName}}
                                    .Where(e => ids.Contains(e.{{COL_ID}}))
                                    .ToDictionary(e => e.{{COL_ID}});
                                var list = completedTasks.ToDictionary(
                                    kv => kv.Key,
                                    kv => entities.GetValueOrDefault(kv.Key));

                                // そのバッチが完了した旨をDBに登録
                                var now = DateTime.Now;
                                foreach (var item in list) {
                                    if (item.Value == null) {
                                        logger.LogError("タスク {Id} の完了情報の記録に失敗しました", item.Key);
                                        continue;
                                    }
                                    item.Value.FinishTime = now;
                                    item.Value.State = completedTasks[item.Key].IsCompletedSuccessfully
                                        ? {{ENUM_BGTASKSTATE}}.{{ENUM_BGTASKSTATE_SUCCESS}}
                                        : {{ENUM_BGTASKSTATE}}.{{ENUM_BGTASKSTATE_FAULT}};
                                    dbContext.SaveChanges();

                                    runningTasks.Remove(item.Key);
                                }
                            }
                        }
                    }
                    """;
            }
        };
    }
}
