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
                AggregateMember.ValueMember vm => vm.Options.MemberType.GetTypeScriptTypeName(),
                AggregateMember.Ref @ref => new DataClassForRefTargetKeys(@ref.RefTo, @ref.RefTo).TsTypeName,
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
                    public {{GetMemberTypeNameCSharp(m, Type)}}? {{m.MemberName}} { get; set; }
                """)}}
                {{If(_aggregate.IsRoot(), () => $$"""

                    {{WithIndent(RenderToDbEntity(), "    ")}}
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
                  {{m.MemberName}}: {{GetMemberTypeNameTypeScript(m, Type)}} | undefined
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
        #endregion 値


        #region 更新前イベント（エラーデータ構造体）
        /// <summary>
        /// エラーメッセージ用構造体 C#型名
        /// </summary>
        internal string ErrorDataCsClassName => Type == E_Type.Create
            ? $"{_aggregate.Item.PhysicalName}CreateCommandErrorData"
            : $"{_aggregate.Item.PhysicalName}SaveCommandErrorData";

        /// <summary>
        /// エラーデータ構造体を定義します（C#）
        /// </summary>
        internal string RenderCSharpErrorStructure(CodeRenderingContext context) {
            var members = GetOwnMembers().Select(m => {
                if (m is AggregateMember.ValueMember || m is AggregateMember.Ref) {
                    return new { m.MemberName, Type = ErrorReceiver.RECEIVER };
                } else if (m is AggregateMember.Children children) {
                    var descendant = new DataClassForSave(children.ChildrenAggregate, Type);
                    return new { m.MemberName, Type = $"{ErrorReceiver.RECEIVER_LIST}<{descendant.ErrorDataCsClassName}>" };
                } else {
                    var descendant = new DataClassForSave(((AggregateMember.RelationMember)m).MemberAggregate, Type);
                    return new { m.MemberName, Type = descendant.ErrorDataCsClassName };
                }
            }).ToArray();

            return $$"""
                /// <summary>
                /// {{_aggregate.Item.DisplayName}}のエラーデータ
                /// </summary>
                public partial class {{ErrorDataCsClassName}} : {{ErrorReceiver.RECEIVER}} {
                {{members.SelectTextTemplate(m => $$"""
                    public {{m.Type}} {{m.MemberName}} { get; } = new();
                """)}}

                {{If(members.Length > 0, () => $$"""
                    protected override IEnumerable<{{ErrorReceiver.RECEIVER}}> EnumerateChildren() {
                {{members.SelectTextTemplate(m => $$"""
                        yield return {{m.MemberName}};
                """)}}
                    }
                """)}}

                    public override IEnumerable<JsonNode> ToJsonNodes(string? path) {
                        // このオブジェクト自身に対するエラー
                        foreach (var node in base.ToJsonNodes(path)) {
                            yield return node;
                        }

                        // 各メンバーに対するエラー
                        var p = path == null ? string.Empty : $"{path}.";
                {{members.SelectTextTemplate(m => $$"""
                        foreach (var node in {{m.MemberName}}.ToJsonNodes($"{p}{{m.MemberName}}")) yield return node;
                """)}}
                    }
                }
                """;
        }
        #endregion 更新前イベント（エラーデータ構造体）


        #region 更新後イベント
        /// <summary>
        /// 更新前イベント引数 C#クラス名
        /// </summary>
        internal string AfterSaveContextCsClassName => $"{_aggregate.Item.PhysicalName}AfterSaveEventArgs";
        /// <summary>
        /// 更新前イベント引数を定義します（C#）
        /// </summary>
        internal string RenderCSharpAfterSaveEventArgs(CodeRenderingContext context) {
            return $$"""
                /// <summary>
                /// {{_aggregate.Item.DisplayName}}の更新後イベント
                /// </summary>
                public partial class {{AfterSaveContextCsClassName}} {
                    // 特になし
                }
                """;
        }
        #endregion 更新後イベント


        #region 読み取り専用用構造体
        /// <summary>
        /// エラーメッセージ用構造体 C#クラス名
        /// </summary>
        internal string ReadOnlyCsClassName => Type == E_Type.Create
            ? $"{_aggregate.Item.PhysicalName}CreateCommandReadOnlyData"
            : $"{_aggregate.Item.PhysicalName}SaveCommandReadOnlyData";
        /// <summary>
        /// エラーメッセージ用構造体 TypeScript型名
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
                /// {{_aggregate.Item.DisplayName}}のエラーメッセージ格納用クラス
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
