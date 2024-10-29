using Nijo.Core;
using Nijo.Models.WriteModel2Features;
using Nijo.Parts.Utility;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.RefTo {
    /// <summary>
    /// ほかの集約から参照されるときの画面表示用データクラス
    /// </summary>
    internal class RefDisplayData {
        /// <summary>
        /// ほかの集約から参照されるときの画面表示用データクラス
        /// </summary>
        /// <param name="agg">集約</param>
        /// <param name="refEntry">参照エントリー</param>
        internal RefDisplayData(GraphNode<Aggregate> agg, GraphNode<Aggregate> refEntry) {
            _aggregate = agg;
            _refEntry = refEntry;
        }

        /// <summary>
        /// 必ずしもルート集約とは限らない
        /// </summary>
        private readonly GraphNode<Aggregate> _aggregate;
        /// <summary>
        /// 参照エントリー
        /// </summary>
        internal readonly GraphNode<Aggregate> _refEntry;

        internal string CsClassName => _refEntry == _aggregate
            ? $"{_refEntry.Item.PhysicalName}RefTarget"
            : $"{_refEntry.Item.PhysicalName}RefTarget_{GetRelationHistory().Join("の")}";
        internal string TsTypeName => _refEntry == _aggregate
            ? $"{_refEntry.Item.PhysicalName}RefTarget"
            : $"{_refEntry.Item.PhysicalName}RefTarget_{GetRelationHistory().Join("の")}";
        private IEnumerable<string> GetRelationHistory() {
            foreach (var edge in _aggregate.PathFromEntry().Since(_refEntry)) {
                if (edge.IsParentChild() && edge.Source == edge.Terminal) {
                    yield return edge.Initial.As<Aggregate>().Item.PhysicalName;
                } else {
                    yield return edge.RelationName.ToCSharpSafe();
                }
            }
        }

        internal IEnumerable<AggregateMember.AggregateMemberBase> GetOwnMembers() {
            foreach (var member in _aggregate.GetMembers()) {
                if (member is AggregateMember.ValueMember vm
                    && vm.DeclaringAggregate != _aggregate) continue;

                if (member is not AggregateMember.Parent
                    && member.DeclaringAggregate != _aggregate) continue;

                // 例えば参照エントリーが子でこの集約が親のときにChildrenを列挙してしまうと無限ループするので回避する
                if (member is AggregateMember.RelationMember rm) {
                    var source = _aggregate.Source?.As<Aggregate>();
                    if (source == rm.Relation) continue;
                }

                yield return member;
            }
        }

        internal IEnumerable<RefDisplayDataDescendant> GetChildMembers() {
            foreach (var member in GetOwnMembers().OfType<AggregateMember.RelationMember>()) {
                yield return new RefDisplayDataDescendant(member, _refEntry);
            }
        }

        internal IEnumerable<AggregateMember.AggregateMemberBase> GetKeys() {
            return _aggregate.GetKeys();
        }

        /// <summary>
        /// データ構造を定義します（C#）
        /// </summary>
        internal string RenderCSharp(CodeRenderingContext context) {
            // この集約を参照エントリーとして生成されるクラスを再帰的に
            var asEntry = _aggregate.AsEntry();
            var refTargets = new List<RefDisplayData>();
            void CollectRecursively(RefDisplayData refTarget) {
                refTargets.Add(refTarget);
                foreach (var child in refTarget.GetChildMembers()) {
                    CollectRecursively(child);
                }
            }
            CollectRecursively(new RefDisplayData(asEntry, asEntry));

            return refTargets.SelectTextTemplate(rt => $$"""
                /// <summary>{{rt._refEntry.Item.DisplayName}}が他の集約から参照されたときの{{rt._aggregate.Item.DisplayName}}の画面表示用データ型</summary>
                public partial class {{rt.CsClassName}} {
                {{rt.GetOwnMembers().SelectTextTemplate(m => $$"""
                    public virtual {{GetCSharpMemberType(m)}}? {{GetMemberName(m)}} { get; set; }
                """)}}
                }
                """);
        }
        /// <summary>
        /// データ構造を定義します（TypeScript）
        /// </summary>
        internal string RenderTypeScript(CodeRenderingContext context) {
            // この集約を参照エントリーとして生成されるクラスを再帰的に
            var asEntry = _aggregate.AsEntry();
            var refTargets = new List<RefDisplayData>();
            void CollectRecursively(RefDisplayData refTarget) {
                refTargets.Add(refTarget);
                foreach (var child in refTarget.GetChildMembers()) {
                    CollectRecursively(child);
                }
            }
            CollectRecursively(new RefDisplayData(asEntry, asEntry));

            return refTargets.SelectTextTemplate(rt => $$"""
                /** {{rt._refEntry.Item.DisplayName}}が他の集約から参照されたときの{{rt._aggregate.Item.DisplayName}}の画面表示用データ型 */
                export type {{rt.TsTypeName}} = {
                {{rt.GetOwnMembers().SelectTextTemplate(m => $$"""
                  {{GetMemberName(m)}}: {{GetTypeScriptMemberType(m)}}
                """)}}
                }
                """);
        }


        #region ほかのモデルとの変換
        /// <summary>
        /// WriteModelのDBエンティティから参照先検索結果への変換
        /// </summary>
        /// <param name="instance">DBエンティティのインスタンス名</param>
        /// <returns></returns>
        internal string RenderConvertFromWriteModelDbEntity(string instance) {
            // - 子孫要素を参照するデータを引数の配列中から探すためにはキーで引き当てる必要があるが、
            //   子孫要素のラムダ式の中ではその外にある変数を参照するしかない
            // - 複数経路の参照があるケースを想定してGraphPathもキーに加えている
            var pkVarNames = new Dictionary<(AggregateMember.ValueMember, GraphPath), string>();

            string RenderRecursively(GraphNode<Aggregate> renderingAggregate, GraphNode<Aggregate> dbEntityAggregate, string dbEntityInstance, bool renderNewStatement) {
                // 主キー辞書
                var keys = renderingAggregate.GetKeys().OfType<AggregateMember.ValueMember>();
                foreach (var key in keys) {
                    var path = key.DeclaringAggregate.PathFromEntry();
                    if (!pkVarNames.ContainsKey((key.Declared, path)))
                        pkVarNames.Add((key.Declared, path), $"{dbEntityInstance}.{key.Declared.GetFullPathAsDbEntity(dbEntityAggregate).Join("?.")}");
                }

                string RenderMemberStatement(AggregateMember.AggregateMemberBase member) {
                    if (member is AggregateMember.ValueMember vm) {
                        return $$"""
                            {{dbEntityInstance}}.{{vm.Declared.GetFullPathAsDbEntity(dbEntityAggregate).Join("?.")}}
                            """;

                    } else if (member is AggregateMember.Ref @ref) {
                        return RenderRefSearchResultRecursively(@ref.RefTo, dbEntityInstance, dbEntityAggregate, false);

                        string RenderRefSearchResultRecursively(GraphNode<Aggregate> renderingAgg, string instance, GraphNode<Aggregate> instanceAgg, bool renderNewClassName) {
                            var rsr = new RefDisplayData(renderingAgg, _refEntry);
                            var @new = renderNewClassName
                                ? $"new {rsr.CsClassName}"
                                : $"new()";
                            return $$"""
                                {{@new}} {
                                {{rsr.GetOwnMembers().SelectTextTemplate(m => $$"""
                                    {{GetMemberName(m)}} = {{WithIndent(RenderRefSearchResultMember(m), "    ")}},
                                """)}}
                                }
                                """;

                            string RenderRefSearchResultMember(AggregateMember.AggregateMemberBase m) {
                                if (m is AggregateMember.ValueMember vm3) {
                                    return $$"""
                                    {{instance}}.{{vm3.Declared.GetFullPathAsDbEntity(instanceAgg).Join("?.")}}
                                    """;

                                } else if (m is AggregateMember.Children children3) {
                                    var depth = children3.Owner.PathFromEntry().Count();
                                    var x = depth == 0 ? "x" : $"x{depth}";
                                    return $$"""
                                    {{instance}}.{{children3.GetFullPathAsDbEntity(instanceAgg).Join(".")}}.Select({{x}} => {{RenderRefSearchResultRecursively(children3.ChildrenAggregate, x, children3.ChildrenAggregate, true)}}).ToList()
                                    """;

                                } else if (m is AggregateMember.RelationMember rm) {
                                    return RenderRefSearchResultRecursively(rm.MemberAggregate, instance, instanceAgg, false);

                                } else {
                                    throw new NotImplementedException();
                                }
                            }
                        }

                    } else if (member is AggregateMember.Children children) {
                        var depth = children.Owner.PathFromEntry().Count();
                        var x = depth == 0 ? "x" : $"x{depth}";
                        return $$"""
                            {{dbEntityInstance}}.{{children.GetFullPathAsDbEntity(dbEntityAggregate).Join(".")}}.Select({{x}} => {{RenderRecursively(children.ChildrenAggregate, children.ChildrenAggregate, x, true)}}).ToList()
                            """;

                    } else if (member is AggregateMember.RelationMember rm) {
                        return RenderRecursively(rm.MemberAggregate, dbEntityAggregate, dbEntityInstance, false);

                    } else {
                        throw new NotImplementedException();
                    }
                }

                var rsr = new RefDisplayData(renderingAggregate, _refEntry);
                var newStatement = renderNewStatement ? $"new {rsr.CsClassName}()" : "new()";
                var pk = keys.Select(vm => pkVarNames[(vm.Declared, vm.DeclaringAggregate.PathFromEntry())]);
                return $$"""
                {{newStatement}} {
                {{rsr.GetOwnMembers().SelectTextTemplate(m => $$"""
                    {{GetMemberName(m)}} = {{WithIndent(RenderMemberStatement(m), "    ")}},
                """)}}
                }
                """;
            }
            return RenderRecursively(_aggregate, _aggregate, instance, true);
        }

        /// <summary>
        /// ReadModelの検索結果オブジェクトからRef表示用データへの変換
        /// </summary>
        internal string RenderConvertFromRefSearchResult(string instance, GraphNode<Aggregate> instanceAggregate, bool renderNewClassName) {
            var pkDict = new Dictionary<AggregateMember.ValueMember, string>();
            return RenderConvertFromRefSearchResultPrivate(instance, instanceAggregate, renderNewClassName, pkDict);
        }
        private string RenderConvertFromRefSearchResultPrivate(
            string instance,
            GraphNode<Aggregate> instanceAggregate,
            bool renderNewClassName,
            Dictionary<AggregateMember.ValueMember, string> pkDict) {

            // 主キー。レンダリング中の集約がChildrenの場合は親のキーをラムダ式の外の変数から参照する必要がある
            var keys = new List<string>();
            foreach (var key in _aggregate.GetKeys().OfType<AggregateMember.ValueMember>()) {
                if (!pkDict.TryGetValue(key.Declared, out var keyString)) {
                    keyString = $"{instance}.{key.Declared.GetFullPathAsRefSearchResult(since: instanceAggregate).Join("?.")}";
                    pkDict.Add(key.Declared, keyString);
                }
                keys.Add(keyString);
            }

            var newStatement = renderNewClassName
                ? $"new {CsClassName}()"
                : $"new()";
            return $$"""
                {{newStatement}} {
                {{GetOwnMembers().SelectTextTemplate(m => $$"""
                    {{GetMemberName(m)}} = {{WithIndent(RenderMember(m), "    ")}},
                """)}}
                }
                """;

            string RenderMember(AggregateMember.AggregateMemberBase member) {
                if (member is AggregateMember.ValueMember vm) {
                    return $$"""
                        {{instance}}.{{vm.Declared.GetFullPathAsRefSearchResult(instanceAggregate).Join("?.")}}
                        """;
                } else if (member is AggregateMember.Children children) {
                    var rdd = new RefDisplayData(children.ChildrenAggregate, _refEntry);
                    var pathToArray = children.GetFullPathAsRefSearchResult(since: instanceAggregate);
                    var depth = member.Owner.PathFromEntry().Count();
                    var x = depth == 0 ? "x" : $"x{depth}";
                    return $$"""
                        {{instance}}.{{pathToArray.Join("?.")}}.Select({{x}} => {{rdd.RenderConvertFromRefSearchResultPrivate(x, children.ChildrenAggregate, true, pkDict)}}).ToList() ?? []
                        """;

                } else if (member is AggregateMember.RelationMember rel) {
                    var rdd = new RefDisplayData(rel.MemberAggregate, _refEntry);
                    return $$"""
                        {{rdd.RenderConvertFromRefSearchResultPrivate(instance, instanceAggregate, false, pkDict)}}
                        """;

                } else {
                    throw new NotImplementedException();
                }
            }

        }

        /// <summary>
        /// TypeScriptで <see cref="DataClassForRefTargetKeys"/> への変換処理をレンダリングします。
        /// ダミーデータ生成処理で使用。
        /// </summary>
        internal string RenderConvertToTsWriteModelKey(string instance, GraphNode<Aggregate> instanceAggregate) {
            return Render(new DataClassForRefTargetKeys(_aggregate, _refEntry));

            string Render(DataClassForRefTargetKeys writeModel) {
                return $$"""
                    {
                    {{writeModel.GetValueMembers().SelectTextTemplate(m => $$"""
                      {{m.MemberName}}: {{instance}}.{{m.Member.Declared.GetFullPathAsDataClassForRefTarget(since: instanceAggregate).Join("?.")}},
                    """)}}
                    {{writeModel.GetRelationMembers().SelectTextTemplate(m => $$"""
                      {{m.MemberName}}: {{WithIndent(Render(m), "  ")}},
                    """)}}
                    }
                    """;
            }
        }
        #endregion ほかのモデルとの変換


        #region TypeScript側オブジェクト新規作成関数
        internal string TsNewObjectFunction => $"createNew{TsTypeName}";
        internal string RenderTsNewObjectFunction(CodeRenderingContext ctx) {
            return $$"""
                /* {{TsTypeName}} の新しいインスタンスを作成して返します。 */
                export const {{TsNewObjectFunction}} = (): {{TsTypeName}} => ({{RenderAggregate(this)}})
                """;

            string RenderAggregate(RefDisplayData refDisplayData) {
                return $$"""
                    {
                    {{refDisplayData.GetOwnMembers().SelectTextTemplate(m => $$"""
                      {{GetMemberName(m)}}: {{WithIndent(RenderMember(m), "  ")}},
                    """)}}
                    }
                    """;
            }

            string RenderMember(AggregateMember.AggregateMemberBase member) {
                if (member is AggregateMember.ValueMember) {
                    return "undefined";

                } else if (member is AggregateMember.Children) {
                    return "[]";

                } else if (member is AggregateMember.RelationMember rm) {
                    return RenderAggregate(new RefDisplayDataDescendant(rm, _refEntry));

                } else {
                    throw new NotImplementedException();
                }
            }
        }
        #endregion TypeScript側オブジェクト新規作成関数


        #region メンバー用staticメソッド
        internal const string PARENT = "PARENT";
        /// <summary>
        /// メンバー名
        /// </summary>
        internal static string GetMemberName(AggregateMember.AggregateMemberBase member) {
            if (member is AggregateMember.Parent) {
                return PARENT;
            } else {
                return member.MemberName;
            }
        }
        /// <summary>
        /// メンバーのC#型名
        /// </summary>
        private string GetCSharpMemberType(AggregateMember.AggregateMemberBase member) {
            if (member is AggregateMember.ValueMember vm) {
                return vm.Options.MemberType.GetCSharpTypeName();

            } else if (member is AggregateMember.Children children) {
                var refTo = new RefDisplayData(children.ChildrenAggregate, _refEntry);
                return $"List<{refTo.CsClassName}>";

            } else if (member is AggregateMember.RelationMember rel) {
                var refTo = new RefDisplayData(rel.MemberAggregate, _refEntry);
                return refTo.CsClassName;

            } else {
                throw new NotImplementedException();
            }
        }
        /// <summary>
        /// メンバーのTypeScript型名
        /// </summary>
        private string GetTypeScriptMemberType(AggregateMember.AggregateMemberBase member) {
            if (member is AggregateMember.ValueMember vm) {
                return $"{vm.Options.MemberType.GetTypeScriptTypeName()} | undefined";

            } else if (member is AggregateMember.Children children) {
                var refTo = new RefDisplayData(children.ChildrenAggregate, _refEntry);
                return $"{refTo.CsClassName}[]";

            } else if (member is AggregateMember.RelationMember rel) {
                var refTo = new RefDisplayData(rel.MemberAggregate, _refEntry);
                return refTo.CsClassName;

            } else {
                throw new NotImplementedException();
            }
        }
        #endregion メンバー用staticメソッド
    }

    internal class RefDisplayDataDescendant : RefDisplayData {
        internal RefDisplayDataDescendant(AggregateMember.RelationMember rel, GraphNode<Aggregate> refEntry) : base(rel.MemberAggregate, refEntry) {
            MemberInfo = rel;
        }

        internal AggregateMember.RelationMember MemberInfo { get; }
    }

    internal static partial class GetFullPathExtensions {

        /// <summary>
        /// エントリーからのパスを <see cref="RefDisplayData"/> の インスタンスの型のルールにあわせて返す。
        /// エントリーから全体が参照先検索結果クラスである場合のみ使用。
        /// 参照元検索結果の一部として参照先が含まれる場合は <see cref="ReadModel2Features.GetFullPathExtensions.GetFullPathAsDataClassForDisplay"/> を使用すること
        /// </summary>
        internal static IEnumerable<string> GetFullPathAsDataClassForRefTarget(this GraphNode<Aggregate> aggregate, GraphNode<Aggregate>? since = null, GraphNode<Aggregate>? until = null) {
            var path = aggregate.PathFromEntry();
            if (since != null) path = path.Since(since);
            if (until != null) path = path.Until(until);
            foreach (var edge in path) {
                if (edge.IsParentChild() && edge.Source == edge.Terminal) {
                    yield return RefDisplayData.PARENT;
                } else {
                    yield return edge.RelationName;
                }
            }
        }

        /// <inheritdoc cref="GetFullPathAsDataClassForRefTarget(GraphNode{Aggregate}, GraphNode{Aggregate}?, GraphNode{Aggregate}?)"/>
        internal static IEnumerable<string> GetFullPathAsDataClassForRefTarget(this AggregateMember.AggregateMemberBase member, GraphNode<Aggregate>? since = null, GraphNode<Aggregate>? until = null) {
            var fullpath = member.Owner
                .GetFullPathAsDataClassForRefTarget(since, until)
                .ToArray();
            foreach (var path in fullpath) {
                yield return path;
            }
            yield return RefDisplayData.GetMemberName(member);
        }
    }
}
