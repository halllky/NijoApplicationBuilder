using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.WebClient {
    partial class DescencantForms : ITemplate {
        internal DescencantForms(GraphNode<Aggregate> aggregate, CodeRenderingContext ctx)
            : this(aggregate.GetInstanceClass().AsEntry(), ctx) { }
        internal DescencantForms(GraphNode<AggregateInstance> aggregateInstance, CodeRenderingContext ctx) {
            _ctx = ctx;
            _aggregateInstance = aggregateInstance;
        }
        private readonly CodeRenderingContext _ctx;
        private readonly GraphNode<AggregateInstance> _aggregateInstance;

        private string TypesImport => $"../../{Path.GetFileNameWithoutExtension(new types(_ctx).FileName)}";

        public string FileName => "components.tsx";

        private IEnumerable<Component> EnumerateDescendantComponents() {
            return _aggregateInstance
                .EnumerateDescendants()
                .Select(desc => new Component(desc));
        }
        internal IEnumerable<string> EnumerateComponentNames() {
            return EnumerateDescendantComponents().Select(c => c.ComponentName);
        }

        private void RenderBody(Component desc, string indent) {
            var body = new AggregateInstanceFormBody(desc.AggregateInstance, _ctx);
            body.PushIndent(indent);
            WriteLine(body.TransformText());
        }


        internal class Component {
            internal Component(GraphNode<AggregateInstance> instance) {
                AggregateInstance = instance;
            }
            internal GraphNode<AggregateInstance> AggregateInstance { get; }

            internal string ComponentName => $"{AggregateInstance.Item.TypeScriptTypeName}View";
            internal bool IsChildren => AggregateInstance.GetParent()?.IsChildren() == true;

            internal IReadOnlyDictionary<GraphEdge<AggregateInstance>, string> GetArguments() {
                // 祖先コンポーネントの中に含まれるChildrenの数だけ、
                // このコンポーネントのその配列中でのインデックスが特定されている必要があるので、引数で渡す
                var ancestors = AggregateInstance
                    .EnumerateAncestors()
                    .SkipLast(1)
                    .Where(a => a.IsChildren() == true)
                    .ToArray();

                var dict = new Dictionary<GraphEdge<AggregateInstance>, string>();
                for (int i = 0; i < ancestors.Length; i++) {
                    dict.Add(ancestors[i], $"index_{i}");
                }
                return dict;
            }

            internal string GetUseFieldArrayName() {
                var path = new List<string>();
                var args = GetArguments();
                var ancestors = AggregateInstance.EnumerateAncestors().ToArray();

                foreach (var ancestor in ancestors) {
                    path.Add(ancestor.RelationName);
                    if (ancestor != ancestors.Last() && ancestor.IsChildren()) path.Add($"${{{args[ancestor]}}}");
                }

                return path.Join(".");
            }

            internal void RenderCaller(ITemplate template) {
                var args = GetArguments()
                    .SkipLast(1)
                    .Select(x => $" {x.Value}={{{x.Value}}}");
                template.WriteLine($"<{ComponentName}{args.Join(string.Empty)} />");
            }
        }
    }
}
