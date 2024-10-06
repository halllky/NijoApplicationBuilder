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

                if (member is AggregateMember.Parent) {
                    continue;

                } else if (member is AggregateMember.ValueMember vm) {
                    if (vm.DeclaringAggregate != renderingAggregate) continue; // 親のキーの子側でのチェックは不要。参照先のキーはRefの分岐でチェック。
                    if (!vm.IsKey && !vm.IsRequired) continue;

                    // stringならIsNullOrWhiteSpaceで判定、それ以外はnullか否かで判定
                    var path = vm.Declared.GetFullPathAsDbEntity(since: instanceAggregate);
                    var isEmpty = vm.Options.MemberType.GetCSharpTypeName() == "string"
                        ? $"string.IsNullOrWhiteSpace({instance}.{path.Join("?.")})"
                        : $"{instance}.{path.Join("?.")} == null";

                    yield return $$"""
                        if ({{isEmpty}}) {
                            e.{{GetErrorMemberPath(member).Join(".")}}.AddError("{{memberDisplayName}}を入力してください。");
                        }
                        """;

                } else if (member is AggregateMember.Ref @ref) {
                    if (!@ref.Relation.IsPrimary() && !@ref.Relation.IsRequired()) continue;

                    var isEmpty = new List<string>();
                    var refKeys = @ref.RefTo
                        .GetKeys()
                        .OfType<AggregateMember.ValueMember>();
                    foreach (var refKey in refKeys) {
                        // stringならIsNullOrWhiteSpaceで判定、それ以外はnullか否かで判定
                        var path = refKey.Declared.GetFullPathAsDbEntity(since: instanceAggregate);
                        isEmpty.Add(refKey.Options.MemberType.GetCSharpTypeName() == "string"
                            ? $"string.IsNullOrWhiteSpace({instance}.{path.Join("?.")})"
                            : $"{instance}.{path.Join("?.")} == null");
                    }
                    yield return $$"""
                        if ({{WithIndent(isEmpty, "    || ")}}) {
                            e.{{GetErrorMemberPath(@ref).Join(".")}}.AddError("{{memberDisplayName}}を指定してください。");
                        }
                        """;

                } else if (member is AggregateMember.Child child) {
                    foreach (var expr in RenderAggregate(child.ChildAggregate, instance, instanceAggregate)) {
                        yield return expr;
                    }
                } else if (member is AggregateMember.VariationItem variationItem) {
                    foreach (var expr in RenderAggregate(variationItem.VariationAggregate, instance, instanceAggregate)) {
                        yield return expr;
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
