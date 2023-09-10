using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.InstanceHandling {
    partial class FormOfAggregateInstance : TemplateBase {
        internal FormOfAggregateInstance(GraphNode<Aggregate> aggregateInstance, CodeRenderingContext ctx) {
            _ctx = ctx;
            _instance = aggregateInstance;

            PropNameWidth = GetPropNameFlexBasis(_instance
                .GetMembers()
                .Select(p => p.PropertyName));
        }
        private readonly CodeRenderingContext _ctx;
        private readonly GraphNode<Aggregate> _instance;

        public override string FileName => "components.tsx";

        private string PropNameWidth { get; }

        internal class Component {
            internal Component(GraphNode<Aggregate> instance) {
                AggregateInstance = instance;
            }
            internal GraphNode<Aggregate> AggregateInstance { get; }

            internal virtual string ComponentName => $"{AggregateInstance.Item.TypeScriptTypeName}View";
            internal bool IsChildren => AggregateInstance.IsChildrenMember();

            internal string GetUseFieldArrayName() {
                var path = new List<string>();
                var args = GetArguments(AggregateInstance);
                var ancestors = AggregateInstance.PathFromEntry().ToArray();

                foreach (var ancestor in ancestors) {
                    path.Add(ancestor.RelationName);
                    if (ancestor != ancestors.Last() && ancestor.Terminal.IsChildrenMember()) path.Add($"${{{args[ancestor]}}}");
                }

                return path.Join(".");
            }

            internal string RenderCaller() {
                var args = GetArguments(AggregateInstance)
                    .SkipLast(1)
                    .Select(x => $" {x.Value}={{{x.Value}}}")
                    .Join(string.Empty);
                return $"<{ComponentName}{args} />";
            }
        }


        #region STATIC
        internal const string INPUT_WIDTH = "w-80";
        private static RegisterName GetRegisterName(GraphNode<Aggregate> instance, AggregateMember.AggregateMemberBase? prop = null) {
            var path = new List<IRegistrationPath>();
            foreach (var edge in instance.PathFromEntry()) {
                path.Add(new RelatedAggregate { Aggregate = edge.Terminal });
                if (edge.Terminal.IsChildrenMember()) path.Add(new ArrayIndex { Aggregate = edge.Terminal });
            }
            if (prop != null) path.Add(new LastProperty { Property = prop });
            return new RegisterName { Path = path };
        }
        private class RegisterName {
            internal required IList<IRegistrationPath> Path { get; init; }
            internal string Value => Path
                .Select(p => p is ArrayIndex arrayIndex ? ("${" + p.Name + "}") : p.Name)
                .Join(".");
        }
        private interface IRegistrationPath {
            internal string Name { get; }
        }
        private class RelatedAggregate : IRegistrationPath {
            internal required GraphNode Aggregate { get; init; }
            public string Name => Aggregate.Source!.RelationName;
        }
        private class ArrayIndex : IRegistrationPath {
            internal required GraphNode Aggregate { get; init; }
            public string Name => Aggregate
                .PathFromEntry()
                .Where(edge => edge.Terminal.IsChildrenMember())
                .Select((_, i) => $"index_{i}")
                .Last();
        }
        private class LastProperty : IRegistrationPath {
            internal required AggregateMember.AggregateMemberBase Property { get; init; }
            public string Name => Property.PropertyName;
        }

        internal static IReadOnlyDictionary<GraphEdge, string> GetArguments(GraphNode<Aggregate> instance) {
            // 祖先コンポーネントの中に含まれるChildrenの数だけ、
            // このコンポーネントのその配列中でのインデックスが特定されている必要があるので、引数で渡す
            var args = GetRegisterName(instance).Path
                .OfType<ArrayIndex>()
                .ToDictionary(
                    arrayIndex => arrayIndex.Aggregate.GetParent()!,
                    arrayIndex => arrayIndex.Name);
            return args;
        }

        internal static string GetPropNameFlexBasis(IEnumerable<string> propNames) {
            var maxCharWidth = propNames
                .Select(prop => prop.CalculateCharacterWidth())
                .DefaultIfEmpty()
                .Max();

            var a = (maxCharWidth + 1) / 2; // tailwindのbasisはrem基準（全角文字n文字）のため偶数にそろえる
            var b = a + 1; // ちょっと横幅に余裕をもたせるための +1
            var c = Math.Min(96, b * 4); // tailwindでは basis-96 が最大なので

            return $"basis-{c}";
        }
        #endregion STATIC
    }
}
