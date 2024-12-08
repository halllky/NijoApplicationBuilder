using Nijo.Core;
using Nijo.Parts.WebServer;
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
        /// <see cref="AggregateMember.AggregateMemberBase"/> のC#の型名を返します。
        /// このメソッドの戻り値にnull許容演算子はつきません。
        /// </summary>
        internal static string GetMemberTypeNameCSharp(AggregateMember.AggregateMemberBase member, E_Type type) {
            return member switch {
                AggregateMember.ValueMember vm => vm.Options.MemberType.GetCSharpTypeName(),
                AggregateMember.Ref @ref => new DataClassForRefTargetKeys(@ref.RefTo, @ref.RefTo).CsClassName,
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
                AggregateMember.ValueMember vm => $"{vm.Options.MemberType.GetTypeScriptTypeName()} | undefined",
                AggregateMember.Ref @ref => $"{new DataClassForRefTargetKeys(@ref.RefTo, @ref.RefTo).TsTypeName} | undefined",
                AggregateMember.Parent => throw new NotImplementedException(), // Parentはこのデータクラスのメンバーにならない
                AggregateMember.Children children => $"{new DataClassForSave(children.ChildrenAggregate, type).TsTypeName}[]",
                AggregateMember.Child child => new DataClassForSave(child.ChildAggregate, type).TsTypeName,
                AggregateMember.VariationItem variation => new DataClassForSave(variation.VariationAggregate, type).TsTypeName,
                _ => throw new NotImplementedException(), // ありえないので例外
            };
        }

        internal DataClassForSave(GraphNode<Aggregate> agg, E_Type type) {
            _aggregate = agg;
            Type = type;
        }
        private readonly GraphNode<Aggregate> _aggregate;
        internal E_Type Type { get; }

        /// <summary>
        /// 自身のプロパティとして定義されるメンバーを列挙します。
        /// </summary>
        internal IEnumerable<AggregateMember.AggregateMemberBase> GetOwnMembers() {
            return _aggregate
                .GetMembers()
                .Where(m => m.DeclaringAggregate == _aggregate);
        }


        #region 値
        /// <summary>
        /// C#クラス名
        /// </summary>
        internal string CsClassName => Type == E_Type.Create
            ? $"{_aggregate.Item.PhysicalName}CreateCommand"
            : $"{_aggregate.Item.PhysicalName}SaveCommand";
        /// <summary>
        /// TypeScript型名
        /// </summary>
        internal string TsTypeName => Type == E_Type.Create
            ? $"{_aggregate.Item.PhysicalName}CreateCommand"
            : $"{_aggregate.Item.PhysicalName}SaveCommand";

        /// <summary>
        /// データ構造を定義します（C#）
        /// </summary>
        internal string RenderCSharp(CodeRenderingContext context) {
            return $$"""
                public partial class {{CsClassName}} {
                {{GetOwnMembers().SelectTextTemplate(m => $$"""
                    public {{GetMemberTypeNameCSharp(m, Type)}}? {{GetMemberName(m)}} { get; set; }
                """)}}
                {{If(_aggregate.IsRoot(), () => $$"""

                    {{WithIndent(RenderToDbEntity(), "    ")}}

                    {{WithIndent(RenderFromDbEntity(), "    ")}}
                """)}}
                }
                """;
        }
        /// <summary>
        /// データ構造を定義します（TypeScript）
        /// </summary>
        internal string RenderTypeScript(CodeRenderingContext context) {
            return $$"""
                export type {{TsTypeName}} = {
                {{GetOwnMembers().SelectTextTemplate(m => $$"""
                  {{GetMemberName(m)}}: {{GetMemberTypeNameTypeScript(m, Type)}}
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
                        pkVarNames.Add((key.Declared, path), $"{instanceName}.{key.Declared.GetFullPathAsForSave(since: instanceAgg).Join("?.")}");
                }

                foreach (var member in agg.GetMembers()) {
                    if (member is AggregateMember.ValueMember vm) {
                        var path = vm.DeclaringAggregate.PathFromEntry();
                        var value = pkVarNames.TryGetValue((vm.Declared, path), out var ancestorInstanceValue)
                            ? ancestorInstanceValue
                            : $"{instanceName}.{vm.Declared.GetFullPathAsForSave(since: instanceAgg).Join("?.")}";

                        yield return $$"""
                            {{vm.MemberName}} = {{value}},
                            """;

                    } else if (member is AggregateMember.Children children) {
                        var nav = children.GetNavigationProperty();
                        var loopVar = $"item{children.ChildrenAggregate.EnumerateAncestors().Count()}";
                        var dbEntity = new EFCoreEntity(nav.Relevant.Owner);

                        yield return $$"""
                            {{children.MemberName}} = {{instanceName}}.{{member.GetFullPathAsForSave(since: instanceAgg).Join("?.")}}?.Select({{loopVar}} => new {{dbEntity.ClassName}} {
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

        /// <summary>
        /// <see cref="EFCoreEntity"/> をこのクラスのオブジェクトに変換するメソッドの名前
        /// </summary>
        internal const string FROM_DBENTITY = "FromDbEntity";
        /// <summary>
        /// EFCoreEntityの項目をこのクラスにマッピングする処理をレンダリングします。
        /// </summary>
        private string RenderFromDbEntity() {

            var efCoreEntity = new EFCoreEntity(_aggregate);

            // - 子孫要素を参照するデータを引数の配列中から探すためにはキーで引き当てる必要があるが、
            //   子孫要素のラムダ式の中ではその外にある変数を参照するしかない
            // - 複数経路の参照があるケースを想定してGraphPathもキーに加えている
            var pkVarNames = new Dictionary<(AggregateMember.ValueMember, GraphPath), string>();

            return $$"""
                /// <summary>
                /// {{_aggregate.Item.DisplayName}}のオブジェクトをデータベースに保存する形に変換します。
                /// </summary>
                public static {{CsClassName}} {{FROM_DBENTITY}}({{efCoreEntity.ClassName}} dbEntity) {
                    return new {{CsClassName}} {
                        {{WithIndent(RenderBodyOfFromDbEntity(this, _aggregate, "dbEntity"), "        ")}}
                    };
                }
                """;

            IEnumerable<string> RenderBodyOfFromDbEntity(DataClassForSave writeModel, GraphNode<Aggregate> instanceAgg, string instanceName) {

                var keys = writeModel._aggregate.GetKeys().OfType<AggregateMember.ValueMember>();
                foreach (var key in keys) {
                    var path = key.DeclaringAggregate.PathFromEntry();
                    if (!pkVarNames.ContainsKey((key.Declared, path)))
                        pkVarNames.Add((key.Declared, path), $"{instanceName}.{key.Declared.GetFullPathAsDbEntity(since: instanceAgg).Join("?.")}");
                }

                foreach (var member in writeModel.GetOwnMembers()) {
                    if (member is AggregateMember.ValueMember vm) {
                        var path = vm.DeclaringAggregate.PathFromEntry();
                        var value = pkVarNames.TryGetValue((vm.Declared, path), out var ancestorInstanceValue)
                            ? ancestorInstanceValue
                            : $"{instanceName}.{vm.Declared.GetFullPathAsDbEntity(since: instanceAgg).Join("?.")}";

                        yield return $$"""
                            {{vm.MemberName}} = {{value}},
                            """;

                    } else if (member is AggregateMember.Children children) {
                        var nav = children.GetNavigationProperty();
                        var loopVar = $"item{children.ChildrenAggregate.EnumerateAncestors().Count()}";
                        var childrenWriteModel = new DataClassForSave(nav.Relevant.Owner, Type);

                        yield return $$"""
                            {{children.MemberName}} = {{instanceName}}.{{member.GetFullPathAsDbEntity(since: instanceAgg).Join("?.")}}?.Select({{loopVar}} => new {{childrenWriteModel.CsClassName}} {
                                {{WithIndent(RenderBodyOfFromDbEntity(childrenWriteModel, children.ChildrenAggregate, loopVar), "    ")}}
                            }).ToList() ?? new List<{{childrenWriteModel.CsClassName}}>(),
                            """;

                    } else if (member is AggregateMember.RelationMember childOrVariation
                        && (member is AggregateMember.Child || member is AggregateMember.VariationItem)) {
                        var nav = childOrVariation.GetNavigationProperty();
                        var childWriteModel = new DataClassForSave(nav.Relevant.Owner, Type);

                        yield return $$"""
                            {{childOrVariation.MemberName}} = new {{childWriteModel.CsClassName}} {
                                {{WithIndent(RenderBodyOfFromDbEntity(childWriteModel, instanceAgg, instanceName), "    ")}}
                            },
                            """;
                    }
                }
            }
        }
        #endregion 値


        #region 更新前イベント（エラー、警告、インフォメーションデータ構造体）
        /// <summary>
        /// メッセージ用構造体 C#インターフェース名
        /// </summary>
        internal string MessageDataCsInterfaceName => $"I{_aggregate.Item.PhysicalName}SaveCommandMessages";
        /// <summary>
        /// メッセージ用構造体 C#型名（ダミーデータ作成時ぐらいにしか使われないはず）
        /// </summary>
        internal string MessageDataCsClassName => $"{_aggregate.Item.PhysicalName}SaveCommandMessages";

        /// <summary>
        /// メッセージ構造体を定義します（C#）
        /// </summary>
        internal string RenderCSharpMessageStructure(CodeRenderingContext context) {
            var members = GetOwnMembers().Select(m => {
                if (m is AggregateMember.ValueMember || m is AggregateMember.Ref) {
                    return new { MemberInfo = m, m.MemberName, Type = DisplayMessageContainer.INTERFACE };
                } else if (m is AggregateMember.Children children) {
                    var descendant = new DataClassForSave(children.ChildrenAggregate, Type);
                    return new { MemberInfo = m, m.MemberName, Type = $"{DisplayMessageContainer.CONCRETE_CLASS_LIST}<{descendant.MessageDataCsInterfaceName}>" };
                } else {
                    var descendant = new DataClassForSave(((AggregateMember.RelationMember)m).MemberAggregate, Type);
                    return new { MemberInfo = m, m.MemberName, Type = descendant.MessageDataCsInterfaceName };
                }
            }).ToArray();

            string[] args = _aggregate.IsChildrenMember()
                ? ["IEnumerable<string> path", "int index"] // 子配列の場合は自身のインデックスを表す変数を親から受け取る必要がある
                : ["IEnumerable<string> path"];
            string[] @base = _aggregate.IsChildrenMember()
                ? ["[.. path, index.ToString()]"]
                : ["path"];

            return $$"""
                /// <summary>
                /// {{_aggregate.Item.DisplayName}}の更新処理中に発生したメッセージを画面表示するための入れ物
                /// </summary>
                public interface {{MessageDataCsInterfaceName}} : {{DisplayMessageContainer.INTERFACE}} {
                {{members.SelectTextTemplate(m => $$"""
                    {{m.Type}} {{m.MemberName}} { get; }
                """)}}
                }
                /// <summary>
                /// {{_aggregate.Item.DisplayName}}の更新処理中に発生したメッセージを画面表示するための入れ物の具象クラス
                /// </summary>
                public partial class {{MessageDataCsClassName}} : {{DisplayMessageContainer.ABSTRACT_CLASS}}, {{MessageDataCsInterfaceName}} {
                    public {{MessageDataCsClassName}}({{args.Join(", ")}}) : base({{@base.Join(", ")}}) {
                {{members.SelectTextTemplate(m => $$"""
                        {{WithIndent(RenderConstructor(m.MemberInfo), "        ")}}
                """)}}
                    }

                {{members.SelectTextTemplate(m => $$"""
                    public {{m.Type}} {{m.MemberName}} { get; }
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

            static string RenderConstructor(AggregateMember.AggregateMemberBase member) {
                if (member is AggregateMember.ValueMember || member is AggregateMember.Ref) {
                    return $$"""
                        {{member.MemberName}} = new {{DisplayMessageContainer.CONCRETE_CLASS}}([.. path, "{{member.MemberName}}"]);
                        """;

                } else if (member is AggregateMember.Children children) {
                    var desc = new DataClassForSave(children.ChildrenAggregate, E_Type.UpdateOrDelete);
                    return $$"""
                        {{member.MemberName}} = new {{DisplayMessageContainer.CONCRETE_CLASS_LIST}}<{{desc.MessageDataCsInterfaceName}}>([.. path, "{{member.MemberName}}"], i => {
                            return new {{desc.MessageDataCsClassName}}([.. path, "{{member.MemberName}}"], i);
                        });
                        """;

                } else if (member is AggregateMember.RelationMember rel) {
                    var desc = new DataClassForSave(rel.MemberAggregate, E_Type.UpdateOrDelete);
                    return $$"""
                        {{member.MemberName}} = new {{desc.MessageDataCsClassName}}([.. path, "{{member.MemberName}}"]);
                        """;

                } else {
                    throw new NotImplementedException();
                }
            }
        }
        #endregion 更新前イベント（エラー、警告、インフォメーションデータ構造体）


        #region 読み取り専用用構造体
        /// <summary>
        /// 読み取り専用用構造体 C#クラス名
        /// </summary>
        internal string ReadOnlyCsClassName => Type == E_Type.Create
            ? $"{_aggregate.Item.PhysicalName}CreateCommandReadOnlyData"
            : $"{_aggregate.Item.PhysicalName}SaveCommandReadOnlyData";
        /// <summary>
        /// 読み取り専用用構造体 TypeScript型名
        /// </summary>
        internal string ReadOnlyTsTypeName => Type == E_Type.Create
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
            var members = new List<string>();
            foreach (var m in GetOwnMembers()) {
                if (m is AggregateMember.ValueMember || m is AggregateMember.Ref) {
                    members.Add($"public bool {m.MemberName} {{ get; set; }}");

                } else if (m is AggregateMember.Children children) {
                    var descendant = new DataClassForSave(children.ChildrenAggregate, Type);
                    members.Add($"public List<{descendant.ReadOnlyCsClassName}> {m.MemberName} {{ get; }} = new();");

                } else if (m is AggregateMember.RelationMember rel) {
                    var descendant = new DataClassForSave(rel.MemberAggregate, Type);
                    members.Add($"public {descendant.ReadOnlyCsClassName} {m.MemberName} {{ get; }} = new();");
                }
            }
            return $$"""
                /// <summary>
                /// {{_aggregate.Item.DisplayName}}の読み取り専用用構造体用クラス
                /// </summary>
                public sealed class {{ReadOnlyCsClassName}} {
                    [JsonPropertyName("{{THIS_IS_READONLY_TS}}")]
                    public bool {{THIS_IS_READONLY_CS}} { get; set; }
                    {{WithIndent(members, "    ")}}
                }
                """;
        }
        /// <summary>
        /// どの項目が読み取り専用かを表すための構造体を定義します（TypeScript）
        /// </summary>
        internal string RenderTypeScriptReadOnlyStructure(CodeRenderingContext context) {
            var members = new List<string>();
            foreach (var m in GetOwnMembers()) {
                if (m is AggregateMember.ValueMember || m is AggregateMember.Ref) {
                    members.Add($"{m.MemberName}?: string[]");

                } else if (m is AggregateMember.Children children) {
                    var descendant = new DataClassForSave(children.ChildrenAggregate, Type);
                    members.Add($"{m.MemberName}?: {descendant.ReadOnlyTsTypeName}[]");

                } else if (m is AggregateMember.RelationMember rel) {
                    var descendant = new DataClassForSave(rel.MemberAggregate, Type);
                    members.Add($"{m.MemberName}?: {descendant.ReadOnlyTsTypeName}");
                }
            }
            return $$"""
                /** {{_aggregate.Item.DisplayName}}の読み取り専用情報格納用の型 */
                export type {{ReadOnlyTsTypeName}} = {
                  {{THIS_IS_READONLY_TS}}?: string[]
                  {{WithIndent(members, "  ")}}
                }
                """;
        }
        #endregion 読み取り専用用構造体


        internal string TsNewObjectFnName => Type == E_Type.Create
            ? $"createNew{_aggregate.Item.PhysicalName}CreateCommand"
            : $"createNew{_aggregate.Item.PhysicalName}SaveCommand";
        /// <summary>
        /// TypeScriptの新規オブジェクト作成関数をレンダリングします。
        /// </summary>
        internal string RenderTsNewObjectFunction(CodeRenderingContext context) {

            static string RenderObject(GraphNode<Aggregate> agg, E_Type type) {
                var forSave = new DataClassForSave(agg, type);
                return $$"""
                    {
                    {{forSave.GetOwnMembers().SelectTextTemplate(member => $$"""
                      {{member.MemberName}}: {{WithIndent(RenderMemberValue(member, type), "  ")}},
                    """)}}
                    }
                    """;
            }
            static string RenderMemberValue(AggregateMember.AggregateMemberBase member, E_Type type) {
                if (member is AggregateMember.ValueMember vm) {
                    return type == E_Type.Create && vm.Options.MemberType is Core.AggregateMemberTypes.Uuid
                        ? "UUID.generate()"
                        : "undefined";
                } else if (member is AggregateMember.Ref) {
                    return "undefined";
                } else if (member is AggregateMember.Children) {
                    return "[]";
                } else if (member is AggregateMember.Child child) {
                    return RenderObject(child.ChildAggregate, type);
                } else if (member is AggregateMember.VariationItem variationItem) {
                    return RenderObject(variationItem.VariationAggregate, type);
                } else if (member is AggregateMember.Variation variation) {
                    var first = variation.GetGroupItems().First();
                    return $"'{first.Key}'";
                } else {
                    throw new NotImplementedException();
                }
            }

            return $$"""
                /** {{_aggregate.Item.DisplayName}}の{{(Type == E_Type.Create ? "新規作成用" : "更新用")}}コマンドを作成します。 */
                export const {{TsNewObjectFnName}} = (): {{TsTypeName}} => ({{RenderObject(_aggregate, Type)}})
                """;
        }

        /// <summary>
        /// <see cref="DataClassForSave"/> と <see cref="DataClassForRefTargetKeys"/> のルールに合わせたメンバー名を返します。
        /// </summary>
        internal static string GetMemberName(AggregateMember.AggregateMemberBase member) {
            if (member is AggregateMember.Parent) {

                if (member.Owner.IsOutOfEntryTree()) {
                    return DataClassForRefTargetKeys.PARENT;
                } else {
                    return $"Parent/* エラー！{nameof(DataClassForSave)}では子は親への参照をもっていない */";
                }

            } else {
                return member.MemberName;
            }
        }
    }

    partial class GetFullPathExtensions {

        /// <summary>
        /// エントリーからのパスを
        /// <see cref="DataClassForSave"/> と
        /// <see cref="DataClassForRefTargetKeys"/> の
        /// インスタンスの型のルールにあわせて返す。
        /// </summary>
        internal static IEnumerable<string> GetFullPathAsForSave(this GraphNode<Aggregate> aggregate, GraphNode<Aggregate>? since = null, GraphNode<Aggregate>? until = null) {
            var path = aggregate.PathFromEntry();
            if (since != null) path = path.Since(since);
            if (until != null) path = path.Until(until);

            foreach (var edge in path) {
                if (edge.Source == edge.Terminal && edge.IsParentChild()) {
                    // 子から親へ向かう経路の場合
                    if (edge.Initial.As<Aggregate>().IsOutOfEntryTree()) {
                        yield return DataClassForRefTargetKeys.PARENT;
                    } else {
                        yield return $"/* エラー！{nameof(DataClassForSave)}では子は親の参照を持っていません */";
                    }
                } else {
                    yield return edge.RelationName;
                }
            }
        }

        /// <inheritdoc cref="GetFullPathAsForSave(GraphNode{Aggregate}, GraphNode{Aggregate}?, GraphNode{Aggregate}?)"/>
        internal static IEnumerable<string> GetFullPathAsForSave(this AggregateMember.AggregateMemberBase member, GraphNode<Aggregate>? since = null, GraphNode<Aggregate>? until = null) {
            foreach (var path in member.Owner.GetFullPathAsForSave(since, until)) {
                yield return path;
            }
            yield return member.MemberName;
        }
    }
}
