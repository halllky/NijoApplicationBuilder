using Nijo.Core;
using Nijo.Models.ReadModel2Features;
using Nijo.Parts.WebServer;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.WriteModel2Features {
    /// <summary>
    /// 保存処理関連のコンテキスト引数
    /// </summary>
    internal class SaveContext : ISummarizedFile {

        private readonly List<GraphNode<Aggregate>> _writeModels = new();
        internal void AddWriteModel(GraphNode<Aggregate> rootAggregate) {
            _writeModels.Add(rootAggregate);
        }

        /// <summary>
        /// 一括更新処理全体を通しての状態を持つクラス。
        /// カスタマイズ処理の中でこのクラスを直に参照することはない想定。
        /// </summary>
        internal const string STATE_CLASS_NAME = "BatchUpdateState";
        /// <summary>
        /// データ作成・更新・削除前イベント引数
        /// </summary>
        internal const string BEFORE_SAVE = "BeforeSaveEventArgs";
        /// <summary>
        /// データ作成・更新・削除の後、トランザクションのコミット前に実行されるイベントの引数
        /// </summary>
        internal const string AFTER_SAVE_EVENT_ARGS = "AfterSaveEventArgs";
        /// <summary>
        /// 一括更新処理の細かい挙動を呼び出し元で指定できるようにするためのオプション
        /// </summary>
        internal const string SAVE_OPTIONS = "SaveOptions";

        void ISummarizedFile.OnEndGenerating(CodeRenderingContext context) {
            context.CoreLibrary.UtilDir(utilDir => {
                utilDir.Generate(Render());
            });
        }

        internal SourceFile Render() => new SourceFile {
            FileName = "SaveContext.cs",
            RenderContent = context => {
                var saveCommands = _writeModels.Select(agg => new {
                    CreateCommand = new DataClassForSave(agg, DataClassForSave.E_Type.Create),
                    SaveCommand = new DataClassForSave(agg, DataClassForSave.E_Type.UpdateOrDelete),
                });

                return $$"""
                    using System.Text.Json.Nodes;

                    namespace {{context.Config.RootNamespace}};

                    /// <summary>
                    /// 一括更新処理の細かい挙動を呼び出し元で指定できるようにするためのオプション
                    /// </summary>
                    public partial class {{SAVE_OPTIONS}} {
                        /// <summary>
                        /// trueの場合、 <see cref="AddConfirm" /> による警告があっても更新処理が続行されます。
                        /// 画面側で警告に対して「はい(Yes)」が選択されたあとのリクエストではこの値がtrueになります。
                        /// </summary>
                        public required bool IgnoreConfirm { get; init; }
                    }

                    /// <summary>
                    /// 一括更新処理全体を通しての状態を持つクラス。
                    /// カスタマイズ処理の中でこのクラスを直に参照することはない想定。
                    /// </summary>
                    public partial class {{STATE_CLASS_NAME}} {
                        public {{STATE_CLASS_NAME}}({{SAVE_OPTIONS}} options) {
                            Options = options;
                        }

                        public {{SAVE_OPTIONS}} Options { get; }

                        /// <summary>
                        /// エラーが出ていたとしても強制的にトランザクションをコミットするかどうか。
                        /// 一度でもtrueになった後はfalseに戻ることはない。
                        /// </summary>
                        public bool ForceCommit { get; private set; } = false;
                        /// <summary>
                        /// たとえエラーが出ていても処理終了時にトランザクションをコミットするよう指定します。
                        /// </summary>
                        public void ShouldCommit() {
                            ForceCommit = true;
                        }

                        #region メッセージ
                        private readonly Dictionary<int, {{DisplayMessageContainer.ABSTRACT_CLASS}}> _errors = new();
                        /// <summary>
                        /// メッセージデータの入れ物のインスタンスを、一括更新の引数の配列のインデックスと紐づけて登録します。
                        /// 「○件目でエラーが発生しました」といったように何番目のデータでエラーなどが起きたかを表示するのに必要になります。
                        /// </summary>
                        public void RegisterErrorDataWithIndex(int errorItemIndex, {{DisplayMessageContainer.ABSTRACT_CLASS}} errorData) {
                            _errors[errorItemIndex] = errorData;
                        }
                        public bool HasError() {
                            return _errors.Values.Any(e => e.HasError());
                        }
                        public JsonNode GetErrorDataJson() {
                            var arr = new JsonArray();
                            foreach (var kv in _errors) {
                                foreach (var pathAndMessages in kv.Value.ToReactHookFormErrors()) {
                                    pathAndMessages.Insert(0, kv.Key); // 何番目のデータでエラーが発生したかの情報を加える
                                    arr.Add(pathAndMessages);
                                }
                            }
                            return arr;
                        }
                        #endregion メッセージ

                        #region 警告
                        private readonly List<string> _confirms = new();
                        public void AddConfirm(string message) {
                            _confirms.Add(message);
                        }
                        public bool HasConfirm() {
                            // 「保存します。よろしいですか？」の確認があるので常に真
                            return true;
                            // return _confirms.Count > 0 || _errors.Values.Any(x => x.HasConfirm());
                        }
                        public IEnumerable<string> GetConfirms() {
                            if (_confirms.Count > 0) {
                                return _confirms;
                            } else if (_errors.Values.Any(x => x.HasConfirm())) {
                                return ["警告があります。続行してよいですか？"];
                            } else {
                                return ["保存します。よろしいですか？"];
                            }
                        }
                        #endregion 警告
                    }

                    /// <summary>
                    /// 一括更新処理のデータ1件分のコンテキスト引数。エラーメッセージや確認メッセージなどを書きやすくするためのもの。
                    /// </summary>
                    /// <typeparam name="TMessage">ユーザーに通知するメッセージデータの構造体</typeparam>
                    public partial class {{BEFORE_SAVE}}<TMessage> {
                        public {{BEFORE_SAVE}}({{STATE_CLASS_NAME}} state, TMessage messages) {
                            _state = state;
                            Messages = messages;
                        }
                        private readonly {{STATE_CLASS_NAME}} _state;

                        /// <inheritdoc cref="{{SAVE_OPTIONS}}" />
                        public {{SAVE_OPTIONS}} Options => _state.Options;

                        /// <summary>ユーザーに通知するメッセージデータ</summary>
                        public TMessage Messages { get; }

                        /// <summary>
                        /// <para>
                        /// 更新処理を実行してもよいかどうかをユーザーに問いかけるメッセージを追加します。
                        /// </para>
                        /// <para>
                        /// ボタンの意味を統一してユーザーが混乱しないようにするため、
                        /// 「はい(Yes)」を選択したときに処理が続行され、
                        /// 「いいえ(No)」を選択したときに処理が中断されるような文言にしてください。
                        /// </para>
                        /// <para>
                        /// <see cref="IgnoreConfirm"/> がfalseのリクエストで何らかのコンファームが発生した場合、
                        /// 更新処理は中断されます。
                        /// </para>
                        /// </summary>
                        public void AddConfirm(string message) {
                            _state.AddConfirm(message);
                        }

                        /// <summary>
                        /// 警告が1件以上あるかどうかを返します。
                        /// </summary>
                        public bool HasConfirm() {
                            return _state.HasConfirm();
                        }
                    }

                    /// <summary>
                    /// 更新後イベント引数
                    /// </summary>
                    public partial class {{AFTER_SAVE_EVENT_ARGS}} {
                        public {{AFTER_SAVE_EVENT_ARGS}}({{STATE_CLASS_NAME}} batchUpdateState) {
                            _batchUpdateState = batchUpdateState;
                        }
                        protected readonly {{STATE_CLASS_NAME}} _batchUpdateState;
                    }
                    """;
            },
        };
    }
}
