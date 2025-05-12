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
    /// このオプション属性の説明文
    /// </summary>
    public required string HelpText { get; init; }
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
    public required Func<IModel, bool> IsAvailableModelMembers { get; init; }
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
            ソースコード上にあらわれる物理名とは別に表示用名称を設けたい場合に指定してください。
            表示用名称に改行を含めることはできません。
            """,
        Validate = ctx => {
            // 改行不可
            if (ctx.Value.Contains('\n')) ctx.AddError("改行を含めることはできません。");
        },
        IsAvailableModelMembers = model => {
            return true;
        },
    };

    internal static NodeOption DbName = new() {
        AttributeName = "DbName",
        DisplayName = "データベース上名称",
        Type = E_NodeOptionType.String,
        HelpText = $$"""
            データベースのテーブル名またはカラム名を明示的に指定したい場合に設定してください。
            集約に定義した場合はテーブル名、値に定義した場合はカラム名になります。
            未指定の場合、物理名がそのままテーブル名やカラム名になります。
            ここで指定する値に改行を含めることはできません。
            """,
        Validate = ctx => {
            // 改行不可
            if (ctx.Value.Contains('\n')) ctx.AddError("改行を含めることはできません。");
        },
        IsAvailableModelMembers = model => {
            if (model is DataModel) return true;
            return false;
        },
    };

    internal static NodeOption LatinName = new() {
        AttributeName = "LatinName",
        DisplayName = "ラテン語名",
        Type = E_NodeOptionType.String,
        HelpText = $$"""
            URLなど、ラテン語名しか用いることができない部分の名称を明示的に指定したい場合に設定してください。
            既定では集約を表す一意な文字列から生成されたハッシュ値が用いられます。
            ここで指定する値に改行を含めることはできません。
            """,
        Validate = ctx => {
            // 改行不可
            if (ctx.Value.Contains('\n')) ctx.AddError("改行を含めることはできません。");
        },
        IsAvailableModelMembers = model => {
            return true;
        },
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
            var nodeType = ctx.SchemaParseContext.GetNodeType(ctx.XElement);

            // モデルの種類を判定
            if (ctx.SchemaParseContext.TryGetModel(ctx.XElement, out var model)) {
                // コマンドモデルの場合はキー属性を定義できない
                if (model is CommandModel) {
                    ctx.AddError("コマンドモデルでは主キー属性を定義できません。");
                    return;
                }

                // データモデルの子集約には主キー属性を付与できない
                if (model is DataModel && nodeType == E_NodeType.ChildAggregate) {
                    ctx.AddError("データモデルの子集約には主キー属性を付与できません。");
                    return;
                }

                // クエリモデルの子集約・子配列には主キー属性を付与できない
                if (model is QueryModel && (nodeType == E_NodeType.ChildAggregate || nodeType == E_NodeType.ChildrenAggregate)) {
                    ctx.AddError("クエリモデルの子集約・子配列には主キー属性を付与できません。");
                    return;
                }
            }
        },
        IsAvailableModelMembers = model => {
            if (model is DataModel) return true;
            if (model is QueryModel) return true;
            return false;
        },
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
            // 特に制約なし
        },
        IsAvailableModelMembers = model => {
            return true;
        },
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
            // データモデルのルート集約のみ許可
            if (ctx.NodeType != E_NodeType.RootAggregate) {
                ctx.AddError("このオプションはルート集約にのみ指定できます。");
                return;
            }

            if (ctx.SchemaParseContext.TryGetModel(ctx.XElement, out var model)) {
                if (model is not DataModel) {
                    ctx.AddError("このオプションはデータモデルにのみ指定できます。");
                }
            }
        },
        IsAvailableModelMembers = model => {
            return model is DataModel;
        },
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
                ctx.AddError($"このオプションを使用するためには{GenerateDefaultQueryModel.AttributeName}属性の指定が必須です。");
            }

            // データモデルのルート集約のみ許可
            if (ctx.NodeType != E_NodeType.RootAggregate) {
                ctx.AddError("このオプションはルート集約にのみ指定できます。");
                return;
            }

            if (ctx.SchemaParseContext.TryGetModel(ctx.XElement, out var model)) {
                if (!(model is DataModel)) {
                    ctx.AddError("このオプションはデータモデルにのみ指定できます。");
                }
            }
        },
        IsAvailableModelMembers = model => {
            return false;
        },
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
            // クエリモデルのみ許可
            if (ctx.SchemaParseContext.TryGetModel(ctx.XElement, out var model)) {
                if (!(model is QueryModel)) {
                    ctx.AddError("このオプションはクエリモデルにのみ指定できます。");
                }
            }
        },
        IsAvailableModelMembers = model => {
            if (model is QueryModel) return true;
            return false;
        },
    };
    internal static NodeOption HasLifeCycle = new() {
        AttributeName = "HasLifeCycle",
        DisplayName = "独立ライフサイクル",
        Type = E_NodeOptionType.Boolean,
        HelpText = $$"""
            画面上で追加削除されるタイミングが親と異なるかどうか。
            """,
        Validate = ctx => {
            // クエリモデルのみ許可
            if (ctx.SchemaParseContext.TryGetModel(ctx.XElement, out var model)) {
                if (!(model is QueryModel)) {
                    ctx.AddError("このオプションはクエリモデルにのみ指定できます。");
                }
            }
        },
        IsAvailableModelMembers = model => {
            if (model is QueryModel) return true;
            return false;
        },
    };
    #endregion QueryModel用


    #region CommandModel用
    internal const string REF_TO_OBJECT_DISPLAY_DATA = "DisplayData";
    internal const string REF_TO_OBJECT_SEARCH_CONDITION = "SearchCondition";

    internal static NodeOption RefToObject = new() {
        AttributeName = "RefToObject",
        DisplayName = "参照先オブジェクト",
        Type = E_NodeOptionType.String,
        HelpText = $$"""
            CommandModelはQueryModelの検索条件か画面表示用データのいずれかしか参照できない。
            その2種のうちどちらを参照するかの指定。
            "{{REF_TO_OBJECT_DISPLAY_DATA}}"か"{{REF_TO_OBJECT_SEARCH_CONDITION}}"のみ指定可能。
            """,
        Validate = ctx => {
            if (ctx.Value != REF_TO_OBJECT_DISPLAY_DATA && ctx.Value != REF_TO_OBJECT_SEARCH_CONDITION) {
                ctx.AddError($"{REF_TO_OBJECT_DISPLAY_DATA}か{REF_TO_OBJECT_SEARCH_CONDITION}のみ指定可能です。");
            }

            // コマンドモデルでの外部参照の場合のみ許可
            if (ctx.NodeType != E_NodeType.Ref) {
                ctx.AddError("このオプションは外部参照（ref-to）にのみ指定できます。");
                return;
            }

            if (ctx.SchemaParseContext.TryGetModel(ctx.XElement, out var model)) {
                if (model is not CommandModel) {
                    ctx.AddError("このオプションはコマンドモデルの外部参照にのみ指定できます。");
                }
            }
        },
        IsAvailableModelMembers = model => {
            if (model is CommandModel) return true;
            return false;
        },
    };
    #endregion CommandModel用


    #region StaticEnumModel用
    internal static NodeOption StaticEnumValue = new() {
        AttributeName = Models.StaticEnumModelModules.StaticEnumValueDef.ATTR_KEY,
        DisplayName = "静的列挙型値",
        Type = E_NodeOptionType.String,
        HelpText = $$"""
            静的列挙型の区分値を指定します。
            C#のenumの値となるため、整数で指定してください。
            """,
        Validate = ctx => {
            // 整数値のみ許可
            if (!int.TryParse(ctx.Value, out _)) {
                ctx.AddError("整数値で指定してください。");
            }
        },
        IsAvailableModelMembers = model => {
            return model is StaticEnumModel;
        },
    };
    #endregion StaticEnumModel用


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
        IsAvailableModelMembers = model => {
            return true;
        },
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
        IsAvailableModelMembers = model => {
            return true;
        },
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
        IsAvailableModelMembers = model => {
            return true;
        },
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
        IsAvailableModelMembers = model => {
            return true;
        },
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
        IsAvailableModelMembers = model => {
            return model is DataModel;
        },
    };
    #endregion ValueMember用
}
