using Nijo.Core;
using Nijo.Models.RefTo;
using Nijo.Models.WriteModel2Features;
using Nijo.Parts.Utility;
using Nijo.Parts.WebServer;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.ReadModel2Features {
    /// <summary>
    /// 画面表示用データクラス
    /// </summary>
    internal class DataClassForDisplay {
        internal DataClassForDisplay(GraphNode<Aggregate> agg) {
            Aggregate = agg;
        }
        internal GraphNode<Aggregate> Aggregate { get; }

        internal const string BASE_CLASS_NAME = "DisplayDataClassBase";

        /// <summary>C#クラス名</summary>
        internal string CsClassName => $"{Aggregate.Item.PhysicalName}DisplayData";
        /// <summary>TypeScript型名</summary>
        internal string TsTypeName => $"{Aggregate.Item.PhysicalName}DisplayData";

        /// <summary>値が格納されるプロパティの名前（C#）</summary>
        internal const string VALUES_CS = "Values";
        /// <summary>値が格納されるプロパティの名前（TypeScript）</summary>
        internal const string VALUES_TS = "values";
        /// <summary>値クラス名</summary>
        internal string ValueCsClassName => $"{CsClassName}Values";

        /// <summary>メッセージ情報が格納されるプロパティの名前（C#）</summary>
        internal const string MESSAGES_CS = "Messages";
        /// <summary>メッセージ情報が格納されるプロパティの名前（TypeScript）</summary>
        internal const string MESSAGES_TS = "messages";
        /// <summary>メンバーでなくオブジェクト自身へのメッセージ（C#）</summary>
        internal const string OWN_MESSAGES_CS = "OwnMessages";
        /// <summary>メンバーでなくオブジェクト自身へのメッセージ（TypeScript）</summary>
        internal const string OWN_MESSAGES_TS = "ownMessages";
        /// <summary>メッセージ用構造体 C#クラス名</summary>
        internal string MessageDataCsClassName => $"{CsClassName}Messages";

        /// <summary>読み取り専用か否かが格納されるプロパティの名前（C#）</summary>
        internal const string READONLY_CS = "ReadOnly";
        /// <summary>読み取り専用か否かが格納されるプロパティの名前（TypeScript）</summary>
        internal const string READONLY_TS = "readOnly";
        /// <summary>全項目が読み取り専用か否か（C#）</summary>
        internal const string ALL_READONLY_CS = "AllReadOnly";
        /// <summary>全項目が読み取り専用か否か（TypeScript）</summary>
        internal const string ALL_READONLY_TS = "allReadOnly";
        /// <summary>メッセージ用構造体 C#クラス名</summary>
        internal string ReadOnlyDataCsClassName => $"{CsClassName}ReadOnly";

        /// <summary>
        /// インスタンスを一意に表す文字列（C#）。
        /// 新規作成の場合はUUID。閲覧・更新・削除のときは主キーの値の配列のJSON。
        /// 
        /// 新規作成データの場合は画面上で主キー項目を編集可能であり、
        /// 別途何らかの識別子を設けないと同一性を判定する方法が無いため、この項目が必要になる。
        /// </summary>
        internal const string INSTANCE_KEY_CS = "InstanceKey";
        /// <summary>
        /// インスタンスを一意に表す文字列（C#）。
        /// 新規作成の場合はUUID。閲覧・更新・削除のときは主キーの値の配列のJSON。
        /// 
        /// 新規作成データの場合は画面上で主キー項目を編集可能であり、
        /// 別途何らかの識別子を設けないと同一性を判定する方法が無いため、この項目が必要になる。
        /// </summary>
        internal const string INSTANCE_KEY_TS = "instanceKey";

        /// <summary>このデータがDBに保存済みかどうか（C#）。つまり新規作成のときはfalse, 閲覧・更新・削除のときはtrue</summary>
        internal const string EXISTS_IN_DB_CS = "ExistsInDatabase";
        /// <summary>このデータがDBに保存済みかどうか（TypeScript）。つまり新規作成のときはfalse, 閲覧・更新・削除のときはtrue</summary>
        internal const string EXISTS_IN_DB_TS = "existsInDatabase";

        /// <summary>画面上で何らかの変更が加えられてから、保存処理の実行でその変更が確定するまでの間、trueになる（C#）</summary>
        internal const string WILL_BE_CHANGED_CS = "WillBeChanged";
        /// <summary>画面上で何らかの変更が加えられてから、保存処理の実行でその変更が確定するまでの間、trueになる（TypeScript）</summary>
        internal const string WILL_BE_CHANGED_TS = "willBeChanged";

        /// <summary>画面上で削除が指示されてから、保存処理の実行でその削除が確定するまでの間、trueになる（C#）</summary>
        internal const string WILL_BE_DELETED_CS = "WillBeDeleted";
        /// <summary>画面上で削除が指示されてから、保存処理の実行でその削除が確定するまでの間、trueになる（TypeScript）</summary>
        internal const string WILL_BE_DELETED_TS = "willBeDeleted";

        /// <summary>楽観排他制御用のバージョニング情報をもつプロパティの名前（C#側）</summary>
        internal const string VERSION_CS = "Version";
        /// <summary>楽観排他制御用のバージョニング情報をもつプロパティの名前（TypeScript側）</summary>
        internal const string VERSION_TS = "version";

        /// <summary>追加・更新・削除のいずれかの区分を返すメソッドの名前</summary>
        internal const string GET_SAVE_TYPE = "GetSaveType";

        /// <summary>
        /// 追加・更新・削除のタイミングが親要素と異なるか否か
        /// </summary>
        internal bool HasLifeCycle => Aggregate.IsRoot() || Aggregate.Item.Options.HasLifeCycle;
        /// <summary>
        /// インスタンスキー（<see cref="INSTANCE_KEY_CS"/>, <see cref="INSTANCE_KEY_TS"/>）を持つかどうか。
        /// 追加更新削除のタイミングが親と異なる場合以外であっても、配列の要素の場合は配列の並び順が変わるなどしても
        /// 従前の要素を追跡できるようにしておくためにインスタンスキーをもつ。
        /// </summary>
        internal bool HasInstanceKey => HasLifeCycle || Aggregate.IsChildrenMember();

        /// <summary>
        /// <see cref="VALUES_CS"/>, <see cref="VALUES_TS"/> に含まれるメンバーを列挙します。
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<AggregateMember.AggregateMemberBase> GetOwnMembers() {
            foreach (var member in Aggregate.GetMembers()) {
                if (member is AggregateMember.ValueMember) {
                    if (member.DeclaringAggregate == Aggregate) yield return member;

                } else if (member is AggregateMember.Ref) {
                    yield return member;

                }
            }
        }
        /// <summary>
        /// 子要素の画面表示用クラスを列挙します。
        /// </summary>
        internal IEnumerable<DataClassForDisplayDescendant> GetChildMembers() {
            return Aggregate
                .GetMembers()
                .Where(m => m is AggregateMember.Child
                         || m is AggregateMember.Children
                         || m is AggregateMember.VariationItem)
                .Select(m => new DataClassForDisplayDescendant((AggregateMember.RelationMember)m));
        }


        /// <summary>
        /// C#の基底クラスをレンダリングします。
        /// </summary>
        internal static SourceFile RenderBaseClass() => new SourceFile {
            FileName = "DisplayDataClassBase.cs",
            RenderContent = context => {
                return $$"""
                    /// <summary>
                    /// 画面表示用データの基底クラス
                    /// </summary>
                    public abstract partial class {{BASE_CLASS_NAME}} {
                    }
                    """;
            },
        };

        /// <summary>
        /// クラス定義をレンダリングします（C#）
        /// </summary>
        internal string RenderCSharpDeclaring(CodeRenderingContext context) {
            // クラスが継承しているクラスや実装しているインターフェース
            var implements = new List<string>();
            if (Aggregate.IsRoot()) implements.Add(BASE_CLASS_NAME);
            if (HasLifeCycle) implements.Add(ISaveCommandConvertible.INTERFACE_NAME);

            return $$"""
                /// <summary>
                /// {{Aggregate.Item.DisplayName}}の画面表示用データ構造
                /// </summary>
                public partial class {{CsClassName}} {{(implements.Count == 0 ? "" : $": {implements.Join(", ")} ")}}{
                {{If(HasInstanceKey, () => $$"""
                    /// <summary>
                    /// インスタンスを一意に表す文字列。新規作成の場合はUUID。閲覧・更新・削除のときは主キーの値の配列のJSON。
                    /// 新規作成データの場合は画面上で主キー項目を編集可能であり、
                    /// 別途何らかの識別子を設けないと同一性を判定する方法が無いため、この項目が必要になる。
                    /// </summary>
                    [JsonPropertyName("{{INSTANCE_KEY_TS}}")]
                    public required virtual {{InstanceKey.CS_CLASS_NAME}} {{INSTANCE_KEY_CS}} { get; set; }
                """)}}

                    /// <summary>値</summary>
                    [JsonPropertyName("{{VALUES_TS}}")]
                    public virtual {{ValueCsClassName}} {{VALUES_CS}} { get; set; } = new();
                {{GetChildMembers().SelectTextTemplate(member => member.Aggregate.IsChildrenMember() ? $$"""
                    /// <summary>{{member.Aggregate.Item.DisplayName}}</summary>
                    public virtual List<{{member.CsClassName}}> {{member.MemberName}} { get; set; } = new();
                """ : $$"""
                    /// <summary>{{member.Aggregate.Item.DisplayName}}</summary>
                    public virtual {{member.CsClassName}} {{member.MemberName}} { get; set; } = new();
                """)}}

                {{If(HasLifeCycle, () => $$"""
                    /// <summary>このデータがDBに保存済みかどうか</summary>
                    [JsonPropertyName("{{EXISTS_IN_DB_TS}}")]
                    public virtual required bool {{EXISTS_IN_DB_CS}} { get; set; }
                    /// <summary>このデータに更新がかかっているかどうか</summary>
                    [JsonPropertyName("{{WILL_BE_CHANGED_TS}}")]
                    public virtual bool {{WILL_BE_CHANGED_CS}} { get; set; }
                    /// <summary>このデータが更新確定時に削除されるかどうか</summary>
                    [JsonPropertyName("{{WILL_BE_DELETED_TS}}")]
                    public virtual bool {{WILL_BE_DELETED_CS}} { get; set; }
                    /// <summary>楽観排他制御用のバージョニング情報</summary>
                    [JsonPropertyName("{{VERSION_TS}}")]
                    public virtual required int? {{VERSION_CS}} { get; set; }
                """)}}
                    /// <summary>メッセージ</summary>
                    [JsonPropertyName("{{MESSAGES_TS}}")]
                    public virtual {{MessageDataCsClassName}} {{MESSAGES_CS}} { get; set; } = new();
                    /// <summary>どの項目が読み取り専用か</summary>
                    [JsonPropertyName("{{READONLY_TS}}")]
                    public virtual {{ReadOnlyDataCsClassName}} {{READONLY_CS}} { get; set; } = new();
                {{If(HasLifeCycle, () => $$"""

                    /// <summary>
                    /// このオブジェクトの状態から、保存時に追加・更新・削除のうちどの処理が実行されるべきかを表す区分を返します。
                    /// </summary>
                    public {{DataClassForSaveBase.ADD_MOD_DEL_ENUM_CS}} {{GET_SAVE_TYPE}}() {
                        return (({{ISaveCommandConvertible.INTERFACE_NAME}})this).{{ISaveCommandConvertible.GET_SAVE_TYPE}}();
                    }
                """)}}
                }
                {{RenderCsValueClass(context)}}
                {{RenderCsMessageClass(context)}}
                {{RenderCsReadonlyClass(context)}}
                """;
        }
        /// <summary>
        /// 型定義をレンダリングします（TypeScript）
        /// </summary>
        internal string RenderTypeScriptDeclaring(CodeRenderingContext context) {
            return $$"""
                /** {{Aggregate.Item.DisplayName}}の画面表示用データ構造 */
                export type {{TsTypeName}} = {
                {{If(HasInstanceKey, () => $$"""
                  /**
                   * インスタンスを一意に表す文字列。新規作成の場合はUUID。閲覧・更新・削除のときは主キーの値の配列のJSON。
                   * 新規作成データの場合は画面上で主キー項目を編集可能であり、
                   * 別途何らかの識別子を設けないと同一性を判定する方法が無いため、この項目が必要になる。
                   */
                  {{INSTANCE_KEY_TS}}: string
                """)}}

                  /** 値 */
                  {{VALUES_TS}}: {{WithIndent(RenderTsValueType(context), "  ")}}
                {{GetChildMembers().SelectTextTemplate(member => member.Aggregate.IsChildrenMember() ? $$"""
                  /** {{member.Aggregate.Item.DisplayName}} */
                  {{member.MemberName}}: {{member.TsTypeName}}[]
                """ : $$"""
                  /** {{member.Aggregate.Item.DisplayName}} */
                  {{member.MemberName}}: {{member.TsTypeName}}
                """)}}

                {{If(HasLifeCycle, () => $$"""
                  /** このデータがDBに保存済みかどうか */
                  {{EXISTS_IN_DB_TS}}: boolean
                  /** このデータに更新がかかっているかどうか */
                  {{WILL_BE_CHANGED_TS}}: boolean
                  /** このデータが更新確定時に削除されるかどうか */
                  {{WILL_BE_DELETED_TS}}: boolean
                  /** 楽観排他制御用のバージョニング情報 */
                  {{VERSION_TS}}: number | undefined
                """)}}
                  /** メッセージ */
                  {{MESSAGES_TS}}?: {{WithIndent(RenderMessageTs(context), "  ")}}
                  /** どの項目が読み取り専用か */
                  {{READONLY_TS}}?: {{WithIndent(RenderReadonlyTsType(context), "  ")}}
                }
                """;
        }


        #region 値
        private string RenderCsValueClass(CodeRenderingContext context) {
            return $$"""
                /// <summary>
                /// {{Aggregate.Item.DisplayName}}の画面表示用データの値の部分
                /// </summary>
                public partial class {{ValueCsClassName}} {
                {{GetOwnMembers().SelectTextTemplate(member => $$"""
                    /// <summary>{{member.MemberName}}</summary>
                    public virtual {{GetMemberCsType(member)}}? {{member.MemberName}} { get; set; }
                """)}}
                }
                """;
        }
        private string RenderTsValueType(CodeRenderingContext context) {
            return $$"""
                {
                {{GetOwnMembers().SelectTextTemplate(member => $$"""
                  {{member.MemberName}}?: {{GetMemberTsType(member)}}
                """)}}
                }
                """;
        }
        #endregion 値


        #region メッセージ用構造体
        private string RenderCsMessageClass(CodeRenderingContext context) {
            return $$"""
                /// <summary>
                /// {{Aggregate.Item.DisplayName}}の画面表示用データのメッセージ情報格納部分
                /// </summary>
                public partial class {{MessageDataCsClassName}} {
                    /// <summary>{{Aggregate.Item.DisplayName}}自身についてのメッセージ</summary>
                    [JsonPropertyName("{{OWN_MESSAGES_TS}}")]
                    public virtual {{MessageContainer.CS_CLASS_NAME}} {{OWN_MESSAGES_CS}} { get; } = new();
                {{GetOwnMembers().SelectTextTemplate(member => $$"""
                    /// <summary>{{member.MemberName}}についてのメッセージ</summary>
                    public virtual {{MessageContainer.CS_CLASS_NAME}} {{member.MemberName}} { get; } = new();
                """)}}
                }
                """;
        }
        private string RenderMessageTs(CodeRenderingContext context) {
            return $$"""
                {
                  /** {{Aggregate.Item.DisplayName}}自身についてのメッセージ */
                  {{OWN_MESSAGES_TS}}?: {{MessageContainer.TS_TYPE_NAME}}
                {{GetOwnMembers().SelectTextTemplate(member => $$"""
                  /** {{member.MemberName}}についてのメッセージ */
                  {{member.MemberName}}?: {{MessageContainer.TS_TYPE_NAME}}
                """)}}
                }
                """;
        }
        #endregion メッセージ用構造体


        #region 読み取り専用用構造体
        private string RenderCsReadonlyClass(CodeRenderingContext context) {
            return $$"""
                /// <summary>
                /// {{Aggregate.Item.DisplayName}}の画面表示用データの読み取り専用情報格納部分
                /// </summary>
                public partial class {{ReadOnlyDataCsClassName}} {
                    /// <summary>{{Aggregate.Item.DisplayName}}全体が読み取り専用か否か</summary>
                    [JsonPropertyName("{{ALL_READONLY_TS}}")]
                    public virtual {{ReadOnlyInfo.CS_CLASS_NAME}} {{ALL_READONLY_CS}} { get; } = new();
                {{GetOwnMembers().SelectTextTemplate(member => $$"""
                    /// <summary>{{member.MemberName}}が読み取り専用か否か</summary>
                    public virtual {{ReadOnlyInfo.CS_CLASS_NAME}} {{member.MemberName}} { get; } = new();
                """)}}
                }
                """;
        }
        private string RenderReadonlyTsType(CodeRenderingContext context) {
            return $$"""
                {
                  /** {{Aggregate.Item.DisplayName}}全体が読み取り専用か否か */
                  {{ALL_READONLY_TS}}?: {{ReadOnlyInfo.TS_TYPE_NAME}}
                {{GetOwnMembers().SelectTextTemplate(member => $$"""
                  /** {{member.MemberName}}が読み取り専用か否か */
                  {{member.MemberName}}?: {{ReadOnlyInfo.TS_TYPE_NAME}}
                """)}}
                }
                """;
        }
        #endregion 読み取り専用用構造体

        /// <summary>
        /// メンバーのC#型名を返します。null許容演算子は含みません。
        /// </summary>
        internal static string GetMemberCsType(AggregateMember.AggregateMemberBase member) {
            if (member is AggregateMember.ValueMember vm) {
                return vm.Options.MemberType.GetCSharpTypeName();

            } else if (member is AggregateMember.Parent) {
                throw new NotImplementedException(); // Parentのメンバーは定義されないので

            } else if (member is AggregateMember.Ref @ref) {
                var refTarget = new RefDisplayData(@ref.RefTo, @ref.RefTo);
                return refTarget.CsClassName;

            } else if (member is AggregateMember.Children children) {
                var dataClass = new DataClassForDisplayDescendant(children);
                return $"List<{dataClass.CsClassName}>";

            } else {
                var dataClass = new DataClassForDisplayDescendant((AggregateMember.RelationMember)member);
                return dataClass.CsClassName;
            }
        }
        /// <summary>
        /// メンバーのTypeScript型名を返します。
        /// </summary>
        internal static string GetMemberTsType(AggregateMember.AggregateMemberBase member) {
            if (member is AggregateMember.ValueMember vm) {
                return vm.Options.MemberType.GetTypeScriptTypeName();

            } else if (member is AggregateMember.Parent) {
                throw new NotImplementedException(); // Parentのメンバーは定義されないので

            } else if (member is AggregateMember.Ref @ref) {
                var refTarget = new RefDisplayData(@ref.RefTo, @ref.RefTo);
                return refTarget.TsTypeName;

            } else if (member is AggregateMember.Children children) {
                var dataClass = new DataClassForDisplayDescendant(children);
                return $"{dataClass.TsTypeName}[]";

            } else {
                var dataClass = new DataClassForDisplayDescendant((AggregateMember.RelationMember)member);
                return dataClass.TsTypeName;
            }
        }

        /// <summary>
        /// この型のオブジェクトを新規作成する関数の名前
        /// </summary>
        internal string TsNewObjectFunction => $"createNew{TsTypeName}";
        internal string RenderTsNewObjectFunction(CodeRenderingContext context) {

            // 初期値をレンダリングする。nullを返した場合は明示的な初期値なしになる。
            string? RenderOwnMemberInitialize(AggregateMember.AggregateMemberBase member) {
                // UUID型
                if (member is AggregateMember.Schalar schalar
                    && schalar.DeclaringAggregate == Aggregate
                    && schalar.Options.MemberType is Core.AggregateMemberTypes.Uuid) {

                    return $"{member.MemberName}: UUID.generate(),";
                }

                // バリエーション
                if (member is AggregateMember.Variation variation
                    && variation.DeclaringAggregate == Aggregate) {

                    return $"{member.MemberName}: '{variation.GetGroupItems().First().TsValue}',";
                }

                // 初期値なし
                return null;
            }

            return $$"""
                /** {{Aggregate.Item.DisplayName}}の画面表示用オブジェクトを新規作成します。 */
                export const {{TsNewObjectFunction}} = () => ({
                {{If(HasInstanceKey, () => $$"""
                  {{INSTANCE_KEY_TS}}: JSON.stringify(UUID.generate()) as Util.ItemKey,
                """)}}
                  {{VALUES_TS}}: {
                {{GetOwnMembers().Select(RenderOwnMemberInitialize).OfType<string>().SelectTextTemplate(line => $$"""
                    {{WithIndent(line, "    ")}}
                """)}}
                  },
                {{If(HasLifeCycle, () => $$"""
                  {{EXISTS_IN_DB_TS}}: false,
                  {{WILL_BE_CHANGED_TS}}: true,
                  {{WILL_BE_DELETED_TS}}: false,
                  {{VERSION_TS}}: undefined,
                """)}}
                })
                """;
        }
    }

    /// <summary>
    /// <see cref="DataClassForDisplay"/> のうちルート集約でないもの
    /// </summary>
    internal class DataClassForDisplayDescendant : DataClassForDisplay {
        internal DataClassForDisplayDescendant(AggregateMember.RelationMember memberInfo) : base(memberInfo.MemberAggregate) {
            MemberInfo = memberInfo;
        }

        internal AggregateMember.RelationMember MemberInfo { get; }

        internal string MemberName => MemberInfo.MemberName;
        internal bool IsArray => MemberInfo.MemberAggregate.IsChildrenMember();

        internal IEnumerable<string> GetFullPath(GraphNode<Aggregate>? since = null) {
            return MemberInfo.GetFullPathAsDataClassForDisplay(E_CsTs.CSharp, since);
        }
    }

    partial class GetFullPathExtensions {
        /// <summary>
        /// エントリーからのパスを
        /// <see cref="DataClassForDisplay"/> と
        /// <see cref="RefTo.RefDisplayData"/> の
        /// インスタンスの型のルールにあわせて返す。
        /// </summary>
        internal static IEnumerable<string> GetFullPathAsDataClassForDisplay(this GraphNode<Aggregate> aggregate, E_CsTs csTs, GraphNode<Aggregate>? since = null, GraphNode<Aggregate>? until = null) {
            var path = aggregate.PathFromEntry();
            if (since != null) path = path.Since(since);
            if (until != null) path = path.Until(until);
            foreach (var edge in path) {

                if (edge.Source == edge.Terminal && edge.IsParentChild()) {
                    // 子から親へ向かう経路の場合
                    if (edge.Initial.As<Aggregate>().IsOutOfEntryTree()) {
                        yield return RefDisplayData.PARENT;
                    } else {
                        yield return $"/* エラー！{nameof(DataClassForDisplay)}では子は親の参照を持っていません */";
                    }
                } else if (edge.IsRef()) {
                    yield return csTs == E_CsTs.CSharp
                        ? DataClassForDisplay.VALUES_CS
                        : DataClassForDisplay.VALUES_TS;
                    yield return edge.RelationName;

                } else {
                    yield return edge.RelationName;
                }
            }
        }

        /// <inheritdoc cref="GetFullPathAsDataClassForDisplay(GraphNode{Aggregate}, GraphNode{Aggregate}?, GraphNode{Aggregate}?)"/>
        internal static IEnumerable<string> GetFullPathAsDataClassForDisplay(this AggregateMember.AggregateMemberBase member, E_CsTs csTs, GraphNode<Aggregate>? since = null, GraphNode<Aggregate>? until = null) {
            var fullpath = member.Owner
                .GetFullPathAsDataClassForDisplay(csTs, since, until)
                .ToArray();
            foreach (var path in fullpath) {
                yield return path;
            }

            if (member is AggregateMember.ValueMember && !member.Owner.IsOutOfEntryTree()
                || member is AggregateMember.Ref) {
                yield return csTs == E_CsTs.CSharp
                    ? DataClassForDisplay.VALUES_CS
                    : DataClassForDisplay.VALUES_TS;
            }

            yield return member.MemberName;
        }

        /// <summary>
        /// React hook form のregister名でのフルパス
        /// </summary>
        /// <param name="arrayIndexes">配列インデックスを指定する変数の名前</param>
        internal static IEnumerable<string> GetFullPathAsReactHookFormRegisterName(this GraphNode<Aggregate> aggregate, E_CsTs csts, IEnumerable<string>? arrayIndexes = null) {
            return GetFullPathAsReactHookFormRegisterName(aggregate, csts, false, arrayIndexes);
        }

        /// <summary>
        /// React hook form のregister名でのフルパス
        /// </summary>
        /// <param name="arrayIndexes">配列インデックスを指定する変数の名前</param>
        internal static IEnumerable<string> GetFullPathAsReactHookFormRegisterName(this AggregateMember.AggregateMemberBase member, E_CsTs csts, IEnumerable<string>? arrayIndexes = null) {
            foreach (var path in GetFullPathAsReactHookFormRegisterName(member.Owner, csts, true, arrayIndexes)) {
                yield return path;
            }
            yield return csts == E_CsTs.CSharp
                ? DataClassForDisplay.VALUES_CS
                : DataClassForDisplay.VALUES_TS;
            yield return member.MemberName;
        }

        private static IEnumerable<string> GetFullPathAsReactHookFormRegisterName(this GraphNode<Aggregate> aggregate, E_CsTs csts, bool enumerateLastChildrenIndex, IEnumerable<string>? arrayIndexes) {
            var currentArrayIndex = 0;

            foreach (var edge in aggregate.PathFromEntry()) {
                if (edge.Source == edge.Terminal) {

                    if (edge.IsParentChild()) {
                        yield return RefDisplayData.PARENT; // 子から親に向かって辿る場合

                    } else if (edge.IsRef()) {
                        throw new InvalidOperationException($"有向グラフの矢印の先から元に向かうパターンは親子だけなのでこの分岐にくることはあり得ないはず");
                    }

                } else {
                    var dataClass = new DataClassForDisplay(edge.Initial.As<Aggregate>());
                    var terminal = edge.Terminal.As<Aggregate>();

                    if (edge.IsParentChild()) {
                        yield return dataClass
                            .GetChildMembers()
                            .Single(p => p.MemberInfo.MemberAggregate == terminal)
                            .MemberName;

                        // 子要素が配列の場合はその配列の何番目の要素かを指定する必要がある
                        if (terminal.IsChildrenMember()
                            // "….Children.${}" の最後の配列インデックスを列挙するか否か
                            && (enumerateLastChildrenIndex || terminal != aggregate)) {

                            var arrayIndex = arrayIndexes?.ElementAtOrDefault(currentArrayIndex);
                            yield return $"${{{arrayIndex}}}";

                            currentArrayIndex++;
                        }

                    } else if (edge.IsRef()) {
                        yield return csts == E_CsTs.CSharp
                            ? DataClassForDisplay.VALUES_CS
                            : DataClassForDisplay.VALUES_TS;

                        yield return dataClass
                            .GetOwnMembers()
                            .OfType<AggregateMember.RelationMember>()
                            .Single(m => m.MemberAggregate == terminal)
                            .MemberName;

                    } else {
                        throw new InvalidOperationException($"有向グラフの矢印の先から元に向かうパターンは親子か参照だけなのでこの分岐にくることはあり得ないはず");
                    }
                }
            }
        }
    }
}
