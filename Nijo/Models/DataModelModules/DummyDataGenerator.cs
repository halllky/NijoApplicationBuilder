using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Parts.Common;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nijo.Models.DataModelModules {
    internal class DummyDataGenerator : IMultiAggregateSourceFile {

        internal const string CLASS_NAME = "DummyDataGenerator";
        internal const string GENERATE_ASYNC = "GenerateAsync";
        private const string DUMMY_DATA_GENERATE_OPTIONS = "DummyDataGenerateOptions";

        private static string CreateAggregateMethodName(RootAggregate rootAggregate) => $"CreateRandom{rootAggregate.PhysicalName}";
        private static string CreatePatternMethodName(RootAggregate rootAggregate) => $"CreatePatternsOf{rootAggregate.PhysicalName}";
        private static string GetValueMemberValueMethodName(IValueMemberType type) => $"GetRandom{type.TypePhysicalName}";
        private static string GeneratedList(AggregateBase aggregate) => $"Generated{aggregate.PhysicalName}";

        private readonly List<RootAggregate> _rootAggregates = [];
        private readonly Lock _lock = new();

        internal DummyDataGenerator Add(RootAggregate rootAggregate) {
            lock (_lock) {
                _rootAggregates.Add(rootAggregate);
                return this;
            }
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
                    utilDir.Generate(RenderDummyDataGenerateOptionsCSharp(ctx));
                });
            });
            ctx.ReactProject(dir => {
                dir.Directory("util", utilDir => {
                    utilDir.Generate(RenderDummyDataGenerateOptionsTypeScript(ctx));
                });
            });
        }

        private SourceFile RenderDummyDataGenerator(CodeRenderingContext ctx) {
            // データフロー順に並び替え
            var rootAggregatesOrderByDataFlow = _rootAggregates
                .OrderByDataFlow()
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
                    public abstract class {{CLASS_NAME}} {

                        /// <summary>
                        /// ダミーデータ作成処理を実行します。
                        /// 現在登録されているデータは全て削除されます。
                        /// </summary>
                        public async Task {{GENERATE_ASYNC}}({{I_DUMMY_DATA_OUTPUT}} dummyDataOutput, {{DUMMY_DATA_GENERATE_OPTIONS}}? options = null) {

                            // ランダム値採番等のコンテキスト
                            var context = new {{DUMMY_DATA_GENERATE_CONTEXT}} {
                                Random = new Random(0),
                                Metadata = new(),
                            };

                            // データフローの順番でダミーデータのパターンを作成
                    {{rootAggregatesOrderByDataFlow.SelectTextTemplate(rootAggregate => $$"""
                            if (options?.{{rootAggregate.PhysicalName}} != false) {
                                context.{{GeneratedList(rootAggregate)}} = {{CreatePatternMethodName(rootAggregate)}}(context).ToArray();
                                context.ResetSequence();
                            }
                    """)}}

                            // データフローの順番で登録実行
                    {{rootAggregatesOrderByDataFlow.SelectTextTemplate(rootAggregate => $$"""
                            {{WithIndent(RenderOutputting(rootAggregate), "        ")}}
                    """)}}
                        }


                        #region ルート集約毎のパターン作成処理
                    {{rootAggregatesOrderByDataFlow.SelectTextTemplate(agg => $$"""
                        {{WithIndent(RenderCreatePatternMethod(agg), "    ")}}
                    """)}}
                        #endregion ルート集約毎のパターン作成処理


                        #region ルート集約毎のインスタンス1件作成処理
                    {{rootAggregatesOrderByDataFlow.SelectTextTemplate(agg => $$"""
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

                        } else if (node is ChildAggregate child) {
                            var parent = (AggregateBase?)node.PreviousNode ?? throw new InvalidOperationException("ありえない");
                            var nav = new EFCoreEntity.NavigationOfParentChild(parent, child);
                            selected.Add($".Select(e => e.{nav.Principal.OtherSidePhysicalName}).OfType<{nav.Principal.GetOtherSideCsTypeName()}>()");

                        } else if (node is ChildrenAggregate children) {
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
                var saveCommand = new SaveCommand(rootAggregate, SaveCommand.E_Type.Create);

                // 唯一の主キーがenum型か判定
                var keys = rootAggregate.GetOwnKeys().ToArray();
                var isSingleKeyEnum = keys.Length == 1 && keys[0] is ValueMember vm1 && vm1.Type is ValueMemberTypes.StaticEnumMember;
                var isSingleKeyRefTo = keys.Length == 1 && keys[0] is RefToMember;

                if (isSingleKeyEnum) {
                    // 唯一の主キーがenum型の場合はその型の値の数だけループしてパターンを作成
                    var enumType = ((ValueMember)keys[0]).Type;
                    return $$"""
                        /// <summary>{{rootAggregate.DisplayName}}のテストデータのパターン作成</summary>
                        protected virtual IEnumerable<{{saveCommand.CsClassNameCreate}}> {{CreatePatternMethodName(rootAggregate)}}({{DUMMY_DATA_GENERATE_CONTEXT}} context) {
                            for (var i = 0; i < Enum.GetValues<{{enumType.CsDomainTypeName}}>().Length; i++) {
                                yield return {{CreateAggregateMethodName(rootAggregate)}}(context, i);
                            }
                        }
                        """;

                } else if (isSingleKeyRefTo) {
                    // 唯一の主キーがrefto型の場合は作成済みの参照先データの数だけループしてパターンを作成
                    var refTo = (RefToMember)keys[0];

                    // 参照先データの件数
                    string arrayCount;
                    if (refTo.RefTo is RootAggregate) {
                        arrayCount = $"context.{GeneratedList(refTo.RefTo)}.Count";
                    } else {
                        var pathFromRoot = new List<string>();
                        foreach (var node in refTo.RefTo.GetPathFromRoot()) {
                            if (node is RootAggregate) {
                                pathFromRoot.Add($"{GeneratedList(node)}");

                            } else if (node is ChildAggregate child) {
                                var parent = (AggregateBase?)node.PreviousNode ?? throw new InvalidOperationException("ありえない");
                                var nav = new EFCoreEntity.NavigationOfParentChild(parent, child);
                                pathFromRoot.Add($"Select(x => x.{nav.Principal.OtherSidePhysicalName}).OfType<{nav.Principal.GetOtherSideCsTypeName()}>()");

                            } else if (node is ChildrenAggregate children) {
                                var parent = (AggregateBase?)node.PreviousNode ?? throw new InvalidOperationException("ありえない");
                                var nav = new EFCoreEntity.NavigationOfParentChild(parent, children);
                                pathFromRoot.Add($"SelectMany(x => x.{nav.Principal.OtherSidePhysicalName})");
                            }
                        }
                        arrayCount = $"context.{pathFromRoot.Join(".")}.Count()";
                    }

                    return $$"""
                        /// <summary>{{rootAggregate.DisplayName}}のテストデータのパターン作成</summary>
                        protected virtual IEnumerable<{{saveCommand.CsClassNameCreate}}> {{CreatePatternMethodName(rootAggregate)}}({{DUMMY_DATA_GENERATE_CONTEXT}} context) {
                            var count = {{arrayCount}};
                            for (var i = 0; i < count; i++) {
                                yield return {{CreateAggregateMethodName(rootAggregate)}}(context, i);
                            }
                        }
                        """;

                } else {
                    // 主キーが特殊でない場合はとりあえず適当に20件作成
                    return $$"""
                        /// <summary>{{rootAggregate.DisplayName}}のテストデータのパターン作成</summary>
                        protected virtual IEnumerable<{{saveCommand.CsClassNameCreate}}> {{CreatePatternMethodName(rootAggregate)}}({{DUMMY_DATA_GENERATE_CONTEXT}} context) {
                            for (var i = 0; i < 20; i++) {
                                yield return {{CreateAggregateMethodName(rootAggregate)}}(context, i);
                            }
                        }
                        """;
                }
            }

            static string RenderCreateRootAggregateMethod(RootAggregate rootAggregate) {
                var saveCommand = new SaveCommand(rootAggregate, SaveCommand.E_Type.Create);

                return $$"""
                    /// <summary>{{rootAggregate.DisplayName}}の作成コマンドのインスタンスを作成して返します。</summary>
                    protected virtual {{saveCommand.CsClassNameCreate}} {{CreateAggregateMethodName(rootAggregate)}}({{DUMMY_DATA_GENERATE_CONTEXT}} context, int itemIndex) {
                        return new {{saveCommand.CsClassNameCreate}} {
                            {{WithIndent(RenderBody(saveCommand), "        ")}}
                        };
                    }
                    """;

                static IEnumerable<string> RenderBody(SaveCommand saveCommand) {

                    // 唯一の主キーがenumまたはreftoかを判定
                    var keys = saveCommand.Aggregate.GetOwnKeys().ToArray();
                    var isSingleKeyEnum = keys.Length == 1 && keys[0] is ValueMember vm1 && vm1.Type is ValueMemberTypes.StaticEnumMember;
                    var isSingleKeyRefTo = keys.Length == 1 && keys[0] is RefToMember;

                    foreach (var member in saveCommand.GetMembers()) {
                        if (member is SaveCommand.SaveCommandValueMember vm) {

                            // 唯一の主キーがenumである場合はキー重複を避ける必要があるのでランダム値にする余地がない
                            if (isSingleKeyEnum && member.IsKey) {
                                var enumTypeName = ((ValueMember)keys[0]).Type.CsDomainTypeName;
                                var loopVar = saveCommand.Aggregate is RootAggregate
                                    ? "itemIndex"
                                    : "i";

                                yield return $$"""
                                    {{member.PhysicalName}} = Enum.GetValues<{{enumTypeName}}>().ElementAt({{loopVar}}),
                                    """;
                                continue;
                            }

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

                            var owner = refTo.Member.Owner.DisplayName.Replace("\"", "\\\"");
                            var memberName = refTo.Member.DisplayName.Replace("\"", "\\\"");
                            var refToName = refTo.Member.RefTo.DisplayName.Replace("\"", "\\\"");

                            var convertToKeyClass = refTo.Member.RefTo.GetPathFromRoot().Any(agg => agg is ChildrenAggregate)
                                ? $".SelectMany(x => {keyClass.ClassName}.{KeyClass.KeyClassEntry.FROM_SAVE_COMMAND}(x))"
                                : $".Select(x => {keyClass.ClassName}.{KeyClass.KeyClassEntry.FROM_SAVE_COMMAND}(x))";

                            if (refTo.Member.IsKey) {
                                // refがキーの場合はキー重複を防ぐためインデックス順に振る
                                var loopVar = saveCommand.Aggregate is RootAggregate
                                    ? "itemIndex"
                                    : "i";
                                yield return $$"""
                                    {{member.PhysicalName}} = context.{{GeneratedList(refToRoot)}}
                                        {{convertToKeyClass}}
                                        .ElementAtOrDefault({{loopVar}})
                                        ?? throw new InvalidOperationException($"{{owner}}の{{memberName}}のキー重複を防ぐため{{refToName}}には少なくとも{{{loopVar}} + 1}件のデータがある必要がありますが、{context.{{GeneratedList(refToRoot)}}.Count}件しかありません。"),
                                    """;

                            } else if (refTo.Member.IsRequired) {
                                // refが必須の場合はその時点で参照先が1件も無いときに例外を出す
                                yield return $$"""
                                    {{member.PhysicalName}} = context.{{GeneratedList(refToRoot)}}.Count == 0
                                        ? throw new InvalidOperationException("{{owner}}の{{memberName}}に設定するためのインスタンスを探そうとしましたが、{{refToName}}が1件も作成されていません。")
                                        : context.{{GeneratedList(refToRoot)}}
                                            {{convertToKeyClass}}
                                            .ElementAt(context.Random.Next(0, context.{{GeneratedList(refToRoot)}}.Count)),
                                    """;

                            } else {
                                // 必須でない場合はその時点で参照先が1件も無いときはnull
                                yield return $$"""
                                    {{member.PhysicalName}} = context.{{GeneratedList(refToRoot)}}.Count == 0
                                        ? null
                                        : context.{{GeneratedList(refToRoot)}}
                                            {{convertToKeyClass}}
                                            .ElementAt(context.Random.Next(0, context.{{GeneratedList(refToRoot)}}.Count)),
                                    """;
                            }

                        } else if (member is SaveCommand.SaveCommandChildMember child) {
                            yield return $$"""
                                {{member.PhysicalName}} = new() {
                                    {{WithIndent(RenderBody(child), "    ")}}
                                },
                                """;

                        } else if (member is SaveCommand.SaveCommandChildrenMember children) {
                            // childrenの唯一の主キーがenumまたはreftoかを判定
                            var childrenKeys = children.Aggregate.GetOwnKeys().ToArray();
                            var isChildrenSingleKeyEnum = childrenKeys.Length == 1 && childrenKeys[0] is ValueMember vm2 && vm2.Type is ValueMemberTypes.StaticEnumMember;
                            var isChildrenSingleKeyRefTo = childrenKeys.Length == 1 && childrenKeys[0] is RefToMember;

                            if (isChildrenSingleKeyEnum) {
                                // キー重複を防ぐためにはenum型の場合はランダム値にする余地がないので Enum.GetValuesの数だけループ。
                                var enumType = ((ValueMember)childrenKeys[0]).Type;

                                yield return $$"""
                                    {{member.PhysicalName}} = Enumerable.Range(0, Enum.GetValues<{{enumType.CsDomainTypeName}}>().Length).Select(i => new {{children.CsClassNameCreate}} {
                                        {{WithIndent(RenderBody(children), "    ")}}
                                    }).ToList(),
                                    """;

                            } else if (isChildrenSingleKeyRefTo) {
                                // キー重複を防ぐためにはreftoの場合はランダム値にする余地がないので 参照先のデータの数だけループ。
                                var refToMember = (RefToMember)childrenKeys[0];
                                yield return $$"""
                                    {{member.PhysicalName}} = Enumerable.Range(0, context.{{GeneratedList(refToMember.RefTo)}}.Count).Select(i => new {{children.CsClassNameCreate}} {
                                        {{WithIndent(RenderBody(children), "    ")}}
                                    }).ToList(),
                                    """;

                            } else {
                                // キーが特殊でない場合は適当にとりあえず4件作成する
                                yield return $$"""
                                    {{member.PhysicalName}} = Enumerable.Range(0, 4).Select(i => new {{children.CsClassNameCreate}} {
                                        {{WithIndent(RenderBody(children), "    ")}}
                                    }).ToList(),
                                    """;
                            }


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
            // データフロー順に並び替え
            var rootAggregatesOrderByDataFlow = _rootAggregates
                .OrderByDataFlow()
                .ToArray();

            return new SourceFile {
                FileName = "DummyDataGenerateContext.cs",
                Contents = $$"""
                    using System;

                    namespace {{ctx.Config.RootNamespace}};

                    /// <summary>ダミーデータ作成処理コンテキスト情報</summary>
                    public sealed class {{DUMMY_DATA_GENERATE_CONTEXT}} {
                        public required Random Random { get; init; }
                        public required {{Metadata.CS_CLASSNAME}} Metadata { get; init; }

                        #region シーケンス
                        private int _sequence = 0;
                        /// <summary>
                        /// シーケンス値を取得します。
                        /// この値はルート集約単位で一意です。
                        /// </summary>
                        public int GetNextSequence() {
                            return _sequence++;
                        }
                        /// <summary>
                        /// シーケンス値をリセットします。
                        /// </summary>
                        public void ResetSequence() {
                            _sequence = 0;
                        }
                        #endregion シーケンス

                    {{rootAggregatesOrderByDataFlow.SelectTextTemplate(agg => $$"""
                        {{WithIndent(RenderGetRefTo(agg), "    ")}}
                    """)}}
                    }
                    """,
            };

            static string RenderGetRefTo(RootAggregate aggregate) {
                var saveCommand = new SaveCommand(aggregate, SaveCommand.E_Type.Create);

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


        #region オプションクラス
        private SourceFile RenderDummyDataGenerateOptionsCSharp(CodeRenderingContext ctx) {
            // データフロー順に並び替え
            var rootAggregatesOrderByDataFlow = _rootAggregates
                .OrderByDataFlow()
                .ToArray();

            return new SourceFile {
                FileName = "DummyDataGenerateOptions.cs",
                Contents = $$"""
                    using System.Text.Json.Serialization;

                    namespace {{ctx.Config.RootNamespace}};

                    /// <summary>
                    /// ダミーデータ作成処理のオプション
                    /// </summary>
                    public sealed class {{DUMMY_DATA_GENERATE_OPTIONS}} {
                    {{rootAggregatesOrderByDataFlow.SelectTextTemplate(agg => $$"""
                        /// <summary>{{agg.DisplayName}}およびその子孫テーブルのダミーデータを作成するかどうか</summary>
                        [JsonPropertyName("{{agg.PhysicalName}}")]
                        public bool {{agg.PhysicalName}} { get; set; } = true;
                    """)}}
                    }
                    """,
            };
        }
        private SourceFile RenderDummyDataGenerateOptionsTypeScript(CodeRenderingContext ctx) {
            // データフロー順に並び替え
            var rootAggregatesOrderByDataFlow = _rootAggregates
                .OrderByDataFlow()
                .ToArray();

            return new SourceFile {
                FileName = "DummyDataGenerateOptions.ts",
                Contents = $$"""
                    /** ダミーデータ作成処理のオプション */
                    export type {{DUMMY_DATA_GENERATE_OPTIONS}} = {
                    {{rootAggregatesOrderByDataFlow.SelectTextTemplate(agg => $$"""
                      {{agg.PhysicalName}}: boolean
                    """)}}
                    }
                    /** ダミーデータ作成処理のオプション新規作成関数 */
                    export const createNewDummyDataGenerateOptions = (): {{DUMMY_DATA_GENERATE_OPTIONS}} => ({
                    {{rootAggregatesOrderByDataFlow.SelectTextTemplate(agg => $$"""
                      {{agg.PhysicalName}}: true,
                    """)}}
                    })
                    """,
            };
        }
        #endregion オプションクラス
    }
}
