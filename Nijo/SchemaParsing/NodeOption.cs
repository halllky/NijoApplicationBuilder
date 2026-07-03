using Nijo.CodeGenerating;
using Nijo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
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
    /// この属性の値の型
    /// </summary>
    public required E_NodeOptionType Type { get; init; }
    /// <summary>
    /// <see cref="E_NodeOptionType"/> が列挙体の場合のみに使用される、選択肢の候補
    /// </summary>
    public string[]? TypeEnumValues { get; init; }
    /// <summary>
    /// この属性が特定のモデル・ノード種別の組み合わせで利用可能かどうかを判定する。
    /// GUI上での属性入力欄の活性制御に使用される。
    /// </summary>
    public required Func<IModel, E_NodeType, bool> IsAvailable { get; init; }
    /// <summary>
    /// この属性に対する入力検証のうち、IsAvailableでは判定できない複雑な検証。
    /// 保存時にサーバー側で実行される。
    /// </summary>
    public required Action<NodeOptionValidateContext> ValidateOthers { get; init; }
}

/// <summary>
/// ノードオプションの値の型
/// </summary>
public enum E_NodeOptionType {
    Boolean,
    String,
    Integer,
    EnumSelect,
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
        IsAvailable = (model, nodeType) => {
            return true;
        },
        ValidateOthers = ctx => {
            // 改行不可
            if (ctx.Value.Contains('\n')) ctx.AddError("改行を含めることはできません。");
        },
    };

    internal static NodeOption DisplayNameIsEmpty = new() {
        AttributeName = "DisplayNameIsEmpty",
        DisplayName = "表示用名称を空文字にする",
        Type = E_NodeOptionType.Boolean,
        HelpText = $$"""
            表示用名称を明示的に空文字にしたい場合に指定してください。
            {{DisplayName.AttributeName}} 属性と同時に指定することはできません。
            """,
        IsAvailable = (model, nodeType) => {
            return true;
        },
        ValidateOthers = ctx => {
            if (ctx.XElement.Attribute(DisplayName.AttributeName) != null) {
                ctx.AddError($"{DisplayName.AttributeName} 属性と同時に指定することはできません。");
            }
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
            複数の集約で同じテーブル名を指定することはできません。
            """,
        IsAvailable = (model, nodeType) => {
            return model is DataModel || model is QueryModel;
        },
        ValidateOthers = ctx => {
            // QueryModelの場合はビューにマッピングされる場合のみ指定可能
            if (ctx.SchemaParseContext.TryGetModel(ctx.XElement, out var model) && model is QueryModel) {
                var root = ctx.XElement.GetRootAggregateElement();
                var mapToView = root.Attribute(MapToView!.AttributeName);

                if (mapToView == null) ctx.AddError("ビューにマッピングされない場合は指定できません。");
            }

            // 改行不可
            if (ctx.Value.Contains('\n')) ctx.AddError("改行を含めることはできません。");
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
        IsAvailable = (model, nodeType) => {
            return true;
        },
        ValidateOthers = ctx => {
            // 改行不可
            if (ctx.Value.Contains('\n')) ctx.AddError("改行を含めることはできません。");
        },
    };

    internal static NodeOption IsKey = new() {
        AttributeName = "IsKey",
        DisplayName = "キー",
        Type = E_NodeOptionType.Boolean,
        HelpText = $$"""
            この項目がその集約のキーであることを表します。
            DataModelのルート集約またはChildrenの場合、指定必須。
            Commandの要素には指定不可。
            QueryModelのキーはルート集約にのみ指定可能。

            なお自動生成される処理でQueryModelのキーに依存する処理は無い。
            DisplayDataの主キーアサイン関数にのみ影響する。
            （カスタマイズ処理でURLとDisplayDataの間のデータのやり取りに使用する想定）
            """,
        IsAvailable = (model, nodeType) => {
            return (model is DataModel
                 || model is QueryModel)
                 && (nodeType == E_NodeType.ValueMember
                  || nodeType == E_NodeType.Ref);
        },
        ValidateOthers = ctx => {
            if (!ctx.SchemaParseContext.TryGetModel(ctx.XElement, out var model)) return;

            var owner = ctx.XElement.Parent;
            if (owner == null) return;

            var ownerType = ctx.SchemaParseContext.GetNodeType(owner);

            // データモデルの子集約には付与不可
            if (model is DataModel && ownerType == E_NodeType.ChildAggregate) {
                ctx.AddError("データモデルの子集約にはキーを指定できません。");
            }

            // クエリモデルの子集約には付与不可
            if (model is QueryModel && ownerType == E_NodeType.ChildAggregate) {
                ctx.AddError("クエリモデルの子集約にはキーを指定できません。");
            }

            // クエリモデルの子配列は、ビューにマッピングされない場合は付与不可
            if (model is QueryModel && ownerType == E_NodeType.ChildrenAggregate) {
                var root = ctx.XElement.GetRootAggregateElement();
                var mapToView = root.Attribute(MapToView!.AttributeName);

                if (mapToView == null) {
                    ctx.AddError("クエリモデルの子配列で、ビューにマッピングされない場合はキーを指定できません。");
                }
            }

            // クエリモデルのルート集約は、ビューにマッピングされない場合は付与不可
            if (model is QueryModel && ownerType == E_NodeType.RootAggregate) {
                var root = ctx.XElement.GetRootAggregateElement();
                var mapToView = root.Attribute(MapToView!.AttributeName);

                if (mapToView == null) {
                    ctx.AddError("クエリモデルのルート集約で、ビューにマッピングされない場合はキーを指定できません。");
                }
            }
        },
    };

    internal static NodeOption IsNotNull = new() {
        AttributeName = "IsNotNull",
        DisplayName = "NOT NULL",
        Type = E_NodeOptionType.Boolean,
        HelpText = $$"""
            RDBMS 上で NOT NULL 制約がつくことを表します。
            また、新規登録処理や更新処理でアプリケーション側で必須入力チェック処理が行われるようになります。
            """,
        IsAvailable = (model, nodeType) => {
            return model is DataModel
                && (nodeType == E_NodeType.ValueMember
                 || nodeType == E_NodeType.Ref);
        },
        ValidateOthers = ctx => {
            // 特に制約なし
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
        IsAvailable = (model, nodeType) => {
            // データモデルのルート集約のみ許可
            return model is DataModel && nodeType == E_NodeType.RootAggregate;
        },
        ValidateOthers = ctx => {
            // IsAvailableで基本的な判定は完了しているため、追加の検証は不要
        },
    };
    internal static NodeOption UseSoftDelete = new() {
        AttributeName = "UseSoftDelete",
        DisplayName = "論理削除を行う",
        Type = E_NodeOptionType.Boolean,
        HelpText = $$"""
            ルート集約の削除処理を物理削除ではなく論理削除に切り替えます。
            削除時に元テーブルから行を削除し、同名+"_DELETED" のテーブルへコピーします。
            復元処理は自動生成されないため、手動で実装する必要があります。
            これは、復元時に新しいキーを採番するか、既存の新規作成処理を流用して復元するか、
            単に削除前と同じ値でINSERTするだけか、といった仕様はケースバイケースで異なり、
            コード生成で一律に対応するのが難しいためです。
            """,
        IsAvailable = (model, nodeType) => {
            // データモデルのルート集約のみ許可
            return model is DataModel && nodeType == E_NodeType.RootAggregate;
        },
        ValidateOthers = ctx => {
            // IsAvailableで基本的な判定は完了しているため、追加の検証は不要
        },
    };
    internal static NodeOption UniqueConstraints = new() {
        AttributeName = "UniqueConstraints",
        DisplayName = "ユニーク制約",
        Type = E_NodeOptionType.String,
        HelpText = $$"""
            ユニーク制約を指定します。対象の属性は {{nameof(UniqueId)}} 属性で指定します。
            複数のカラムから成るユニーク制約を指定する場合は、対象の属性をカンマ(,)で区切ってください。
            複数指定する場合はセミコロン(;)で区切ってください。
            例: "xxxx;yyyy;zzzz;" と指定した場合、Xの列、Yの列、Zの列それぞれにユニーク制約がつきます。
            例: "xxxx,yyyy;zzzz;" と指定した場合、Xの列とYの列の組み合わせと、Zの列にユニーク制約がつきます。
            """,
        IsAvailable = (model, nodeType) => {
            return model is DataModel
                && (nodeType == E_NodeType.RootAggregate
                || nodeType == E_NodeType.ChildAggregate
                || nodeType == E_NodeType.ChildrenAggregate);
        },
        ValidateOthers = ctx => {
            var raw = ctx.Value?.Trim();
            if (string.IsNullOrEmpty(raw)) {
                ctx.AddError("ユニーク制約の定義が空です。少なくとも1つ以上の対象項目を指定してください。");
                return;
            }

            // 「;」区切りでユニーク制約1件分、「,」区切りでカラム1件分
            var constraintTexts = raw
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToArray();
            if (constraintTexts.Length == 0) {
                ctx.AddError("ユニーク制約の定義が空です。書式を確認してください。");
                return;
            }

            // 同一テーブル（同一集約）直下の UniqueId 付き要素のみ収集
            var directMembers = ctx.XElement.Elements().ToArray();
            var elementsByUniqueId = directMembers
                .Where(e => e.Attribute(SchemaParseContext.ATTR_UNIQUE_ID) != null)
                .GroupBy(e => e.Attribute(SchemaParseContext.ATTR_UNIQUE_ID)!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            // 主キー(IsKey)で構成される UniqueId セット（完全一致の重複定義を検出するため）
            var keyUniqueIds = directMembers
                .Where(e => e.Attribute(IsKey.AttributeName) != null)
                .Select(e => e.Attribute(SchemaParseContext.ATTR_UNIQUE_ID)?.Value)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToArray();
            var primaryKeyCanonical = keyUniqueIds.Length > 0
                ? string.Join(",", keyUniqueIds)
                : null;

            // 同じ制約の二重定義検出用
            var seenConstraintKeys = new HashSet<string>(StringComparer.Ordinal);

            foreach (var constraintText in constraintTexts) {
                var ids = constraintText
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToArray();

                if (ids.Length == 0) {
                    ctx.AddError("ユニーク制約の定義に空のグループがあります。',' や ';' の位置を確認してください。");
                    continue;
                }

                // ソートして順序に依存しないキーを作成
                var canonical = string.Join(",", ids.OrderBy(id => id, StringComparer.Ordinal));
                if (!seenConstraintKeys.Add(canonical)) {
                    ctx.AddError($"同じユニーク制約 '{constraintText}' が複数回指定されています。");
                }

                // 主キーと完全に同じ組み合わせであれば冗長
                if (primaryKeyCanonical != null && canonical == primaryKeyCanonical) {
                    ctx.AddError("主キーで構成されるユニーク制約は暗黙に存在するため、UniqueConstraints に同じ組み合わせを指定する必要はありません。");
                }

                foreach (var id in ids) {
                    if (!elementsByUniqueId.TryGetValue(id, out var targetElements) || targetElements.Count == 0) {
                        // 指定されたIDの要素がなければエラー
                        ctx.AddError($"{SchemaParseContext.ATTR_UNIQUE_ID}='{id}' を持つ要素が見つかりません。");
                        continue;
                    }

                    if (targetElements.Count > 1) {
                        // ID重複エラー（同一集約内で UniqueId が一意でない）
                        ctx.AddError($"{SchemaParseContext.ATTR_UNIQUE_ID}='{id}' を持つ要素が同じ集約内に複数存在します。{SchemaParseContext.ATTR_UNIQUE_ID} 属性は集約内で一意である必要があります。");
                    }

                    // Child や Children など、値メンバー／外部参照以外は指定不可
                    foreach (var el in targetElements) {
                        var nodeType = ctx.SchemaParseContext.GetNodeType(el);
                        if (nodeType != E_NodeType.ValueMember && nodeType != E_NodeType.Ref) {
                            ctx.AddError($"UniqueConstraints で指定できる対象は値メンバーまたは外部参照のみです。{SchemaParseContext.ATTR_UNIQUE_ID}='{id}' は {nodeType} を指しています。");
                            break;
                        }
                    }
                }
            }
        },
    };
    internal static NodeOption IsGenericLookupTable = new() {
        AttributeName = "IsGenericLookupTable",
        DisplayName = "汎用テーブル",
        Type = E_NodeOptionType.Boolean,
        HelpText = $$"""
            俗に汎用マスタ、区分マスタ、コードマスタなどと呼ばれるテーブル。
            国コード、通貨単位、分析集計表の分類など、システムの運用中に値の追加削除が発生しうる区分が格納されます。
            構成と指定は、複数の主キーから構成され、その主キーのうち一部がソースコード上にハードコードされ、
            残りの主キーが動的に登録されていきます。
            通常の列挙体（区分値がシステム運用中に変わらないもの）はこれではなくEnumで定義してください。
            """,
        IsAvailable = (model, nodeType) => {
            // データモデルのルート集約のみ許可
            return model is DataModel && nodeType == E_NodeType.RootAggregate;
        },
        ValidateOthers = ctx => {
            // IsAvailableで基本的な判定は完了しているため、追加の検証は不要
        },
    };
    internal static NodeOption IsHardCodedPrimaryKey = new() {
        AttributeName = "IsHardCodedPrimaryKey",
        DisplayName = "ハードコード",
        HelpText = $$"""
            {{nameof(IsGenericLookupTable)}} と組み合わせて使います。
            主キーのうち、ソースコード上にハードコードされる主キーに指定されます。
            この属性が指定される項目は {{nameof(IsKey)}} が指定されている必要があります。
            """,
        Type = E_NodeOptionType.Boolean,
        IsAvailable = (model, nodeType) => {
            // データモデルのValueMemberにのみ適用可能
            return model is DataModel && nodeType == E_NodeType.ValueMember;
        },
        ValidateOthers = ctx => {
            // IsGenericLookupTable と組み合わせて使う必要がある
            var root = ctx.XElement.GetRootAggregateElement();
            var isGenericLookupTable = root.Attribute(IsGenericLookupTable.AttributeName);
            if (isGenericLookupTable == null) {
                ctx.AddError($"この属性は {IsGenericLookupTable.AttributeName} 属性と組み合わせて使用する必要があります。");
                return;
            }

            // ルート集約の属性でなければいけない
            var owner = ctx.XElement.Parent;
            if (owner == null || ctx.SchemaParseContext.GetNodeType(owner) != E_NodeType.RootAggregate) {
                ctx.AddError($"この属性はルート集約の属性にのみ指定可能です。");
                return;
            }

            // この属性が指定される項目は IsKey も指定されている必要がある
            var isKey = ctx.XElement.Attribute(IsKey.AttributeName);
            if (isKey == null) {
                ctx.AddError($"この属性が指定される項目は {IsKey.AttributeName} 属性も指定する必要があります。");
            }
        },
    };
    internal static NodeOption GenericLookupCategory = new() {
        AttributeName = "Category",
        DisplayName = "カテゴリ",
        Type = E_NodeOptionType.String,
        HelpText = $$"""
            汎用参照テーブルへの参照（ref-to）に指定します。
            {{nameof(IsGenericLookupTable)}} が指定されたテーブルへの参照において、
            どのカテゴリの値を参照するかを指定してください。
            例えば「Countries」「Strategies」などのカテゴリ名を指定します。
            参照先テーブルの {{nameof(IsGenericLookupTable)}} 属性と組み合わせて使います。
            """,
        IsAvailable = (model, nodeType) => {
            return model is DataModel && nodeType == E_NodeType.Ref;
        },
        ValidateOthers = ctx => {
            // 参照先が汎用参照テーブルでなければエラー
            var typeAttr = ctx.XElement.Attribute(SchemaParseContext.ATTR_NODE_TYPE)?.Value;
            if (typeAttr == null) return;

            var refTo = ctx.SchemaParseContext.FindRefTo(ctx.XElement);
            if (refTo == null) {
                ctx.AddError("参照先が見つかりません。");
                return;
            }
            if (refTo.Attribute(IsGenericLookupTable.AttributeName) == null) {
                ctx.AddError($"この属性は {IsGenericLookupTable.AttributeName} が指定された汎用参照テーブルへの参照にのみ指定可能です。");
            }
        },
    };
    #endregion DataModel用


    #region QueryModel用
    internal static NodeOption MapToView = new() {
        AttributeName = "MapToView",
        DisplayName = "ビューへマッピング",
        Type = E_NodeOptionType.Boolean,
        HelpText = $$"""
            この集約をデータベースのビュー(View)にマッピングする場合に指定してください。
            指定した場合、Entity Framework CoreのToView()を使用してビューにマッピングされるEFCoreEntityが生成されます。
            ビュー定義自体はソースコード生成の対象外となり、手動で作成する必要があります。
            """,
        IsAvailable = (model, nodeType) => {
            // クエリモデルのルート集約のみ許可
            return model is QueryModel && nodeType == E_NodeType.RootAggregate;
        },
        ValidateOthers = ctx => {
            // IsAvailableで基本的な判定は完了しているため、追加の検証は不要
        },
    };
    internal static NodeOption OnlySearchCondition = new() {
        AttributeName = "OnlySearchCondition",
        DisplayName = "検索条件のみに生成",
        Type = E_NodeOptionType.Boolean,
        HelpText = $$"""
            この属性を検索条件にのみレンダリングし、検索結果にはレンダリングしません。
            検索条件のみに生成される属性は処理の挙動の指示にのみ使われる想定です。
            例: 「退職後1年以上経過している人のみ抽出」のような、検索条件の絞り込みにのみ必要な属性。
            """,
        IsAvailable = (model, nodeType) => {
            // QueryModelのValueMemberのみ許可
            return model is QueryModel && nodeType == E_NodeType.ValueMember;
        },
        ValidateOthers = ctx => {
            // IsAvailableで基本的な判定は完了しているため、追加の検証は不要
        },
    };
    internal static NodeOption StringSearchBehavior = new() {
        AttributeName = "StringSearchBehavior",
        DisplayName = "検索挙動",
        Type = E_NodeOptionType.EnumSelect,
        TypeEnumValues = new[] {
            STRING_SEARCH_BEHAVIOR_PARTIAL,
            STRING_SEARCH_BEHAVIOR_FORWARD,
            STRING_SEARCH_BEHAVIOR_BACKWARD,
            STRING_SEARCH_BEHAVIOR_EXACT,
            STRING_SEARCH_BEHAVIOR_RANGE,
        },
        HelpText = $$"""
            検索時の挙動を指定します。
            - {{STRING_SEARCH_BEHAVIOR_PARTIAL}}: 部分一致（デフォルト）
            - {{STRING_SEARCH_BEHAVIOR_FORWARD}}: 前方一致
            - {{STRING_SEARCH_BEHAVIOR_BACKWARD}}: 後方一致
            - {{STRING_SEARCH_BEHAVIOR_EXACT}}: 完全一致
            - {{STRING_SEARCH_BEHAVIOR_RANGE}}: 範囲検索（辞書順で以上・以下）
            """,
        IsAvailable = (model, nodeType) => {
            return (model is QueryModel || model is DataModel)
                && nodeType == E_NodeType.ValueMember;
        },
        ValidateOthers = ctx => {
            // DataModel の場合は GenerateDefaultQueryModel が指定されている場合のみ許可
            if (ctx.SchemaParseContext.TryGetModel(ctx.XElement, out var model) && model is DataModel) {
                var root = ctx.XElement.GetRootAggregateElement();
                var generateDefaultQueryModel = root.Attribute(GenerateDefaultQueryModel!.AttributeName);

                if (generateDefaultQueryModel == null) {
                    ctx.AddError("検索処理が自動生成されないDataModelのため指定できません。");
                    return;
                }
            }

            // 文字列型の値メンバーにのみ指定可能
            if (!ctx.SchemaParseContext.TryResolveMemberType(ctx.XElement, out var valueMemberType)
                || valueMemberType is not ValueMemberTypes.Word
                && valueMemberType is not ValueMemberTypes.Description
                && valueMemberType is not ValueMemberTypes.ValueObjectMember) {
                ctx.AddError("この属性は文字列型の値メンバーにのみ指定可能です。");
                return;
            }

            // 指定可能な値の確認
            var validValues = new[] {
                STRING_SEARCH_BEHAVIOR_PARTIAL,
                STRING_SEARCH_BEHAVIOR_FORWARD,
                STRING_SEARCH_BEHAVIOR_BACKWARD,
                STRING_SEARCH_BEHAVIOR_EXACT,
                STRING_SEARCH_BEHAVIOR_RANGE,
            };
            if (!validValues.Contains(ctx.Value)) {
                ctx.AddError($"検索挙動には {string.Join(", ", validValues)} のいずれかを指定してください。");
            }
        },
    };
    internal const string STRING_SEARCH_BEHAVIOR_PARTIAL = "Partial";
    internal const string STRING_SEARCH_BEHAVIOR_FORWARD = "Forward";
    internal const string STRING_SEARCH_BEHAVIOR_BACKWARD = "Backward";
    internal const string STRING_SEARCH_BEHAVIOR_EXACT = "Exact";
    internal const string STRING_SEARCH_BEHAVIOR_RANGE = "Range";
    #endregion QueryModel用


    #region CommandModel用
    /// <summary>
    /// コマンドモデルから参照可能なクエリモデルのオブジェクトの種類と、そのインスタンス作成処理
    /// </summary>
    internal static Dictionary<string, Func<ImmutableSchema.AggregateBase, ICreatablePresentationLayerStructure>> AvailableFromCommandToQuery => new() {
        { REF_TO_OBJECT_DISPLAY_DATA, (aggregate) => new Models.QueryModelModules.DisplayData(aggregate) },
        { REF_TO_OBJECT_SEARCH_CONDITION, (aggregate) => new Models.QueryModelModules.SearchCondition.Entry(aggregate.GetRoot()) },
        { REF_TO_OBJECT_REF_TARGET, (aggregate) => new Models.QueryModelModules.DisplayDataRef.Entry(aggregate) },
    };
    internal static NodeOption Parameter = new() {
        AttributeName = "Parameter",
        DisplayName = "コマンドモデルの引数の型",
        Type = E_NodeOptionType.String,
        HelpText = $$"""
            コマンドモデルの引数の型を指定します。
            以下のいずれかを指定できます:
            - 構造体モデルのルート集約名
            - クエリモデルのルート集約名 + {{string.Join(" または ", AvailableFromCommandToQuery.Keys)}}
            指定しなかった場合は引数なしのコマンドとみなされます。
            """,
        IsAvailable = (model, nodeType) => {
            // コマンドモデルのルート集約のみ許可
            return model is CommandModel && nodeType == E_NodeType.RootAggregate;
        },
        ValidateOthers = ctx => {
            // 値未指定は不正
            var splitted = ctx.Value.Split(':', StringSplitOptions.RemoveEmptyEntries);
            var targetPhysicalName = splitted.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(targetPhysicalName)) {
                ctx.AddError("参照先が未指定です。");
                return;
            }

            // コロンが含まれるならその後ろは DisplayData または SearchCondition のみ
            var targetType = splitted.Skip(1).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(targetType) &&
                !AvailableFromCommandToQuery.ContainsKey(targetType)) {
                ctx.AddError($"参照先の種類は {string.Join(" または ", AvailableFromCommandToQuery.Keys)} のみ指定できます。");
                return;
            }

            // 参照先の存在確認
            var targetElement = ctx.SchemaParseContext.Document
                .Root
                ?.Element(SchemaParseContext.SECTION_DATA_STRUCTURES)
                ?.Element(targetPhysicalName);
            if (targetElement == null) {
                ctx.AddError($"参照先の集約が見つかりません。物理名: {targetPhysicalName}");
                return;
            }
        },
    };

    internal static NodeOption ReturnValue = new() {
        AttributeName = "ReturnValue",
        DisplayName = "コマンドモデルの戻り値の型",
        Type = E_NodeOptionType.String,
        HelpText = $$"""
            コマンドモデルの戻り値の型を指定します。
            以下のいずれかを指定できます:
            - 構造体モデルのルート集約名
            - クエリモデルのルート集約名 + {{string.Join(" または ", AvailableFromCommandToQuery.Keys.Select(key => $":{key}"))}}
            指定しなかった場合は戻り値なしのコマンドとみなされます。
            """,
        IsAvailable = (model, nodeType) => {
            // コマンドモデルのルート集約のみ許可
            return model is CommandModel && nodeType == E_NodeType.RootAggregate;
        },
        ValidateOthers = ctx => {
            // 値未指定は不正
            var splitted = ctx.Value.Split(':', StringSplitOptions.RemoveEmptyEntries);
            var targetPhysicalName = splitted.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(targetPhysicalName)) {
                ctx.AddError("参照先が未指定です。");
                return;
            }

            // コロンが含まれるならその後ろは DisplayData または SearchCondition のみ
            var targetType = splitted.Skip(1).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(targetType) &&
                !AvailableFromCommandToQuery.ContainsKey(targetType)) {
                ctx.AddError($"参照先の種類は {string.Join(" または ", AvailableFromCommandToQuery.Keys)} のみ指定できます。");
                return;
            }

            // 参照先の存在確認
            var targetElement = ctx.SchemaParseContext.Document
                .Root
                ?.Element(SchemaParseContext.SECTION_DATA_STRUCTURES)
                ?.Element(targetPhysicalName);
            if (targetElement == null) {
                ctx.AddError($"参照先の集約が見つかりません。物理名: {targetPhysicalName}");
                return;
            }
        },
    };
    #endregion CommandModel用


    #region StructureModel用
    internal const string REF_TO_OBJECT_DISPLAY_DATA = "DisplayData";
    internal const string REF_TO_OBJECT_SEARCH_CONDITION = "SearchCondition";
    internal const string REF_TO_OBJECT_REF_TARGET = "RefTarget";

    /// <summary>
    /// Structureモデルから参照可能なクエリモデルのオブジェクトの種類
    /// </summary>
    internal static List<string> AvailableFromStructureToQuery => new() {
        REF_TO_OBJECT_DISPLAY_DATA,
        REF_TO_OBJECT_REF_TARGET,
    };

    internal static NodeOption RefToObject = new() {
        AttributeName = "RefToObject",
        DisplayName = "参照先オブジェクト",
        Type = E_NodeOptionType.EnumSelect,
        TypeEnumValues = AvailableFromStructureToQuery.ToArray(),
        HelpText = $$"""
            CommandModelまたはStructureModelはQueryModelの画面表示用データ、外部参照のいずれかしか参照できない。
            その2種のうちどちらを参照するかの指定。
            {{string.Join(", ", AvailableFromStructureToQuery.Select(key => $"\"{key}\""))}}のみ指定可能。
            """,
        IsAvailable = (model, nodeType) => {
            // CommandModelまたはStructureModelの外部参照のみ許可
            return (model is CommandModel || model is StructureModel)
                && nodeType == E_NodeType.Ref;
        },
        ValidateOthers = ctx => {
            if (!AvailableFromStructureToQuery.Contains(ctx.Value)) {
                ctx.AddError($"{string.Join(" または ", AvailableFromStructureToQuery)}のみ指定可能です。");
            }
        },
    };
    #endregion StructureModel用


    #region ValueMember用
    internal static NodeOption MaxLength = new() {
        AttributeName = "MaxLength",
        DisplayName = "最大長",
        Type = E_NodeOptionType.Integer,
        HelpText = $$"""
            RDBMS上での文字列項目の最大長。整数で指定してください。
            """,
        IsAvailable = (model, nodeType) => {
            return model is DataModel
                && nodeType == E_NodeType.ValueMember;
        },
        ValidateOthers = ctx => {
            // 整数値のみ許可
            if (!int.TryParse(ctx.Value, out _)) {
                ctx.AddError("整数値で指定してください。");
            }
        },
    };
    internal static NodeOption TotalDigit = new() {
        AttributeName = "TotalDigit",
        DisplayName = "総合桁数",
        Type = E_NodeOptionType.Integer,
        HelpText = $$"""
            数値系属性の整数部桁数 + 小数部桁数
            """,
        IsAvailable = (model, nodeType) => {
            return (model is DataModel
                 || model is QueryModel
                 || model is CommandModel)
                 && nodeType == E_NodeType.ValueMember;
        },
        ValidateOthers = ctx => {
            // 整数値のみ許可
            if (!int.TryParse(ctx.Value, out _)) {
                ctx.AddError("整数値で指定してください。");
            }
        },
    };
    internal static NodeOption DecimalPlace = new() {
        AttributeName = "DecimalPlace",
        DisplayName = "小数部桁数",
        Type = E_NodeOptionType.Integer,
        HelpText = $$"""
            数値系属性の小数部桁数
            """,
        IsAvailable = (model, nodeType) => {
            return (model is DataModel
                 || model is QueryModel
                 || model is CommandModel)
                 && nodeType == E_NodeType.ValueMember;
        },
        ValidateOthers = ctx => {
            // 整数値のみ許可
            if (!int.TryParse(ctx.Value, out _)) {
                ctx.AddError("整数値で指定してください。");
            }
        },
    };
    internal static NodeOption SequenceName = new() {
        AttributeName = "SequenceName",
        DisplayName = "シーケンス名",
        Type = E_NodeOptionType.String,
        HelpText = $$"""
            シーケンス物理名
            """,
        IsAvailable = (model, nodeType) => {
            return model is DataModel && nodeType == E_NodeType.ValueMember;
        },
        ValidateOthers = ctx => {
            // 特に制約なし
        },
    };
    #endregion ValueMember用


    #region ConstantModel用
    internal static NodeOption ConstantType = new() {
        AttributeName = "ConstantType",
        DisplayName = "定数の型",
        Type = E_NodeOptionType.String,
        HelpText = $$"""
            定数の型を指定します。
            string（文字列）、int（整数）、decimal（小数）、template（テンプレート文字列）、child（ネストされた定数グループ）のいずれかを指定してください。
            """,
        IsAvailable = (model, nodeType) => {
            return model is ConstantModel;
        },
        ValidateOthers = ctx => {
            var validTypes = new[] {
                Models.ConstantModelModules.ConstantValueDef.CONSTTYPE_STRING,
                Models.ConstantModelModules.ConstantValueDef.CONSTTYPE_INT,
                Models.ConstantModelModules.ConstantValueDef.CONSTTYPE_DECIMAL,
                Models.ConstantModelModules.ConstantValueDef.CONSTTYPE_TEMPLATE,
                Models.ConstantModelModules.ConstantValueDef.CONSTTYPE_CHILD,
            };
            if (!validTypes.Contains(ctx.Value)) {
                ctx.AddError($"type属性の値「{ctx.Value}」は無効です。{string.Join(", ", validTypes)} のいずれかを指定してください。");
                return;
            }

            // child型の場合はConstantValue属性は不要
            if (ctx.Value == Models.ConstantModelModules.ConstantValueDef.CONSTTYPE_CHILD) {
                return; // child型の場合は追加のバリデーション不要
            }

            // 型に応じた値の妥当性チェック
            var valueAttr = ConstantValue == null
                ? throw new Exception("ありえない")
                : ctx.XElement.Attribute(ConstantValue.AttributeName);
            if (valueAttr is null) {
                ctx.AddError("ConstantValue属性が必須です。");
                return;
            }

            switch (ctx.Value) {
                case Models.ConstantModelModules.ConstantValueDef.CONSTTYPE_INT:
                    if (!int.TryParse(valueAttr.Value, out _)) {
                        ctx.AddError($"int型の値「{valueAttr.Value}」は有効な整数ではありません。");
                    }
                    break;
                case Models.ConstantModelModules.ConstantValueDef.CONSTTYPE_DECIMAL:
                    if (!decimal.TryParse(valueAttr.Value, out _)) {
                        ctx.AddError($"decimal型の値「{valueAttr.Value}」は有効な小数ではありません。");
                    }
                    break;
                case Models.ConstantModelModules.ConstantValueDef.CONSTTYPE_TEMPLATE:
                    // テンプレート型の場合、プレースホルダーは自動判定されるため追加チェックは不要
                    break;
            }
        },
    };

    internal static NodeOption ConstantValue = new() {
        AttributeName = "ConstantValue",
        DisplayName = "定数の値",
        Type = E_NodeOptionType.String,
        HelpText = $$"""
            定数の値を指定します。
            型に応じて適切な形式で指定してください。
            """,
        IsAvailable = (model, nodeType) => {
            return model is ConstantModel;
        },
        ValidateOthers = ctx => {
            // 値の妥当性チェックはConstantTypeで実施
        },
    };
    #endregion ConstantModel用
}
