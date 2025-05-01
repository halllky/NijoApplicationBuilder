using Nijo.CodeGenerating;
using Nijo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.SchemaParsing;

/// <summary>
/// ノードに指定できるオプション属性
/// </summary>
public class NodeOption {
    /// <summary>
    /// XML要素で指定されるときのこのオプションのキー。
    /// XMLの属性名として使用可能な文字のみ使える。
    /// </summary>
    public required string AttributeName { get; init; }
    /// <summary>
    /// 人間にとって分かりやすい名前をつけてください。
    /// </summary>
    public required string DisplayName { get; init; }
    /// <summary>
    /// 真偽値 or 文字列
    /// </summary>
    public required E_NodeOptionType Type { get; init; }
    /// <summary>
    /// この属性に対する入力検証
    /// </summary>
    public required Action<NodeOptionValidateContext> Validate { get; init; }
    /// <summary>
    /// あるモデルのメンバーがこの属性を指定することができるかどうか。
    /// nullの場合は指定可能と判定されます。
    /// </summary>
    public Func<IModel, bool>? IsAvailableModelMembers { get; init; }

    /// <summary>
    /// このオプション属性の説明文
    /// </summary>
    public required string HelpText { get; init; }
    /// <summary>
    /// <see cref="Validate"/> の処理を自然言語で表現したもの。ドキュメント用
    /// </summary>
    public required string ValidateRuleText { get; init; }
    /// <summary>
    /// <see cref="IsAvailableModelMembers"/> の処理を自然言語で表現したもの。ドキュメント用
    /// </summary>
    public required string AvailableModelMembersText { get; init; }
}

/// <summary>
/// 真偽値 or 文字列
/// </summary>
public enum E_NodeOptionType {
    Boolean,
    String,
}

/// <summary>
/// <see cref="NodeOption.Validate"/> の引数
/// </summary>
public class NodeOptionValidateContext {
    /// <summary>XMLで指定されているこの属性の値</summary>
    public required string Value { get; init; }
    /// <summary>検証対象のXML要素</summary>
    public required XElement XElement { get; init; }
    /// <summary>ノード種別</summary>
    public required E_NodeType NodeType { get; init; }
    /// <summary>エラーがあったらここに追加</summary>
    public required Action<string> AddError { get; init; }
    /// <summary>コンテキスト情報</summary>
    public required SchemaParseContext SchemaParseContext { get; init; }
}

// ----------------------------------------

/// <summary>
/// 標準のオプション属性
/// </summary>
internal static class BasicNodeOptions {

    internal static NodeOption DisplayName = new() {
        AttributeName = "DisplayName",
        DisplayName = "表示用名称",
        Type = E_NodeOptionType.String,
        HelpText = $$"""
            物理名と表示用名称を分けたい場合に指定。
            """,
        Validate = ctx => {
            // 改行不可
            if (ctx.Value.Contains('\n')) ctx.AddError("改行を含めることはできません。");
        },
        ValidateRuleText = "改行を含めることはできません。",
        AvailableModelMembersText = "すべてのモデルのメンバーで使用可能です。",
    };

    internal static NodeOption DbName = new() {
        AttributeName = "DbName",
        DisplayName = "データベース上名称",
        Type = E_NodeOptionType.String,
        HelpText = $$"""
            データベースのテーブル名またはカラム名を明示的に指定したい場合に設定してください。
            既定では物理名がそのままテーブル名やカラム名になります。
            """,
        Validate = ctx => {
            // 改行不可
            if (ctx.Value.Contains('\n')) ctx.AddError("改行を含めることはできません。");
        },
        IsAvailableModelMembers = model => {
            if (model is DataModel) return true;
            return false;
        },
        ValidateRuleText = "改行を含めることはできません。",
        AvailableModelMembersText = "DataModelのみで使用可能です。",
    };

    internal static NodeOption LatinName = new() {
        AttributeName = "LatinName",
        DisplayName = "ラテン語名",
        Type = E_NodeOptionType.String,
        HelpText = $$"""
            URLなど、ラテン語名しか用いることができない部分の名称を明示的に指定したい場合に設定してください。
            既定では集約を表す一意な文字列から生成されたハッシュ値が用いられます。
            """,
        Validate = ctx => {
            // 改行不可
            if (ctx.Value.Contains('\n')) ctx.AddError("改行を含めることはできません。");
        },
        ValidateRuleText = "改行を含めることはできません。",
        AvailableModelMembersText = "すべてのモデルのメンバーで使用可能です。",
    };

    internal static NodeOption IsKey = new() {
        AttributeName = "IsKey",
        DisplayName = "キー",
        Type = E_NodeOptionType.Boolean,
        HelpText = $$"""
            この項目がその集約のキーであることを表します。
            ルート集約またはChildrenの場合、指定必須。
            ChildやVariationには指定不可。Commandの要素にも指定不可。
            """,
        Validate = ctx => {

        },
        IsAvailableModelMembers = model => {
            if (model is DataModel) return true;
            if (model is QueryModel) return true;
            return false;
        },
        ValidateRuleText = "検証ルールはありません。",
        AvailableModelMembersText = "DataModelとQueryModelで使用可能です。",
    };

    internal static NodeOption IsRequired = new() {
        AttributeName = "IsRequired",
        DisplayName = "必須",
        Type = E_NodeOptionType.Boolean,
        HelpText = $$"""
            必須項目であることを表します。
            新規登録処理や更新処理での必須入力チェック処理が自動生成されます。
            """,
        Validate = ctx => {

        },
        ValidateRuleText = "検証ルールはありません。",
        AvailableModelMembersText = "すべてのモデルのメンバーで使用可能です。",
    };


