using Nijo.Util.DotnetEx;
using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using Nijo.Ver1.Parts.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.Models.DataModelModules {
    internal class DummyDataGenerator : IMultiAggregateSourceFile {

        internal const string CLASS_NAME = "DummyDataGenerator";
        internal const string GENERATE_ASYNC = "GenerateAsync";

        private static string CreateAggregateMethodName(RootAggregate rootAggregate) => $"CreateRandom{rootAggregate.PhysicalName}";
        private static string CreatePatternMethodName(RootAggregate rootAggregate) => $"CreatePatternsOf{rootAggregate.PhysicalName}";
        private static string DummyValueMethodName(IValueMemberType type) => $"GetRandom{type.TypePhysicalName}";

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
                                Metadata = new(),
                            };
                    {{items.SelectTextTemplate(rootAggregate => $$"""

                            // {{rootAggregate.DisplayName}}
                            {{WithIndent(RenderRootAggregate(rootAggregate), "        ")}}
                    """)}}
                        }


                        #region ルート集約毎のパターン作成処理
                    {{_rootAggregates.SelectTextTemplate(agg => $$"""
                        {{WithIndent(RenderCreatePatternMethod(agg), "    ")}}
                    """)}}
                        #endregion ルート集約毎のパターン作成処理


                        #region ルート集約毎のインスタンス1件作成処理
                    {{_rootAggregates.SelectTextTemplate(agg => $$"""
                        {{WithIndent(RenderCreateRootAggregateMethod(agg), "    ")}}
                    """)}}
                        #endregion ルート集約毎のインスタンス1件作成処理


                        #region 型ごとの標準ダミー値の生成ロジック
                    {{ctx.SchemaParser.GetValueMemberTypes().SelectTextTemplate(type => $$"""
                        protected virtual {{type.CsDomainTypeName}}? GetRandom{{type.TypePhysicalName}}({{DUMMY_DATA_GENERATE_CONTEXT}} context, {{Metadata.VALUE_MEMBER_METADATA_CS}} member) {
                            {{WithIndent(type.RenderCreateDummyDataValueBody(ctx), "        ")}}
                        }
                    """)}}
                        #endregion 型ごとの標準ダミー値の生成ロジック
                    }
                    #endif
                    """,
            };

            static string RenderRootAggregate(RootAggregate rootAggregate) {

                var listVarName = $"createCommands{rootAggregate.PhysicalName}";
                var tree = rootAggregate.EnumerateThisAndDescendants();

                return $$"""
                    var {{listVarName}} = {{CreatePatternMethodName(rootAggregate)}}(context).Select(x => x.{{SaveCommand.TO_DBENTITY}}()).ToArray();
                    {{tree.SelectTextTemplate((agg, ix) => $$"""
                    {{RenderAggregate(agg, ix)}}
                    """)}}
                    """;

                string RenderAggregate(AggregateBase aggregate, int index) {
                    var selected = new List<string>();
                    foreach (var node in aggregate.GetFullPath()) {
                        if (node is RootAggregate) {
                            selected.Add(listVarName);

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

            static string RenderCreatePatternMethod(RootAggregate rootAggregate) {
                var saveCommand = new SaveCommand(rootAggregate);

                return $$"""
                    /// <summary>{{rootAggregate.DisplayName}}のテストデータのパターン作成</summary>
                    protected virtual IEnumerable<{{saveCommand.CsClassNameCreate}}> {{CreatePatternMethodName(rootAggregate)}}({{DUMMY_DATA_GENERATE_CONTEXT}} context) {
                        for (var i = 0; i < 20; i++) {
                            yield return {{CreateAggregateMethodName(rootAggregate)}}(context);
                        }
                    }
                    """;
            }

            static string RenderCreateRootAggregateMethod(RootAggregate rootAggregate) {
                var saveCommand = new SaveCommand(rootAggregate);

                return $$"""
                    /// <summary>{{rootAggregate.DisplayName}}の作成コマンドのインスタンスを作成して返します。</summary>
                    protected virtual {{saveCommand.CsClassNameCreate}} {{CreateAggregateMethodName(rootAggregate)}}({{DUMMY_DATA_GENERATE_CONTEXT}} context) {
                        return new {{saveCommand.CsClassNameCreate}} {
                            {{WithIndent(RenderBody(saveCommand), "        ")}}
                        };
                    }
                    """;

                static IEnumerable<string> RenderBody(SaveCommand saveCommand) {
                    foreach (var member in saveCommand.GetCreateCommandMembers()) {
                        if (member is SaveCommand.SaveCommandValueMember vm) {
                            var path = vm.ValueMember.Owner
                                .GetPathFromRoot()
                                .AsSaveCommand()
                                .ToList();
                            path.Insert(0, vm.ValueMember.Owner.GetRoot().PhysicalName);
                            path.Add(vm.PhysicalName);

                            yield return $$"""
                                {{member.PhysicalName}} = {{DummyValueMethodName(vm.ValueMember.Type)}}(context, context.Metadata.{{path.Join(".")}}),
                                """;

                        } else if (member is SaveCommand.SaveCommandRefMember refTo) {

                        } else if (member is SaveCommand.SaveCommandDescendantMember desc) {

                        } else {
                            throw new NotImplementedException();
                        }
                    }
                }
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
                    public sealed class {{DUMMY_DATA_GENERATE_CONTEXT}} {
                        internal required {{ctx.Config.DbContextName}} DbContext { get; init; }
                        internal required Random Random { get; init; }
                        internal required {{Metadata.CS_CLASSNAME}} Metadata { get; init; }
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
    }
}
