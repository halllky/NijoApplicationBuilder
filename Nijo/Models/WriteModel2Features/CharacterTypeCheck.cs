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
    /// 文字列系項目の文字種チェック
    /// </summary>
    internal class CharacterTypeCheck {

        internal const string METHOD_NAME = "CheckCharacterType";

        /// <summary>
        /// 文字列系項目の文字種チェック。
        /// 新規作成処理と更新処理で計2回出てくる
        /// </summary>
        /// <param name="rootAggregate">ルート集約</param>
        internal static string Render(GraphNode<Aggregate> rootAggregate, CodeRenderingContext ctx) {
            var rootDbEntity = new EFCoreEntity(rootAggregate);
            var dataClass = new DataClassForSave(rootAggregate, DataClassForSave.E_Type.UpdateOrDelete);

            return $$"""
                /// <summary>
                /// 文字列系項目の文字種チェック。違反する項目があった場合はその旨が第2引数のオブジェクト内に追記されます。
                /// </summary>
                public virtual void {{METHOD_NAME}}({{rootDbEntity.ClassName}} dbEntity, {{SaveContext.BEFORE_SAVE}}<{{dataClass.MessageDataCsInterfaceName}}> e) {
                {{If(RenderAggregate(rootAggregate, "dbEntity", rootAggregate).Any(), () => $$"""
                    string? temp;

                """).Else(() => $$"""
                    // 該当項目なし
                """)}}
                    {{WithIndent(RenderAggregate(rootAggregate, "dbEntity", rootAggregate), "    ")}}
                }
                """;
        }

        private static IEnumerable<string> RenderAggregate(GraphNode<Aggregate> renderingAggregate, string instance, GraphNode<Aggregate> instanceAggregate) {
            foreach (var member in renderingAggregate.GetMembers()) {
                var memberDisplayName = member.DisplayName.Replace("\"", "\\\"");

                if (member is AggregateMember.Schalar schalar) {
                    if (schalar.DeclaringAggregate != renderingAggregate) continue; // 親や参照先の項目はParentやRefの分岐でチェックする
                    if (schalar.Options.CharacterType == null) continue;

                    var path = schalar.Declared.GetFullPathAsDbEntity(since: instanceAggregate);
                    var value = $"{instance}.{path.Join("?.")}";

                    var casted = schalar.Options.MemberType switch {
                        Core.AggregateMemberTypes.ValueObjectMember => $"((string?){value})?.Trim()",
                        Core.AggregateMemberTypes.Word => $"{value}?.Trim()",
                        _ => value,
                    };

                    if (schalar.Options.CharacterType == E_CharacterType.半角英数) {
                        yield return $$"""
                            temp = {{casted}};
                            if (!string.IsNullOrEmpty(temp) && !{{RegexCache.CLASS_NAME}}.{{RegexCache.ONLY_ALPHA_NUMERIC}}().IsMatch(temp)) {
                                e.{{GetErrorMemberPath(member).Join(".")}}.AddError("半角英数字のみ使用可能です。");
                            }
                            """;

                    } else {
                        throw new InvalidOperationException("未実装");
                    }

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
