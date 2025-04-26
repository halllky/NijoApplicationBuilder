using Nijo.ImmutableSchema;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.CodeGenerating.Helpers;

/// <summary>
/// マッピング処理の文脈で値メンバーを一意に識別する。
/// 単に <see cref="XElement"/> で識別すると、同じ集約が別の集約に複数経路でref-toしたときに
/// 誤って別のプロパティの値からマッピングしてしまう、という問題を解決する。
///
/// 「末端のValueMemberのXML要素」と「ref-toのエントリーポイント」の情報で識別すれば一意になる。
///
/// <code>
/// // スキーマ定義の例
/// - 従業員
///   - ID (word)
///   - 氏名 (word)
/// - 部署
///   - ID (word)
///   - 部署名 (word)
///   - 主担当 (ref-to:従業員)
///   - 副担当 (ref-to:従業員)
/// </code>
/// <code>
/// // 生成したいコードの例
/// 部署DbEntity ConvertTo部署DbEntity(部署SaveCommand saveCommand) {
///     return new 部署DbEntity {
///         ID = saveCommand.ID,
///         部署名 = saveCommand.部署名,
///         主担当 = new() {
///             ID = saveCommand.主担当.ID,
///             氏名 = saveCommand.主担当.氏名, // XMLスキーマ定義上は副担当の「氏名」と同じ
///         },
///         副担当 = new() {
///             ID = saveCommand.副担当.ID,
///             氏名 = saveCommand.副担当.氏名, // XMLスキーマ定義上は主担当の「氏名」と同じ
///         },
///     };
/// }
/// </code>
/// </summary>
public class SchemaNodeIdentity {

    /// <summary>
    /// 新しい <see cref="SchemaNodeIdentity"/> のインスタンスを作成する
    /// </summary>
    public static SchemaNodeIdentity Create(ISchemaPathNode node) {
        var xElements = new List<XElement>();

        // ref-toのエントリーポイントを列挙する
        var refToEntries = node
            .GetPathFromEntry()
            .OfType<RefToMember>();
        foreach (var refTo in refToEntries) {
            xElements.Add(((ISchemaPathNode)refTo).XElement);
        }

        xElements.Add(node.XElement);

        var pathForDebug = xElements.Select(el => el.Name.LocalName).Join(" > ");
        var hashCode = xElements.Aggregate(0, (agg, curr) => HashCode.Combine(agg, curr.GetHashCode()));

        return new SchemaNodeIdentity(pathForDebug, hashCode);
    }

    private SchemaNodeIdentity(string pathForDebug, int hashCode) {
        _pathForDebug = pathForDebug;
        _hashCode = hashCode;
    }

    private readonly string _pathForDebug;
    private readonly int _hashCode;

    public override int GetHashCode() {
        return _hashCode;
    }
    public override bool Equals(object? obj) {
        if (obj is not SchemaNodeIdentity id) return false;
        return id._hashCode == this._hashCode;
    }
    public override string ToString() {
        return _pathForDebug;
    }
    public static bool operator ==(SchemaNodeIdentity? left, SchemaNodeIdentity? right) {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left._hashCode == right._hashCode;
    }
    public static bool operator !=(SchemaNodeIdentity? left, SchemaNodeIdentity? right) {
        return !(left == right);
    }

}

partial class CodeGeneratingHelperExtensions {

    /// <summary>
    /// マッピング処理の文脈で値メンバーを一意に識別するための識別子に変換する
    /// </summary>
    public static SchemaNodeIdentity ToMappingKey(this ISchemaPathNode node) {
        return SchemaNodeIdentity.Create(node);
    }

}
