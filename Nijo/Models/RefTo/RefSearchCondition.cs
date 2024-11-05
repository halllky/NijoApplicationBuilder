using Nijo.Core;
using Nijo.Models.ReadModel2Features;
using Nijo.Parts.WebClient;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.RefTo {
    /// <summary>
    /// 参照先検索条件。
    /// </summary>
    internal class RefSearchCondition : SearchCondition {
        internal RefSearchCondition(GraphNode<Aggregate> agg, GraphNode<Aggregate> refEntry) : base(agg) {
            _refEntry = refEntry;
        }

        private readonly GraphNode<Aggregate> _refEntry;

        internal override string CsClassName => _refEntry == _aggregate
            ? $"{_refEntry.Item.PhysicalName}RefSearchCondition"
            : $"{_refEntry.Item.PhysicalName}RefSearchCondition_{GetRelationHistory(_aggregate, _refEntry).Join("の")}";
        internal override string TsTypeName => _refEntry == _aggregate
            ? $"{_refEntry.Item.PhysicalName}RefSearchCondition"
            : $"{_refEntry.Item.PhysicalName}RefSearchCondition_{GetRelationHistory(_aggregate, _refEntry).Join("の")}";
        internal override string CsFilterClassName => _refEntry == _aggregate
            ? $"{_refEntry.Item.PhysicalName}RefSearchConditionFilter"
            : $"{_refEntry.Item.PhysicalName}RefSearchConditionFilter_{GetRelationHistory(_aggregate, _refEntry).Join("の")}";
        internal override string TsFilterTypeName => _refEntry == _aggregate
            ? $"{_refEntry.Item.PhysicalName}RefSearchConditionFilter"
            : $"{_refEntry.Item.PhysicalName}RefSearchConditionFilter_{GetRelationHistory(_aggregate, _refEntry).Join("の")}";

        protected override bool IsSearchConditionEntry => _refEntry == _aggregate;

        /// <summary>
        /// 型名重複回避のためにフルパスを型名に含める
        /// </summary>
        private static IEnumerable<string> GetRelationHistory(GraphNode<Aggregate> agg, GraphNode<Aggregate> refEntry) {
            foreach (var edge in agg.PathFromEntry().Since(refEntry)) {
                if (edge.IsParentChild() && edge.Source == edge.Terminal) {
                    yield return edge.Initial.As<Aggregate>().Item.PhysicalName;
                } else {
                    yield return edge.RelationName.ToCSharpSafe();
                }
            }
        }

        internal override IEnumerable<RefDescendantSearchCondition> GetChildMembers() {
            foreach (var rm in _aggregate.GetMembers().OfType<AggregateMember.RelationMember>()) {
                // 無限ループ回避
                if (rm.MemberAggregate == _aggregate.Source?.Source.As<Aggregate>()) continue;

                if (!rm.Relation.IsRef()) {
                    yield return new RefDescendantSearchCondition(rm, _refEntry);

                } else {
                    // 参照先の中でさらに他の集約を参照している場合はRefエントリー仕切りなおし
                    yield return new RefDescendantSearchCondition(rm, rm.MemberAggregate);
                }
            }
        }

        protected override IEnumerable<string> GetFullPathForRefRHFRegisterName(AggregateMember.Ref @ref) {
            return @ref.GetFullPathAsRefSearchConditionFilter(E_CsTs.TypeScript);
        }


        #region UIコンポーネント
        internal string UiComponentName => $"RefTo{_aggregate.Item.PhysicalName}SearchCondition";
        /// <summary>
        /// 各画面の検索条件欄のこの集約へのref-to部分のコンポーネントをレンダリングします。
        /// </summary>
        internal string RenderUiComponent(CodeRenderingContext ctx) {

            var formUiContext = new FormUIRenderingContext {
                CodeRenderingContext = ctx,
                GetReactHookFormFieldPath = vm => vm.Declared.GetFullPathAsRefSearchConditionFilter(E_CsTs.TypeScript, _refEntry).Skip(1), // 先頭の "filter." をはじくためにSkip(1)
                Register = "registerEx2",
                RenderReadOnlyStatement = vm => string.Empty, // 検索条件欄の項目が読み取り専用になることはない
                RenderErrorMessage = vm => throw new InvalidOperationException("検索条件欄では項目ごとにエラーメッセージを表示するという概念が無い"),
            };
            var rootNode = new VForm2.IndentNode(new VForm2.JSXElementLabel("props.displayName"));
            BuildVForm(this, rootNode);

            return $$"""
                /**
                 * {{_aggregate.Item.DisplayName}}の検索条件のUIコンポーネント。
                 * VFrom2のItemやIndentとしてレンダリングされます。
                 * **【注意】このコンポーネントをAutoColumnに包むかどうかはnijo.xml側で制御する必要があります**
                 */
                export const {{UiComponentName}} = <
                  /** react hook form が管理しているデータの型。このコンポーネント内部ではなく画面全体の型。 */
                  TFieldValues extends ReactHookForm.FieldValues = ReactHookForm.FieldValues,
                  /** react hook form が管理しているデータの型の各プロパティへの名前。 */
                  TFieldName extends ReactHookForm.FieldPath<TFieldValues> = ReactHookForm.FieldPath<TFieldValues>
                >(props: {
                  displayName: string
                  name: ReactHookForm.PathValue<TFieldValues, TFieldName> extends (Types.{{TsFilterTypeName}} | undefined) ? TFieldName : never
                  registerEx: Util.UseFormExRegisterEx<TFieldValues>
                }) => {
                  // React hook form のメンバーパスがこのコンポーネントの外（呼ぶ側）とこのコンポーネント内部で分断されるが、
                  // そのどちらでもTypeScriptの型検査が効くようにするために内外のパスをつなげる関数
                  const getPath = (path: ReactHookForm.FieldPath<Types.{{TsFilterTypeName}}>): TFieldName => `${props.name}.${path}` as TFieldName
                  const registerEx2 = <P extends ReactHookForm.FieldPath<Types.{{TsFilterTypeName}}>>(path: P) => {
                    // onChangeの型がうまく推論されないので明示的にキャストしている
                    return props.registerEx(getPath(path)) as unknown as Util.RegisterExReturns<Types.{{TsFilterTypeName}}, P>
                  }

                  const { CustomUiComponent } = useCustomizerContext()

                  return (
                    {{WithIndent(rootNode.Render(ctx), "    ")}}
                  )
                }
                """;

            void BuildVForm(SearchCondition refSearchCondition, VForm2 section) {

                /// <see cref="SearchCondition.RenderVForm2"/> とロジックを合わせる

                var renderedMembers = refSearchCondition.GetOwnMembers().Select(m => new {
                    MemberInfo = (AggregateMember.AggregateMemberBase)m.Member,
                    m.DisplayName,
                    Descendant = (DescendantSearchCondition?)null,
                }).Concat(refSearchCondition.GetChildMembers().Select(m => new {
                    MemberInfo = (AggregateMember.AggregateMemberBase)m.MemberInfo,
                    m.DisplayName,
                    Descendant = (DescendantSearchCondition?)m,
                }));

                foreach (var member in renderedMembers.OrderBy(m => m.MemberInfo.Order)) {
                    if (member.MemberInfo is AggregateMember.ValueMember vm) {
                        if (vm.Options.InvisibleInGui) continue; // 非表示項目

                        if (vm.Options.SearchConditionCustomUiComponentName == null) {
                            // 既定の検索条件コンポーネント
                            var body = vm.Options.MemberType.RenderSearchConditionVFormBody(vm, formUiContext);
                            section.Append(new VForm2.ItemNode(new VForm2.StringLabel(member.DisplayName), false, body));

                        } else {
                            // カスタマイズ検索条件コンポーネント
                            var fullpath = formUiContext.GetReactHookFormFieldPath(vm);
                            var body = $$"""
                                <{{AutoGeneratedCustomizer.CUSTOM_UI_COMPONENT}}.{{vm.Options.SearchConditionCustomUiComponentName}} {...{{formUiContext.Register}}(`{{fullpath.Join(".")}}`)} readOnly={false} />
                                """;
                            section.Append(new VForm2.ItemNode(new VForm2.StringLabel(member.DisplayName), false, body));
                        }

                    } else if (member.MemberInfo is AggregateMember.Ref @ref) {
                        var fullpath = GetFullPathForRefRHFRegisterName(@ref).Skip(1); // 先頭の "filter." をはじくためにSkip(1)

                        if (@ref.SearchConditionCustomUiComponentName == null) {
                            // 参照先ref-to既定の検索条件コンポーネント
                            var sc = new RefSearchCondition(@ref.RefTo, _refEntry);
                            var componentName = $"{RefToFile.GetImportAlias(@ref.RefTo)}.{sc.UiComponentName}";
                            var body = $$"""
                               <{{componentName}}
                                 displayName="{{@ref.DisplayName.Replace("\"", "&quot;")}}"
                                 name={getPath(`{{fullpath.Join(".")}}`) as Extract<Parameters<typeof {{componentName}}>['0']['name'], never>}
                                 registerEx={props.registerEx}
                               />
                               """;
                            section.Append(new VForm2.UnknownNode(body, true));

                        } else {
                            // 参照先ref-toカスタマイズ検索条件コンポーネント
                            var body = $$"""
                                <{{AutoGeneratedCustomizer.CUSTOM_UI_COMPONENT}}.{{@ref.SearchConditionCustomUiComponentName}} {...{{formUiContext.Register}}(`{{fullpath.Join(".")}}`)} readOnly={false} />
                                """;
                            section.Append(new VForm2.ItemNode(new VForm2.StringLabel(member.DisplayName), false, body));
                        }

                    } else if (member.MemberInfo is AggregateMember.RelationMember rm) {
                        // 入れ子コンポーネント
                        var childSection = new VForm2.IndentNode(new VForm2.StringLabel(member.DisplayName));
                        section.Append(childSection);
                        BuildVForm(member.Descendant!, childSection);

                    } else {
                        throw new NotImplementedException();
                    }
                }
            }
        }
        internal string RenderCustomizersDeclaring() {
            return $$"""
                /**
                 * {{_aggregate.Item.DisplayName}}の検索条件のUIコンポーネント。
                 * VFrom2のItemやIndentとしてレンダリングされます。
                 * **【注意】このコンポーネントをAutoColumnに包むかどうかはnijo.xml側で制御する必要があります**
                 */
                {{UiComponentName}}?: <
                  /** react hook form が管理しているデータの型。このコンポーネント内部ではなく画面全体の型。 */
                  TFieldValues extends ReactHookForm.FieldValues = ReactHookForm.FieldValues,
                  /** react hook form が管理しているデータの型の各プロパティへの名前。 */
                  TFieldName extends ReactHookForm.FieldPath<TFieldValues> = ReactHookForm.FieldPath<TFieldValues>
                >(props: {
                  displayName: string
                  name: ReactHookForm.PathValue<TFieldValues, TFieldName> extends (AggregateType.{{TsFilterTypeName}} | undefined) ? TFieldName : never
                  registerEx: Util.UseFormExRegisterEx<TFieldValues>
                }) => React.ReactNode
                """;
        }
        #endregion UIコンポーネント


        internal const string PARENT = "PARENT";

        /// <summary>
        /// <see cref="RefSearchCondition"/> と <see cref="DescendantSearchCondition"/> の両方の性質を併せ持つ。
        /// Parentも存在しうるので厳密にはDescendantという名称は正しくない。
        /// </summary>
        internal class RefDescendantSearchCondition : DescendantSearchCondition {
            internal RefDescendantSearchCondition(AggregateMember.RelationMember relationMember, GraphNode<Aggregate> refEntry) : base(relationMember) {
                _asRSC = new RefSearchCondition(relationMember.MemberAggregate, refEntry);
            }

            /// <summary>
            /// このクラスにおける <see cref="DescendantSearchCondition"/> と異なる部分のロジックは
            /// <see cref="RefSearchCondition"/> とまったく同じため、そのロジックを流用する
            /// </summary>
            private readonly RefSearchCondition _asRSC;

            internal override string CsClassName => _asRSC.CsClassName;
            internal override string TsTypeName => _asRSC.TsTypeName;
            internal override string CsFilterClassName => _asRSC.CsFilterClassName;
            internal override string TsFilterTypeName => _asRSC.TsFilterTypeName;
            protected override bool IsSearchConditionEntry => _asRSC.IsSearchConditionEntry;
            internal override IEnumerable<RefDescendantSearchCondition> GetChildMembers() => _asRSC.GetChildMembers();
            protected override IEnumerable<string> GetFullPathForRefRHFRegisterName(AggregateMember.Ref @ref) => _asRSC.GetFullPathForRefRHFRegisterName(@ref);
        }
    }


    internal static partial class GetFullPathExtensions {
        /// <summary>
        /// エントリーからのパスを
        /// <see cref="SearchCondition"/> と
        /// <see cref="RefTo.RefSearchCondition"/> の
        /// インスタンスの型のルールにあわせて返す。
        /// </summary>
        internal static IEnumerable<string> GetFullPathAsRefSearchConditionFilter(this GraphNode<Aggregate> aggregate, E_CsTs csts, GraphNode<Aggregate>? since = null, GraphNode<Aggregate>? until = null) {
            var entry = aggregate.GetEntry();

            var path = aggregate.PathFromEntry();
            if (since != null) path = path.Since(since);
            if (until != null) path = path.Until(until);

            var first = true;
            foreach (var edge in path) {
                if (first) {
                    yield return csts == E_CsTs.CSharp
                        ? RefSearchCondition.FILTER_CS
                        : RefSearchCondition.FILTER_TS;
                    first = false;
                }

                if (edge.Source == edge.Terminal && edge.IsParentChild()) {
                    // 子から親へ向かう経路の場合
                    yield return RefDisplayData.PARENT;
                } else {
                    yield return edge.RelationName;
                }
            }
        }

        /// <inheritdoc cref="GetFullPathAsRefSearchConditionFilter(GraphNode{Aggregate}, E_CsTs, GraphNode{Aggregate}?, GraphNode{Aggregate}?)"/>
        internal static IEnumerable<string> GetFullPathAsRefSearchConditionFilter(this AggregateMember.AggregateMemberBase member, E_CsTs csts, GraphNode<Aggregate>? since = null, GraphNode<Aggregate>? until = null) {
            var fullpath = member.Owner
                .GetFullPathAsRefSearchConditionFilter(csts, since, until)
                .ToArray();
            if (fullpath.Length == 0) {
                yield return csts == E_CsTs.CSharp
                    ? RefSearchCondition.FILTER_CS
                    : RefSearchCondition.FILTER_TS;
            }
            foreach (var path in fullpath) {
                yield return path;
            }
            yield return member.MemberName;
        }
    }
}
