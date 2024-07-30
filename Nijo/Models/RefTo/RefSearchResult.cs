using Nijo.Core;
using Nijo.Models.WriteModel2Features;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.RefTo {
    /// <summary>
    /// ほかの集約から参照されるときのためのデータクラス
    /// </summary>
    internal class RefSearchResult {
        /// <summary>
        /// ほかの集約から参照されるときのためのデータクラス
        /// </summary>
        /// <param name="agg">集約</param>
        /// <param name="refEntry">参照エントリー</param>
        internal RefSearchResult(GraphNode<Aggregate> agg, GraphNode<Aggregate> refEntry) {
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
        private readonly GraphNode<Aggregate> _refEntry;

        internal string CsClassName => _refEntry == _aggregate
            ? $"{_refEntry.Item.PhysicalName}RefTarget"
            : $"{_refEntry.Item.PhysicalName}RefTarget_{_aggregate.Item.PhysicalName}";
        internal string TsTypeName => _refEntry == _aggregate
            ? $"{_refEntry.Item.PhysicalName}RefTarget"
            : $"{_refEntry.Item.PhysicalName}RefTarget_{_aggregate.Item.PhysicalName}";

        private IEnumerable<AggregateMember.AggregateMemberBase> GetOwnMembers() {
            foreach (var member in _aggregate.GetMembers()) {
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

        /// <summary>
        /// データ構造を定義します（C#）
        /// </summary>
        internal string RenderCSharp(CodeRenderingContext context) {
            // この集約を参照エントリーとして生成されるクラスを再帰的に
            var asEntry = _aggregate.AsEntry();
            var refTargets = new List<RefSearchResult>();
            void CollectRecursively(RefSearchResult refTarget) {
                refTargets.Add(refTarget);
                foreach (var rel in refTarget.GetOwnMembers().OfType<AggregateMember.RelationMember>()) {
                    CollectRecursively(new RefSearchResult(rel.MemberAggregate, asEntry));
                }
            }
            CollectRecursively(new RefSearchResult(asEntry, asEntry));

            return refTargets.SelectTextTemplate(rt =>  $$"""
                /// <summary>{{rt._refEntry.Item.DisplayName}}が他の集約から参照されたときの{{rt._aggregate.Item.DisplayName}}のデータ型</summary>
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
            var refTargets = new List<RefSearchResult>();
            void CollectRecursively(RefSearchResult refTarget) {
                refTargets.Add(refTarget);
                foreach (var rel in refTarget.GetOwnMembers().OfType<AggregateMember.RelationMember>()) {
                    CollectRecursively(new RefSearchResult(rel.MemberAggregate, asEntry));
                }
            }
            CollectRecursively(new RefSearchResult(asEntry, asEntry));

            return refTargets.SelectTextTemplate(rt => $$"""
                /** {{rt._refEntry.Item.DisplayName}}が他の集約から参照されたときの{{rt._aggregate.Item.DisplayName}}のデータ型 */
                export type {{rt.TsTypeName}} = {
                {{rt.GetOwnMembers().SelectTextTemplate(m => $$"""
                  {{GetMemberName(m)}}?: {{GetTypeScriptMemberType(m)}}
                """)}}
                }
                """);
        }

        /// <summary>
        /// WriteModelのDBエンティティから参照先検索結果への変換
        /// </summary>
        /// <param name="instance">DBエンティティのインスタンス名</param>
        /// <returns></returns>
        internal string RenderConvertFromWriteModelDbEntity(string instance) {
            string RenderRecursively(GraphNode<Aggregate> renderingAggregate, GraphNode<Aggregate> dbEntityAggregate, string dbEntityInstance, bool renderNewStatement) {
                var dbEntityMembers = new EFCoreEntity(renderingAggregate)
                    .GetTableColumnMembers()
                    .Select(vm => vm.Declared)
                    .ToHashSet();

                string RenderMemberStatement(AggregateMember.AggregateMemberBase member) {
                    if (member is AggregateMember.ValueMember vm) {
                        return $$"""
                            {{dbEntityInstance}}.{{dbEntityMembers.Single(vm2 => vm2.Declared == vm.Declared).GetFullPathAsDbEntity(dbEntityAggregate).Join(".")}}
                            """;

                    } else if (member is AggregateMember.Ref @ref) {
                        string RenderRefSearchResultRecursively(GraphNode<Aggregate> agg) {
                            var rsr = new RefSearchResult(agg, _refEntry);
                            return $$"""
                                new() {
                                {{rsr.GetOwnMembers().SelectTextTemplate(m => $$"""
                                    {{GetMemberName(m)}} = {{WithIndent(RenderRefSearchResultMember(m), "    ")}},
                                """)}}
                                }
                                """;
                        }
                        string RenderRefSearchResultMember(AggregateMember.AggregateMemberBase m) {
                            if (m is AggregateMember.ValueMember vm3) {
                                return $$"""
                                    {{dbEntityInstance}}.{{dbEntityMembers.Single(vm4 => vm4.Declared == vm3.Declared).GetFullPathAsDbEntity(dbEntityAggregate).Join(".")}}
                                    """;

                            } else if (m is AggregateMember.Children children3) {
                                var depth = children3.Owner.PathFromEntry().Count();
                                var x = depth == 0 ? "x" : $"x{depth}";
                                return $$"""
                                    {{dbEntityInstance}}.{{children3.GetFullPathAsDbEntity(dbEntityAggregate).Join(".")}}.Select({{x}} => {{RenderRefSearchResultRecursively(children3.ChildrenAggregate)}}).ToList()
                                    """;

                            } else if (m is AggregateMember.RelationMember rm) {
                                return RenderRefSearchResultRecursively(rm.MemberAggregate);

                            } else {
                                throw new NotImplementedException();
                            }
                        }
                        return RenderRefSearchResultRecursively(@ref.RefTo);

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

                var rsr = new RefSearchResult(renderingAggregate, _refEntry);
                var newStatement = renderNewStatement ? $"new {rsr.CsClassName}()" : "new()";
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
                var refTo = new RefSearchResult(children.ChildrenAggregate, _refEntry);
                return $"List<{refTo.CsClassName}>";

            } else if (member is AggregateMember.RelationMember rel) {
                var refTo = new RefSearchResult(rel.MemberAggregate, _refEntry);
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
                return vm.Options.MemberType.GetTypeScriptTypeName();

            } else if (member is AggregateMember.Children children) {
                var refTo = new RefSearchResult(children.ChildrenAggregate, _refEntry);
                return $"{refTo.CsClassName}[]";

            } else if (member is AggregateMember.RelationMember rel) {
                var refTo = new RefSearchResult(rel.MemberAggregate, _refEntry);
                return refTo.CsClassName;

            } else {
                throw new NotImplementedException();
            }
        }
        #endregion メンバー用staticメソッド
    }

    internal static partial class GetFullPathExtensions {

        /// <summary>
        /// エントリーからのパスを <see cref="RefSearchCondition"/> の インスタンスの型のルールにあわせて返す。
        /// エントリーから全体が参照先検索結果クラスである場合のみ使用。
        /// 参照元検索結果の一部として参照先が含まれる場合は <see cref="ReadModel2Features.GetFullPathExtensions.GetFullPathAsDataClassForDisplay(GraphNode{Aggregate}, GraphNode{Aggregate}?, GraphNode{Aggregate}?)"/> を使用すること
        /// </summary>
        internal static IEnumerable<string> GetFullPathAsDataClassForRefTarget(this GraphNode<Aggregate> aggregate, GraphNode<Aggregate>? since = null, GraphNode<Aggregate>? until = null) {
            var path = aggregate.PathFromEntry();
            if (since != null) path = path.Since(since);
            if (until != null) path = path.Until(until);
            foreach (var edge in path) {
                if (edge.IsParentChild() && edge.Source == edge.Terminal) {
                    yield return RefSearchResult.PARENT;
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
            yield return RefSearchResult.GetMemberName(member);
        }
    }
}
