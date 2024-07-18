using Nijo.Core;
using Nijo.Features.Storing;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.WriteModel2Features {
    /// <summary>
    /// 登録・更新・削除のトランザクションのひとかたまりの粒度のデータクラス。
    /// </summary>
    internal class DataClassForSave {

        internal enum E_Type {
            /// <summary>
            /// 新規登録用のデータクラスであることを示す。
            /// 新規登録時には存在しえない項目（DBで採番されるシーケンスや、排他制御のためのバージョンなど）が無い。
            /// </summary>
            Create,
            /// <summary>
            /// 更新・削除用のデータクラスであることを示す。
            /// すでに永続化されているが故に存在する項目（DBで採番されるシーケンスや、排他制御のためのバージョンなど）をもつ。
            /// </summary>
            UpdateOrDelete,
        }

        /// <summary>
        /// 追加・更新・削除のいずれかを表す区分のenum名（C#側）。
        /// </summary>
        internal const string ADD_MOD_DEL_CS = "E_AddOrModOrDel";
        /// <summary>
        /// 追加・更新・削除のいずれかを表す区分の型名（TypeScript側）。
        /// </summary>
        internal const string ADD_MOD_DEL_TS = "AddOrModOrDelType";

        /// <summary>
        /// 追加・更新・削除のいずれかを表す区分（C#側）の定義をレンダリングします。
        /// </summary>
        internal static string RenderAddModDelEnum() {
            return $$"""
                /// <summary>追加・更新・削除のいずれかを表す区分</summary>
                public enum {{ADD_MOD_DEL_CS}} {
                    /// <summary>新規追加</summary>
                    ADD,
                    /// <summary>更新</summary>
                    MOD,
                    /// <summary>削除</summary>
                    DEL,
                    /// <summary>変更なし</summary>
                    NONE,
                }
                """;
        }
        /// <summary>
        /// 追加・更新・削除のいずれかを表す区分（TypeScript側）の定義をレンダリングします。
        /// </summary>
        internal static string RenderAddModDelType() {
            return $$"""
                /** 追加・更新・削除のいずれかを表す区分 */
                export type {{ADD_MOD_DEL_TS}} = 'ADD' | 'MOD' | 'DEL' | 'NONE'
                """;
        }

        /// <summary>
        /// <see cref="AggregateMember.AggregateMemberBase"/> のC#の型名を返します。
        /// このメソッドの戻り値にnull許容演算子はつきません。
        /// </summary>
        internal static string GetMemberTypeNameCSharp(AggregateMember.AggregateMemberBase member, E_Type type) {
            return member switch {
                AggregateMember.ValueMember vm => vm.Options.MemberType.GetCSharpTypeName(),
                AggregateMember.Ref @ref => new ForRef.DataClassForSaveRefTarget(@ref.RefTo).CsClassName,
                AggregateMember.Parent => throw new NotImplementedException(), // Parentはこのデータクラスのメンバーにならない
                AggregateMember.Children children => $"List<{new DataClassForSave(children.ChildrenAggregate, type).CsClassName}>",
                AggregateMember.Child child => new DataClassForSave(child.ChildAggregate, type).CsClassName,
                AggregateMember.VariationItem variation => new DataClassForSave(variation.VariationAggregate, type).CsClassName,
                _ => throw new NotImplementedException(), // ありえないので例外
            };
        }
        /// <summary>
        /// <see cref="AggregateMember.AggregateMemberBase"/> のTypeScriptの型名を返します。
        /// </summary>
        internal static string GetMemberTypeNameTypeScript(AggregateMember.AggregateMemberBase member, E_Type type) {
            return member switch {
                AggregateMember.ValueMember vm => vm.Options.MemberType.GetTypeScriptTypeName(),
                AggregateMember.Ref @ref => new ForRef.DataClassForSaveRefTarget(@ref.RefTo).TsTypeName,
                AggregateMember.Parent => throw new NotImplementedException(), // Parentはこのデータクラスのメンバーにならない
                AggregateMember.Children children => $"{new DataClassForSave(children.ChildrenAggregate, type).TsTypeName}[]",
                AggregateMember.Child child => new DataClassForSave(child.ChildAggregate, type).TsTypeName,
                AggregateMember.VariationItem variation => new DataClassForSave(variation.VariationAggregate, type).TsTypeName,
                _ => throw new NotImplementedException(), // ありえないので例外
            };
        }

        internal DataClassForSave(GraphNode<Aggregate> agg, E_Type type) {
            _aggregate = agg;
            _type = type;
        }
        private readonly GraphNode<Aggregate> _aggregate;
        private readonly E_Type _type;

        /// <summary>
        /// 自身のプロパティとして定義されるメンバーを列挙します。
        /// </summary>
        private IEnumerable<AggregateMember.AggregateMemberBase> GetOwnMembers() {
            return _aggregate
                .GetMembers()
                .Where(m => m.DeclaringAggregate == _aggregate);
        }


        #region 値
        /// <summary>
        /// C#クラス名
        /// </summary>
        internal string CsClassName => _type == E_Type.Create
            ? $"{_aggregate.Item.PhysicalName}CreateCommand"
            : $"{_aggregate.Item.PhysicalName}SaveCommand";
        /// <summary>
        /// TypeScript型名
        /// </summary>
        internal string TsTypeName => _type == E_Type.Create
            ? $"{_aggregate.Item.PhysicalName}CreateCommand"
            : $"{_aggregate.Item.PhysicalName}SaveCommand";

        /// <summary>
        /// 追加・更新・削除のいずれかを表す区分のプロパティ名（C#側）。
        /// </summary>
        internal const string UPDATE_TYPE_CS = "_UpdateType";
        /// <summary>
        /// 追加・更新・削除のいずれかを表す区分プロパティ名（TypeScript側）。
        /// </summary>
        internal const string UPDATE_TYPE_TS = "_updateType";

        /// <summary>
        /// 楽観排他制御用のバージョニング情報をもつプロパティの名前（C#側）
        /// </summary>
        internal const string VERSION_CS = "_Version";
        /// <summary>
        /// 楽観排他制御用のバージョニング情報をもつプロパティの名前（TypeScript側）
        /// </summary>
        internal const string VERSION_TS = "_version";

        /// <summary>
        /// データ構造を定義します（C#）
        /// </summary>
        internal string RenderCSharp(CodeRenderingContext context) {
            // TODO #35 null許容に関して、型を堅牢にすべき
            return $$"""
                public partial class {{CsClassName}} {
                {{If(_aggregate.IsRoot(), () => $$"""
                    /// <summary>追加・更新・削除のいずれかを表す区分</summary>
                    [JsonPropertyName("{{UPDATE_TYPE_TS}}")]
                    public required {{ADD_MOD_DEL_CS}} {{UPDATE_TYPE_CS}} { get; init; }
                """)}}

                {{GetOwnMembers().SelectTextTemplate(m => $$"""
                    public {{GetMemberTypeNameCSharp(m, _type)}}? {{m.MemberName}} { get; set; }
                """)}}

                {{If(_type == E_Type.UpdateOrDelete && _aggregate.IsRoot(), () => $$"""
                    /// <summary>楽観排他制御用のバージョニング情報</summary>
                    [JsonPropertyName("{{VERSION_TS}}")]
                    public required int {{VERSION_CS}} { get; set; }
                """)}}

                    {{WithIndent(RenderToDbEntity(), "    ")}}
                }
                """;
        }
        /// <summary>
        /// データ構造を定義します（TypeScript）
        /// </summary>
        internal string RenderTypeScript(CodeRenderingContext context) {
            // TODO #35 optionalに関して、型を堅牢にすべき
            return $$"""
                export type {{TsTypeName}} = {
                {{If(_aggregate.IsRoot(), () => $$"""
                  /** 追加・更新・削除のいずれかを表す区分 */
                  {{UPDATE_TYPE_TS}}: {{ADD_MOD_DEL_TS}}
                """)}}
                {{GetOwnMembers().SelectTextTemplate(m => $$"""
                  {{m.MemberName}}?: {{GetMemberTypeNameTypeScript(m, _type)}}
                """)}}
                {{If(_type == E_Type.UpdateOrDelete && _aggregate.IsRoot(), () => $$"""
                  /** 楽観排他制御用のバージョニング情報 */
                  {{VERSION_TS}}: number
                """)}}
                }
                """;
        }

        /// <summary>
        /// このクラスのオブジェクトを <see cref="EFCoreEntity"/> 型に変換するメソッドの名前
        /// </summary>
        internal const string TO_DBENTITY = "ToDbEntity";

        /// <summary>
        /// このクラスの項目をEFCoreEntityにマッピングする処理をレンダリングします。
        /// </summary>
        private string RenderToDbEntity() {
            // - 子孫要素を参照するデータを引数の配列中から探すためにはキーで引き当てる必要があるが、
            //   子孫要素のラムダ式の中ではその外にある変数を参照するしかない
            // - 複数経路の参照があるケースを想定してGraphPathもキーに加えている
            var pkVarNames = new Dictionary<(AggregateMember.ValueMember, GraphPath), string>();

            IEnumerable<string> RenderBodyOfToDbEntity(GraphNode<Aggregate> agg, GraphNode<Aggregate> instanceAgg, string instanceName) {

                var keys = agg.GetKeys().OfType<AggregateMember.ValueMember>();
                foreach (var key in keys) {
                    var path = key.DeclaringAggregate.PathFromEntry();
                    if (!pkVarNames.ContainsKey((key.Declared, path)))
                        pkVarNames.Add((key.Declared, path), $"{instanceName}.{key.Declared.GetFullPath(since: instanceAgg).Join("?.")}");
                }

                foreach (var member in agg.GetMembers()) {
                    if (member is AggregateMember.ValueMember vm) {
                        var path = vm.DeclaringAggregate.PathFromEntry();
                        var value = pkVarNames.TryGetValue((vm.Declared, path), out var ancestorInstanceValue)
                            ? ancestorInstanceValue
                            : $"{instanceName}.{vm.Declared.GetFullPath(since: instanceAgg).Join("?.")}";

                        yield return $$"""
                            {{vm.MemberName}} = {{value}},
                            """;

                    } else if (member is AggregateMember.Children children) {
                        var nav = children.GetNavigationProperty();
                        var loopVar = $"item{children.ChildrenAggregate.EnumerateAncestors().Count()}";
                        var dbEntity = new EFCoreEntity(nav.Relevant.Owner);

                        yield return $$"""
                            {{children.MemberName}} = {{instanceName}}.{{member.GetFullPath(since: instanceAgg).Join("?.")}}?.Select({{loopVar}} => new {{dbEntity.ClassName}} {
                                {{WithIndent(RenderBodyOfToDbEntity(children.ChildrenAggregate, children.ChildrenAggregate, loopVar), "    ")}}
                            }).ToHashSet() ?? new HashSet<{{dbEntity.ClassName}}>(),
                            """;

                    } else if (member is AggregateMember.RelationMember childOrVariation
                        && (member is AggregateMember.Child || member is AggregateMember.VariationItem)) {
                        var nav = childOrVariation.GetNavigationProperty();
                        var dbEntity = new EFCoreEntity(nav.Relevant.Owner);

                        yield return $$"""
                            {{childOrVariation.MemberName}} = new {{dbEntity.ClassName}} {
                                {{WithIndent(RenderBodyOfToDbEntity(childOrVariation.MemberAggregate, instanceAgg, instanceName), "    ")}}
                            },
                            """;
                    }
                }
            }

            var returnType = new EFCoreEntity(_aggregate).ClassName;

            return $$"""
                /// <summary>
                /// {{_aggregate.Item.DisplayName}}のオブジェクトをデータベースに保存する形に変換します。
                /// </summary>
                public {{returnType}} {{TO_DBENTITY}}() {
                    return new {{returnType}} {
                        {{WithIndent(RenderBodyOfToDbEntity(_aggregate, _aggregate, "this"), "        ")}}
                    };
                }
                """;
        }
        #endregion 値


        #region エラーメッセージ用構造体
        /// <summary>
        /// エラーメッセージ用構造体 C#クラス名
        /// </summary>
        internal string ErrorDataCsClassName => _type == E_Type.Create
            ? $"{_aggregate.Item.PhysicalName}CreateCommandErrorData"
            : $"{_aggregate.Item.PhysicalName}SaveCommandErrorData";
        /// <summary>
        /// エラーメッセージ用構造体 TypeScript型名
        /// </summary>
        internal string ErrorDataTsTypeName => _type == E_Type.Create
            ? $"{_aggregate.Item.PhysicalName}CreateCommandErrorData"
            : $"{_aggregate.Item.PhysicalName}SaveCommandErrorData";

        /// <summary>
        /// データクラスのメンバーではなくデータクラス自身につくエラー
        /// </summary>
        internal const string OWN_ERRORS_CS = "_OwnErrors";
        /// <summary>
        /// データクラスのメンバーではなくデータクラス自身につくエラー
        /// </summary>
        internal const string OWN_ERRORS_TS = "_ownErrors";

        /// <summary>
        /// エラーメッセージ格納用の構造体を定義します（C#）
        /// </summary>
        internal string RenderCSharpErrorStructure(CodeRenderingContext context) {

            string Render(GraphNode<Aggregate> agg) {
                var dataClass = new DataClassForSave(agg, _type);
                var members = new List<string>();
                foreach (var m in dataClass.GetOwnMembers()) {
                    if (m is AggregateMember.ValueMember || m is AggregateMember.Ref) {
                        members.Add($"public List<string> {m.MemberName} {{ get; }} = new();");

                    } else if (m is AggregateMember.Children children) {
                        var descendant = new DataClassForSave(children.ChildrenAggregate, _type);
                        members.Add($"public List<{descendant.ErrorDataCsClassName}> {m.MemberName} {{ get; }} = new();");

                    } else if (m is AggregateMember.RelationMember rel) {
                        var descendant = new DataClassForSave(rel.MemberAggregate, _type);
                        members.Add($"public {descendant.ErrorDataCsClassName} {m.MemberName} {{ get; }} = new();");
                    }
                }
                return $$"""
                    /// <summary>
                    /// {{agg.Item.DisplayName}}のエラーメッセージ格納用クラス
                    /// </summary>
                    public sealed class {{dataClass.ErrorDataCsClassName}} {
                        [JsonPropertyName("{{OWN_ERRORS_TS}}")]
                        public List<string> {{OWN_ERRORS_CS}} { get; } = new();
                        {{WithIndent(members, "    ")}}
                    }
                    """;
            }

            return $$"""
                #region エラーメッセージ格納用クラス
                {{_aggregate.EnumerateThisAndDescendants().SelectTextTemplate(agg => $$"""
                {{Render(agg)}}
                """)}}
                #endregion エラーメッセージ格納用クラス
                """;
        }
        /// <summary>
        /// エラーメッセージ格納用の構造体を定義します（TypeScript）
        /// </summary>
        internal string RenderTypeScriptErrorStructure(CodeRenderingContext context) {

            string Render(GraphNode<Aggregate> agg) {
                var dataClass = new DataClassForSave(agg, _type);
                var members = new List<string>();
                foreach (var m in dataClass.GetOwnMembers()) {
                    if (m is AggregateMember.ValueMember || m is AggregateMember.Ref) {
                        members.Add($"{m.MemberName}?: string[]");

                    } else if (m is AggregateMember.Children children) {
                        var descendant = new DataClassForSave(children.ChildrenAggregate, _type);
                        members.Add($"{m.MemberName}?: {descendant.ErrorDataTsTypeName}[]");

                    } else if (m is AggregateMember.RelationMember rel) {
                        var descendant = new DataClassForSave(rel.MemberAggregate, _type);
                        members.Add($"{m.MemberName}?: {descendant.ErrorDataTsTypeName}");
                    }
                }
                return $$"""
                    /** {{agg.Item.DisplayName}}のエラーメッセージ格納用の型 */
                    export type {{dataClass.ErrorDataTsTypeName}} = {
                      {{OWN_ERRORS_TS}}?: string[]
                      {{WithIndent(members, "  ")}}
                    }
                    """;
            }

            return $$"""
                // --------------------------------
                // エラーメッセージ格納用の型
                {{_aggregate.EnumerateThisAndDescendants().SelectTextTemplate(agg => $$"""

                {{Render(agg)}}
                """)}}

                """;
        }
        #endregion エラーメッセージ用構造体


        #region 読み取り専用用構造体
        /// <summary>
        /// エラーメッセージ用構造体 C#クラス名
        /// </summary>
        internal string ReadOnlyCsClassName => _type == E_Type.Create
            ? $"{_aggregate.Item.PhysicalName}CreateCommandReadOnlyData"
            : $"{_aggregate.Item.PhysicalName}SaveCommandReadOnlyData";
        /// <summary>
        /// エラーメッセージ用構造体 TypeScript型名
        /// </summary>
        internal string ReadOnlyTsTypeName => _type == E_Type.Create
            ? $"{_aggregate.Item.PhysicalName}CreateCommandReadOnlyData"
            : $"{_aggregate.Item.PhysicalName}SaveCommandReadOnlyData";

        /// <summary>
        /// データクラスのメンバーではなくデータクラス自身が読み取り専用か否か
        /// </summary>
        internal const string THIS_IS_READONLY_CS = "_ThisObjectIsReadOnly";
        /// <summary>
        /// データクラスのメンバーではなくデータクラス自身が読み取り専用か否か
        /// </summary>
        internal const string THIS_IS_READONLY_TS = "_thisObjectIsReadOnly";

        /// <summary>
        /// どの項目が読み取り専用かを表すための構造体を定義します（C#）
        /// </summary>
        internal string RenderCSharpReadOnlyStructure(CodeRenderingContext context) {

            string Render(GraphNode<Aggregate> agg) {
                var dataClass = new DataClassForSave(agg, _type);
                var members = new List<string>();
                foreach (var m in dataClass.GetOwnMembers()) {
                    if (m is AggregateMember.ValueMember || m is AggregateMember.Ref) {
                        members.Add($"public bool {m.MemberName} {{ get; set; }}");

                    } else if (m is AggregateMember.Children children) {
                        var descendant = new DataClassForSave(children.ChildrenAggregate, _type);
                        members.Add($"public List<{dataClass.ReadOnlyCsClassName}> {m.MemberName} {{ get; }} = new();");

                    } else if (m is AggregateMember.RelationMember rel) {
                        var descendant = new DataClassForSave(rel.MemberAggregate, _type);
                        members.Add($"public {dataClass.ReadOnlyCsClassName} {m.MemberName} {{ get; }} = new();");
                    }
                }
                return $$"""
                    /// <summary>
                    /// {{agg.Item.DisplayName}}のエラーメッセージ格納用クラス
                    /// </summary>
                    public sealed class {{dataClass.ReadOnlyCsClassName}} {
                        [JsonPropertyName("{{THIS_IS_READONLY_TS}}")]
                        public bool {{THIS_IS_READONLY_CS}} { get; set; }
                        {{WithIndent(members, "    ")}}
                    }
                    """;
            }

            return $$"""
                #region どの項目が読み取り専用か否かの情報を格納するクラス
                {{_aggregate.EnumerateThisAndDescendants().SelectTextTemplate(agg => $$"""
                {{Render(agg)}}
                """)}}
                #endregion どの項目が読み取り専用か否かの情報を格納するクラス
                """;
        }
        /// <summary>
        /// どの項目が読み取り専用かを表すための構造体を定義します（TypeScript）
        /// </summary>
        internal string RenderTypeScriptReadOnlyStructure(CodeRenderingContext context) {

            string Render(GraphNode<Aggregate> agg) {
                var dataClass = new DataClassForSave(agg, _type);
                var members = new List<string>();
                foreach (var m in dataClass.GetOwnMembers()) {
                    if (m is AggregateMember.ValueMember || m is AggregateMember.Ref) {
                        members.Add($"{m.MemberName}?: string[]");

                    } else if (m is AggregateMember.Children children) {
                        var descendant = new DataClassForSave(children.ChildrenAggregate, _type);
                        members.Add($"{m.MemberName}?: {descendant.ReadOnlyTsTypeName}[]");

                    } else if (m is AggregateMember.RelationMember rel) {
                        var descendant = new DataClassForSave(rel.MemberAggregate, _type);
                        members.Add($"{m.MemberName}?: {descendant.ReadOnlyTsTypeName}");
                    }
                }
                return $$"""
                    /** {{agg.Item.DisplayName}}の読み取り専用情報格納用の型 */
                    export type {{dataClass.ReadOnlyTsTypeName}} = {
                      {{THIS_IS_READONLY_TS}}?: string[]
                      {{WithIndent(members, "  ")}}
                    }
                    """;
            }

            return $$"""
                // --------------------------------
                // どの項目が読み取り専用か否かの情報を格納する型
                {{_aggregate.EnumerateThisAndDescendants().SelectTextTemplate(agg => $$"""

                {{Render(agg)}}
                """)}}

                """;
        }
        #endregion 読み取り専用用構造体
    }
}
