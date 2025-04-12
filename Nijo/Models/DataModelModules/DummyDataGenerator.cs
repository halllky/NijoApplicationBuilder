using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Parts.Common;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.DataModelModules {
    internal class DummyDataGenerator : IMultiAggregateSourceFile {

        internal const string CLASS_NAME = "DummyDataGenerator";
        internal const string GENERATE_ASYNC = "GenerateAsync";

        private static string CreateAggregateMethodName(RootAggregate rootAggregate) => $"CreateRandom{rootAggregate.PhysicalName}";
        private static string CreatePatternMethodName(RootAggregate rootAggregate) => $"CreatePatternsOf{rootAggregate.PhysicalName}";
        private static string GetValueMemberValueMethodName(IValueMemberType type) => $"GetRandom{type.TypePhysicalName}";
        private static string GeneratedList(RootAggregate aggregate) => $"Generated{aggregate.PhysicalName}";

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

                        /// <summary>
                        /// ダミーデータ作成処理を実行します。
                        /// 現在登録されているデータは全て削除されます。
                        /// </summary>
                        public async Task {{GENERATE_ASYNC}}({{I_DUMMY_DATA_OUTPUT}} dummyDataOutput) {

                            // ランダム値採番等のコンテキスト
                            var context = new {{DUMMY_DATA_GENERATE_CONTEXT}} {
                                Random = new Random(0),
                                Metadata = new(),
                            };

                            // データフローの順番でダミーデータのパターンを作成
                    {{items.SelectTextTemplate(rootAggregate => $$"""
                            context.{{GeneratedList(rootAggregate)}} = {{CreatePatternMethodName(rootAggregate)}}(context).ToArray();
                    """)}}

                            // データフローの順番で登録実行
                    {{items.SelectTextTemplate(rootAggregate => $$"""
                            {{WithIndent(RenderOutputting(rootAggregate), "        ")}}
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

            static IEnumerable<string> RenderOutputting(RootAggregate rootAggregate) {
                foreach (var aggregate in rootAggregate.EnumerateThisAndDescendants()) {
                    var selected = new List<string>();
                    foreach (var node in aggregate.GetPathFromEntry()) {
                        if (node is RootAggregate) {
                            selected.Add($"context.{GeneratedList(rootAggregate)}.Select(x => x.{SaveCommand.TO_DBENTITY}())");

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

                    yield return $$"""
                        await dummyDataOutput.OutputAsync({{selected.Join("")}});
                        """;
                }
            }

            static string RenderCreatePatternMethod(RootAggregate rootAggregate) {
                var saveCommand = new SaveCommand(rootAggregate);

                return $$"""
                    /// <summary>{{rootAggregate.DisplayName}}のテストデータのパターン作成</summary>
                    protected virtual IEnumerable<{{saveCommand.CsClassNameCreate}}> {{CreatePatternMethodName(rootAggregate)}}({{DUMMY_DATA_GENERATE_CONTEXT}} context) {
                        for (var i = 0; i < 20; i++) {
                            yield return {{CreateAggregateMethodName(rootAggregate)}}(context, i);
                        }
                    }
                    """;
            }

            static string RenderCreateRootAggregateMethod(RootAggregate rootAggregate) {
                var saveCommand = new SaveCommand(rootAggregate);

                return $$"""
                    /// <summary>{{rootAggregate.DisplayName}}の作成コマンドのインスタンスを作成して返します。</summary>
                    protected virtual {{saveCommand.CsClassNameCreate}} {{CreateAggregateMethodName(rootAggregate)}}({{DUMMY_DATA_GENERATE_CONTEXT}} context, int itemIndex) {
                        return new {{saveCommand.CsClassNameCreate}} {
                            {{WithIndent(RenderBody(saveCommand), "        ")}}
                        };
                    }
                    """;

                static IEnumerable<string> RenderBody(SaveCommand saveCommand) {
                    foreach (var member in saveCommand.GetCreateCommandMembers()) {
                        if (member is SaveCommand.SaveCommandValueMember vm) {
                            var path = new List<string>();

                            var root = vm.Member.Owner.GetRoot();
                            path.Add(root.PhysicalName);

                            path.AddRange(vm.Member.Owner
                                .GetPathFromRoot()
                                .AsSaveCommand());

                            path.Add(vm.PhysicalName);

                            yield return $$"""
                                {{member.PhysicalName}} = {{GetValueMemberValueMethodName(vm.Member.Type)}}(context, context.Metadata.{{path.Join(".")}}),
                                """;

                        } else if (member is SaveCommand.SaveCommandRefMember refTo) {
                            // contextに登録されているインスタンスから適当なものを選んでキーに変換する
                            var refToRoot = refTo.Member.RefTo.GetRoot();
                            var keyClass = new KeyClass.KeyClassEntry(refTo.Member.RefTo.AsEntry());

                            // contextの配列から子孫までのパス
                            var treePath = new List<string>();
                            foreach (var agg in refTo.Member.RefTo.GetPathFromRoot()) {
                                if (agg is RootAggregate) {
                                    continue;

                                } else if (agg is ChildAggreagte child) {
                                    var saveCommandMember = new SaveCommand.SaveCommandChildMember(child);
                                    treePath.Add($".Select(x => x.{saveCommandMember.PhysicalName})");

                                } else if (agg is ChildrenAggreagte children) {
                                    var saveCommandMember = new SaveCommand.SaveCommandChildrenMember(children);
                                    treePath.Add($".SelectMany(x => x.{saveCommandMember.PhysicalName})");

                                } else {
                                    throw new NotImplementedException();
                                }
                            }

                            var owner = refTo.Member.Owner.DisplayName.Replace("\"", "\\\"");
                            var memberName = refTo.DisplayName.Replace("\"", "\\\"");
                            var refToName = refTo.Member.RefTo.DisplayName.Replace("\"", "\\\"");

                            if (refTo.Member.IsKey) {
                                // refがキーの場合はキー重複を防ぐためインデックス順に振る
                                yield return $$"""
                                    {{member.PhysicalName}} = context.{{GeneratedList(refToRoot)}}
                                    {{treePath.SelectTextTemplate(path => $$"""
                                        {{path}}
                                    """)}}
                                        .Select(cmd => new {{keyClass.ClassName}} {
                                            {{WithIndent(RenderKeyClassBodyConverting(keyClass), "        ")}}
                                        })
                                        .ElementAtOrDefault(itemIndex)
                                        ?? throw new InvalidOperationException($"{{owner}}の{{memberName}}のキー重複を防ぐため{{refToName}}には少なくとも{itemIndex + 1}件のデータがある必要がありますが、{context.{{GeneratedList(refToRoot)}}.Count}件しかありません。"),
                                    """;

                            } else if (refTo.Member.IsRequired) {
                                // refが必須の場合はその時点で参照先が1件も無いときに例外を出す
                                yield return $$"""
                                    {{member.PhysicalName}} = context.{{GeneratedList(refToRoot)}}.Count == 0
                                        ? throw new InvalidOperation("{{owner}}の{{memberName}}に設定するためのインスタンスを探そうとしましたが、{{refToName}}が1件も作成されていません。")
                                        : context.{{GeneratedList(refToRoot)}}
                                    {{treePath.SelectTextTemplate(path => $$"""
                                            {{path}}
                                    """)}}
                                            .Select(cmd => new {{keyClass.ClassName}} {
                                                {{WithIndent(RenderKeyClassBodyConverting(keyClass), "            ")}}
                                            })
                                            .ElementAt(context.Random.Next(0, context.{{GeneratedList(refToRoot)}}.Count)),
                                    """;

                            } else {
                                // 必須でない場合はその時点で参照先が1件も無いときはnull
                                yield return $$"""
                                    {{member.PhysicalName}} = context.{{GeneratedList(refToRoot)}}.Count == 0
                                        ? null
                                        : context.{{GeneratedList(refToRoot)}}
                                    {{treePath.SelectTextTemplate(path => $$"""
                                            {{path}}
                                    """)}}
                                            .Select(cmd => new {{keyClass.ClassName}} {
                                                {{WithIndent(RenderKeyClassBodyConverting(keyClass), "            ")}}
                                            })
                                            .ElementAt(context.Random.Next(0, context.{{GeneratedList(refToRoot)}}.Count)),
                                    """;
                            }

                            // SaveCommand から KeyClass への変換
                            static IEnumerable<string> RenderKeyClassBodyConverting(KeyClass.IKeyClassStructure keyClassStructure) {
                                foreach (var member in keyClassStructure.GetMembers()) {
                                    if (member is KeyClass.KeyClassValueMember vm) {
                                        var path = vm.Member
                                            .GetPathFromRoot()
                                            .AsSaveCommand();
                                        yield return $$"""
                                            {{member.PhysicalName}} = cmd.{{path.Join(".")}},
                                            """;

                                    } else if (member is KeyClass.KeyClassRefMember rm) {
                                        yield return $$"""
                                            {{member.PhysicalName}} = new() {
                                                {{WithIndent(RenderKeyClassBodyConverting(rm.MemberKeyClassEntry), "    ")}}
                                            },
                                            """;

                                    } else if (member is KeyClass.KeyClassParentMember pm) {
                                        yield return $$"""
                                            {{member.PhysicalName}} = new() {
                                                {{WithIndent(RenderKeyClassBodyConverting(pm), "    ")}}
                                            },
                                            """;

                                    } else {
                                        throw new NotImplementedException();
                                    }
                                }
                            }

                        } else if (member is SaveCommand.SaveCommandChildMember child) {
                            yield return $$"""
                                {{member.PhysicalName}} = new() {
                                    {{WithIndent(RenderBody(child), "    ")}}
                                },
                                """;

                        } else if (member is SaveCommand.SaveCommandChildrenMember children) {
                            yield return $$"""
                                {{member.PhysicalName}} = Enumerable.Range(0, 4).Select(_ => new {{children.CsClassNameCreate}} {
                                    {{WithIndent(RenderBody(children), "    ")}}
                                }).ToList(),
                                """;

                        } else {

                            throw new NotImplementedException();
                        }
                    }
                }
            }
        }


        #region コンテキスト
        private const string DUMMY_DATA_GENERATE_CONTEXT = "DummyDataGenerateContext";

        private SourceFile RenderDummyDataGenerateContext(CodeRenderingContext ctx) {
            return new SourceFile {
                FileName = "DummyDataGenerateContext.cs",
                Contents = $$"""
                    using System;

                    namespace {{ctx.Config.RootNamespace}};

                    /// <summary>ダミーデータ作成処理コンテキスト情報</summary>
                    public sealed class {{DUMMY_DATA_GENERATE_CONTEXT}} {
                        public required Random Random { get; init; }
                        public required {{Metadata.CS_CLASSNAME}} Metadata { get; init; }

                    {{_rootAggregates.SelectTextTemplate(agg => $$"""
                        {{WithIndent(RenderGetRefTo(agg), "    ")}}
                    """)}}
                    }
                    """,
            };

            static string RenderGetRefTo(RootAggregate aggregate) {
                var saveCommand = new SaveCommand(aggregate);

                return $$"""
                    /// <summary>このメソッドが呼ばれた時点で作成済みの{{aggregate.DisplayName}}</summary>
                    public IReadOnlyList<{{saveCommand.CsClassNameCreate}}> {{GeneratedList(aggregate)}} { get; set; } = [];
                    """;
            }
        }
        #endregion コンテキスト


        #region DB操作
        private const string I_DUMMY_DATA_OUTPUT = "IDummyDataOutput";

        private static SourceFile RenderBulkInsertInterface(CodeRenderingContext ctx) {
            return new SourceFile {
                FileName = "IDummyDataOutput.cs",
                Contents = $$"""
                    namespace {{ctx.Config.RootNamespace}};

                    /// <summary>
                    /// ダミーデータのインスタンスを実際にデータベースに登録したり何らかのファイルに出力したりする機能を提供します。
                    /// </summary>
                    public interface {{I_DUMMY_DATA_OUTPUT}} {
                        /// <summary>
                        /// ダミーデータのインスタンスを出力します。
                        /// データベースに登録したり何らかのファイルに出力したりしてください。
                        /// </summary>
                        /// <param name="entities">登録対象データ。EFCoreのエンティティの配列</param>
                        Task OutputAsync<TEntity>(IEnumerable<TEntity> entities);
                    }
                    """,
            };
        }
        #endregion DB操作
    }
}
