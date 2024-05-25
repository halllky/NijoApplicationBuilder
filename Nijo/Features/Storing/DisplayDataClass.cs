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

        internal const string OWN_MEMBERS = "own_members";
        /// <summary>
        /// リモートリポジトリに存在しないインスタンス（react-hook-formが作成したインスタンス）ならfalse or undefined,
        /// 存在するインスタンス（ユーザーが作成したインスタンス）ならtrue。
        /// UI上でChildrenの主キーが変更可能かどうかの制御、削除のアクションが起きた時の挙動などに使用
        /// </summary>
        internal const string EXISTS_IN_REMOTE_REPOS = "existsInRemoteRepository";
        /// <summary>
        /// 画面上で何らかの変更が加えられてから、リモートリポジトリで削除されるまでの間、trueになる
        /// </summary>
        internal const string WILL_BE_CHANGED = "willBeChanged";
        /// <summary>
        /// 画面上で削除が指示されてから、リモートリポジトリで削除されるまでの間、trueになる
        /// </summary>
        internal const string WILL_BE_DELETED = "willBeDeleted";
        internal const string LOCAL_REPOS_ITEMKEY = "localRepositoryItemKey";

        /// <summary>
        /// <see cref="OWN_MEMBERS"/> 構造体の中に宣言されるプロパティを列挙します。
        /// </summary>
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

        /// <summary>
        /// 新規オブジェクト作成のリテラルをレンダリングします。
        /// </summary>
        /// <param name="initValue">初期値を宣言するソースコード。 "{member.MemberName}: {0}," の{0}の形にすること。nullを返した場合は既定の値で初期化される。</param>
        internal string RenderNewObjectLiteral(Func<AggregateMember.AggregateMemberBase, string?>? initValue = null) {

            // 初期値をレンダリングする。nullを返した場合は明示的な初期値なしになる。
            string? RenderOwnMemberInitialize(AggregateMember.AggregateMemberBase member) {
                // 引数で指定された初期値
                var argInitValue = initValue?.Invoke(member);
                if (argInitValue != null) {
                    return $"{member.MemberName}: {argInitValue},";
                }

                // UUID型
                if (member is AggregateMember.Schalar schalar
                    && schalar.DeclaringAggregate == MainAggregate
                    && schalar.Options.MemberType is Uuid) {

                    return $"{member.MemberName}: UUID.generate(),";
                }

                // バリエーション
                if (member is AggregateMember.Variation variation
                    && variation.DeclaringAggregate == MainAggregate) {

                    return $"{member.MemberName}: '{variation.GetGroupItems().First().Key}',";
                }

                // 初期値なし
                return null;
            }

            return $$"""
                {
                {{If(MainAggregate.IsRoot() || MainAggregate.IsChildrenMember(), () => $$"""
                  {{LOCAL_REPOS_ITEMKEY}}: JSON.stringify(UUID.generate()) as Util.ItemKey,
                  {{EXISTS_IN_REMOTE_REPOS}}: false,
                  {{WILL_BE_CHANGED}}: true,
                  {{WILL_BE_DELETED}}: false,
                """)}}
                  {{OWN_MEMBERS}}: {
                {{MainAggregate.GetMembers().Select(RenderOwnMemberInitialize).OfType<string>().SelectTextTemplate(line => $$"""
                    {{WithIndent(line, "    ")}}
                """)}}
                  },
                }
                """;
        }

        internal string ConvertFnNameToLocalRepositoryType => $"convert{MainAggregate.Item.ClassName}ToLocalRepositoryItem";

        /// <summary>
        /// データ型変換関数 (<see cref="DisplayDataClass"/> => <see cref="TransactionScopeDataClass"/>)
        /// </summary>
        internal string RenderConvertFnToLocalRepositoryType() {

            string RenderItem(DisplayDataClass dc, string instance) {

                string RenderOwnMemberValue(AggregateMember.AggregateMemberBase member) {
                    if (member is AggregateMember.Ref @ref) {
                        // @refまでの経路はDisplayDataClassのルールで, それ以降の経路はDisplayRefTargetClassのルールで計算するため、AsEntryしている
                        var refTargetData = new DisplayRefTargetClass(@ref.RefTo.AsEntry());
                        var refTargetMembers = refTargetData
                            .GetDisplayMembers()
                            .OfType<AggregateMember.ValueMember>()
                            .ToArray();

                        var keyArray = KeyArray.Create(@ref.MemberAggregate);
                        var keyArrayType = $"[{keyArray.Select(k => $"{k.TsType} | undefined").Join(", ")}]";

                        string RenderRefTargetKeyNameValue(AggregateMember.RelationMember refOrParent) {
                            var keyname = new TransactionScopeRefTargetClass(refOrParent.MemberAggregate);
                            return $$"""
                                {
                                {{keyname.GetOwnMembers().OfType<AggregateMember.ValueMember>().SelectTextTemplate(vm => $$"""
                                  {{vm.MemberName}}: {{instance}}.{{@ref.GetFullPathAsSingleViewDataClass().Join("?.")}}?.{{refTargetMembers.Single(r => r.Declared == vm.Declared).Declared.GetFullPath(since: @ref.RefTo).Join("?.")}},
                                """)}}
                                }
                                """;
                            // var keyname = new RefTargetKeyName(refOrParent.MemberAggregate);
                            // return $$"""
                            //     {
                            //     {{keyname.GetOwnKeyMembers().SelectTextTemplate(m => m is AggregateMember.RelationMember refOrParent2 ? $$"""
                            //       {{m.MemberName}}: {{WithIndent(RenderRefTargetKeyNameValue(refOrParent2), "  ")}},
                            //     """ : $$"""
                            //       {{m.MemberName}}: {{instance}}.{{@ref.GetFullPathAsSingleViewDataClass().Join("?.")}}
                            //         ? (JSON.parse({{instance}}.{{@ref.GetFullPathAsSingleViewDataClass().Join(".")}}) as {{keyArrayType}})[{{keyArray.Single(k => k.Member.Declared == ((AggregateMember.ValueMember)m).Declared).Index}}]
                            //         : undefined,
                            //     """)}}
                            //     }
                            //     """;
                        }
                        return RenderRefTargetKeyNameValue(@ref);

                    } else {
                        return $$"""
                            {{instance}}?.{{member.GetFullPathAsSingleViewDataClass().Join("?.")}}
                            """;
                    }
                }
                return $$"""
                    {
                    {{dc.GetOwnProps().SelectTextTemplate(p => $$"""
                      {{p.Member.MemberName}}: {{WithIndent(RenderOwnMemberValue(p.Member), "  ")}},
                    """)}}
                    {{dc.GetChildProps().SelectTextTemplate(p => p.MemberInfo is AggregateMember.Children ? $$"""
                      {{p.MemberInfo?.MemberName}}: {{instance}}.{{p.MemberInfo?.MemberAggregate.GetFullPathAsSingleViewDataClass().Join("?.")}}?.map(x{{p.MemberInfo?.MemberName}} => ({{WithIndent(RenderItem(new DisplayDataClass(p.MainAggregate.AsEntry()), $"x{p.MemberInfo?.MemberName}"), "  ")}})),
                    """ : $$"""
                      {{p.MemberInfo?.MemberName}}: {{WithIndent(RenderItem(p, instance), "  ")}},
                    """)}}
                    }
                    """;
            }

            return $$"""
                /** 画面に表示されるデータ型を登録更新される粒度の型に変換します。 */
                export const {{ConvertFnNameToLocalRepositoryType}} = (displayData: {{TsTypeName}}) => {
                  const item0: Util.LocalRepositoryItem<{{MainAggregate.Item.TypeScriptTypeName}}> = {
                    itemKey: displayData.{{LOCAL_REPOS_ITEMKEY}},
                    existsInRemoteRepository: displayData.{{EXISTS_IN_REMOTE_REPOS}},
                    willBeChanged: displayData.{{WILL_BE_CHANGED}},
                    willBeDeleted: displayData.{{WILL_BE_DELETED}},
                    item: {{WithIndent(RenderItem(this, "displayData"), "    ")}},
                  }
                {{GetRefFromPropsRecursively().SelectTextTemplate((x, i) => x.IsArray ? $$"""

                  const item{{i + 1}}: Util.LocalRepositoryItem<{{x.Item1.MainAggregate.Item.TypeScriptTypeName}}>[] = displayData{{string.Concat(x.Path.Select(p => $"{Environment.NewLine}    ?.{p}"))}}
                    .filter((y): y is Exclude<typeof y, undefined> => y !== undefined)
                    .map(y => ({
                      itemKey: y.{{LOCAL_REPOS_ITEMKEY}},
                      existsInRemoteRepository: y.{{EXISTS_IN_REMOTE_REPOS}},
                      willBeChanged: y.{{WILL_BE_CHANGED}},
                      willBeDeleted: y.{{WILL_BE_DELETED}},
                      item: {{WithIndent(RenderItem(new DisplayDataClass(x.Item1.MainAggregate.AsEntry()), "y"), "      ")}},
                    })) ?? []
                """ : $$"""

                  const item{{i + 1}}: Util.LocalRepositoryItem<{{x.Item1.MainAggregate.Item.TypeScriptTypeName}}> | undefined = displayData{{x.Path.Select(p => $"?.{p}").Join("")}} === undefined
                    ? undefined
                    : {
                      itemKey: displayData{{x.Path.Select(p => $".{p}").Join("")}}.{{LOCAL_REPOS_ITEMKEY}},
                      existsInRemoteRepository: displayData{{x.Path.Select(p => $".{p}").Join("")}}.{{EXISTS_IN_REMOTE_REPOS}},
                      willBeChanged: displayData{{x.Path.Select(p => $".{p}").Join("")}}.{{WILL_BE_CHANGED}},
                      willBeDeleted: displayData{{x.Path.Select(p => $".{p}").Join("")}}.{{WILL_BE_DELETED}},
                      item: {{WithIndent(RenderItem(x.Item1, "displayData"), "      ")}},
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
        }

        /// <summary>
        /// データ型変換関数 (<see cref="TransactionScopeDataClass"/> => <see cref="DisplayDataClass"/>)
        /// </summary>
        internal string RenderConvertToDisplayDataClass(string mainArgName) {

            // 子孫要素を参照するデータを引数の配列中から探すためにはキーで引き当てる必要があるが、
            // 子孫要素のラムダ式の中ではその外にある変数を参照するしかない
            var pkVarNames = new Dictionary<AggregateMember.ValueMember, string>();
            foreach (var key in MainAggregate.GetKeys().OfType<AggregateMember.ValueMember>()) {
                pkVarNames.Add(key.Declared, $"{mainArgName}.{key.Declared.GetFullPath().Join("?.")}");
            }

            string Render(DisplayDataClass dc, string instance, bool inLambda) {
                var keys = inLambda
                    ? dc.MainAggregate.AsEntry().GetKeys().OfType<AggregateMember.ValueMember>()
                    : dc.MainAggregate.GetKeys().OfType<AggregateMember.ValueMember>();

                foreach (var key in keys) {
                    // 実際にはここでcontinueされるのは親のキーだけのはず。Render関数はルートから順番に呼び出されるので
                    if (pkVarNames.ContainsKey(key.Declared)) continue;

                    /// 変換元のデータ型が <see cref="TransactionScopeDataClass"/> のため、キーが <see cref="TransactionScopeRefTargetClass"/> の可能性がある。そのためキーのオーナーからのフルパスにしている
                    pkVarNames.Add(key.Declared, $"{instance}?.{key.Declared.GetFullPath(since: key.Owner).Join("?.")}");
                }

                var ownMembers = dc.MainAggregate
                    .AsEntry()
                    .GetMembers()
                    .Where(m => m.DeclaringAggregate == dc.MainAggregate
                             && (m is AggregateMember.ValueMember || m is AggregateMember.Ref));
                var depth = dc.MainAggregate.EnumerateAncestors().Count();

                return $$"""
                    {
                    {{If(dc.MainAggregate.IsRoot() || dc.MainAggregate.IsChildrenMember(), () => $$"""
                      {{LOCAL_REPOS_ITEMKEY}}: JSON.stringify([{{keys.Select(k => pkVarNames[k.Declared]).Join(", ")}}]) as ItemKey,
                      {{EXISTS_IN_REMOTE_REPOS}}: true,
                      {{WILL_BE_CHANGED}}: false,
                      {{WILL_BE_DELETED}}: false,
                    """)}}
                      {{OWN_MEMBERS}}: {
                    {{ownMembers.SelectTextTemplate(m => $$"""
                        {{m.MemberName}}: {{instance}}?.{{m.MemberName}},
                    """)}}
                      },
                    {{dc.GetChildProps().SelectTextTemplate(p => p.IsArray ? $$"""
                      {{p.PropName}}: {{instance}}?.{{p.MemberInfo?.MemberName}}?.map(x{{depth}} => ({{WithIndent(Render(p, $"x{depth}", true), "  ")}})),
                    """ : $$"""
                      {{p.PropName}}: {{WithIndent(Render(p, $"{instance}?.{p.MemberInfo?.MemberName}", false), "  ")}},
                    """)}}
                    }
                    """;
            }

            return Render(this, mainArgName, false);
        }

        internal string RenderTypeScriptDataClassDeclaration() {
            if (!MainAggregate.IsRoot()) throw new InvalidOperationException();

            return MainAggregate.EnumerateThisAndDescendants().SelectTextTemplate(agg => {
                var dataClass = new DisplayDataClass(agg);

                return $$"""
                    /** {{agg.Item.DisplayName}}の画面表示用データ */
                    export type {{dataClass.TsTypeName}} = {
                    {{If(agg.IsRoot() || agg.IsChildrenMember(), () => $$"""
                      {{LOCAL_REPOS_ITEMKEY}}: Util.ItemKey
                      {{EXISTS_IN_REMOTE_REPOS}}: boolean
                      {{WILL_BE_CHANGED}}: boolean
                      {{WILL_BE_DELETED}}: boolean
                    """)}}
                      {{OWN_MEMBERS}}: {
                    {{dataClass.GetOwnProps().SelectTextTemplate(p => $$"""
                        {{p.PropName}}?: {{p.PropType}}
                    """)}}
                      }
                    {{dataClass.GetChildProps().SelectTextTemplate(p => $$"""
                      {{p.PropName}}?: {{(p.IsArray ? $"{new DisplayDataClass(p.MainAggregate).TsTypeName}[]" : new DisplayDataClass(p.MainAggregate).TsTypeName)}}
                    """)}}
                    {{dataClass.GetRefFromProps().SelectTextTemplate(p => $$"""
                      {{p.PropName}}?: {{(p.IsArray ? $"{new DisplayDataClass(p.MainAggregate).TsTypeName}[]" : new DisplayDataClass(p.MainAggregate).TsTypeName)}}
                    """)}}
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
            internal string PropType => Member is AggregateMember.Ref @ref
                ? new DisplayRefTargetClass(@ref.RefTo).TsTypeName
                : Member.TypeScriptTypename;
        }

        internal class RelationProp : DisplayDataClass {
            internal RelationProp(GraphEdge<Aggregate> relation, AggregateMember.RelationMember? memberInfo)
                : base(relation.IsRef() ? relation.Initial : relation.Terminal) {
                MemberInfo = memberInfo;
            }
            /// <summary>
            /// Ref From プロパティの場合はnull
            /// </summary>
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

    internal static partial class StoringExtensions {

        #region DisplayDataClassのパス
        /// <summary>
        /// エントリーからのパスを <see cref="DisplayDataClass"/> のデータ構造にあわせて返す。
        /// たとえば自身のメンバーならその前に <see cref="DisplayDataClass.OWN_MEMBERS"/> を挟むなど
        /// </summary>
        internal static IEnumerable<string> GetFullPathAsSingleViewDataClass(this GraphNode<Aggregate> aggregate, GraphNode<Aggregate>? since = null) {
            return GetFullPathAsSingleViewDataClass(aggregate, since, out var _);
        }
        /// <summary>
        /// エントリーからのパスを <see cref="DisplayDataClass"/> のデータ構造にあわせて返す。
        /// たとえば自身のメンバーならその前に <see cref="DisplayDataClass.OWN_MEMBERS"/> を挟むなど
        /// </summary>
        internal static IEnumerable<string> GetFullPathAsSingleViewDataClass(this AggregateMember.AggregateMemberBase member, GraphNode<Aggregate>? since = null) {
            bool enumeratingRefTargetKeyName;
            foreach (var path in GetFullPathAsSingleViewDataClass(member.Owner, since, out enumeratingRefTargetKeyName)) {
                yield return path;
            }
            if (!enumeratingRefTargetKeyName) {
                yield return DisplayDataClass.OWN_MEMBERS;
            }
            yield return member.MemberName;
        }
        private static IEnumerable<string> GetFullPathAsSingleViewDataClass(this GraphNode<Aggregate> aggregate, GraphNode<Aggregate>? since, out bool enumeratingRefTargetKeyName) {
            var paths = new List<string>();
            enumeratingRefTargetKeyName = false;

            var pathFromEntry = aggregate.PathFromEntry();
            if (since != null) pathFromEntry = pathFromEntry.Since(since);

            foreach (var edge in pathFromEntry) {
                if (edge.Source == edge.Terminal) {

                    if (edge.IsParentChild()) {
                        paths.Add(AggregateMember.PARENT_PROPNAME); // 子から親に向かって辿る場合

                    } else if (edge.IsRef()) {
                        var dataClass = new DisplayDataClass(edge.Terminal.As<Aggregate>());
                        paths.Add(dataClass
                            .GetRefFromProps()
                            .Single(p => p.MainAggregate.Source == edge)
                            .PropName);

                        enumeratingRefTargetKeyName = false;

                    } else {
                        throw new InvalidOperationException($"有向グラフの矢印の先から元に向かうパターンは親子か参照だけなのでこの分岐にくることはあり得ないはず");
                    }

                } else {

                    if (edge.IsParentChild()) {
                        var dc = new DisplayDataClass(edge.Initial.As<Aggregate>());
                        paths.Add(dc
                            .GetChildProps()
                            .Single(p => p.MainAggregate == edge.Terminal.As<Aggregate>())
                            .PropName);

                        enumeratingRefTargetKeyName = false;

                    } else if (edge.IsRef()) {
                        if (!enumeratingRefTargetKeyName) {
                            paths.Add(DisplayDataClass.OWN_MEMBERS);
                        }

                        /// <see cref="TransactionScopeRefTargetClass"/> の仕様に合わせる
                        paths.Add(edge.RelationName);

                        enumeratingRefTargetKeyName = true;

                    } else {
                        throw new InvalidOperationException($"有向グラフの矢印の元から先に向かうパターンは親子か参照だけなのでこの分岐にくることはあり得ないはず");
                    }
                }
            }

            return paths;
        }
        #endregion DisplayDataClassのパス


        #region useFormContextのパス
        /// <summary>
        /// React Hook Form の記法に従ったルートオブジェクトからの登録名のパスを返します。
        /// </summary>
        /// <param name="arrayIndexes">配列インデックスを指定する変数の名前</param>
        internal static IEnumerable<string> GetRHFRegisterName(this GraphNode<Aggregate> aggregate, IEnumerable<string>? arrayIndexes = null) {
            foreach (var path in EnumerateRHFRegisterName(aggregate, false, arrayIndexes)) {
                yield return path;
            }
        }
        /// <summary>
        /// React Hook Form の記法に従ったルートオブジェクトからの登録名のパスを返します。
        /// </summary>
        /// <param name="arrayIndexes">配列インデックスを指定する変数の名前</param>
        internal static IEnumerable<string> GetRHFRegisterName(this AggregateMember.AggregateMemberBase member, IEnumerable<string>? arrayIndexes = null) {
            foreach (var path in EnumerateRHFRegisterName(member.Owner, true, arrayIndexes)) {
                yield return path;
            }
            yield return DisplayDataClass.OWN_MEMBERS;
            yield return member.MemberName;
        }

        private static IEnumerable<string> EnumerateRHFRegisterName(this GraphNode<Aggregate> aggregate, bool enumerateLastChildrenIndex, IEnumerable<string>? arrayIndexes) {
            var currentArrayIndex = 0;

            foreach (var edge in aggregate.PathFromEntry()) {
                if (edge.Source == edge.Terminal) {

                    if (edge.IsParentChild()) {
                        yield return AggregateMember.PARENT_PROPNAME; // 子から親に向かって辿る場合

                    } else if (edge.IsRef()) {
                        var dc = new DisplayDataClass(edge.Terminal.As<Aggregate>());
                        yield return dc
                            .GetRefFromProps()
                            .Single(p => p.MainAggregate.Source == edge)
                            .PropName;

                    } else {
                        throw new InvalidOperationException($"有向グラフの矢印の先から元に向かうパターンは親子か参照だけなのでこの分岐にくることはあり得ないはず");
                    }

                } else {

                    if (edge.IsParentChild()) {
                        var dataClass = new DisplayDataClass(edge.Initial.As<Aggregate>());
                        var terminal = edge.Terminal.As<Aggregate>();

                        yield return dataClass
                            .GetChildProps()
                            .Single(p => p.MainAggregate == terminal)
                            .PropName;

                        // 子要素が配列の場合はその配列の何番目の要素かを指定する必要がある
                        if (terminal.IsChildrenMember()
                            // "….Children.${}" の最後の配列インデックスを列挙するか否か
                            && (enumerateLastChildrenIndex || terminal != aggregate)) {

                            var arrayIndex = arrayIndexes?.ElementAtOrDefault(currentArrayIndex);
                            yield return $"${{{arrayIndex}}}";

                            currentArrayIndex++;
                        }

                    } else if (edge.IsRef()) {
                        yield return edge.RelationName;

                    } else {
                        throw new InvalidOperationException($"有向グラフの矢印の先から元に向かうパターンは親子か参照だけなのでこの分岐にくることはあり得ないはず");
                    }
                }
            }
        }
        #endregion useFormContextのパス
    }
}
