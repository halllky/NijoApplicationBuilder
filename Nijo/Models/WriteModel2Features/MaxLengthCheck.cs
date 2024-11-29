using Nijo.Core;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.WriteModel2Features {
    /// <summary>
    /// 文字列最大長チェック処理
    /// </summary>
    internal class MaxLengthCheck {

        internal const string METHOD_NAME = "CheckMaxLength";

        /// <summary>
        /// 文字列最大長チェック処理をレンダリングします。
        /// 新規作成処理と更新処理で計2回出てくる
        /// </summary>
        /// <param name="rootAggregate">ルート集約</param>
        internal static string Render(GraphNode<Aggregate> rootAggregate, CodeRenderingContext ctx) {
            var rootDbEntity = new EFCoreEntity(rootAggregate);
            var dataClass = new DataClassForSave(rootAggregate, DataClassForSave.E_Type.UpdateOrDelete);

            return $$"""
                /// <summary>
                /// 文字列最大長チェック処理。違反する項目があった場合はその旨が第2引数のオブジェクト内に追記されます。
                /// </summary>
                public virtual void {{METHOD_NAME}}({{rootDbEntity.ClassName}} dbEntity, {{SaveContext.BEFORE_SAVE}}<{{dataClass.MessageDataCsInterfaceName}}> e) {
                    {{WithIndent(RenderAggregate(rootAggregate, "dbEntity", rootAggregate), "    ")}}
                }
                """;
        }

        private static IEnumerable<string> RenderAggregate(GraphNode<Aggregate> renderingAggregate, string instance, GraphNode<Aggregate> instanceAggregate) {
            foreach (var member in renderingAggregate.GetMembers()) {
                var memberDisplayName = member.DisplayName.Replace("\"", "\\\"");

                if (member is AggregateMember.Schalar schalar) {
                    if (schalar.DeclaringAggregate != renderingAggregate) continue; // 親や参照先の項目はParentやRefの分岐でチェックする
                    if (schalar.Options.MaxLength == null) continue;

                    var path = schalar.Declared.GetFullPathAsDbEntity(since: instanceAggregate);
                    var cast = schalar.Options.MemberType is Core.AggregateMemberTypes.ValueObjectMember
                        ? "(string?)"
                        : "";

                    yield return $$"""
                        if (!DotnetExtensions.IsStringWithinLimit({{cast}}{{instance}}.{{path.Join("?.")}}, {{schalar.Options.MaxLength}})) {
                            e.{{GetErrorMemberPath(member).Join(".")}}.AddError("{{schalar.Options.MaxLength}}文字以内で入力してください。");
                        }
                        """;

                } else if (member is AggregateMember.Parent) {
                    continue;

                } else if (member is AggregateMember.Ref) {
                    continue;

                } else if (member is AggregateMember.Child child) {
                    yield return $$"""

                        {{WithIndent(RenderAggregate(child.ChildAggregate, instance, instanceAggregate), "")}}
                        """;

                } else if (member is AggregateMember.VariationItem variationItem) {
                    yield return $$"""

                        {{WithIndent(RenderAggregate(variationItem.VariationAggregate, instance, instanceAggregate), "")}}
                        """;

                } else if (member is AggregateMember.Variation) {
                    continue;

                } else if (member is AggregateMember.Children children) {
                    var childrenPath = children.GetFullPathAsDbEntity(since: instanceAggregate);

                    var depth = renderingAggregate.EnumerateAncestors().Count();
                    var i = depth == 0 ? "i" : $"i{depth}";
                    var item = depth == 0 ? "item" : $"item{depth}";
                    yield return $$"""

                        for (var {{i}} = 0; {{i}} < {{instance}}.{{childrenPath.Join("?.")}}.Count; {{i}}++) {
                            var {{item}} = {{instance}}.{{childrenPath.Join("!.")}}.ElementAt({{i}});

                            {{WithIndent(RenderAggregate(children.ChildrenAggregate, item, children.ChildrenAggregate), "    ")}}
                        }
                        """;
                }
            }
        }

        /// <summary>
        /// エラーメッセージの該当プロパティのパスを返す。
        /// 配列インデックスの名前は i, i1, i2, ... で決め打ち。
        /// </summary>
        private static IEnumerable<string> GetErrorMemberPath(AggregateMember.AggregateMemberBase member) {
            /// 決め打ち。<see cref="SaveContext"/> のファイルを参照。
            yield return "Messages";

            foreach (var e in member.Owner.PathFromEntry()) {
                var edge = e.As<Aggregate>();

                if (!edge.IsParentChild()) throw new InvalidOperationException("この分岐にくることは無いはず");

                var child = edge.Terminal.AsChildRelationMember();
                if (child is AggregateMember.Children children) {
                    var depth = children.ChildrenAggregate.EnumerateAncestors().Count() - 1; // 深さはChildren自身ではなく親基準なのでマイナス1
                    var i = depth == 0 ? "i" : $"i{depth}";
                    yield return $"{child.MemberName}[{i}]";

                } else {
                    yield return child.MemberName;
                }
            }

            yield return member.MemberName;
        }
    }
}
