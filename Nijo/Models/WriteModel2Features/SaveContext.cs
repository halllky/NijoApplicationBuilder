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
        private readonly List<GraphNode<Aggregate>> _readModels = new();
        internal void AddWriteModel(GraphNode<Aggregate> rootAggregate) {
            _writeModels.Add(rootAggregate);
        }
        internal void AddReadModel(GraphNode<Aggregate> rootAggregate) {
            _readModels.Add(rootAggregate);
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
        /// 保存コマンドのインスタンスと紐づいたエラーメッセージの入れ物を返すメソッド
        /// </summary>
        internal const string GET_ERR_MSG_CONTAINER = "GetErrorMessageContainer";
        /// <summary>
        /// 一括更新処理の細かい挙動を呼び出し元で指定できるようにするためのオプション
        /// </summary>
        private const string SAVE_OPTIONS = "SaveOptions";

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
                var displayData = _readModels.Select(agg => new {
                    DisplayData = new DataClassForDisplay(agg),
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

                        #region エラー
                        private readonly Dictionary<int, {{ErrorReceiver.RECEIVER}}> _errors = new();
                        public void RegisterErrorData(int errorItemIndex, {{ErrorReceiver.RECEIVER}} errorData) {
                            _errors[errorItemIndex] = errorData;
                        }
                        public bool HasError() {
                            return _errors.Values.Any(e => e.HasError());
                        }
                        public string GetErrorDataJson() {
                            var array = new JsonArray();
                            foreach (var kv in _errors.OrderBy(kv => kv.Key)) {
                                var node = kv.Value.ToJsonNode();
                                array.Add(node);
                            }
                            return array.ToJsonString();
                        }
                        #endregion エラー

                        #region 警告
                        private readonly List<string> _confirms = new();
                        public void AddConfirm(string message) {
                            _confirms.Add(message);
                        }
                        public bool HasConfirm() {
                            return _confirms.Count > 0;
                        }
                        #endregion 警告

                        #region エラーメッセージ用ユーティリティ
                        /// <summary>更新コマンドとエラーメッセージコンテナの紐づけ</summary>
                        private readonly Dictionary<object, {{ErrorReceiver.RECEIVER}}> _errorMessageContainerDict = new();

                        /// <summary>
                        /// エラーメッセージの入れ物のオブジェクトを取得します。
                        /// 戻り値のインスタンスは引数のコマンドと紐づけられており、一括更新処理全体を通じて1つに定まります。
                        /// </summary>
                        public {{ErrorReceiver.RECEIVER}} {{GET_ERR_MSG_CONTAINER}}(object obj) {
                            if (!_errorMessageContainerDict.TryGetValue(obj, out var receiver)) {
                                // 引数のコマンドと対応するエラーメッセージが登録されていない場合はここで作成する
                                receiver = obj switch {
                    {{saveCommands.SelectTextTemplate(x => $$"""
                                    {{DataClassForSaveBase.CREATE_COMMAND}}<{{x.CreateCommand.CsClassName}}> => new {{x.CreateCommand.ErrorDataCsClassName}}(),
                                    {{DataClassForSaveBase.UPDATE_COMMAND}}<{{x.SaveCommand.CsClassName}}> => new {{x.SaveCommand.ErrorDataCsClassName}}(),
                                    {{DataClassForSaveBase.DELETE_COMMAND}}<{{x.SaveCommand.CsClassName}}> => new {{x.SaveCommand.ErrorDataCsClassName}}(),
                    """)}}
                    {{displayData.SelectTextTemplate(x => $$"""
                                    {{x.DisplayData.CsClassName}} => new {{x.DisplayData.MessageDataCsClassName}}(),
                    """)}}
                                    _ => new {{ErrorReceiver.RECEIVER}}(), // この分岐にくることはありえない
                                };
                                _errorMessageContainerDict[obj] = receiver;
                            }
                            return receiver;
                        }
                        #endregion エラーメッセージ用ユーティリティ
                    }

                    /// <summary>
                    /// 一括更新処理のデータ1件分のコンテキスト引数。エラーメッセージや確認メッセージなどを書きやすくするためのもの。
                    /// </summary>
                    /// <typeparam name="TError">ユーザーに通知するエラーデータの構造体</typeparam>
                    public partial class {{BEFORE_SAVE}}<TError> {
                        public {{BEFORE_SAVE}}({{STATE_CLASS_NAME}} state, TError errors) {
                            _state = state;
                            Errors = errors;
                        }
                        private readonly {{STATE_CLASS_NAME}} _state;

                        /// <inheritdoc cref="{{SAVE_OPTIONS}}" />
                        public {{SAVE_OPTIONS}} Options => _state.Options;

                        /// <summary>ユーザーに通知するエラーデータ</summary>
                        public TError Errors { get; }

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
                    """;
            },
        };
    }
}
