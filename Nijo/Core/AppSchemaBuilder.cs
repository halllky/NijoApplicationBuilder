using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nijo.Core {

    // TODO: それぞれのオプションの説明を書く
    public sealed class AggregateBuildOption {
        public bool? IsPrimary { get; set; }
        public bool? IsArray { get; set; }

        /// <summary>
        /// Childrenにのみ設定可能。配列の要素が0件のときにエラーにするかどうか。
        /// </summary>
        public bool? IsRequiredArray { get; set; }

        public GroupOption? IsVariationGroupMember { get; set; }
        public bool? InvisibleInGui { get; set; }
        public string? Handler { get; set; }
        /// <summary>
        /// <see cref="Models.ReadModel2"/> において、この集約の登録・更新・削除のタイミングが親集約と別々かどうかを表す。
        /// 別々な場合、この集約のオブジェクトを画面上で追加したり削除したりすることができたり、
        /// 親の内容が画面上で変更されていてもこの集約に変更がなければ更新がかからなかったりする。
        /// </summary>
        public bool HasLifeCycle { get; set; }
        /// <summary>
        /// 画面から編集できる集約かそうでないかを表します。
        /// </summary>
        public bool IsReadOnlyAggregate { get; set; }
        /// <summary>
        /// この集約が <see cref="Models.WriteModel2"/> の場合に
        /// 既定の <see cref="Models.ReadModel2"/> を生成するかどうか。
        /// DBのデータ型と画面のデータ型が完全一致する場の使用を想定している。
        /// </summary>
        public bool GenerateDefaultReadModel { get; set; }

        /// <summary>
        /// <see cref="Models.CommandModel"/> 用のステップ属性。その入力項目がウィザード形式の入力画面の何番目かを表す。
        /// ルート集約の直下の <see cref="AggregateMember.Child"/> でのみ指定可能。
        /// ステップありなしは混在不可能（ステップをつけるなら全てのChildをステップにする必要がある）。
        /// </summary>
        public int? Step { get; set; }

        /// <summary>
        /// VFormにおけるラベル列の横幅。単位はCSSのrem。
        /// </summary>
        public decimal? EstimatedLabelWidth { get; set; }
        /// <summary>
        /// VFormにおける入れ子の最大の深さ。左端列の横幅の計算に使う。未指定の場合は自動計算。
        /// </summary>
        public int? FormDepth { get; set; }

        public sealed class GroupOption {
            public required string GroupName { get; init; }
            public required string GroupDisplayName { get; init; }
            public required string Key { get; init; }
        }

        /// <summary>
        /// バリデーションの詳細画面のUIを強制的にコンボボックスにする
        /// </summary>
        public bool? IsCombo { get; init; }
        /// <summary>
        /// バリデーションの詳細画面のUIを強制的にラジオボタンにする
        /// </summary>
        public bool? IsRadio { get; init; }

        /// <summary>
        /// 画面表示名
        /// </summary>
        public string? DisplayName { get; set; }
        /// <summary>
        /// データベーステーブル名
        /// </summary>
        public string? DbName { get; set; }
        /// <summary>
        /// ラテン語名
        /// </summary>
        public string? LatinName { get; set; }
    }
    // TODO: それぞれのオプションの説明を書く
    public sealed class AggregateMemberBuildOption {
        public string? MemberType { get; set; }
        public bool? IsPrimary { get; set; }
        public bool? IsDisplayName { get; set; }
        public bool? IsNameLike { get; set; }
        public bool? IsRequired { get; set; }
        public string? IsReferenceTo { get; set; }
        public bool? InvisibleInGui { get; set; }
        /// <summary>生成後のソースで外から注入して、中で React context 経由で参照する詳細画面用コンポーネント。ValueMemberまたはRefでのみ使用</summary>
        public string? SingleViewCustomUiComponentName { get; set; }
        /// <summary>生成後のソースで外から注入して、中で React context 経由で参照する詳細画面用コンポーネント。ValueMemberまたはRefでのみ使用</summary>
        public string? SearchConditionCustomUiComponentName { get; set; }
        /// <summary>テキストボックスの横幅。文字列型と数値型のValueMemberでのみ有効</summary>
        public TextBoxWidth? UiWidthRem { get; set; }
        /// <summary>フォームのUIで横幅いっぱい占有するかどうか</summary>
        public bool? WideInVForm { get; set; }

        /// <summary>列挙体の詳細画面のUIを強制的にコンボボックスにする</summary>
        [Obsolete("#60 最終的にnijo.xmlではなくApp.tsxで決められるようにするためこの属性は削除する")]
        public bool? IsCombo { get; set; }
        /// <summary>列挙体の詳細画面のUIを強制的にラジオボタンにする</summary>
        [Obsolete("#60 最終的にnijo.xmlではなくApp.tsxで決められるようにするためこの属性は削除する")]
        public bool? IsRadio { get; set; }

        /// <summary>
        /// 画面表示名
        /// </summary>
        public string? DisplayName { get; set; }
        /// <summary>
        /// データベーステーブル名 or カラム名
        /// </summary>
        public string? DbName { get; set; }

        /// <summary>
        /// 検索条件の挙動
        /// </summary>
        public E_SearchBehavior? SearchBehavior { get; set; }

        /// <summary>
        /// 文字列型の最大長
        /// </summary>
        public int? MaxLength { get; set; }

        /// <summary>
        /// 列挙体のSQLパラメータの型
        /// </summary>
        public string? EnumSqlParamType { get; set; }
    }
    public sealed class EnumValueOption {
        public string PhysicalName { get; set; } = string.Empty;
        public int? Value { get; set; }
        public string? DisplayName { get; set; }
    }
    public sealed class TextBoxWidth {
        public required E_ZenHan ZenHan { get; init; }
        public required int CharCount { get; set; }

        public string GetCssValue() {
            // 確認に用いたフォントは font-family:"Cascadia Mono", "BIZ UDGothic"。
            // 0.5m は テキストボックス内のパディング。
            var width = ZenHan == E_ZenHan.Zenkaku
                ? 0.5m + (CharCount * 1.050m)
                : 0.5m + (CharCount * 0.615m);

            // 小数第三位以下は誤差として小数第二位で切り上げる
            var ceiled = Math.Ceiling(width * 100) / 100;

            return $"{ceiled}rem";
        }

        public enum E_ZenHan {
            Zenkaku,
            Hankaku,
        }
    }


    internal static class DirectedEdgeExtensions {
        internal const string REL_ATTR_RELATION_TYPE = "relationType";
        internal const string REL_ATTRVALUE_HAVING = "having";
        internal const string REL_ATTRVALUE_PARENT_CHILD = "child";
        internal const string REL_ATTRVALUE_REFERENCE = "reference";

        internal const string REL_ATTR_MULTIPLE = "multiple";
        internal const string REL_ATTR_VARIATIONGROUPNAME = "variation-group-name";
        internal const string REL_ATTR_VARIATIONGROUP_DISPLAYNAME = "variation-group-display-name";
        internal const string REL_ATTR_VARIATIONSWITCH = "switch";
        internal const string REL_ATTR_IS_PRIMARY = "is-primary";
        internal const string REL_ATTR_IS_INSTANCE_NAME = "is-instance-name";
        internal const string REL_ATTR_IS_NAME_LIKE = "is-name-like";
        internal const string REL_ATTR_IS_REQUIRED = "is-required";
        internal const string REL_ATTR_IS_WIDE = "is-wide";
        internal const string REL_ATTR_INVISIBLE_IN_GUI = "invisible-in-gui";
        internal const string REL_ATTR_SINGLEVIEW_CUSTOM_UI_COMPONENT_NAME = "singleview-custom-ui-component-name";
        internal const string REL_ATTR_SEARCHCONDITION_CUSTOM_UI_COMPONENT_NAME = "searchcondition-custom-ui-component-name";
        internal const string REL_ATTR_DISPLAY_NAME = "display-name";
        internal const string REL_ATTR_DB_NAME = "ref-db-name";
        internal const string REL_ATTR_MEMBER_ORDER = "relation-aggregate-order";
        internal const string REL_ATTR_IS_COMBO = "is-combo";
        internal const string REL_ATTR_IS_RADIO = "is-radio";


        // ----------------------------- GraphEdge extensions -----------------------------

        internal static bool IsPrimary(this GraphEdge graphEdge) {
            return graphEdge.Attributes.TryGetValue(REL_ATTR_IS_PRIMARY, out var bln) && (bool)bln!;
        }
        internal static bool IsInstanceName(this GraphEdge graphEdge) {
            return graphEdge.Attributes.TryGetValue(REL_ATTR_IS_INSTANCE_NAME, out var bln) && (bool)bln!;
        }
        internal static bool IsNameLike(this GraphEdge graphEdge) {
            return graphEdge.Attributes.TryGetValue(REL_ATTR_IS_NAME_LIKE, out var bln) && (bool)bln!;
        }
        internal static bool IsRequired(this GraphEdge graphEdge) {
            return graphEdge.Attributes.TryGetValue(REL_ATTR_IS_REQUIRED, out var bln) && (bool)bln!;
        }
        internal static bool? IsWide(this GraphEdge graphEdge) {
            return graphEdge.Attributes.TryGetValue(REL_ATTR_IS_WIDE, out var bln) ? (bool?)bln : null;
        }
        internal static bool InvisibleInGui(this GraphEdge graphEdge) {
            return graphEdge.Attributes.TryGetValue(REL_ATTR_INVISIBLE_IN_GUI, out var bln) && (bool)bln!;
        }
        internal static bool IsRef(this GraphEdge graphEdge) {
            return graphEdge.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                && (string)type! == REL_ATTRVALUE_REFERENCE;
        }
        internal static bool IsParentChild(this GraphEdge graphEdge) {
            return graphEdge.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                && (string)type! == REL_ATTRVALUE_PARENT_CHILD;
        }

        internal static int GetMemberOrder(this GraphEdge graphEdge) {
            return (int)graphEdge.Attributes[REL_ATTR_MEMBER_ORDER]!;
        }

        internal static string? GetDisplayName(this GraphEdge graphEdge) {
            return graphEdge.Attributes
                .TryGetValue(REL_ATTR_DISPLAY_NAME, out var displayName)
                && !string.IsNullOrWhiteSpace((string?)displayName)
                ? (string)displayName
                : null;
        }
    }

    internal class VariationGroup<T> where T : IGraphNode {
        internal GraphNode<T> Owner => VariationAggregates.First().Value.Initial.As<T>();
        internal required string GroupName { get; init; }
        internal required IReadOnlyDictionary<string, GraphEdge<T>> VariationAggregates { get; init; }
        internal required string? DisplayName { get; init; }
        internal required string? DbName { get; init; }
        internal required int MemberOrder { get; init; }
        internal required bool IsCombo { get; init; }
        internal required bool IsRadio { get; init; }
        internal bool IsPrimary => VariationAggregates.First().Value.IsPrimary();
        internal bool IsInstanceName => VariationAggregates.First().Value.IsInstanceName();
        internal bool IsNameLike => VariationAggregates.First().Value.IsNameLike();
        internal bool RequiredAtDB => VariationAggregates.First().Value.IsRequired();
        internal bool InvisibleInGui => VariationAggregates.First().Value.InvisibleInGui();

        internal string CsEnumType => $"E_{GroupName.ToCSharpSafe()}";
    }
}
