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
    /// 必須入力チェック処理
    /// </summary>
    internal class RequiredCheck {

        internal const string METHOD_NAME = "CheckRequired";

        /// <summary>
        /// 必須チェック処理をレンダリングします。
        /// 新規作成処理と更新処理で計2回出てくる
        /// </summary>
        /// <param name="rootAggregate">ルート集約</param>
        internal static string Render(GraphNode<Aggregate> rootAggregate, CodeRenderingContext ctx) {
            var rootDbEntity = new EFCoreEntity(rootAggregate);
            var dataClass = new DataClassForSave(rootAggregate, DataClassForSave.E_Type.UpdateOrDelete);

            return $$"""
                /// <summary>
                /// 必須チェック処理。空の項目があった場合はその旨がは第2引数のオブジェクト内に追記されます。
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
                    if (schalar.DeclaringAggregate != renderingAggregate) continue; // 親や参照先のキーはParentやRefの分岐でチェックする
                    if (!schalar.IsKey && !schalar.IsRequired) continue;

                    // stringならIsNullOrWhiteSpaceで判定、それ以外はnullか否かで判定
                    var path = schalar.Declared.GetFullPathAsDbEntity(since: instanceAggregate);
                    var isEmpty = schalar.Options.MemberType.GetCSharpTypeName() == "string"
                        ? $"string.IsNullOrWhiteSpace({instance}.{path.Join("?.")})"
                        : $"{instance}.{path.Join("?.")} == null";

                    yield return $$"""
                        if ({{isEmpty}}) {
                            e.{{GetErrorMemberPath(member).Join(".")}}.AddError("{{memberDisplayName}}を入力してください。");
                        }
                        """;

                } else if (member is AggregateMember.Parent parent) {
                    // 親のキーは指定不要（親エンティティにある子へのナビゲーションプロパティが設定されているならばEFCoreが暗黙的に設定してくれる）
                    continue;

                } else if (member is AggregateMember.Ref @ref) {
                    if (!@ref.Relation.IsPrimary() && !@ref.Relation.IsRequired()) continue;

                    var isEmpty = new List<string>();
                    foreach (var fk in @ref.GetForeignKeys()) {
                        // stringならIsNullOrWhiteSpaceで判定、それ以外はnullか否かで判定
                        var path = fk.GetFullPathAsDbEntity(since: instanceAggregate);
                        isEmpty.Add(fk.Options.MemberType.GetCSharpTypeName() == "string"
                            ? $"string.IsNullOrWhiteSpace({instance}.{path.Join("?.")})"
                            : $"{instance}.{path.Join("?.")} == null");
                    }
                    yield return $$"""
                        if ({{WithIndent(isEmpty, "    || ")}}) {
                            e.{{GetErrorMemberPath(@ref).Join(".")}}.AddError("{{memberDisplayName}}を指定してください。");
                        }
                        """;

                } else if (member is AggregateMember.Child child) {
                    yield return $$"""

                        // {{child.DisplayName}} の各項目の必須チェック
                        {{WithIndent(RenderAggregate(child.ChildAggregate, instance, instanceAggregate), "")}}
                        """;

                } else if (member is AggregateMember.Variation variation) {
                    if (variation.DeclaringAggregate != renderingAggregate) continue; // 親や参照先のキーはParentやRefの分岐でチェックする

                    var switchPath = variation.GetFullPathAsDbEntity(since: instanceAggregate);

                    // バリエーションの種別は必須指定されなくても常に必須
                    yield return $$"""
                        if ({{instance}}.{{switchPath.Join("?.")}} == null) {
                            e.{{GetErrorMemberPath(variation).Join(".")}}.AddError("{{memberDisplayName}}が未指定です。");
                        }
                        """;

                    // バリエーションの子要素はその種別の場合のみチェック
                    foreach (var variationItem in variation.GetGroupItems()) {
                        yield return $$"""

                            // {{variation.DisplayName}} が '{{variationItem.Key}}:{{variationItem.DisplayName}}' の場合のみ必須チェック
                            if ({{instance}}.{{switchPath.Join("?.")}} == {{variation.CsEnumType}}.{{variationItem.Relation.RelationName}}) {
                                {{WithIndent(RenderAggregate(variationItem.VariationAggregate, instance, instanceAggregate), "    ")}}
                            }
                            """;
                    }

                } else if (member is AggregateMember.Children children) {
                    var childrenPath = children.GetFullPathAsDbEntity(since: instanceAggregate);

                    // 子配列自体に必須属性がついている場合、配列の要素が0件ならエラー
                    if (children.Relation.IsRequired()) {
                        yield return $$"""
                            if ({{instance}}.{{childrenPath.Join("?.")}} != null
                                && {{instance}}.{{childrenPath.Join(".")}}.Count == 0) {
                                e.{{GetErrorMemberPath(children).Join(".")}}.AddError("{{memberDisplayName}}には1件以上指定する必要があります。");
                            }
                            """;
                    }

                    var depth = renderingAggregate.EnumerateAncestors().Count();
                    var i = depth == 0 ? "i" : $"i{depth}";
                    var item = depth == 0 ? "item" : $"item{depth}";
                    yield return $$"""

                        // {{children.DisplayName}} の各項目の必須チェック
                        for (var {{i}} = 0; {{i}} < ({{instance}}.{{childrenPath.Join("?.")}}.Count ?? 0); {{i}}++) {
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
