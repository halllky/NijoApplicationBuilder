using Nijo.Core;
using Nijo.Parts.WebClient;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.ReadModel2Features {
    /// <summary>
    /// 詳細画面の一部を構成する、集約1個と対応するReactコンポーネント。
    /// </summary>
    internal class SingleViewAggregateComponent {

        internal SingleViewAggregateComponent(DataClassForDisplay dataClass) {
            _dataClass = dataClass;
        }

        private readonly DataClassForDisplay _dataClass;

        private string GetComponentName() {
            if (_dataClass.Aggregate.IsOutOfEntryTree()) {
                var refEntry = _dataClass.Aggregate.GetRefEntryEdge().RelationName;
                return $"{_dataClass.Aggregate.Item.PhysicalName}ViewOf{refEntry}Reference";

            } else {
                return $"{_dataClass.Aggregate.Item.PhysicalName}View";
            }
        }

        /// <summary>
        /// このコンポーネントが受け取る引数の名前のリスト。
        /// 祖先コンポーネントの中に含まれるChildrenの数だけ、
        /// このコンポーネントのその配列中でのインデックスが特定されている必要があるので、それを引数で受け取る。
        /// </summary>
        private IReadOnlyList<string> GetArguments() {
            return _dataClass.Aggregate
                .PathFromEntry()
                .Where(edge => edge.Terminal != _dataClass.Aggregate
                            && edge.Terminal.As<Aggregate>().IsChildrenMember())
                .Select((_, i) => $"index_{i}")
                .ToArray();
        }

        internal string RenderCaller() {
            return $$"""
                <{{GetComponentName()}} />
                """;
        }

        /// <summary>
        /// 子孫要素のコンポーネントも含めてレンダリングします。
        /// </summary>
        internal string RenderDeclaring(ReactPageRenderingContext context) {

        }

        /// <summary>
        /// この集約のフォームを組み立てます。
        /// </summary>
        private void BuildVForm2(ReactPageRenderingContext context, VerticalFormBuilder formBuilder) {
            if ()
        }
        /// <summary>
        /// 通常のフォームとしてレンダリングします。
        /// </summary>
        private VerticalFormBuilder BuildVerticalForm(ReactPageRenderingContext context) {
            var formBuilder = new VerticalFormBuilder();

            foreach (var member in _dataClass.GetOwnMembers()) {
                if (member is AggregateMember.ValueMember vm) {
                    formBuilder.AddItem(
                        vm.Options.MemberType is Core.AggregateMemberTypes.Sentence,
                        member.MemberName,
                        E_VForm2LabelType.String,
                        vm.Options.MemberType.RenderVFormBody(vm, context));

                } else if (member is AggregateMember.Ref @ref) {
                    void BuildRecursively(RefTo.RefSearchResult refTarget, VerticalFormSection section) {
                        foreach (var refMember in refTarget.GetOwnMembers()) {
                            if (refMember is AggregateMember.ValueMember vm2) {
                                formBuilder.AddItem(
                                    vm2.Options.MemberType is Core.AggregateMemberTypes.Sentence,
                                    member.MemberName,
                                    E_VForm2LabelType.String,
                                    vm2.Options.MemberType.RenderVFormBody(vm2, context));

                            } else if (refMember is AggregateMember.RelationMember rel) {
                                var relRefTarget = new RefTo.RefSearchResult(rel.MemberAggregate, @ref.RefTo);
                                var relSection = formBuilder.AddSection(rel.MemberName, E_VForm2LabelType.String);
                                BuildRecursively(relRefTarget, relSection);
                            }
                        }
                    }
                    var refTarget = new RefTo.RefSearchResult(@ref.RefTo, @ref.RefTo);
                    var section = new VerticalFormSection(@ref.MemberName, E_VForm2LabelType.String);
                    BuildRecursively(refTarget, section);
                }
            }
            foreach (var childDataClass in _dataClass.GetChildMembers()) {
                var childComponent = new SingleViewAggregateComponent(childDataClass);
                formBuilder.AddUnknownParts(childComponent.RenderCaller());
            }

            return formBuilder;
        }
        /// <summary>
        /// 表としてレンダリングします。
        /// </summary>
        private void BuildDataTable(ReactPageRenderingContext context, VerticalFormBuilder formBuilder) {

        }
    }
}