    #region DataModel用
    internal static NodeOption GenerateDefaultQueryModel = new() {
        AttributeName = "GenerateDefaultQueryModel",
        DisplayName = "DataModelと全く同じ型のQueryModelを生成するかどうか",
        Type = E_NodeOptionType.Boolean,
        HelpText = $$"""
            きわめて単純なマスタデータなど、データベース上のデータ構造と
            それを表示・編集する画面のデータ構造が完全一致する場合、
            この項目を指定するとDataModelと全く同じ型のQueryModelのモジュールが生成される。
            """,
        Validate = ctx => {

        },
        IsAvailableModelMembers = model => {
            return false;
        },
        ValidateRuleText = "検証ルールはありません。",
        AvailableModelMembersText = "モデルメンバーでは使用できません。",
    };
    internal static NodeOption GenerateBatchUpdateCommand = new() {
        AttributeName = "GenerateBatchUpdateCommand",
        DisplayName = "DataModelと全く同じ型のQueryModelの一括更新用のWebエンドポイント・Reactフック・アプリケーションサービスを生成するかどうか",
        Type = E_NodeOptionType.Boolean,
        HelpText = $$"""
            標準の更新ロジックで一括更新処理を生成する場合に指定。
            """,
        Validate = ctx => {
            // このオプションを使用するためにはGenerateDefaultQueryModelの指定が必須
            if (ctx.XElement.Attribute(GenerateDefaultQueryModel.AttributeName) == null) {
                ctx.AddError("このオプションを使用するためにはGenerateDefaultQueryModelの指定が必須");
            }
        },
        IsAvailableModelMembers = model => {
            return false;
        },
        ValidateRuleText = "このオプションを使用するためにはGenerateDefaultQueryModelの指定が必須です。",
        AvailableModelMembersText = "モデルメンバーでは使用できません。",
    };
    #endregion DataModel用


    #region QueryModel用
    internal static NodeOption IsReadOnly = new() {
        AttributeName = "IsReadOnly",
        DisplayName = "読み取り専用集約",
        Type = E_NodeOptionType.Boolean,
        HelpText = $$"""
            このQueryModelが読み取り専用かどうか
            """,
        Validate = ctx => {

        },
        IsAvailableModelMembers = model => {
            if (model is QueryModel) return true;
            return false;
        },
        ValidateRuleText = "検証ルールはありません。",
        AvailableModelMembersText = "QueryModelでのみ使用可能です。",
    };
    internal static NodeOption HasLifeCycle = new() {
        AttributeName = "HasLifeCycle",
        DisplayName = "独立ライフサイクル",
        Type = E_NodeOptionType.Boolean,
        HelpText = $$"""
            画面上で追加削除されるタイミングが親と異なるかどうか。
            """,
        Validate = ctx => {

        },
        IsAvailableModelMembers = model => {
            if (model is QueryModel) return true;
            return false;
        },
        ValidateRuleText = "検証ルールはありません。",
        AvailableModelMembersText = "QueryModelでのみ使用可能です。",
    };
    #endregion QueryModel用


    #region ValueMember用
    internal static NodeOption MaxLength = new() {
        AttributeName = "MaxLength",
        DisplayName = "最大長",
        Type = E_NodeOptionType.String,
        HelpText = $$"""
            文字列項目の最大長。整数で指定してください。
            """,
        Validate = ctx => {

        },
        ValidateRuleText = "検証ルールはありません。",
        AvailableModelMembersText = "すべてのモデルのメンバーで使用可能です。",
    };
    internal static NodeOption CharacterType = new() {
        AttributeName = "CharacterType",
        DisplayName = "文字種",
        Type = E_NodeOptionType.String,
        HelpText = $$"""
            文字種。半角、半角英数、など
            """,
        Validate = ctx => {

        },
        ValidateRuleText = "検証ルールはありません。",
        AvailableModelMembersText = "すべてのモデルのメンバーで使用可能です。",
    };
    internal static NodeOption TotalDigit = new() {
        AttributeName = "TotalDigit",
        DisplayName = "総合桁数",
        Type = E_NodeOptionType.String,
        HelpText = $$"""
            数値系属性の整数部桁数 + 小数部桁数
            """,
        Validate = ctx => {

        },
        ValidateRuleText = "検証ルールはありません。",
        AvailableModelMembersText = "すべてのモデルのメンバーで使用可能です。",
    };
    internal static NodeOption DecimalPlace = new() {
        AttributeName = "DecimalPlace",
        DisplayName = "小数部桁数",
        Type = E_NodeOptionType.String,
        HelpText = $$"""
            数値系属性の小数部桁数
            """,
        Validate = ctx => {

        },
        ValidateRuleText = "検証ルールはありません。",
        AvailableModelMembersText = "すべてのモデルのメンバーで使用可能です。",
    };
    internal static NodeOption SequenceName = new() {
        AttributeName = "SequenceName",
        DisplayName = "シーケンス名",
        Type = E_NodeOptionType.String,
        HelpText = $$"""
            シーケンス物理名
            """,
        Validate = ctx => {

        },
        ValidateRuleText = "検証ルールはありません。",
        AvailableModelMembersText = "すべてのモデルのメンバーで使用可能です。",
    };
    #endregion ValueMember用
}
