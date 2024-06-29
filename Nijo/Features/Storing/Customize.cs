using Nijo.Core;
using Nijo.Parts;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Features.Storing {
    /// <summary>
    /// 保存機能にかかわるカスタマイズ仕様
    /// </summary>
    internal class Customize {
        internal Customize(GraphNode<Aggregate> aggregate) {
            _aggregate = aggregate;
        }
        private readonly GraphNode<Aggregate> _aggregate;

        internal string CreatingMethodName => $"On{_aggregate.Item.PhysicalName}Creating";
        internal string UpdatingMethodName => $"On{_aggregate.Item.PhysicalName}Updating";
        internal string DeletingMethodName => $"On{_aggregate.Item.PhysicalName}Deleting";
        internal string CreatedMethodName => $"On{_aggregate.Item.PhysicalName}Created";
        internal string UpdatedMethodName => $"On{_aggregate.Item.PhysicalName}Updated";
        internal string DeletedMethodName => $"On{_aggregate.Item.PhysicalName}Deleted";

        internal static string BEFORE_CREATE_EVENT_ARGS = "IBeforeCreateEventArgs";
        internal static string BEFORE_UPDATE_EVENT_ARGS = "IBeforeUpdateEventArgs";
        internal static string BEFORE_DELETE_EVENT_ARGS = "IBeforeDeleteEventArgs";
        internal static string AFTER_CREATE_EVENT_ARGS = "IAfterCreateEventArgs";
        internal static string AFTER_UPDATE_EVENT_ARGS = "IAfterUpdateEventArgs";
        internal static string AFTER_DELETE_EVENT_ARGS = "IAfterDeleteEventArgs";

        internal static void RenderBaseClasses(CodeRenderingContext ctx) {
            ctx.WebApiProject.UtilDir(utilDir => {
                utilDir.Generate(new SourceFile {
                    FileName = $"SaveEventArgs.cs",
                    RenderContent = ctx => {
                        return $$"""
                            namespace {{ctx.Config.RootNamespace}} {

                                #region 更新前イベント引数
                                public interface IBeforeSaveEventArg {
                                    /// <summary>
                                    /// 更新処理を実行してもよいかどうかをユーザーに問いかけるメッセージを追加します。
                                    /// ボタンの意味を統一してユーザーが混乱しないようにするため、
                                    /// 「はい(Yes)」を選択したときに処理が続行され、
                                    /// 「いいえ(No)」を選択したときに処理が中断されるような文言にしてください。
                                    /// </summary>
                                    void AddConfirm(string message);

                                    /// <summary>
                                    /// trueの場合、 <see cref="AddConfirm" /> による警告があっても更新処理が続行されます。
                                    /// 画面側で警告に対して「はい(Yes)」が選択されたあとのリクエストではこの値がtrueになります。
                                    /// </summary>
                                    bool IgnoreConfirm { get; }

                                    /// <summary>
                                    /// エラーを追加します。更新処理は実行されなくなります。
                                    /// </summary>
                                    /// <param name="key">UI上で強調表示されるメンバーへのパス。HTMLのformのnameのルールに従ってください。</param>
                                    void AddError(string key, string message);
                                }
                                public interface {{BEFORE_CREATE_EVENT_ARGS}}<TSaveCommand> : IBeforeSaveEventArg {
                                    /// <summary>作成されるデータ</summary>
                                    TSaveCommand Data { get; }
                                }
                                public interface {{BEFORE_UPDATE_EVENT_ARGS}}<TSaveCommand> : IBeforeSaveEventArg {
                                    /// <summary>更新前データ</summary>
                                    TSaveCommand Before { get; }
                                    /// <summary>更新後データ</summary>
                                    TSaveCommand After { get; }
                                }
                                public interface {{BEFORE_DELETE_EVENT_ARGS}}<TSaveCommand> : IBeforeSaveEventArg {
                                    /// <summary>削除されるデータ</summary>
                                    TSaveCommand Data { get; }
                                }
                                #endregion 更新前イベント引数

                                #region 更新後イベント引数
                                public interface {{AFTER_CREATE_EVENT_ARGS}}<TSaveCommand> {
                                    /// <summary>作成されたデータ</summary>
                                    TSaveCommand Created { get; }
                                }
                                public interface {{AFTER_UPDATE_EVENT_ARGS}}<TSaveCommand> {
                                    /// <summary>更新前データ</summary>
                                    TSaveCommand BeforeUpdate { get; }
                                    /// <summary>更新後データ</summary>
                                    TSaveCommand AfterUpdate { get; }
                                }
                                public interface {{AFTER_DELETE_EVENT_ARGS}}<TSaveCommand> {
                                    /// <summary>削除されたデータ</summary>
                                    TSaveCommand Deleted { get; }
                                }
                                #endregion 更新後イベント引数
                            }
                            """;
                    },
                });
            });

            // 将来の変更容易性の確保のため、具象クラスは簡単に参照できないようにprivate宣言で作る
            ctx.AddAppSrvMethod($$"""
                #region 更新イベント引数
                private class BeforeSaveEventArg {
                    public required bool IgnoreConfirm { get; init; }
                    public List<string> Confirms { get; } = new();
                    public List<(string Key, string Message)> Errors { get; } = new();

                    public void AddConfirm(string message) => Confirms.Add(message);
                    public void AddError(string key, string message) => Errors.Add((key, message));
                }
                private class BeforeCreateEventArgs<TSaveCommand> : BeforeSaveEventArg, {{BEFORE_CREATE_EVENT_ARGS}}<TSaveCommand> {
                    public required TSaveCommand Data { get; init; }
                }
                private class BeforeUpdateEventArgs<TSaveCommand> : BeforeSaveEventArg, {{BEFORE_UPDATE_EVENT_ARGS}}<TSaveCommand> {
                    public required TSaveCommand Before { get; init; }
                    public required TSaveCommand After { get; init; }
                }
                private class BeforeDeleteEventArgs<TSaveCommand> : BeforeSaveEventArg, {{BEFORE_DELETE_EVENT_ARGS}}<TSaveCommand> {
                    public required TSaveCommand Data { get; init; }
                }

                private class AfterCreateEventArgs<TSaveCommand> : {{AFTER_CREATE_EVENT_ARGS}}<TSaveCommand> {
                    public required TSaveCommand Created { get; init; }
                }
                private class AfterUpdateEventArgs<TSaveCommand> : {{AFTER_UPDATE_EVENT_ARGS}}<TSaveCommand> {
                    public required TSaveCommand BeforeUpdate { get; init; }
                    public required TSaveCommand AfterUpdate { get; init; }
                }
                private class AfterDeleteEventArgs<TSaveCommand> : {{AFTER_DELETE_EVENT_ARGS}}<TSaveCommand> {
                    public required TSaveCommand Deleted { get; init; }
                }
                #endregion 更新イベント引数
                """);
        }
    }
}
