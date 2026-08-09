using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Nijo.SchemaParsing;

namespace Nijo.WebService.SchemaEditor;

/// <summary>
/// スキーマ定義のバリデーションエラーを、XML要素のUniqueIdをキーにして引けるように整理したもの。
/// エラーを表示したあとに画面側で要素の並び替え、名称変更、などの操作があっても
/// エラーの内容を変更しないために、IDをキーにしてエラーを管理する。
/// 具体的には以下のようなJSONオブジェクトになる。
/// <code>
/// {
///   "xxxx-xxxx-...": {
///     "_own": ["xxxが不正です。", "yyyが不正です。"], // XML要素自体に対するエラー
///     "DbName": ["テーブル名が不正です。"], // XMLAttributeに対するエラー
///     "MaxLength": ["この項目に最大文字数は設定できません。"], // XMLAttributeに対するエラー
///     ...
///   },
///   "yyyy-yyyy-...": {
///     ...
///   },
///   ...
/// }
/// </code>
/// </summary>
public class ValidationErrorMap {

    // この名前はReact側と合わせる必要がある
    private const string OWN_ERRORS = "_own";

    /// <summary>
    /// スキーマ定義のエラーをReactで使うエラー形式に変換する。
    /// </summary>
    internal static JsonObject FromValidationErrors(
        IEnumerable<SchemaParseContext.ValidationError> errors,
        IReadOnlyDictionary<XElement, string> uuidToXmlElement) {

        var result = new JsonObject();
        foreach (var error in errors) {
            var id = uuidToXmlElement.TryGetValue(error.XElement, out var found)
                ? found
                // 表示先が分からない場合はルートに表示する。
                // generate時の場合は保存されたxmlファイルを元に生成を行うのでクライアント側のIDが存在しない
                : "root";

            var thisXmlErrors = new JsonObject();
            result[id] = thisXmlErrors;

            // XML要素自体に対するエラー
            var ownErrors = new JsonArray();
            foreach (var ownError in error.OwnErrors) {
                ownErrors.Add(ownError);
            }

            // XML要素の属性に対するエラー
            thisXmlErrors[OWN_ERRORS] = ownErrors;
            foreach (var attributeError in error.AttributeErrors) {
                var attributeErrors = new JsonArray();
                foreach (var errorMessage in attributeError.Value) {
                    attributeErrors.Add(errorMessage);
                }
                thisXmlErrors[attributeError.Key] = attributeErrors;
            }
        }

        return result;
    }
}
