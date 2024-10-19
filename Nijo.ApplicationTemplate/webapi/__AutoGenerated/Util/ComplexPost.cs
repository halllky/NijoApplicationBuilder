using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using NIJO_APPLICATION_TEMPLATE;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace NIJO_APPLICATION_TEMPLATE_WebApi {

    /// <summary>
    /// Reactフック側の処理と組み合わせて複雑な挙動を実現するPOSTリクエスト。
    /// 例えば後述のような挙動を実現する。
    /// 詳細な挙動を調べる場合はReact側のcomplexPost関連のソースも併せて参照のこと。
    /// 
    /// <list type="bullet">
    /// <item>ブラウザからサーバーへのリクエストで入力フォームの内容とファイル内容を同時に送信し（multipart/form-data）、サーバー側ではそれを意識せず利用できるようにする</item>
    /// <item>「～ですがよろしいですか？」の確認ダイアログの表示と、それがOKされたときに同じ内容のリクエストを再送信する</item>
    /// <item>POSTレスポンスの結果を React hook forms のsetErrorを利用して画面上の各項目の脇に表示</item>
    /// <item>POSTレスポンスで返されたファイルのダウンロードを自動的に開始する</item>
    /// <item>POSTレスポンスのタイミングで React Router を使った別画面へのリダイレクト</item>
    /// </list>
    /// </summary>
    [ModelBinder(BinderType = typeof(GenericComplexPostRequestBinder))]
    public class ComplexPostRequest<T> : ComplexPostRequest {
#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
        /// <summary>
        /// 入力フォームの内容
        /// </summary>
        public T Data { get; set; }
#pragma warning restore CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
    }


    /// <inheritdoc cref="ComplexPostRequest{T}"/>
    [ModelBinder(BinderType = typeof(ComplexPostRequestBinder))]
    public class ComplexPostRequest {
        /// <summary>
        /// 「～ですがよろしいですか？」の確認を無視します。
        /// </summary>
        public bool IgnoreConfirm { get; set; }


        #region HTTPリクエストとC#クラスの変換
        /// <summary>
        /// HTTPリクエストの内容をパースして <see cref="ComplexPostRequest{T}"/> クラスのインスタンスを作成します。
        /// </summary>
        protected class GenericComplexPostRequestBinder : IModelBinder {

            public Task BindModelAsync(ModelBindingContext bindingContext) {
                try {
                    var options = new JsonSerializerOptions();
                    NIJO_APPLICATION_TEMPLATE.Util.ModifyJsonSrializerOptions(options);
                    options.Converters.Add(new MultipartFormDataFileConevrter(bindingContext.HttpContext));

                    // data
                    var dataJson = bindingContext.HttpContext.Request.Form[PARAM_DATA];
                    var dataType = bindingContext.ModelType.GenericTypeArguments[0];
                    var parsedData = JsonSerializer.Deserialize(dataJson!, dataType, options);

                    // ignoreConfirm
                    var ignoreConfirm = bindingContext.HttpContext.Request.Form.TryGetValue(PARAM_IGNORE_CONFIRM, out var strIgnoreConfirm)
                        ? (bool.TryParse(strIgnoreConfirm, out var b) ? b : false)
                        : false;

                    // パラメータクラスのインスタンスを作成
                    var instance = Activator.CreateInstance(bindingContext.ModelType) ?? throw new NullReferenceException();
                    bindingContext.ModelType.GetProperty(nameof(ComplexPostRequest<object>.Data))!.SetValue(instance, parsedData);
                    bindingContext.ModelType.GetProperty(nameof(IgnoreConfirm))!.SetValue(instance, ignoreConfirm);

                    bindingContext.Result = ModelBindingResult.Success(instance);
                    return Task.CompletedTask;

                } catch (Exception) {
                    bindingContext.Result = ModelBindingResult.Failed();
                    return Task.CompletedTask;
                }
            }

            /// <summary>
            /// <see cref="ComplexPostRequest{T}"/> の仕組みでは、
            /// HTTPリクエストで入力フォームの内容とファイル送信が同時に行われるとき、
            /// ファイル内容のバイナリは Content-Type: multipart/form-data の別パートとして分離して送信されるようにし、
            /// 元の入力フォームでは当該別パートのNameだけを保持している。
            /// このコンバータは、別々のパートに分離されたファイルのオブジェクトを、入力フォームのJSONの元々そのファイルが添付されていた位置に復元する役割を持つ。
            /// </summary>
            private class MultipartFormDataFileConevrter : JsonConverter<IFormFile> {
                public MultipartFormDataFileConevrter(HttpContext httpContext) {
                    _httpContext = httpContext;
                }
                private readonly HttpContext _httpContext;

                public override IFormFile? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
                    // クライアント側のReactフック内でHTTPリクエストボディを組み立てる際に発行される一意なUUIDを使って該当のファイルを探す
                    var fileId = reader.GetString();
                    if (fileId == null) return null;
                    return _httpContext.Request.Form.Files.GetFile(fileId);
                }

                public override void Write(Utf8JsonWriter writer, IFormFile value, JsonSerializerOptions options) {
                    // サーバー側からクライアント側へJSONの一部としてファイルを送ることはない。
                }
            }
        }

        /// <summary>
        /// HTTPリクエストの内容をパースして <see cref="ComplexPostRequest"/> クラスのインスタンスを作成します。
        /// </summary>
        protected class ComplexPostRequestBinder : IModelBinder {
            public Task BindModelAsync(ModelBindingContext bindingContext) {

                var instance = new ComplexPostRequest();

                instance.IgnoreConfirm = bindingContext.HttpContext.Request.Form.TryGetValue(PARAM_IGNORE_CONFIRM, out var strIgnoreConfirm)
                    ? (bool.TryParse(strIgnoreConfirm, out var b) ? b : false)
                    : false;

                bindingContext.Result = ModelBindingResult.Success(instance);

                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// multipart/form-data 内の入力内容データJSONの項目のキー。
        /// この名前はReact側の処理と一致させておく必要がある。
        /// </summary>
        private const string PARAM_DATA = "data";
        /// <summary>
        /// multipart/form-data 内の入力内容データJSONの項目のキー。
        /// この名前はReact側の処理と一致させておく必要がある。
        /// </summary>
        private const string PARAM_IGNORE_CONFIRM = "ignoreConfirm";
        #endregion HTTPリクエストとC#クラスの変換
    }


    /// <summary>
    /// Reactフック側の処理と組み合わせて複雑な挙動を実現するHTTPレスポンス。
    /// 例えば後述のような挙動を実現する。
    /// 詳細な挙動を調べる場合はReact側のcomplexPost関連のソースも併せて参照のこと。
    /// 
    /// <list type="bullet">
    /// <item>ブラウザからサーバーへのリクエストで入力フォームの内容とファイル内容を同時に送信し（multipart/form-data）、サーバー側ではそれを意識せず利用できるようにする</item>
    /// <item>「～ですがよろしいですか？」の確認ダイアログの表示と、それがOKされたときに同じ内容のリクエストを再送信する</item>
    /// <item>POSTレスポンスの結果を React hook forms のsetErrorを利用して画面上の各項目の脇に表示</item>
    /// <item>POSTレスポンスで返されたファイルのダウンロードを自動的に開始する</item>
    /// <item>POSTレスポンスのタイミングで React Router を使った別画面へのリダイレクト</item>
    /// </list>
    /// </summary>
    public static class ComplexPostResponse {
        /// <summary>
        /// 「～ですがよろしいですか？」の確認ダイアログを表示します。
        /// </summary>
        /// <param name="confirm">確認メッセージ内容</param>
        /// <param name="detail">画面内の各項目の脇に表示するメッセージ。React hook form のsetErrorで使われるオブジェクトのルールに合わせる必要がある。</param>
        public static IActionResult ShowConfirmUsingReactHook(this ControllerBase controller, IEnumerable<string> confirm, JsonObject? detail = null) {
            // このHTTPステータスコードと戻り値のオブジェクトの型は React hook 側と合わせる必要がある
            return controller.Accepted(new { confirm, detail });
        }

        /// <summary>
        /// 画面上の各項目にバリデーションエラーを表示します。
        /// </summary>
        /// <param name="detail">画面内の各項目の脇に表示するメッセージ。React hook form のsetErrorで使われるオブジェクトのルールに合わせる必要がある。</param>
        public static IActionResult ShowErrorsUsingReactHook(this ControllerBase controller, JsonObject? detail = null) {
            // このHTTPステータスコードと戻り値のオブジェクトの型は React hook 側と合わせる必要がある
            return controller.UnprocessableEntity(new { detail });
        }

        /// <summary>
        /// クライアント側にファイルを返し、そのダウンロードを開始させます。
        /// </summary>
        /// <param name="bytes">ファイル内容</param>
        /// <param name="contentType">HTTPレスポンスヘッダに含める Content-Type</param>
        public static IActionResult DownloadFileUsingReactHook(this ControllerBase controller, byte[] bytes, string contentType) {
            // Content-Typeが "application/json" 以外でなければならない旨は React hook 側と合わせる必要がある
            return controller.File(bytes, contentType);
        }

        /// <summary>
        /// クライアント側に処理結果を返します。
        /// </summary>
        /// <typeparam name="T">処理結果データの型</typeparam>
        /// <param name="data">処理結果データの内容</param>
        public static IActionResult ReturnsDataUsingReactHook<T>(this ControllerBase controller, T data) {
            // このHTTPステータスコードと戻り値のオブジェクトの型は React hook 側と合わせる必要がある
            return controller.Content(Util.ToJson(new { type = "data", data }), "application/json");
        }

        /// <summary>
        /// React Router 側でのリダイレクトを実行させます。
        /// （HTTPレスポンスとしてリダイレクトを行うわけではありません）
        /// </summary>
        /// <param name="url">リダイレクト先URL</param>
        public static IActionResult RedirectUsingReactHook(this ControllerBase controller, string url) {
            // このHTTPステータスコードと戻り値のオブジェクトの型は React hook 側と合わせる必要がある
            return controller.Content(Util.ToJson(new { type = "redirect", url }), "application/json");
        }

        /// <summary>
        /// 処理が成功した旨を表すトーストメッセージを表示させます。
        /// </summary>
        /// <param name="text">メッセージ。未指定の場合は既定のメッセージが表示されます。</param>
        public static IActionResult ShowSuccessMessageReactHook(this ControllerBase controller, string? text = null) {
            // このHTTPステータスコードと戻り値のオブジェクトの型は React hook 側と合わせる必要がある
            return controller.Content(Util.ToJson(new { type = "toast", text }), "application/json");
        }
    }
}
