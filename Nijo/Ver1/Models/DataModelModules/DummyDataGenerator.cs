using Nijo.Util.DotnetEx;
using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.Models.DataModelModules {
    internal class DummyDataGenerator : IMultiAggregateSourceFile {

        internal const string CLASS_NAME = "DummyDataGenerator";
        internal const string GENERATE_ASYNC = "GenerateAsync";

        private readonly List<RootAggregate> _rootAggregates = [];

        internal DummyDataGenerator Add(RootAggregate rootAggregate) {
            _rootAggregates.Add(rootAggregate);
            return this;
        }

        void IMultiAggregateSourceFile.RegisterDependencies(IMultiAggregateSourceFileManager ctx) {
            // 特になし
        }

        void IMultiAggregateSourceFile.Render(CodeRenderingContext ctx) {
            ctx.CoreLibrary(dir => {
                dir.Directory("Debugging", utilDir => {
                    utilDir.Generate(RenderBulkInsertInterface(ctx));
                    utilDir.Generate(RenderDummyDataGenerator(ctx));
                    utilDir.Generate(RenderDummyDataGenerateContext(ctx));
                });
            });
        }

        private SourceFile RenderDummyDataGenerator(CodeRenderingContext ctx) {
            var items = ctx
                .ToOrderedByDataFlow(_rootAggregates) // データフロー順に並び替え
                .ToArray();

            return new SourceFile {
                FileName = "DummyDataGenerator.cs",
                Contents = $$"""
                    // 何らかの事故で本番環境で実行されてしまう可能性を排除するためDEBUGビルドでのみ有効とする
                    #if DEBUG

                    namespace {{ctx.Config.RootNamespace}};

                    /// <summary>
                    /// デバッグ用のダミーデータ作成処理
                    /// </summary>
                    public class {{CLASS_NAME}} {

                        public {{CLASS_NAME}}({{ctx.Config.DbContextName}} dbContext, {{I_DUMMY_DATA_IO}} dummyDataIO) {
                            _dbContext = dbContext;
                            _dummyDataIO = dummyDataIO;
                        }
                        private readonly {{ctx.Config.DbContextName}} _dbContext;
                        private readonly {{I_DUMMY_DATA_IO}} _dummyDataIO;

                        /// <summary>
                        /// ダミーデータ作成処理を実行します。
                        /// 現在登録されているデータは全て削除されます。
                        /// </summary>
                        public async Task {{GENERATE_ASYNC}}() {

                            // いま登録されているデータは全件削除
                            await _dummyDataIO.DestroyAllDataAsync();

                            // ランダム値採番等のコンテキスト
                            var context = new {{DUMMY_DATA_GENERATE_CONTEXT}} {
                                DbContext = _dbContext,
                                Random = new Random(0),
                            };
                    {{items.SelectTextTemplate(rootAggregate => $$"""

                            // {{rootAggregate.DisplayName}}
                            {
                                {{WithIndent(RenderRootAggregate(rootAggregate), "            ")}}
                            }
                    """)}}
                        }

                        #region ステップ1: 機械的にパターンかけあわせ
                    {{_rootAggregates.SelectTextTemplate(agg => $$"""
                        {{WithIndent(RenderCreatingMethod(agg), "    ")}}
                    """)}}
                        #endregion ステップ1: 機械的にパターンかけあわせ


                        #region ステップ2: かけあわせたパターンを手修正
                    {{_rootAggregates.SelectTextTemplate(agg => $$"""
                        {{WithIndent(RenderCreatedMethod(agg), "    ")}}
                    """)}}
                        #endregion ステップ2: かけあわせたパターンを手修正
                    }
                    #endif
                    """,
            };

            static string RenderRootAggregate(RootAggregate rootAggregate) {

                var tree = rootAggregate.EnumerateThisAndDescendants();

                return $$"""
                    var patterns = {{GetCreatingMethodName(rootAggregate)}}().Build().ToList();
                    var createCommands = {{GetCreatedMethodName(rootAggregate)}}(patterns).Select(x => x.{{SaveCommand.TO_DBENTITY}}()).ToArray();

                    {{tree.SelectTextTemplate((agg, ix) => $$"""
                    {{RenderAggregate(agg, ix)}}
                    """)}}
                    """;

                static string RenderAggregate(AggregateBase aggregate, int index) {
                    var selected = new List<string>();
                    foreach (var node in aggregate.GetFullPath()) {
                        if (node is RootAggregate) {
                            selected.Add("createCommands");

                        } else if (node is ChildAggreagte child) {
                            var parent = (AggregateBase?)node.PreviousNode ?? throw new InvalidOperationException("ありえない");
                            var nav = new EFCoreEntity.NavigationOfParentChild(parent, child);
                            selected.Add($".Select(e => e.{nav.Principal.OtherSidePhysicalName})");

                        } else if (node is ChildrenAggreagte children) {
                            var parent = (AggregateBase?)node.PreviousNode ?? throw new InvalidOperationException("ありえない");
                            var nav = new EFCoreEntity.NavigationOfParentChild(parent, children);
                            selected.Add($".SelectMany(e => e.{nav.Principal.OtherSidePhysicalName})");
                        }
                    }

                    return $$"""
                        await _dummyDataIO.ExecuteBulkInsertAsync({{selected.Join("")}});
                        """;
                }
            }

            static string RenderCreatingMethod(RootAggregate rootAggregate) {
                var saveCommand = new SaveCommand(rootAggregate);

                return $$"""
                    /// <summary>{{rootAggregate.DisplayName}}の属性のパターンを機械的に組み合わせてテストデータのパターンを作成します。</summary>
                    protected virtual {{I_PATTERN_BUILDER}}<{{saveCommand.CsClassNameCreate}}> {{GetCreatingMethodName(rootAggregate)}}() {
                        あっちからもってくる;
                    }
                    """;
            }

            static string RenderCreatedMethod(RootAggregate rootAggregate) {
                var saveCommand = new SaveCommand(rootAggregate);

                return $$"""
                    /// <summary>テスト毎の細かい需要を反映するため、{{rootAggregate.DisplayName}}の属性のパターンを機械的に組み合わせたあとの結果を手修正します。</summary>
                    /// <param name="items">パターン掛け合わせ後の配列。自由に中身を編集したり追加したりクリアしたりしてください。</param>
                    /// <returns>編集結果。itemsをそのまま返しても、全く違う配列を返してもよい</returns>
                    protected virtual IEnumerable<{{saveCommand.CsClassNameCreate}}> {{GetCreatedMethodName(rootAggregate)}}(List<{{saveCommand.CsClassNameCreate}}> items) {
                        // テストデータを細かく編集したい場合はこのメソッドをオーバーライドして手修正処理を記述してください。
                        return items;
                    }
                    """;
            }
        }


        #region コンテキスト
        private const string DUMMY_DATA_GENERATE_CONTEXT = "DummyDataGenerateContext";

        private static SourceFile RenderDummyDataGenerateContext(CodeRenderingContext ctx) {
            return new SourceFile {
                FileName = "DummyDataGenerateContext.cs",
                Contents = $$"""
                    using System;

                    namespace {{ctx.Config.RootNamespace}};

                    /// <summary>ダミーデータ作成処理コンテキスト情報</summary>
                    internal sealed class {{DUMMY_DATA_GENERATE_CONTEXT}} {
                        internal required {{ctx.Config.DbContextName}} DbContext { get; init; }
                        internal required Random Random { get; init; }
                    }
                    """,
            };
        }
        #endregion コンテキスト


        #region DB操作
        private const string I_DUMMY_DATA_IO = "IDummyDataIO";

        private static SourceFile RenderBulkInsertInterface(CodeRenderingContext ctx) {
            return new SourceFile {
                FileName = "IDummyDataIO.cs",
                Contents = $$"""
                    namespace {{ctx.Config.RootNamespace}};

                    /// <summary>
                    /// デバッグ用機能。
                    /// 大量のダミーデータを高速に一括更新する機能や、テーブル上の全データを削除する機能を提供します。
                    /// </summary>
                    public interface {{I_DUMMY_DATA_IO}} {
                        /// <summary>
                        /// データベース上の全データを削除します。
                        /// </summary>
                        Task DestroyAllDataAsync();
                        /// <summary>
                        /// 大量データの高速一括更新を実行します。
                        /// </summary>
                        /// <param name="entities">登録対象データ。EFCoreのエンティティの配列</param>
                        Task ExecuteBulkInsertAsync<TEntity>(IEnumerable<TEntity> entities);
                    }
                    """,
            };
        }
        #endregion DB操作


        #region パターンビルダー
        private const string I_PATTERN_BUILDER = "ITestPatternBuilder";
        private static string GetCreatingMethodName(RootAggregate rootAggregate) => $"OnPatternCreating{rootAggregate.PhysicalName}";
        private static string GetCreatedMethodName(RootAggregate rootAggregate) => $"OnPatternCreated{rootAggregate.PhysicalName}";

        internal static SourceFile RenderPatternBuilderInterface(CodeRenderingContext ctx) {
            return new SourceFile {
                FileName = "ITestPatternBuilder.cs",
                Contents = $$"""
                    namespace {{ctx.Config.RootNamespace}};

                    public interface {{I_PATTERN_BUILDER}}<T> {
                        あっちからもってくる;
                    }
                    """,
            };
        }
        #endregion パターンビルダー
    }
}
