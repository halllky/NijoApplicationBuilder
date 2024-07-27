using Nijo.Core;
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
    internal class DataClassForRefTarget {
        /// <summary>
        /// ほかの集約から参照されるときのためのデータクラス
        /// </summary>
        /// <param name="agg">集約</param>
        /// <param name="refEntry">参照エントリー</param>
        internal DataClassForRefTarget(GraphNode<Aggregate> agg, GraphNode<Aggregate> refEntry) {
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
            ? $"{_aggregate.Item.PhysicalName}RefTarget"
            : $"{_aggregate.Item.PhysicalName}RefTargetVia{_refEntry.Item.PhysicalName}";
        internal string TsTypeName => _refEntry == _aggregate
            ? $"{_aggregate.Item.PhysicalName}RefTarget"
            : $"{_aggregate.Item.PhysicalName}RefTargetVia{_refEntry.Item.PhysicalName}";

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
            var refTargets = new List<DataClassForRefTarget>();
            void CollectRecursively(DataClassForRefTarget refTarget) {
                refTargets.Add(refTarget);
                foreach (var rel in refTarget.GetOwnMembers().OfType<AggregateMember.RelationMember>()) {
                    CollectRecursively(new DataClassForRefTarget(rel.MemberAggregate, asEntry));
                }
            }
            CollectRecursively(new DataClassForRefTarget(asEntry, asEntry));

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
            var refTargets = new List<DataClassForRefTarget>();
            void CollectRecursively(DataClassForRefTarget refTarget) {
                refTargets.Add(refTarget);
                foreach (var rel in refTarget.GetOwnMembers().OfType<AggregateMember.RelationMember>()) {
                    CollectRecursively(new DataClassForRefTarget(rel.MemberAggregate, asEntry));
                }
            }
            CollectRecursively(new DataClassForRefTarget(asEntry, asEntry));

            return refTargets.SelectTextTemplate(rt => $$"""
                /** {{rt._refEntry.Item.DisplayName}}が他の集約から参照されたときの{{rt._aggregate.Item.DisplayName}}のデータ型 */
                export type {{rt.TsTypeName}} = {
                {{rt.GetOwnMembers().SelectTextTemplate(m => $$"""
                  {{GetMemberName(m)}}?: {{GetTypeScriptMemberType(m)}}
                """)}}
                }
                """);
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
                var refTo = new DataClassForRefTarget(children.ChildrenAggregate, _refEntry);
                return $"List<{refTo.CsClassName}>";

            } else if (member is AggregateMember.RelationMember rel) {
                var refTo = new DataClassForRefTarget(rel.MemberAggregate, _refEntry);
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
                var refTo = new DataClassForRefTarget(children.ChildrenAggregate, _refEntry);
                return $"{refTo.CsClassName}[]";

            } else if (member is AggregateMember.RelationMember rel) {
                var refTo = new DataClassForRefTarget(rel.MemberAggregate, _refEntry);
                return refTo.CsClassName;

            } else {
                throw new NotImplementedException();
            }
        }
        #endregion メンバー用staticメソッド
    }

    internal static partial class GetFullPathExtensions {

        internal static IEnumerable<string> GetFullPathAsDataClassForRefTarget(this GraphNode<Aggregate> aggregate, GraphNode<Aggregate>? since = null, GraphNode<Aggregate>? until = null) {
            var path = aggregate.PathFromEntry();
            if (since != null) path = path.Since(since);
            if (until != null) path = path.Until(until);
            foreach (var edge in path) {
                if (edge.IsParentChild() && edge.Source == edge.Terminal) {
                    yield return DataClassForRefTarget.PARENT;
                } else {
                    yield return edge.RelationName;
                }
            }
        }
        internal static IEnumerable<string> GetFullPathAsDataClassForRefTarget(this AggregateMember.AggregateMemberBase member, GraphNode<Aggregate>? since = null, GraphNode<Aggregate>? until = null) {
            var fullpath = member.Owner
                .GetFullPathAsDataClassForRefTarget(since, until)
                .ToArray();
            foreach (var path in fullpath) {
                yield return path;
            }
            yield return DataClassForRefTarget.GetMemberName(member);
        }
    }
}
