using Nijo.Core;
using Nijo.Core.AggregateMemberTypes;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Features.Storing {
    /// <summary>
    /// 画面に表示されるデータの形。
    /// データ自身が持っている情報に加え、それがDBに存在するデータか否か、
    /// DBから読み込んだあと変更が加わっているか、などの画面に表示するために必要な情報も保持している。
    /// </summary>
    internal class DisplayDataClass {
        internal DisplayDataClass(GraphNode<Aggregate> aggregate) {
            MainAggregate = aggregate;
        }
        internal GraphNode<Aggregate> MainAggregate { get; }

        internal string TsTypeName => $"{MainAggregate.Item.TypeScriptTypeName}DisplayData";
        internal string TsInitFunctionName => $"create{TsTypeName}";

        internal const string OWN_MEMBERS = "own_members";
        internal const string LOCAL_REPOS_ITEMKEY = "localRepositoryItemKey";
        internal const string LOCAL_REPOS_STATE = "localRepositoryState";
        /// <summary>
        /// 編集画面でDBから読み込んだデータとその画面中で新たに作成されたデータで
        /// 挙動を分けるためのフラグ
        /// </summary>
        internal const string IS_LOADED = "loaded";
        /// <summary>
        /// useFieldArrayの中で配列インデックスをキーに使うと新規追加されたコンボボックスが
        /// その1個上の要素の更新と紐づいてしまうのでクライアント側で要素1個ずつにIDを振る
        ///
        /// TODO: ItemKeyと役割が似ているのでこれがなくてもなんとかなる可能性がある
        /// </summary>
        internal const string OBJECT_ID = "object_id";


        internal IEnumerable<OwnProp> GetOwnProps() {
            return MainAggregate
                .GetMembers()
                .Where(m => m.DeclaringAggregate == MainAggregate
                         && (m is AggregateMember.ValueMember || m is AggregateMember.Ref))
                .Select(m => new OwnProp(MainAggregate, m));
        }
        internal IEnumerable<RelationProp> GetChildProps() {
            var childMembers = MainAggregate
                .GetMembers()
                .OfType<AggregateMember.RelationMember>()
                .Where(m => m is not AggregateMember.Ref
                         && m is not AggregateMember.Parent)
                .Select(m => new RelationProp(m.Relation, m));
            foreach (var item in childMembers) {
                yield return item;
            }
        }
        internal IEnumerable<RelationProp> GetRefFromProps() {
            var refs = MainAggregate
                .GetReferedEdgesAsSingleKey()
                .Where(edge => edge.Initial.Item.Options.Handler == NijoCodeGenerator.Models.WriteModel.Key)
                // TODO: 本当はDistinctを使いたいがAggregateの同一性判断にSourceが入っていない
                .GroupBy(relation => new { agg = relation.Initial.GetRoot(), relation })
                .Select(group => new RelationProp(group.Key.relation, null));
            foreach (var item in refs) {
                yield return item;
            }
        }
        /// <summary>
        /// この集約またはこの集約の子孫を唯一のキーとする集約を再帰的に列挙する
        /// </summary>
        internal IEnumerable<(RelationProp, string[] Path, bool IsArray)> GetRefFromPropsRecursively() {
            var refFromPros = new List<(RelationProp, string[] Path, bool IsArray)>();
            void Collect(DisplayDataClass dc, string[] beforePath, bool beforeIsMany) {
                foreach (var rel in dc.GetRefFromProps().Concat(dc.GetChildProps())) {

                    // thisInstanceName.child_子要素.map(x => x.ref_from_参照元)
                    // 上記のように参照元のルート要素を全部収集する式を作っている
                    string thisPath;
                    if (beforeIsMany) {
                        thisPath = rel.IsArray
                            ? $"flatMap(x => x?.{rel.PropName})"
                            : $"map(x => x?.{rel.PropName})";
                    } else {
                        thisPath = $"{rel.PropName}";
                    }

                    var path = beforePath.Concat(new[] { thisPath }).ToArray();
                    if (rel.Type == RelationProp.E_Type.RefFrom) {
                        refFromPros.Add((rel, path, beforeIsMany || rel.IsArray));
                    }
                    Collect(rel, path, beforeIsMany || rel.IsArray);
                }
            }
            Collect(this, [], false);
            return refFromPros;
        }

        internal string ConvertFnNameToLocalRepositoryType => $"convert{MainAggregate.Item.ClassName}ToLocalRepositoryItem";
        /// <summary>
        /// データ型変換関数 (<see cref="DisplayDataClass"/> => <see cref="TransactionScopeDataClass"/>)
        /// </summary>
        internal string RenderConvertFnToLocalRepositoryType(string? thisInstanceName = null) {

            // TODO: リファクタリング
            // ルート要素のレンダリングとメンバーのレンダリングを分ければこのメソッドがオプショナル引数をとる必要はない

            if (MainAggregate.IsRoot()) {
                return $$"""
                    /** 画面に表示されるデータ型を登録更新される粒度の型に変換します。 */
                    export const {{ConvertFnNameToLocalRepositoryType}} = (displayData: {{TsTypeName}}) => {
                      const item0: Util.LocalRepositoryItem<{{MainAggregate.Item.TypeScriptTypeName}}> = {
                        itemKey: displayData.{{LOCAL_REPOS_ITEMKEY}},
                        state: displayData.{{LOCAL_REPOS_STATE}},
                        item: {
                          ...displayData.{{OWN_MEMBERS}},
                    {{GetChildProps().SelectTextTemplate(p => $$"""
                          {{WithIndent(p.RenderConvertFnToLocalRepositoryType($"{thisInstanceName ?? "displayData"}.{p.PropName}"), "      ")}},
                    """)}}
                        },
                      }
                    {{GetRefFromPropsRecursively().SelectTextTemplate((x, i) => x.IsArray ? $$"""
                      const item{{i + 1}}: Util.LocalRepositoryItem<{{x.Item1.MainAggregate.Item.TypeScriptTypeName}}>[] = displayData{{string.Concat(x.Path.Select(p => $"{Environment.NewLine}    ?.{p}"))}}
                        .filter((y): y is Exclude<typeof y, undefined> => y !== undefined)
                        .map(y => ({
                          itemKey: y.{{LOCAL_REPOS_ITEMKEY}},
                          state: y.{{LOCAL_REPOS_STATE}},
                          item: {
                            ...y.{{OWN_MEMBERS}},
                    {{new DisplayDataClass(x.Item1.MainAggregate.AsEntry()).GetChildProps().SelectTextTemplate(p => $$"""
                            {{WithIndent(p.RenderConvertFnToLocalRepositoryType($"y.{p.PropName}"), "        ")}},
                    """)}}
                          },
                        })) ?? []
                    """ : $$"""
                      const item{{i + 1}}: Util.LocalRepositoryItem<{{x.Item1.MainAggregate.Item.TypeScriptTypeName}}> | undefined = displayData{{string.Concat(x.Path.Select(p => $"?.{p}"))}} === undefined ? undefined : {
                        itemKey: displayData{{string.Concat(x.Path.Select(p => $".{p}"))}}.{{LOCAL_REPOS_ITEMKEY}},
                        state: displayData{{string.Concat(x.Path.Select(p => $".{p}"))}}.{{LOCAL_REPOS_STATE}},
                        item: {
                          ...displayData{{string.Concat(x.Path.Select(p => $".{p}"))}}.{{OWN_MEMBERS}},
                    {{x.Item1.GetChildProps().SelectTextTemplate(p => $$"""
                          {{WithIndent(p.RenderConvertFnToLocalRepositoryType($"{thisInstanceName ?? "displayData"}{string.Concat(x.Path.Select(p => $".{p}"))}.{p.PropName}"), "      ")}},
                    """)}}
                        },
                      }
                    """)}}
                      return [
                        item0,
                    {{GetRefFromPropsRecursively().SelectTextTemplate((_, i) => $$"""
                        item{{i + 1}},
                    """)}}
                      ] as const
                    }
                    """;

            } else {
                var asDescendant = (RelationProp)this;
                var member = asDescendant.MemberInfo!;

                if (member is AggregateMember.Children children) {
                    return $$"""
                        {{member.MemberName}}: {{thisInstanceName ?? "displayData"}}?.map(x => ({
                          ...x.{{OWN_MEMBERS}},
                        {{GetChildProps().SelectTextTemplate(p => $$"""
                          {{WithIndent(p.RenderConvertFnToLocalRepositoryType("x"), "  ")}},
                        """)}}
                        }))
                        """;

                } else {
                    return $$"""
                        {{member.MemberName}}: {
                          ...{{thisInstanceName ?? "displayData"}}.{{OWN_MEMBERS}},
                        {{GetChildProps().SelectTextTemplate(p => $$"""
                          {{WithIndent(p.RenderConvertFnToLocalRepositoryType($"{thisInstanceName ?? "displayData"}.{p.PropName}"), "  ")}},
                        """)}}
                        }
                        """;
                }
            }
        }

        internal string RenderTypeScriptDataClassDeclaration() {
            if (!MainAggregate.IsRoot()) throw new InvalidOperationException();

            return MainAggregate.EnumerateThisAndDescendants().SelectTextTemplate(agg => {
                var dataClass = new DisplayDataClass(agg);

                return $$"""
                    export type {{dataClass.TsTypeName}} = {
                      {{OBJECT_ID}}?: string
                    {{If(agg.IsRoot(), () => $$"""
                      {{LOCAL_REPOS_ITEMKEY}}: Util.ItemKey
                      {{LOCAL_REPOS_STATE}}: Util.LocalRepositoryState
                    """)}}
                      {{OWN_MEMBERS}}: {
                    {{dataClass.GetOwnProps().SelectTextTemplate(p => $$"""
                        {{p.PropName}}?: {{p.Member.TypeScriptTypename}}
                    """)}}
                      }
                    {{dataClass.GetChildProps().SelectTextTemplate(p => $$"""
                      {{p.PropName}}?: {{(p.IsArray ? $"{new DisplayDataClass(p.MainAggregate).TsTypeName}[]" : new DisplayDataClass(p.MainAggregate).TsTypeName)}}
                    """)}}
                    {{dataClass.GetRefFromProps().SelectTextTemplate(p => $$"""
                      {{p.PropName}}?: {{(p.IsArray ? $"{new DisplayDataClass(p.MainAggregate).TsTypeName}[]" : new DisplayDataClass(p.MainAggregate).TsTypeName)}}
                    """)}}
                      {{IS_LOADED}}?: boolean
                    }
                    """;
            });
        }

        internal class OwnProp {
            internal OwnProp(GraphNode<Aggregate> dataClassMainAggregate, AggregateMember.AggregateMemberBase member) {
                _mainAggregate = dataClassMainAggregate;
                Member = member;
            }
            private readonly GraphNode<Aggregate> _mainAggregate;
            internal AggregateMember.AggregateMemberBase Member { get; }

            internal string PropName => Member.MemberName;

            internal IEnumerable<string> GetPathSinceDataClassOwner() {
                yield return OWN_MEMBERS;

                if (Member is AggregateMember.ValueMember vm) {
                    foreach (var path in vm.Declared.GetFullPath(since: _mainAggregate)) {
                        yield return path;
                    }
                } else {
                    foreach (var path in Member.GetFullPath(since: _mainAggregate)) {
                        yield return path;
                    }
                }
            }
        }

        internal class RelationProp : DisplayDataClass {
            internal RelationProp(GraphEdge<Aggregate> relation, AggregateMember.RelationMember? memberInfo)
                : base(relation.IsRef() ? relation.Initial : relation.Terminal) {
                MemberInfo = memberInfo;
            }
            internal AggregateMember.RelationMember? MemberInfo { get; }

            internal enum E_Type {
                Descendant,
                RefFrom,
            }
            internal E_Type Type => MainAggregate.Source?.IsParentChild() == true
                ? E_Type.Descendant
                : E_Type.RefFrom;

            /// <summary>
            /// 従属集約が保管されるプロパティの名前を返します
            /// </summary>
            internal string PropName {
                get {
                    if (MainAggregate.Source == null) {
                        throw new InvalidOperationException("ルート集約のPropは考慮していない");

                    } else if (MainAggregate.Source.IsParentChild()) {
                        return $"child_{MainAggregate.Item.ClassName}";

                    } else {
                        return $"ref_from_{MainAggregate.Source.RelationName.ToCSharpSafe()}_{MainAggregate.Item.ClassName}";
                    }
                }
            }
            /// <summary>
            /// 主たる集約またはそれと1対1の多重度にある集約であればfalse
            /// </summary>
            internal bool IsArray => MemberInfo is AggregateMember.Children;
        }
    }

    internal static class StoringExtensions {
        /// <summary>
        /// エントリーからのパスを <see cref="DisplayDataClass"/> のデータ構造にあわせて返す。
        /// たとえば自身のメンバーならその前に <see cref="DisplayDataClass.OWN_MEMBERS"/> を挟むなど
        /// </summary>
        internal static IEnumerable<string> GetFullPathAsSingleViewDataClass(this AggregateMember.AggregateMemberBase member) {

            var enumeratingRefTargetKeyName = false;

            foreach (var edge in member.Owner.PathFromEntry()) {
                if (edge.Source == edge.Terminal) {
                    // 有向グラフの矢印の先から元に辿るパターン

                    if (edge.IsParentChild()) {
                        yield return AggregateMember.PARENT_PROPNAME; // 子から親に向かって辿る場合

                    } else if (edge.IsRef()) {
                        var dataClass = new DisplayDataClass(edge.Terminal.As<Aggregate>());
                        yield return dataClass
                            .GetRefFromProps()
                            .Single(p => p.MainAggregate.Source == edge)
                            .PropName;

                        enumeratingRefTargetKeyName = false;

                    } else {
                        throw new InvalidOperationException($"有向グラフの矢印の先から元に向かうパターンは親子か参照だけなのでこの分岐にくることはあり得ないはず");
                    }

                } else {
                    // 有向グラフの矢印の元から先に辿るパターン

                    if (edge.IsParentChild()) {
                        var dataClass = new DisplayDataClass(edge.Initial.As<Aggregate>());
                        yield return dataClass
                            .GetChildProps()
                            .Single(p => p.MainAggregate == edge.Terminal.As<Aggregate>())
                            .PropName;

                        enumeratingRefTargetKeyName = false;

                    } else if (edge.IsRef()) {
                        if (!enumeratingRefTargetKeyName) {
                            yield return DisplayDataClass.OWN_MEMBERS;
                        }

                        /// <see cref="RefTargetKeyName"/> の仕様に合わせる
                        yield return edge.RelationName;

                        enumeratingRefTargetKeyName = true;

                    } else {
                        throw new InvalidOperationException($"有向グラフの矢印の先から元に向かうパターンは親子か参照だけなのでこの分岐にくることはあり得ないはず");
                    }
                }
            }

            if (!enumeratingRefTargetKeyName) {
                yield return DisplayDataClass.OWN_MEMBERS;
            }

            yield return member.MemberName;
        }
    }
}
