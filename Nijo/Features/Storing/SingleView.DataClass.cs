using Nijo.Core;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Features.Storing {
    internal class SingleViewDataClass {
        internal SingleViewDataClass(GraphNode<Aggregate> aggregate) {
            MainAggregate = aggregate;
        }
        internal GraphNode<Aggregate> MainAggregate { get; }

        internal string TsTypeName => $"{MainAggregate.Item.TypeScriptTypeName}SingleViewData";

        internal const string OWN_MEMBERS = "own_members";
        /// <summary>
        /// 編集画面でDBから読み込んだデータとその画面中で新たに作成されたデータで
        /// 挙動を分けるためのフラグ
        /// </summary>
        internal const string IS_LOADED = "loaded";
        /// <summary>
        /// - useFieldArrayの中で配列インデックスをキーに使うと新規追加されたコンボボックスが
        ///   その1個上の要素の更新と紐づいてしまうのでクライアント側で要素1個ずつにIDを振る
        /// - TabGroupでどのタブがアクティブになっているかの判定にも使う
        /// </summary>
        internal const string OBJECT_ID = "object_id";


        internal IEnumerable<Prop> GetChildProps() {
            var childMembers = MainAggregate
                .GetMembers()
                .OfType<AggregateMember.RelationMember>()
                .Where(m => m is not AggregateMember.Ref
                         && m is not AggregateMember.Parent)
                .Select(m => new Prop(MainAggregate, m.MemberAggregate));
            foreach (var item in childMembers) {
                yield return item;
            }
        }
        internal IEnumerable<Prop> GetRefFromProps() {
            var refs = MainAggregate
                .EnumerateThisAndDescendants()
                .SelectMany(agg => agg.GetReferedEdgesAsSingleKeyRecursively())
                // TODO: 本当はDistinctを使いたいがAggregateの同一性判断にSourceが入っていない
                .GroupBy(relation => new { agg = relation.Initial.GetRoot(), relation })
                .Select(group => new Prop(MainAggregate, group.Key.agg));
            foreach (var item in refs) {
                yield return item;
            }
        }

        internal string RenderTypeScriptDataClassDeclaration() {
            if (!MainAggregate.IsRoot()) throw new InvalidOperationException();

            return MainAggregate.EnumerateThisAndDescendants().SelectTextTemplate(agg => {
                var schalars = agg
                    .GetMembers()
                    .OfType<AggregateMember.ValueMember>()
                    .Where(m => m.DeclaringAggregate == agg);
                var dataClass = new SingleViewDataClass(agg);

                return $$"""
                    export type {{dataClass.TsTypeName}} = {
                      {{OBJECT_ID}}?: string
                      {{OWN_MEMBERS}}: {
                    {{schalars.SelectTextTemplate(m => $$"""
                        {{m.MemberName}}?: {{m.TypeScriptTypename}}
                    """)}}
                      }
                    {{dataClass.GetChildProps().SelectTextTemplate(p => $$"""
                      {{p.PropName}}?: {{(p.IsArray ? $"{new SingleViewDataClass(p.Aggregate).TsTypeName}[]" : new SingleViewDataClass(p.Aggregate).TsTypeName)}}
                    """)}}
                    {{dataClass.GetRefFromProps().SelectTextTemplate(p => $$"""
                      {{p.PropName}}?: {{(p.IsArray ? $"{new SingleViewDataClass(p.Aggregate).TsTypeName}[]" : new SingleViewDataClass(p.Aggregate).TsTypeName)}}
                    """)}}
                      {{IS_LOADED}}?: boolean
                    }
                    """;
            });
        }

        internal class Prop {
            internal Prop(GraphNode<Aggregate> mainAggregate, GraphNode<Aggregate> refTarget) {
                _mainAggregate = mainAggregate;
                Aggregate = refTarget;
            }
            private readonly GraphNode<Aggregate> _mainAggregate;
            internal GraphNode<Aggregate> Aggregate { get; }

            /// <summary>
            /// 従属集約が保管されるプロパティの名前を返します
            /// </summary>
            internal string PropName {
                get {
                    if (Aggregate.Source == null) {
                        throw new InvalidOperationException("ルート集約のPropは考慮していない");

                    } else if (Aggregate.Source.IsParentChild()) {
                        return $"child_{Aggregate.Item.ClassName}";

                    } else {
                        return $"ref_from_{Aggregate.Source.RelationName.ToCSharpSafe()}_{Aggregate.Item.ClassName}";
                    }
                }
            }
            /// <summary>
            /// 主たる集約またはそれと1対1の多重度にある集約であればfalse
            /// </summary>
            internal bool IsArray {
                get {
                    var start = false;

                    foreach (var edge in Aggregate.PathFromEntry()) {
                        var initial = edge.Initial.As<Aggregate>();
                        var terminal = edge.Terminal.As<Aggregate>();

                        // Childrenの型ならばPathFromEntryの途中から数えなければいけないので
                        if (!start) {
                            if (initial == _mainAggregate) {
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
            internal bool IsMain => Aggregate == _mainAggregate;
        }
    }
}
