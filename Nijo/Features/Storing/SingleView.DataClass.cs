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
    /// SingleViewに表示されるデータの形。
    /// <see cref="TransactionScopeDataClass"/> のデータに加え、その集約に関連する隣の集約もこの範囲に含まれる。
    /// </summary>
    internal class SingleViewDataClass {
        internal SingleViewDataClass(GraphNode<Aggregate> aggregate) {
            MainAggregate = aggregate;
        }
        internal GraphNode<Aggregate> MainAggregate { get; }

        internal string TsTypeName => $"{MainAggregate.Item.TypeScriptTypeName}SingleViewData";
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
        /// TODO: これがなくてもなんとかなる可能性がある
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
                .Select(m => new RelationProp(MainAggregate, m.Relation));
            foreach (var item in childMembers) {
                yield return item;
            }
        }
        internal IEnumerable<RelationProp> GetRefFromProps() {
            var refs = MainAggregate
                .GetReferedEdges()
                .Where(edge => edge.Initial.Item.Options.Handler == NijoCodeGenerator.Models.WriteModel.Key)
                // TODO: 本当はDistinctを使いたいがAggregateの同一性判断にSourceが入っていない
                .GroupBy(relation => new { agg = relation.Initial.GetRoot(), relation })
                .Select(group => new RelationProp(MainAggregate, group.Key.relation));
            foreach (var item in refs) {
                yield return item;
            }
        }

        internal string RenderTypeScriptDataClassDeclaration() {
            if (!MainAggregate.IsRoot()) throw new InvalidOperationException();

            return MainAggregate.EnumerateThisAndDescendants().SelectTextTemplate(agg => {
                var dataClass = new SingleViewDataClass(agg);

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
                      {{p.PropName}}?: {{(p.IsArray ? $"{new SingleViewDataClass(p.MainAggregate).TsTypeName}[]" : new SingleViewDataClass(p.MainAggregate).TsTypeName)}}
                    """)}}
                    {{dataClass.GetRefFromProps().SelectTextTemplate(p => $$"""
                      {{p.PropName}}?: {{(p.IsArray ? $"{new SingleViewDataClass(p.MainAggregate).TsTypeName}[]" : new SingleViewDataClass(p.MainAggregate).TsTypeName)}}
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

        internal class RelationProp : SingleViewDataClass {
            internal RelationProp(GraphNode<Aggregate> entry, GraphEdge<Aggregate> relation)
                : base(relation.IsRef() ? relation.Initial : relation.Terminal) {
                _entryAggregate = entry;
            }
            private readonly GraphNode<Aggregate> _entryAggregate;

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
        /// エントリーからのパスを <see cref="SingleViewDataClass"/> のデータ構造にあわせて返す。
        /// たとえば自身のメンバーならその前に <see cref="SingleViewDataClass.OWN_MEMBERS"/> を挟むなど
        /// </summary>
        internal static IEnumerable<string> GetFullPathAsSingleViewDataClass(this AggregateMember.AggregateMemberBase member) {

            var enumeratingRefTargetKeyName = false;

            foreach (var edge in member.Owner.PathFromEntry()) {
                if (edge.Source == edge.Terminal) {
                    // 有向グラフの矢印の先から元に辿るパターン

                    if (edge.IsParentChild()) {
                        yield return AggregateMember.PARENT_PROPNAME; // 子から親に向かって辿る場合

                    } else if (edge.IsRef()) {
                        var dataClass = new SingleViewDataClass(edge.Terminal.As<Aggregate>());
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
                        var dataClass = new SingleViewDataClass(edge.Initial.As<Aggregate>());
                        yield return dataClass
                            .GetChildProps()
                            .Single(p => p.MainAggregate == edge.Terminal.As<Aggregate>())
                            .PropName;

                        enumeratingRefTargetKeyName = false;

                    } else if (edge.IsRef()) {
                        if (!enumeratingRefTargetKeyName) {
                            yield return SingleViewDataClass.OWN_MEMBERS;
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
                yield return SingleViewDataClass.OWN_MEMBERS;
            }

            yield return member.MemberName;
        }
    }
}
