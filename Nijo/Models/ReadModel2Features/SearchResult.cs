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
        internal bool HasVersion => new DataClassForDisplay(Aggregate).HasVersion;

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
                {{If(HasVersion, () => $$"""
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
                if (edge.IsRef()) {
                    // ありえないパターンだが念のため
                    if (edge.Source == edge.Terminal) {
                        yield return "/* エラー！参照先から参照元へ辿る経路 */";
                        yield break;
                    }

                    yield return edge.RelationName;

                    // 参照先の経路の列挙
                    var refEntry = edge.Terminal.As<Aggregate>();
                    foreach (var refPath in aggregate.GetFullPathAsRefSearchResult(since: refEntry)) {
                        yield return refPath;
                    }
                    yield break;

                } else if (edge.IsParentChild() && edge.Source == edge.Terminal) {
                    yield return "/* エラー！参照先でないのに子から親へ辿る経路はありえない */";
                } else {
                    yield return edge.RelationName;
                }
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

        /* ↑ ↓ ここフルパスとして取得されるものの仕様が大きく違うので注意（もしかしたら下だけで十分で上は不要かもしれない……） */

        /// <summary>
        /// フルパスの途中で配列が出てきた場合はSelectやmapをかける
        /// </summary>
        internal static IEnumerable<string> GetFullPathAsSearchResult(this AggregateMember.AggregateMemberBase member, E_CsTs csts, out bool isArray, GraphNode<Aggregate>? since = null, GraphNode<Aggregate>? until = null) {
            var path = member.Owner.PathFromEntry();
            if (since != null) path = path.Since(since);
            if (until != null) path = path.Until(until);

            isArray = false;
            var edges = path.ToArray();
            var result = new List<string>();
            for (int i = 0; i < edges.Length; i++) {
                var edge = edges[i];

                var relationName = edge.IsParentChild() && edge.Source == edge.Terminal
                    ? RefTo.RefDisplayData.PARENT
                    : edge.RelationName;

                var isMany = false;
                if (edge.IsParentChild()
                    && edge.Source == edge.Initial
                    && edge.Terminal.As<Aggregate>().IsChildrenMember()) {
                    isMany = true;
                }

                if (isMany) {
                    result.Add(isArray
                        ? (csts == E_CsTs.CSharp
                            ? $"SelectMany(x => x.{relationName})"
                            : $"flatMap(x => x.{relationName})")
                        : relationName);
                    isArray = true;

                } else {
                    result.Add(isArray
                        ? (csts == E_CsTs.CSharp
                            ? $"Select(x => x.{relationName})"
                            : $"map(x => x.{relationName})")
                        : relationName);
                }
            }

            result.Add(member.MemberName);
            return result;
        }
    }
}
