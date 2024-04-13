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
                .Select(m => new RelationProp(MainAggregate, m.Relation, m));
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
                .Select(group => new RelationProp(MainAggregate, group.Key.relation, null));
            foreach (var item in refs) {
                yield return item;
            }
        }

        internal string RenderDecomposingToLocalRepositoryType(string thisInstanceName) {
            if (MainAggregate.IsRoot()) {
                // 変換対象となるルート集約を集める
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

                return $$"""
                    const item0: Util.LocalRepositoryItem<AggregateType.{{MainAggregate.Item.TypeScriptTypeName}}> = {
                      itemKey: {{thisInstanceName}}.{{LOCAL_REPOS_ITEMKEY}},
                      state: {{thisInstanceName}}.{{LOCAL_REPOS_STATE}},
                      item: {
                        ...{{thisInstanceName}}.{{OWN_MEMBERS}},
                    {{GetChildProps().SelectTextTemplate(p => $$"""
                        {{WithIndent(p.RenderDecomposingToLocalRepositoryType($"{thisInstanceName}.{p.PropName}"), "    ")}},
                    """)}}
                      },
                    }
                    {{refFromPros.SelectTextTemplate((x, i) => x.IsArray ? $$"""
                    const item{{i + 1}}: Util.LocalRepositoryItem<AggregateType.{{x.Item1.MainAggregate.Item.TypeScriptTypeName}}>[] = {{thisInstanceName}}{{string.Concat(x.Path.Select(p => $"{Environment.NewLine}  ?.{p}"))}}
                      .filter((y): y is Exclude<typeof y, undefined> => y !== undefined)
                      .map(y => ({
                        itemKey: y.{{LOCAL_REPOS_ITEMKEY}},
                        state: y.{{LOCAL_REPOS_STATE}},
                        item: {
                          ...y.{{OWN_MEMBERS}},
                    {{new DisplayDataClass(x.Item1.MainAggregate.AsEntry()).GetChildProps().SelectTextTemplate(p => $$"""
                          {{WithIndent(p.RenderDecomposingToLocalRepositoryType($"y.{p.PropName}"), "      ")}},
                    """)}}
                        },
                      })) ?? []
                    """ : $$"""
                    const item{{i + 1}}: Util.LocalRepositoryItem<AggregateType.{{x.Item1.MainAggregate.Item.TypeScriptTypeName}}> | undefined = {{thisInstanceName}}{{string.Concat(x.Path.Select(p => $"?.{p}"))}} === undefined ? undefined : {
                      itemKey: {{thisInstanceName}}{{string.Concat(x.Path.Select(p => $".{p}"))}}.{{LOCAL_REPOS_ITEMKEY}},
                      state: {{thisInstanceName}}{{string.Concat(x.Path.Select(p => $".{p}"))}}.{{LOCAL_REPOS_STATE}},
                      item: {
                        ...{{thisInstanceName}}{{string.Concat(x.Path.Select(p => $".{p}"))}}.{{OWN_MEMBERS}},
                    {{x.Item1.GetChildProps().SelectTextTemplate(p => $$"""
                        {{WithIndent(p.RenderDecomposingToLocalRepositoryType($"{thisInstanceName}{string.Concat(x.Path.Select(p => $".{p}"))}.{p.PropName}"), "    ")}},
                    """)}}
                      },
                    }
                    """)}}
                    """;

            } else {
                var asDescendant = (RelationProp)this;
                var member = asDescendant.MemberInfo!;

                if (member is AggregateMember.Children children) {
                    return $$"""
                        {{member.MemberName}}: {{thisInstanceName}}?.map(x => ({
                          ...x.{{OWN_MEMBERS}},
                        {{GetChildProps().SelectTextTemplate(p => $$"""
                          {{WithIndent(p.RenderDecomposingToLocalRepositoryType("x"), "  ")}},
                        """)}}
                        }))
                        """;

                } else {
                    return $$"""
                        {{member.MemberName}}: {
                          ...{{thisInstanceName}}.{{OWN_MEMBERS}},
                        {{GetChildProps().SelectTextTemplate(p => $$"""
                          {{WithIndent(p.RenderDecomposingToLocalRepositoryType($"{thisInstanceName}.{p.PropName}"), "  ")}},
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
            internal RelationProp(
                GraphNode<Aggregate> entry,
                GraphEdge<Aggregate> relation,
                AggregateMember.RelationMember? memberInfo)
                : base(relation.IsRef() ? relation.Initial : relation.Terminal) {
                _entryAggregate = entry;
                MemberInfo = memberInfo;
            }
            private readonly GraphNode<Aggregate> _entryAggregate;
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
            internal bool IsArray {
                get {
                    var start = false;

                    foreach (var edge in MainAggregate.PathFromEntry()) {
                        var initial = edge.Initial.As<Aggregate>();
                        var terminal = edge.Terminal.As<Aggregate>();

                        // Childrenの型ならばPathFromEntryの途中から数えなければいけないので
                        if (!start) {
                            if (initial == _entryAggregate) {
                                start = true;
                            } else {
                                continue;
                            }
                        }

                        // 経路の途中にChildrenが含まれるならば多重度:多
                        if (terminal.IsChildrenMember()
                            && terminal.GetParent() == edge.As<Aggregate>()) {
                            return true;
                        }

                        // 経路の途中に主キーでないRefが含まれるならば多重度:多
                        if (edge.IsRef()
                            && !terminal.IsSingleRefKeyOf(initial)) {
                            return true;
                        }
                    }

                    return false;
                }
            }
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
