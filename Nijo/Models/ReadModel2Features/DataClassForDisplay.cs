using Nijo.Core;
using Nijo.Models.RefTo;
using Nijo.Models.WriteModel2Features;
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
        /// 通常、保存時に追加・更新・削除のどの処理となるかは
        /// <see cref="EXISTS_IN_DB_TS"/>, <see cref="WILL_BE_CHANGED_TS"/>, <see cref="WILL_BE_DELETED_TS"/>
        /// から計算されるが、強制的に追加または更新または削除いずれかの処理を走らせたい場合に指定されるプロパティ
        /// </summary>
        internal const string ADD_MOD_DEL_CS = "AddModDel";
        /// <inheritdoc cref="ADD_MOD_DEL_CS"/>
        internal const string ADD_MOD_DEL_TS = "addModDel";

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
        internal bool HasLifeCycle => Aggregate.IsRoot() || Aggregate.IsChildrenMember() || Aggregate.Item.Options.HasLifeCycle;
        /// <summary>
        /// 楽観排他制御用のバージョンをもつかどうか
        /// </summary>
        internal bool HasVersion => Aggregate.IsRoot() || Aggregate.Item.Options.HasLifeCycle;

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
        /// 画面表示用クラスを再帰的に列挙します。
        /// </summary>
        private IEnumerable<DataClassForDisplay> EnumerateThisAndDescendantsRecursively() {
            yield return this;
            foreach (var descendant in GetChildMembers().SelectMany(child => child.EnumerateThisAndDescendantsRecursively())) {
                yield return descendant;
            }
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
                    // TODO #43 ADDMODDDELを明示的に指定できるようにする
                    /// <summary>このデータがDBに保存済みかどうか</summary>
                    [JsonPropertyName("{{EXISTS_IN_DB_TS}}")]
                    public virtual bool {{EXISTS_IN_DB_CS}} { get; set; }
                    /// <summary>このデータに更新がかかっているかどうか</summary>
                    [JsonPropertyName("{{WILL_BE_CHANGED_TS}}")]
                    public virtual bool {{WILL_BE_CHANGED_CS}} { get; set; }
                    /// <summary>このデータが更新確定時に削除されるかどうか</summary>
                    [JsonPropertyName("{{WILL_BE_DELETED_TS}}")]
                    public virtual bool {{WILL_BE_DELETED_CS}} { get; set; }
                """)}}
                {{If(HasVersion, () => $$"""
                    /// <summary>楽観排他制御用のバージョニング情報</summary>
                    [JsonPropertyName("{{VERSION_TS}}")]
                    public virtual int? {{VERSION_CS}} { get; set; }
                """)}}
                    /// <summary>どの項目が読み取り専用か</summary>
                    [JsonPropertyName("{{READONLY_TS}}")]
                    public virtual {{ReadOnlyDataCsClassName}} {{READONLY_CS}} { get; set; } = new();
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
                """)}}
                {{If(HasVersion, () => $$"""
                  /** 楽観排他制御用のバージョニング情報 */
                  {{VERSION_TS}}: number | undefined
                """)}}
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
        /// <summary>
        /// メッセージ用構造体をレンダリングします。
        /// </summary>
        private string RenderCsMessageClass(CodeRenderingContext context) {
            // ReadModel, WriteModel が同型のオブジェクトの場合、
            // Write側の各プロジェクトをそのままRead型の各プロパティにマッピングすればよいのでクラスの内容も自動生成する。
            // それ以外の場合、とりあえずReadModelと同じ型の構造で定義する。
            // （ただ、ほとんどの場合はWriteModelのエラーデータのインターフェースを実装できないと考えられるため、
            //   ここで生成されたクラスが実際に使われることは少ないだろう）
            var areReadAndWriteSame = Aggregate.GetRoot().Item.Options.GenerateDefaultReadModel;

            var members = GetOwnMembers().Select(m => new {
                MemberInfo = m,
                m.MemberName,
                CsTypeName = DisplayMessageContainer.INTERFACE,
                m.Order,
            }).Concat(GetChildMembers().Select(desc => {
                // Read/Write が同形の場合は、WriteModelのインターフェースを実装できるようWrite側のインターフェース名で宣言。
                // そうでない場合は別になんでもよいのでReadModelのクラス名で直接宣言。
                var descType = areReadAndWriteSame
                    ? new DataClassForSave(desc.MemberInfo.MemberAggregate, DataClassForSave.E_Type.UpdateOrDelete).MessageDataCsInterfaceName
                    : desc.MessageDataCsClassName;

                return new {
                    MemberInfo = (AggregateMember.AggregateMemberBase)desc.MemberInfo,
                    desc.MemberName,
                    CsTypeName = desc.MemberInfo is AggregateMember.Children children
                        ? $"{DisplayMessageContainer.CONCRETE_CLASS_LIST}<{descType}>"
                        : descType,
                    desc.MemberInfo.Order,
                };
            }))
            .OrderBy(m => m.Order)
            .ToArray();

            // この集約がグリッドで表示される場合はエラーメッセージの表示方法が特殊
            var isInGrid = Aggregate
                .EnumerateAncestorsAndThis()
                .Any(agg => agg.IsChildrenMember()
                         && agg.CanDisplayAllMembersAs2DGrid());

            // WriteModel側のエラーデータのインターフェース
            var writeModelInterface = new DataClassForSave(Aggregate, DataClassForSave.E_Type.UpdateOrDelete).MessageDataCsInterfaceName;

            // このクラスが継承する基底クラスやインターフェース
            var implements = new List<string>();
            if (isInGrid) {
                implements.Add(DisplayMessageContainer.CONCRETE_CLASS_IN_GRID);
            } else {
                implements.Add(DisplayMessageContainer.ABSTRACT_CLASS);
            }
            if (areReadAndWriteSame) {
                implements.Add(writeModelInterface);
            }

            string[] args = isInGrid
                ? ["IEnumerable<string> path", $"{DisplayMessageContainer.ABSTRACT_CLASS} grid", "int rowIndex"]
                : ["IEnumerable<string> path"];
            string[] @base = isInGrid
                ? ["path, grid, rowIndex"]
                : ["path"];

            return $$"""
                /// <summary>
                /// {{Aggregate.Item.DisplayName}}の画面表示用データのメッセージ情報格納部分。
                {{If(areReadAndWriteSame, () => $$"""
                /// <see cref="{{writeModelInterface}}"/> の更新処理で発生したエラー等を画面項目にマッピングする。
                """).Else(() => $$"""
                /// WriteModelのデータとのマッピングが必要になる場合、このクラスを使用せず、
                /// 別途当該WriteModelのエラーデータのインターフェースを実装したクラスを新規作成して、そちらを使用してください。
                """)}}
                /// </summary>
                public partial class {{MessageDataCsClassName}} : {{implements.Join(", ")}} {
                    public {{MessageDataCsClassName}}({{args.Join(", ")}}) : base({{@base.Join(", ")}}) {
                {{members.SelectTextTemplate(m => $$"""
                        {{WithIndent(RenderConstructor(m.MemberInfo), "        ")}}
                """)}}
                    }

                {{members.SelectTextTemplate(m => $$"""
                    /// <summary>{{m.MemberName}}についてのメッセージ</summary>
                    public {{m.CsTypeName}} {{m.MemberName}} { get; }
                """)}}

                    public override IEnumerable<{{DisplayMessageContainer.INTERFACE}}> EnumerateChildren() {
                {{If(members.Length == 0, () => $$"""
                        yield break;
                """)}}
                {{members.SelectTextTemplate(m => $$"""
                        yield return {{m.MemberName}};
                """)}}
                    }
                }
                """;

            string RenderConstructor(AggregateMember.AggregateMemberBase member) {
                if (member is AggregateMember.ValueMember || member is AggregateMember.Ref) {
                    return isInGrid ? $$"""
                        {{member.MemberName}} = new {{DisplayMessageContainer.CONCRETE_CLASS_IN_GRID}}([.. path, "{{VALUES_TS}}", "{{member.MemberName}}"], grid, rowIndex);
                        """ : $$"""
                        {{member.MemberName}} = new {{DisplayMessageContainer.CONCRETE_CLASS}}([.. path, "{{VALUES_TS}}", "{{member.MemberName}}"]);
                        """;

                } else if (member is AggregateMember.Children children) {
                    var desc = new DataClassForDisplayDescendant(children);
                    return $$"""
                        {{member.MemberName}} = new([.. path, "{{member.MemberName}}"], rowIndex => {
                        {{If(children.ChildrenAggregate.CanDisplayAllMembersAs2DGrid(), () => $$"""
                            return new {{desc.MessageDataCsClassName}}([.. path, "{{member.MemberName}}", rowIndex.ToString()], {{member.MemberName}}!, rowIndex);
                        """).Else(() => $$"""
                            return new {{desc.MessageDataCsClassName}}([.. path, "{{member.MemberName}}", rowIndex.ToString()]);
                        """)}}
                        });
                        """;

                } else if (member is AggregateMember.RelationMember rel) {
                    var desc = new DataClassForDisplayDescendant(rel);
                    return $$"""
                        {{member.MemberName}} = new {{desc.MessageDataCsClassName}}([.. path, "{{member.MemberName}}"]);
                        """;

                } else {
                    throw new NotImplementedException();
                }
            }
        }
        /// <summary>
        /// ReadModel, WriteModel が同型のオブジェクトではない場合のメッセージ構造体定義
        /// </summary>
        private string RenderCsMessageClassOnlyPartialDeclaring(CodeRenderingContext context) {
            return $$"""
                /// <summary>
                /// {{Aggregate.Item.DisplayName}}の画面表示用データのメッセージ情報格納部分
                /// </summary>
                public partial class {{MessageDataCsClassName}} : {{DisplayMessageContainer.ABSTRACT_CLASS}} {
                    // このクラスはpartialクラスです。
                    // ソース自動生成で洗い替えられない場所に別ファイルでこのクラスの実装を記述し、必要なメソッド等はそちらで定義してください。
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
                    public virtual bool {{ALL_READONLY_CS}} { get; set; }
                {{GetOwnMembers().SelectTextTemplate(member => $$"""
                    /// <summary>{{member.MemberName}}が読み取り専用か否か</summary>
                    public virtual bool {{member.MemberName}} { get; set; }
                """)}}
                }
                """;
        }
        private string RenderReadonlyTsType(CodeRenderingContext context) {
            return $$"""
                {
                  /** {{Aggregate.Item.DisplayName}}全体が読み取り専用か否か */
                  {{ALL_READONLY_TS}}?: boolean
                {{GetOwnMembers().SelectTextTemplate(member => $$"""
                  /** {{member.MemberName}}が読み取り専用か否か */
                  {{member.MemberName}}?: boolean
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


        #region クライアント側でのオブジェクト新規作成
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

                // ref
                if (member is AggregateMember.Ref @ref
                    && @ref.DeclaringAggregate == Aggregate) {
                    var refInstance = new RefDisplayData(@ref.RefTo, @ref.RefTo);
                    return $"{member.MemberName}: {refInstance.TsNewObjectFunction}(),";
                }

                // 初期値なし
                return null;
            }

            return $$"""
                /** {{Aggregate.Item.DisplayName}}の画面表示用オブジェクトを新規作成します。 */
                export const {{TsNewObjectFunction}} = (): {{TsTypeName}} => ({
                  {{VALUES_TS}}: {
                {{GetOwnMembers().Select(RenderOwnMemberInitialize).OfType<string>().SelectTextTemplate(line => $$"""
                    {{WithIndent(line, "    ")}}
                """)}}
                  },
                {{GetChildMembers().SelectTextTemplate(m => m.IsArray ? $$"""
                  {{m.MemberName}}: [],
                """ : $$"""
                  {{m.MemberName}}: {{m.TsNewObjectFunction}}(),
                """)}}
                {{If(HasLifeCycle, () => $$"""
                  {{EXISTS_IN_DB_TS}}: false,
                  {{WILL_BE_CHANGED_TS}}: true,
                  {{WILL_BE_DELETED_TS}}: false,
                """)}}
                {{If(HasVersion, () => $$"""
                  {{VERSION_TS}}: undefined,
                """)}}
                })
                """;
        }
        #endregion クライアント側でのオブジェクト新規作成


        #region ディープ・イコール
        /// <summary>
        /// 値比較関数の名前
        /// </summary>
        internal string DeepEqualFunction => $"deepEquals{TsTypeName}";
        internal string RenderDeepEqualFunctionRecursively(CodeRenderingContext context) {
            // 登録更新の対象となる子孫要素（==楽観排他制御用のバージョンを持っている子孫要素）のみレンダリングする
            var rendering = EnumerateThisAndDescendantsRecursively()
                .Where(displayData => displayData.HasVersion);

            return $$"""
                {{rendering.SelectTextTemplate(displayData => displayData.RenderDeepEqualFunction(context))}}
                """;
        }
        private string RenderDeepEqualFunction(CodeRenderingContext context) {
            if (!HasVersion) throw new InvalidOperationException("更新の単位にならないオブジェクトでディープイコール関数は定義不可");

            return $$"""
                /** 2つの{{Aggregate.Item.DisplayName}}オブジェクトの値を比較し、一致しているかを返します。 */
                export const {{DeepEqualFunction}} = (a: {{TsTypeName}}, b: {{TsTypeName}}): boolean => {
                  {{WithIndent(RenderAggregate(this, "a", "b", Aggregate), "  ")}}
                  return true
                }
                """;

            string RenderAggregate(DataClassForDisplay rendering, string a, string b, GraphNode<Aggregate> instanceAgg) {
                var ownMembers = rendering
                    .GetOwnMembers();
                var childMembers = rendering
                    .GetChildMembers()
                    .Where(m => !m.HasVersion); // バージョンを持っている子要素は別のライフサイクルを持つのでこの関数では判定しない

                return $$"""
                    {{ownMembers.SelectTextTemplate(RenderOwnMember)}}
                    {{childMembers.SelectTextTemplate(RenderChildMember)}}
                    """;

                string RenderOwnMember(AggregateMember.AggregateMemberBase member) {
                    if (member is AggregateMember.ValueMember vm) {
                        var fullpath = vm.Declared.GetFullPathAsDataClassForDisplay(E_CsTs.TypeScript, instanceAgg);
                        return $$"""
                            if (({{a}}.{{fullpath.Join("?.")}} ?? undefined) !== ({{b}}.{{fullpath.Join("?.")}} ?? undefined)) return false
                            """;
                    } else if (member is AggregateMember.Ref @ref) {
                        var fullpath = @ref.GetFullPathAsDataClassForDisplay(E_CsTs.TypeScript, instanceAgg);
                        var keyFullpaths = @ref.RefTo
                            .GetKeys()
                            .OfType<AggregateMember.ValueMember>()
                            .Select(vm => vm.Declared.GetFullPathAsDataClassForDisplay(E_CsTs.TypeScript, instanceAgg));
                        return $$"""
                            {{keyFullpaths.SelectTextTemplate(path => $$"""
                            if (({{a}}.{{path.Join("?.")}} ?? undefined) !== ({{b}}.{{path.Join("?.")}} ?? undefined)) return false
                            """)}}
                            """;
                    } else {
                        throw new NotImplementedException();
                    }
                }

                string RenderChildMember(DataClassForDisplayDescendant descendant) {
                    if (descendant.IsArray) {
                        var arrayPath = descendant.MemberInfo.GetFullPathAsDataClassForDisplay(E_CsTs.TypeScript, instanceAgg);
                        var depth = descendant.MemberInfo.Owner.EnumerateAncestors().Count();
                        var i = depth < 1 ? "i" : $"i{depth}";
                        var a1 = $"a{depth}";
                        var b1 = $"b{depth}";
                        return $$"""
                            if ({{a}}.{{arrayPath.Join("?.")}}.length !== {{b}}.{{arrayPath.Join("?.")}}.length) return false;
                            if (({{a}}.{{arrayPath.Join("?.")}} ?? undefined) !== undefined && ({{b}}.{{arrayPath.Join("?.")}} ?? undefined) !== undefined) {
                              for (let {{i}} = 0; {{i}} < {{a}}.{{arrayPath.Join(".")}}.length; {{i}}++) {
                                const {{a1}} = {{a}}.{{arrayPath.Join(".")}}[{{i}}]
                                const {{b1}} = {{b}}.{{arrayPath.Join(".")}}[{{i}}]
                                {{WithIndent(RenderAggregate(descendant, a1, b1, descendant.Aggregate), "    ")}}
                              }
                            }
                            """;

                    } else {
                        return RenderAggregate(descendant, a, b, instanceAgg);
                    }
                }
            }
        }
        #endregion ディープ・イコール


        #region 変更確認
        /// <summary>
        /// 変更確認関数の名前
        /// </summary>
        internal string CheckChangesFunction => $"checkChanges{TsTypeName}";
        internal string RenderCheckChangesFunction(CodeRenderingContext context) {
            var descendants = EnumerateThisAndDescendantsRecursively()
                .OfType<DataClassForDisplayDescendant>()
                .Where(disp => disp.HasVersion)
                .Select((disp, i) => {
                    var path = disp.MemberInfo.GetFullPathAsDataClassForDisplay(E_CsTs.TypeScript, out var isArray);
                    return new {
                        Path = path,
                        IsArray = isArray,
                        DeepEquals = disp.DeepEqualFunction,
                        DefaultValue = $"{disp.Aggregate.Item.PhysicalName}defaultValue{(isArray ? "s" : "")}{i}",
                        CurrentValue = $"{disp.Aggregate.Item.PhysicalName}currentValue{(isArray ? "s" : "")}{i}",
                        disp.TsTypeName,
                        KeyPaths = disp.MemberInfo.MemberAggregate
                            .GetKeys()
                            .OfType<AggregateMember.ValueMember>()
                            .Where(vm => vm.Inherits?.Relation.IsParentChild() != true)
                            .Select(vm => vm.Declared.GetFullPathAsDataClassForDisplay(E_CsTs.TypeScript, disp.MemberInfo.MemberAggregate)),
                    };
                });

            return $$"""
                /** 更新前後の値をディープイコールで判定し、変更があったオブジェクトのwillBeChangedプロパティをtrueに設定して返します。 */
                export const {{CheckChangesFunction}} = ({ defaultValues, currentValues }: {
                  defaultValues: {{TsTypeName}}
                  currentValues: {{TsTypeName}}
                }): boolean => {
                  let anyChanged = false

                  if ({{DeepEqualFunction}}(defaultValues, currentValues)) {
                    currentValues.{{WILL_BE_CHANGED_TS}} = false
                  } else {
                    currentValues.{{WILL_BE_CHANGED_TS}} = true
                    anyChanged = true
                  }

                {{descendants.SelectTextTemplate(x => x.IsArray ? $$"""
                  const {{x.DefaultValue}} = defaultValues.{{x.Path.Join("?.")}} ?? []
                  const {{x.CurrentValue}} = currentValues.{{x.Path.Join("?.")}} ?? []
                  for (const after of {{x.CurrentValue}}) {
                    const before = {{x.DefaultValue}}.find(b =>
                {{x.KeyPaths.SelectTextTemplate((keyPath, i) => $$"""
                      {{(i == 0 ? "" : "&& ")}}b.{{keyPath.Join("?.")}} === after.{{keyPath.Join("?.")}}
                """)}})
                    if (before && {{x.DeepEquals}}(before, after)) {
                      after.{{WILL_BE_CHANGED_TS}} = false
                    } else {
                      after.{{WILL_BE_CHANGED_TS}} = true
                      anyChanged = true
                    }
                  }

                """ : $$"""
                  const {{x.DefaultValue}} = defaultValues.{{x.Path.Join("?.")}}
                  const {{x.CurrentValue}} = currentValues.{{x.Path.Join("?.")}}
                  if ({{x.CurrentValue}}) {
                    if ({{x.DeepEquals}}({{x.DefaultValue}}, {{x.CurrentValue}})) {
                      {{x.CurrentValue}}.{{WILL_BE_CHANGED_TS}} = false
                    } else {
                      {{x.CurrentValue}}.{{WILL_BE_CHANGED_TS}} = true
                      anyChanged = true
                    }
                  }

                """)}}
                  return anyChanged
                }
                """;
        }
        #endregion 変更確認


        #region 主キーを読み取り専用にする
        internal const string SET_KEYS_READONLY = "SetKeysReadOnly";
        internal string RenderSetKeysReadOnly(CodeRenderingContext context) {
            return $$"""
                /// <summary>
                /// {{Aggregate.Item.DisplayName}}の主キー項目を読み取り専用にします。
                /// </summary>
                private void {{SET_KEYS_READONLY}}({{CsClassName}} displayData) {
                    {{WithIndent(RenderAggregate(this, "displayData", Aggregate), "    ")}}
                }
                """;

            static IEnumerable<string> RenderAggregate(DataClassForDisplay dataClass, string instance, GraphNode<Aggregate> instanceAggregate) {

                // 集約自身のキーの読み取り専用
                foreach (var member in dataClass.GetOwnMembers()) {
                    if (member is AggregateMember.ValueMember vm) {
                        if (!vm.IsKey) continue;

                        yield return $$"""
                            {{instance}}.{{vm.Declared.GetFullPathAsDataClassForDisplay(E_CsTs.CSharp, since: instanceAggregate, pathType: E_PathType.ReadOnly).Join(".")}} = true;
                            """;

                    } else if (member is AggregateMember.Ref @ref) {
                        if (!@ref.Relation.IsPrimary()) continue;

                        yield return $$"""
                            {{instance}}.{{@ref.GetFullPathAsDataClassForDisplay(E_CsTs.CSharp, since: instanceAggregate, pathType: E_PathType.ReadOnly).Join(".")}} = true;
                            """;
                    }
                }

                // 子孫集約の読み取り専用
                foreach (var child in dataClass.GetChildMembers()) {
                    if (child.Aggregate.IsChildrenMember()) {
                        var depth = child.Aggregate.EnumerateAncestors().Count();
                        var x = depth == 1 ? "x" : $"x{depth - 1}";
                        yield return $$"""
                            foreach (var {{x}} in {{instance}}.{{child.Aggregate.GetFullPathAsDataClassForDisplay(E_CsTs.CSharp, since: instanceAggregate, pathType: E_PathType.ReadOnly).Join(".")}}) {
                                {{WithIndent(RenderAggregate(child, x, child.Aggregate.AsChildRelationMember().MemberAggregate), "    ")}}
                            }
                            """;

                    } else {
                        yield return $$"""
                            {{WithIndent(RenderAggregate(child, instance, instanceAggregate), "")}}
                            """;
                    }
                }
            }
        }
        #endregion 主キーを読み取り専用にする


        #region UI用の制約定義
        internal string UiConstraintTypeName => $"{Aggregate.Item.PhysicalName}ConstraintType";
        internal string UiConstraingValueName => $"{Aggregate.Item.PhysicalName}Constraints";
        internal string RenderUiConstraintType(CodeRenderingContext ctx) {
            var prefix = ctx.Config.CustomizeAllUi ? "Constraints." : "";

            return $$"""
                /** {{Aggregate.Item.DisplayName}}の各メンバーの制約の型 */
                type {{UiConstraintTypeName}} = {
                  {{WithIndent(RenderMembers(this), "  ")}}
                }
                """;

            string RenderMembers(DataClassForDisplay displayData) {
                return $$"""
                    {{VALUES_TS}}: {
                    {{displayData.GetOwnMembers().SelectTextTemplate(m => $$"""
                    {{If(m is AggregateMember.ValueMember, () => $$"""
                      {{m.MemberName}}: {{prefix}}{{((AggregateMember.ValueMember)m).Options.MemberType.UiConstraintType}}
                    """).ElseIf(m is AggregateMember.Ref, () => $$"""
                      {{m.MemberName}}: {{prefix}}MemberConstraintBase
                    """)}}
                    """)}}
                    }
                    {{displayData.GetChildMembers().SelectTextTemplate(desc => $$"""
                    {{desc.MemberName}}: {
                      {{WithIndent(RenderMembers(desc), "  ")}}
                    }
                    """)}}
                    """;
            }
        }
        internal string RenderUiConstraintValue(CodeRenderingContext ctx) {
            return $$"""
                /** {{Aggregate.Item.DisplayName}}の各メンバーの制約の具体的な値 */
                export const {{UiConstraingValueName}}: {{UiConstraintTypeName}} = {
                  {{WithIndent(RenderMembers(this), "  ")}}
                }
                """;

            static string RenderMembers(DataClassForDisplay displayData) {
                return $$"""
                    {{VALUES_TS}}: {
                    {{displayData.GetOwnMembers().SelectTextTemplate(m => $$"""
                    {{If(m is AggregateMember.ValueMember, () => $$"""
                      {{m.MemberName}}: {
                    {{If(((AggregateMember.ValueMember)m).IsKey || ((AggregateMember.ValueMember)m).IsRequired, () => $$"""
                        required: true,
                    """)}}
                    {{((AggregateMember.ValueMember)m).Options.MemberType.RenderUiConstraintValue((AggregateMember.ValueMember)m).SelectTextTemplate(source => $$"""
                        {{WithIndent(source, "    ")}},
                    """)}}
                      },
                    """).ElseIf(m is AggregateMember.Ref, () => $$"""
                      {{m.MemberName}}: {
                    {{If(((AggregateMember.Ref)m).Relation.IsPrimary() || ((AggregateMember.Ref)m).Relation.IsRequired(), () => $$"""
                        required: true,
                    """)}}
                      },
                    """)}}
                    """)}}
                    },
                    {{displayData.GetChildMembers().SelectTextTemplate(desc => $$"""
                    {{desc.MemberName}}: {
                      {{WithIndent(RenderMembers(desc), "  ")}}
                    },
                    """)}}
                    """;
            }
        }
        #endregion UI用の制約定義


        /// <summary>
        /// <see cref="SearchResult"/> からの変換処理をレンダリングします。
        /// </summary>
        /// <param name="instance">検索結果のインスタンスの名前</param>
        /// <param name="instanceAggregate">instanceの型</param>
        /// <param name="renderNewClassName">new演算子のあとにクラス名をレンダリングするかどうか</param>
        internal string RenderConvertFromSearchResult(string instance, GraphNode<Aggregate> instanceAggregate, bool renderNewClassName) {
            var pkDict = new Dictionary<AggregateMember.ValueMember, string>();
            return RenderConvertFromSearchResultPrivate(instance, instanceAggregate, renderNewClassName, pkDict);
        }
        private string RenderConvertFromSearchResultPrivate(
            string instance,
            GraphNode<Aggregate> instanceAgg,
            bool renderNewClassName,
            Dictionary<AggregateMember.ValueMember, string> pkDict) {

            // 主キー。レンダリング中の集約がChildrenの場合は親のキーをラムダ式の外の変数から参照する必要がある
            var keys = new List<string>();
            foreach (var key in Aggregate.GetKeys().OfType<AggregateMember.ValueMember>()) {
                if (!pkDict.TryGetValue(key.Declared, out var keyString)) {
                    keyString = $"{instance}.{key.Declared.GetFullPathAsSearchResult(instanceAgg).Join("?.")}";
                    pkDict.Add(key.Declared, keyString);
                }
                keys.Add(keyString);
            }

            var newStatement = renderNewClassName
                ? $"new {CsClassName}"
                : $"new()";
            var depth = Aggregate
                .EnumerateAncestors()
                .Count();
            var loopVar = depth == 0 ? "item" : $"item{depth}";

            var version = Aggregate
                .GetFullPathAsSearchResult(instanceAgg)
                .Concat([SearchResult.VERSION]);

            return $$"""
                {{newStatement}} {
                {{If(HasLifeCycle, () => $$"""
                    {{EXISTS_IN_DB_CS}} = true,
                    {{WILL_BE_CHANGED_CS}} = false,
                    {{WILL_BE_DELETED_CS}} = false,
                """)}}
                {{If(HasVersion, () => $$"""
                    {{VERSION_CS}} = {{instance}}.{{version.Join("?.")}},
                """)}}
                    {{VALUES_CS}} = new {{ValueCsClassName}} {
                {{GetOwnMembers().SelectTextTemplate(m => $$"""
                        {{m.MemberName}} = {{WithIndent(RenderOwnMemberConvert(m), "        ")}},
                """)}}
                    },
                {{GetChildMembers().SelectTextTemplate(child => child.IsArray ? $$"""
                    {{child.MemberName}} = {{instance}}.{{child.MemberInfo.GetFullPathAsDataClassForDisplay(E_CsTs.CSharp, instanceAgg).Join("?.")}}?.Select({{loopVar}} => {{WithIndent(child.RenderConvertFromSearchResultPrivate(loopVar, child.Aggregate, true, pkDict), "    ")}}).ToList() ?? [],
                """ : $$"""
                    {{child.MemberName}} = {{WithIndent(child.RenderConvertFromSearchResultPrivate(instance, instanceAgg, false, pkDict), "    ")}},
                """)}}
                }
                """;

            string RenderOwnMemberConvert(AggregateMember.AggregateMemberBase member) {
                if (member is AggregateMember.ValueMember vm) {
                    return $$"""
                        {{instance}}.{{vm.Declared.GetFullPathAsSearchResult(instanceAgg).Join("?.")}}
                        """;

                } else if (member is AggregateMember.Ref @ref) {
                    var refDisplayData = new RefDisplayData(@ref.RefTo, @ref.RefTo);
                    return $$"""
                        {{refDisplayData.RenderConvertFromRefSearchResult(instance, instanceAgg, false)}}
                        """;

                } else {
                    throw new NotImplementedException();
                }
            }
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
    }


    partial class GetFullPathExtensions {
        /// <summary>
        /// エントリーからのパスを
        /// <see cref="DataClassForDisplay"/> と
        /// <see cref="RefDisplayData"/> の
        /// インスタンスの型のルールにあわせて返す。
        /// </summary>
        internal static IEnumerable<string> GetFullPathAsDataClassForDisplay(
            this GraphNode<Aggregate> aggregate,
            E_CsTs csts,
            GraphNode<Aggregate>? since = null,
            GraphNode<Aggregate>? until = null,
            E_PathType pathType = E_PathType.Value) {

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
                    if (!edge.Initial.As<Aggregate>().IsOutOfEntryTree()) {

                        if (csts == E_CsTs.CSharp) {
                            yield return pathType == E_PathType.Value
                                ? DataClassForDisplay.VALUES_CS
                                : DataClassForDisplay.READONLY_CS;
                        } else {
                            yield return pathType == E_PathType.Value
                                ? DataClassForDisplay.VALUES_TS
                                : DataClassForDisplay.READONLY_TS;
                        }
                    }
                    yield return edge.RelationName;

                } else {
                    yield return edge.RelationName;
                }
            }
        }

        /// <summary>
        /// エントリーからのパスを
        /// <see cref="DataClassForDisplay"/> と
        /// <see cref="RefDisplayData"/> の
        /// インスタンスの型のルールにあわせて返す。
        /// </summary>
        internal static IEnumerable<string> GetFullPathAsDataClassForDisplay(
            this AggregateMember.AggregateMemberBase member,
            E_CsTs csts,
            GraphNode<Aggregate>? since = null,
            GraphNode<Aggregate>? until = null,
            E_PathType pathType = E_PathType.Value) {

            var fullpath = member.Owner
                .GetFullPathAsDataClassForDisplay(csts, since, until, pathType)
                .ToArray();
            foreach (var path in fullpath) {
                yield return path;
            }

            if ((member is AggregateMember.Ref || member is AggregateMember.ValueMember)
                && !member.Owner.IsOutOfEntryTree()) {

                if (csts == E_CsTs.CSharp) {
                    yield return pathType == E_PathType.Value
                        ? DataClassForDisplay.VALUES_CS
                        : DataClassForDisplay.READONLY_CS;
                } else {
                    yield return pathType == E_PathType.Value
                        ? DataClassForDisplay.VALUES_TS
                        : DataClassForDisplay.READONLY_TS;
                }
            }

            yield return member.MemberName;
        }
    }


    /*
     * ---------------------------------------------------------
     * Select, SelectMany, map, flatMap 込みのフルパスを返すAPI ここから
     */

    partial class GetFullPathExtensions {
        /// <summary>
        /// フルパスの途中で配列が出てきた場合はSelectやmapをかける
        /// </summary>
        internal static IEnumerable<string> GetFullPathAsDataClassForDisplay(this AggregateMember.AggregateMemberBase member, E_CsTs csts, out bool isArray, GraphNode<Aggregate>? since = null, GraphNode<Aggregate>? until = null) {
            var result = new List<string>();
            isArray = false;
            var path = member.Owner.PathFromEntry();
            if (since != null) path = path.Since(since);
            if (until != null) path = path.Until(until);
            foreach (var e in path) {
                var edge = e.As<Aggregate>();

                if (edge.IsParentChild()) {
                    if (edge.Source == edge.Terminal) {
                        // 子から親へ向かう経路の場合
                        if (edge.Initial.IsOutOfEntryTree()) {
                            result.Add(RefDisplayData.PARENT);
                        } else {
                            result.Add($"/* エラー！{nameof(DataClassForDisplay)}では子は親の参照を持っていません */");
                        }

                    } else {
                        // 親から子へ向かう経路の場合
                        var isMany = edge.Terminal.IsChildrenMember();
                        if (isMany) {
                            result.Add(isArray
                                ? (csts == E_CsTs.CSharp
                                    ? $"SelectMany(x => x.{edge.RelationName})"
                                    : $"flatMap(x => x.{edge.RelationName})")
                                : edge.RelationName);
                            isArray = true;

                        } else {
                            result.Add(isArray
                                ? (csts == E_CsTs.CSharp
                                    ? $"Select(x => x.{edge.RelationName})"
                                    : $"map(x => x.{edge.RelationName})")
                                : edge.RelationName);
                        }
                    }
                } else if (edge.IsRef()) {
                    if (!edge.Initial.IsOutOfEntryTree()) {
                        result.Add(csts == E_CsTs.CSharp
                            ? DataClassForDisplay.VALUES_CS
                            : DataClassForDisplay.VALUES_TS);
                    }
                    result.Add(edge.RelationName);

                } else {
                    throw new NotImplementedException();
                }
            }

            if ((member is AggregateMember.Ref || member is AggregateMember.ValueMember)
                && !member.Owner.IsOutOfEntryTree()) {
                result.Add(csts == E_CsTs.CSharp
                    ? DataClassForDisplay.VALUES_CS
                    : DataClassForDisplay.VALUES_TS);
            }

            if (!isArray && member is AggregateMember.Children) {
                isArray = true;
                result.Add(member.MemberName);

            } else if (isArray) {
                result.Add(csts == E_CsTs.CSharp
                    ? $"Select(x => x.{member.MemberName})"
                    : $"map(x => x.{member.MemberName})");

            } else {
                result.Add(member.MemberName);
            }

            return result;
        }
    }


    /*
     * ---------------------------------------------------------
     * React hook form のregister等に用いるためのパス生成 ここから
     */
    partial class GetFullPathExtensions {

        /// <summary>
        /// React hook form のregister名でのフルパス
        /// </summary>
        /// <param name="arrayIndexes">配列インデックスを指定する変数の名前</param>
        internal static IEnumerable<string> GetFullPathAsReactHookFormRegisterName(this AggregateMember.AggregateMemberBase member, E_PathType pathType, IEnumerable<string>? arrayIndexes = null) {
            var currentArrayIndex = 0;
            var path = member.Owner.PathFromEntry();
            foreach (var e in path) {
                var edge = e.As<Aggregate>();

                if (edge.Source == edge.Terminal && edge.IsParentChild()) {
                    // 子から親へ向かう経路の場合
                    if (edge.Initial.IsOutOfEntryTree()) {
                        yield return RefDisplayData.PARENT;
                    } else {
                        yield return $"/* エラー！{nameof(DataClassForDisplay)}では子は親の参照を持っていません */";
                    }
                } else if (edge.IsRef()) {
                    if (!edge.Initial.IsOutOfEntryTree()) {
                        yield return pathType == E_PathType.Value
                            ? DataClassForDisplay.VALUES_TS
                            : DataClassForDisplay.READONLY_TS;
                    }
                    yield return edge.RelationName;

                } else {
                    yield return edge.RelationName;

                    // 配列インデックス
                    var isChildren = edge.Source == edge.Initial && edge.Terminal.IsChildrenMember();
                    var isLastChildren = member is AggregateMember.Children children && edge == children.Relation; // 配列自身に対するフルパス列挙の場合は末尾の配列インデックスは列挙しない
                    if (isChildren && !isLastChildren) {
                        yield return $"${{{arrayIndexes?.ElementAtOrDefault(currentArrayIndex)}}}";
                        currentArrayIndex++;
                    }
                }
            }

            if ((member is AggregateMember.Ref || member is AggregateMember.ValueMember)
                && !member.Owner.IsOutOfEntryTree()) {
                yield return pathType == E_PathType.Value
                    ? DataClassForDisplay.VALUES_TS
                    : DataClassForDisplay.READONLY_TS;
            }

            if (member is AggregateMember.Parent) {
                if (member.Owner.IsOutOfEntryTree()) {
                    yield return RefDisplayData.PARENT;
                } else {
                    yield return $"/* エラー！{nameof(DataClassForDisplay)}では子は親の参照を持っていません */";
                }
            } else {
                yield return member.MemberName;
            }
        }
    }

    /// <summary>
    /// 画面表示用データのReact hook form のフルパス取得でどの値のパスをとるか
    /// </summary>
    internal enum E_PathType {
        Value,
        ReadOnly,
    }
}
