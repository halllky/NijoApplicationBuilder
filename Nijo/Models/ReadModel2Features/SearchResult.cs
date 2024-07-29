using Castle.Components.DictionaryAdapter.Xml;
using Nijo.Core;
using Nijo.Models.RefTo;
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
    /// <see cref="LoadMethod"/> で使用される、一覧検索と詳細検索のクエリの結果型。
    /// データの形自体は <see cref="DataClassForDisplay"/> とほぼ同じだが、
    /// 画面表示の目的に特価したプロパティ（そのデータがDBから読み込まれたものかどうかのフラグなど）が無く、
    /// 純粋にSQLのクエリの結果の部分のみから構成される。
    /// </summary>
    internal class SearchResult {
        internal SearchResult(GraphNode<Aggregate> agg) {
            Aggregate = agg;
        }
        internal GraphNode<Aggregate> Aggregate { get; }

        internal string CsClassName => $"{Aggregate.Item.PhysicalName}SearchResult";
        internal bool HasLifeCycle => new DataClassForDisplay(Aggregate).HasLifeCycle;

        internal const string VERSION = "Version";

        internal IEnumerable<AggregateMember.AggregateMemberBase> GetOwnMembers() {
            // 検索結果はそのまま画面表示用データにマッピングされるので、画面表示用データのメンバーに準ずる。
            var forDisplay = new DataClassForDisplay(Aggregate);
            foreach (var member in forDisplay.GetOwnMembers()) {
                yield return member;
            }
            foreach (var member in forDisplay.GetChildMembers()) {
                yield return member.MemberInfo;
            }
        }

        /// <summary>
        /// <see cref="DataClassForDisplay.GetMemberCsType(AggregateMember.AggregateMemberBase)"/>
        /// とほぼ同じだがクエリ結果の型に特化
        /// </summary>
        private static string GetMemberCsType(AggregateMember.AggregateMemberBase member) {
            if (member is AggregateMember.ValueMember vm) {
                return vm.Options.MemberType.GetCSharpTypeName();

            } else if (member is AggregateMember.Parent) {
                throw new NotImplementedException(); // Parentのメンバーは定義されないので

            } else if (member is AggregateMember.Ref @ref) {
                var refTarget = new RefSearchResult(@ref.RefTo, @ref.RefTo);
                return refTarget.CsClassName;

            } else if (member is AggregateMember.Children children) {
                var dataClass = new SearchResult(children.ChildrenAggregate);
                return $"List<{dataClass.CsClassName}>";

            } else {
                var dataClass = new SearchResult(((AggregateMember.RelationMember)member).MemberAggregate);
                return dataClass.CsClassName;
            }
        }

        internal string RenderCSharpDeclaring(CodeRenderingContext context) {
            var appSrv = new ApplicationService();
            var loadMethod = new LoadMethod(Aggregate.GetRoot());

            return $$"""
                /// <summary>
                /// <see cref="{{appSrv.ClassName}}.{{loadMethod.AppSrvCreateQueryMethod}}"/> の戻り値の型。
                /// </summary>
                public partial class {{CsClassName}} {
                {{GetOwnMembers().SelectTextTemplate(member => $$"""
                    /// <summary>{{member.MemberName}}</summary>
                    public virtual {{GetMemberCsType(member)}}? {{member.MemberName}} { get; set; }
                """)}}
                {{If(HasLifeCycle, () => $$"""
                    /// <summary>楽観排他制御用のバージョニング用</summary>
                    public required int {{VERSION}} { get; set; }
                """)}}
                }
                """;

        }
    }

    partial class GetFullPathExtensions {

        internal static IEnumerable<string> GetFullPathAsSearchResult(this GraphNode<Aggregate> aggregate, GraphNode<Aggregate>? since = null, GraphNode<Aggregate>? until = null) {
            var path = aggregate.PathFromEntry();
            if (since != null) path = path.Since(since);
            if (until != null) path = path.Until(until);
            foreach (var edge in path) {
                yield return edge.RelationName;
            }
        }
        internal static IEnumerable<string> GetFullPathAsSearchResult(this AggregateMember.AggregateMemberBase member, GraphNode<Aggregate>? since = null, GraphNode<Aggregate>? until = null) {
            var fullpath = member.Owner
                .GetFullPathAsSearchResult(since, until)
                .ToArray();
            foreach (var path in fullpath) {
                yield return path;
            }
            yield return member.MemberName;
        }

        /// <summary>
        /// <see cref="GetFullPathAsSearchResult(AggregateMember.AggregateMemberBase, GraphNode{Aggregate}?, GraphNode{Aggregate}?)"/> と似ているが、
        /// 経路の途中に配列がある場合は .Select または .SelectMany が挟まる。
        /// </summary>
        internal static IEnumerable<string> GetHandlingStatementAsSearchResult(
            this AggregateMember.AggregateMemberBase member,
            string? linqMethodifArray = null,
            Func<string, string>? handlingStatement = null,
            GraphNode<Aggregate>? since = null,
            GraphNode<Aggregate>? until = null) {

            if (linqMethodifArray == null) linqMethodifArray = "Select";
            if (handlingStatement == null) handlingStatement = fullpath => fullpath;

            var path = member.Owner.PathFromEntry();
            if (since != null) path = path.Since(since);
            if (until != null) path = path.Until(until);

            var edges = path.ToArray();
            if (edges.Length == 0) {
                yield return handlingStatement(member.MemberName);
                yield break;
            }

            var isArray = false;
            for (int i = 0; i < edges.Length; i++) {
                var edge = edges[i];
                var isLast = i == edges.Length - 1;

                var relationName = edge.IsParentChild() && edge.Source == edge.Terminal
                    ? RefTo.RefSearchResult.PARENT
                    : edge.RelationName;

                var isMany = false;
                if (edge.IsParentChild()
                    && edge.Source == edge.Initial
                    && edge.Terminal.As<Aggregate>().IsChildrenMember()) {
                    isMany = true;

                } else if (edge.IsRef()
                    && edge.Source == edge.Terminal
                    && edge.Terminal.As<Aggregate>().IsSingleRefKeyOf(edge.Initial.As<Aggregate>())) {
                    isMany = true;
                }

                if (isMany) {
                    yield return isArray
                        ? $"SelectMany(x => x.{relationName})"
                        : relationName;
                    isArray = true;

                } else {
                    yield return isArray
                        ? $"Select(x => x.{relationName})"
                        : relationName;
                }

                if (isLast) {
                    yield return isArray
                        ? $"{linqMethodifArray}(x => {handlingStatement($"x.{member.MemberName}")})"
                        : handlingStatement(member.MemberName);
                }
            }
        }
    }
}
