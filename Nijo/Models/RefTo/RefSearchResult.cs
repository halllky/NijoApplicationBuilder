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
    /// <see cref="RefSearchMethod"/> のアプリケーションサービスの検索メソッドの検索結果の型。
    /// DBへの問い合わせの結果に特化した形。
    /// 検索後に <see cref="RefDisplayData"/> に変換される。
    /// </summary>
    internal class RefSearchResult {

        /// <summary>
        /// <see cref="RefSearchMethod"/> のアプリケーションサービスの検索メソッドの検索結果の型。
        /// DBへの問い合わせの結果に特化した形。
        /// 検索後に <see cref="RefDisplayData"/> に変換される。
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
            ? $"{_refEntry.Item.PhysicalName}RefSearchResult"
            : $"{_refEntry.Item.PhysicalName}RefSearchResult_{GetRelationHistory().Join("の")}";
        internal string TsTypeName => _refEntry == _aggregate
            ? $"{_refEntry.Item.PhysicalName}RefSearchResult"
            : $"{_refEntry.Item.PhysicalName}RefSearchResult_{GetRelationHistory().Join("の")}";
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
            // 検索結果は画面表示用データにマッピングされるため、画面表示用データのメンバーに準拠する
            var displayData = new RefDisplayData(_aggregate, _refEntry);
            return displayData.GetOwnMembers();
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

            return refTargets.SelectTextTemplate(rt => $$"""
                /// <summary>{{rt._refEntry.Item.DisplayName}}が他の集約から参照されたときの{{rt._aggregate.Item.DisplayName}}の検索結果の型</summary>
                public partial class {{rt.CsClassName}} {
                {{rt.GetOwnMembers().SelectTextTemplate(m => $$"""
                    public virtual {{GetCSharpMemberType(m)}}? {{GetMemberName(m)}} { get; set; }
                """)}}
                }
                """);
        }

        /// <summary>
        /// WriteModelのDBエンティティから参照先検索結果への変換処理をレンダリングします。
        /// </summary>
        /// <param name="instance">DBエンティティのインスタンス名</param>
        internal string RenderConvertFromWriteModelDbEntity(string instance) {

            return RenderRecursively(_aggregate, _aggregate, instance, true);

            string RenderRecursively(GraphNode<Aggregate> renderingAggregate, GraphNode<Aggregate> dbEntityAggregate, string dbEntityInstance, bool renderNewStatement) {
                var dbEntityMembers = new EFCoreEntity(renderingAggregate)
                    .GetTableColumnMembers()
                    .Select(vm => vm.Declared)
                    .ToHashSet();
                var rsr = new RefSearchResult(renderingAggregate, _refEntry);
                var newStatement = renderNewStatement ? $"new {rsr.CsClassName}()" : "new()";
                return $$"""
                    {{newStatement}} {
                    {{rsr.GetOwnMembers().SelectTextTemplate(m => $$"""
                        {{GetMemberName(m)}} = {{WithIndent(RenderMemberStatement(m), "    ")}},
                    """)}}
                    }
                    """;

                string RenderMemberStatement(AggregateMember.AggregateMemberBase member) {
                    if (member is AggregateMember.ValueMember vm) {
                        return $$"""
                            {{dbEntityInstance}}.{{dbEntityMembers.Single(vm2 => vm2.Declared == vm.Declared).GetFullPathAsDbEntity(dbEntityAggregate).Join(".")}}
                            """;

                    } else if (member is AggregateMember.Ref @ref) {
                        return RenderRefSearchResultRecursively(@ref.RefTo);

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
            }
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
        #endregion メンバー用staticメソッド
    }
}
