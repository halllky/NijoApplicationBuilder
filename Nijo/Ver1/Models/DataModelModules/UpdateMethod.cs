using Nijo.Util.DotnetEx;
using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using Nijo.Ver1.Parts.Common;
using Nijo.Ver1.Parts.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nijo.Ver1.Models.DataModelModules {
    /// <summary>
    /// 更新処理
    /// </summary>
    internal class UpdateMethod {
        internal UpdateMethod(RootAggregate rootAggregate) {
            _rootAggregate = rootAggregate;
        }
        private readonly RootAggregate _rootAggregate;

        internal string MethodName => $"Update{_rootAggregate.PhysicalName}";
        internal string OnBeforeMethodName => $"OnBeforeUpdate{_rootAggregate.PhysicalName}";
        internal string OnAfterMethodName => $"OnAfterUpdate{_rootAggregate.PhysicalName}Async";

        internal string Render(CodeRenderingContext ctx) {
            var command = new SaveCommand(_rootAggregate);
            var dbEntity = new EFCoreEntity(_rootAggregate);
            var messages = new SaveCommandMessageContainer(_rootAggregate);

            return $$"""
                #region 更新処理
                /// <summary>
                /// {{_rootAggregate.DisplayName}} の更新を実行します。
                /// </summary>
                public virtual void {{MethodName}}({{command.CsClassNameUpdate}} command, {{messages.InterfaceName}} messages, {{PresentationContext.INTERFACE}} context) {
                    // TODO ver.1
                    throw new NotImplementedException();
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
                public virtual async Task {{OnAfterMethodName}}({{dbEntity.CsClassName}} newValue, {{dbEntity.CsClassName}} oldValue, {{messages.InterfaceName}} messages, {{PresentationContext.INTERFACE}} context) {
                    // このメソッドをオーバーライドして処理を実装してください。
                }
                #endregion 更新処理
                """;
        }


        #region Create/Update/Deleleで共通
        internal const string ERR_ID_SAVECHANGES = "ErrorAtSaveChanges";

        internal static void RegisterCommonParts(CodeRenderingContext ctx) {
            ctx.Use<MsgFactory>().AddMessage(
                ERR_ID_SAVECHANGES,
                "登録/更新/削除のタイミングでRDBMS上で何らかのエラーが生じた場合のメッセージ",
                "登録処理でエラーが発生しました: {0}");
        }

        /// <summary>
        /// 子孫要素の EntityState を全てDetachにしていくソースをレンダリングする。
        /// </summary>
        internal static IEnumerable<string> RenderDescendantDetaching(RootAggregate rootAggregate, string rootEntityName) {
            var descendantDbEntities = rootAggregate.EnumerateDescendants().ToArray();

            for (int i = 0; i < descendantDbEntities.Length; i++) {
                var builder = new StringBuilder();
                var paths = descendantDbEntities[i].GetFullPath().ToArray();
                var after_ = $"after{descendantDbEntities[i].PhysicalName}_{i}";

                // before, after それぞれの子孫インスタンスを一次配列に格納する
                void RenderEntityArray() {
                    var tempVar = after_;

                    if (paths.Any(path => path is ChildrenAggreagte)) {
                        // 子集約までの経路の途中に配列が含まれる場合
                        builder.Append($"var {tempVar} = {rootEntityName}");

                        var select = false;
                        foreach (var node in paths) {
                            // Children
                            if (node is ChildrenAggreagte children) {
                                var nav = new EFCoreEntity.NavigationOfParentChild((AggregateBase?)node.PreviousNode ?? throw new InvalidOperationException("ありえない"), children);
                                builder.Append(select
                                    ? $".SelectMany(x => x.{nav.Principal.OtherSidePhysicalName})"
                                    : $".{nav.Principal.OtherSidePhysicalName}");
                                select = true;
                                continue;
                            }
                            // Child
                            if (node is ChildAggreagte child) {
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
