using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.WebClient {
    partial class FormOfAggregateInstance : ITemplate {
        internal FormOfAggregateInstance(GraphNode<Aggregate> aggregate, CodeRenderingContext ctx)
            : this(aggregate.GetInstanceClass().AsEntry(), ctx) { }
        internal FormOfAggregateInstance(GraphNode<AggregateInstance> aggregateInstance, CodeRenderingContext ctx) {
            _ctx = ctx;
            _instance = aggregateInstance;

            PropNameWidth = GetPropNameFlexBasis(_instance
                .GetProperties(ctx.Config)
                .Select(p => p.PropertyName));
        }
        private readonly CodeRenderingContext _ctx;
        private readonly GraphNode<AggregateInstance> _instance;

        private string TypesImport => $"../../{Path.GetFileNameWithoutExtension(new types(_ctx).FileName)}";

        public string FileName => "components.tsx";

        internal IEnumerable<string> EnumerateComponentNames() {
            return _instance
                .EnumerateThisAndDescendants()
                .Select(x => new Component(x).ComponentName);
        }

        private string PropNameWidth { get; }


        #region SCHALAR PROPERTY
        private void RenderSchalarProperty(GraphNode<AggregateInstance> instance, AggregateInstance.SchalarProperty prop) {
            var renderer = new ReactForm(instance, prop);
            foreach (var line in prop.CorrespondingDbColumn.MemberType.RenderUI(renderer)) {
                WriteLine(line);
            }
        }
        private class ReactForm : IGuiFormRenderer {
            internal ReactForm(GraphNode<AggregateInstance> instance, AggregateInstance.SchalarProperty prop) {
                _instance = instance;
                _prop = prop;
            }
            private readonly GraphNode<AggregateInstance> _instance;
            private readonly AggregateInstance.SchalarProperty _prop;

            /// <summary>
            /// Createビュー兼シングルビュー: テキストボックス
            /// </summary>
            public IEnumerable<string> TextBox(bool multiline = false) {
                var name = GetRegisterName(_instance, _prop.PropertyName);
                if (multiline)
                    yield return $"<textarea {{...register(`{name}`)}} className=\"{INPUT_WIDTH}\" readOnly={{pageIsReadOnly}} spellCheck=\"false\" autoComplete=\"off\"></textarea>";
                else
                    yield return $"<input type=\"text\" {{...register(`{name}`)}} className=\"{INPUT_WIDTH}\" readOnly={{pageIsReadOnly}} spellCheck=\"false\" autoComplete=\"off\" />";
            }

            /// <summary>
            /// Createビュー兼シングルビュー: トグル
            /// </summary>
            public IEnumerable<string> Toggle() {
                var name = GetRegisterName(_instance, _prop.PropertyName);
                yield return $"<input type=\"checkbox\" {{...register(`{name}`)}} disabled={{pageIsReadOnly}} />";
            }

            /// <summary>
            /// Createビュー兼シングルビュー: 選択肢（コード自動生成時に要素が確定しているもの）
            /// </summary>
            public IEnumerable<string> Selection() {
                // TODO: enumの値をスキーマから取得する
                var options = new List<KeyValuePair<string, string>>();
                if (!_prop.CorrespondingDbColumn.RequiredAtDB)
                    options.Add(KeyValuePair.Create("", ""));

                // TODO: RegisterNameを使っていない
                yield return $"<select>";
                foreach (var opt in options) {
                    yield return $"  <option selected value=\"{opt.Key}\">";
                    yield return $"    {opt.Value}";
                    yield return $"  </option>";
                }
                yield return $"</select>";
            }
        }
        #endregion SCHALAR PROPERTY


        #region DESCENDANT AGGREGATES
        private void RenderRefAggregateBody(AggregateInstance.RefProperty refProperty) {
            var component = new ComboBox(refProperty.RefTarget.GetCorrespondingAggregate(), _ctx);
            var registerName = GetRegisterName(refProperty.RefTarget);
            component.RenderCaller(this, registerName);
        }
        private void RenderChildrenAggregateBody(AggregateInstance.ChildrenProperty childrenProperty) {
            var component = new Component(childrenProperty.ChildAggregateInstance);
            component.RenderCaller(this);
        }
        private void RenderChildAggregateBody(AggregateInstance.ChildProperty childProperty) {
            var component = new Component(childProperty.ChildAggregateInstance);
            component.RenderCaller(this);
        }
        private void RenderVariationAggregateBody(GraphNode<AggregateInstance> variationAggregateInstance) {
            var component = new Component(variationAggregateInstance);
            component.RenderCaller(this);
        }
        #endregion DESCENDANT AGGREGATES


        internal class Component {
            internal Component(GraphNode<AggregateInstance> instance) {
                AggregateInstance = instance;
            }
            internal GraphNode<AggregateInstance> AggregateInstance { get; }

            internal virtual string ComponentName => $"{AggregateInstance.Item.TypeScriptTypeName}View";
            internal bool IsChildren => AggregateInstance.IsChildrenMember();

            internal virtual IReadOnlyDictionary<GraphEdge, string> GetArguments() {
                // 祖先コンポーネントの中に含まれるChildrenの数だけ、
                // このコンポーネントのその配列中でのインデックスが特定されている必要があるので、引数で渡す
                var ancestors = AggregateInstance
                    .PathFromEntry()
                    .SkipLast(1)
                    .Where(edge => edge.Terminal.IsChildrenMember())
                    .ToArray();

                var dict = new Dictionary<GraphEdge, string>();
                for (int i = 0; i < ancestors.Length; i++) {
                    dict.Add(ancestors[i], $"index_{i}");
                }
                return dict;
            }

            internal string GetUseFieldArrayName() {
                var path = new List<string>();
                var args = GetArguments();
                var ancestors = AggregateInstance.PathFromEntry().ToArray();

                foreach (var ancestor in ancestors) {
                    path.Add(ancestor.RelationName);
                    if (ancestor != ancestors.Last() && ancestor.Terminal.IsChildrenMember()) path.Add($"${{{args[ancestor]}}}");
                }

                return path.Join(".");
            }

            internal void RenderCaller(ITemplate template) {
                var args = GetArguments()
                    .SkipLast(1)
                    .Select(x => $" {x.Value}={{{x.Value}}}")
                    .Join(string.Empty);
                template.WriteLine($"<{ComponentName}{args} />");
            }
        }
        /// <summary>
        /// Childrenはコード自動生成の都合上各要素のコンポーネントと配列のコンポーネントの2個あり、これは前者
        /// </summary>
        private class ComponentOfChildrenListItem : Component {
            internal ComponentOfChildrenListItem(GraphNode<AggregateInstance> instance) : base(instance) { }

            internal override string ComponentName => $"{AggregateInstance.Item.TypeScriptTypeName}ListItemView";

            internal override IReadOnlyDictionary<GraphEdge, string> GetArguments() {
                // 祖先コンポーネントの中に含まれるChildrenの数だけ、
                // このコンポーネントのその配列中でのインデックスが特定されている必要があるので、引数で渡す
                var ancestors = AggregateInstance
                    .PathFromEntry()
                    // .SkipLast(1)
                    .Where(edge => edge.Terminal.IsChildrenMember())
                    .ToArray();

                var dict = new Dictionary<GraphEdge, string>();
                for (int i = 0; i < ancestors.Length; i++) {
                    dict.Add(ancestors[i], $"index_{i}");
                }
                return dict;
            }
        }


        #region STATIC
        internal const string INPUT_WIDTH = "w-80";
        private static string GetRegisterName(GraphNode<AggregateInstance> instance, string? propName = null) {
            var component = new Component(instance);
            var path = component.GetUseFieldArrayName();

            var list = new List<string>();
            if (!string.IsNullOrWhiteSpace(path)) list.Add(path);
            if (instance.IsChildrenMember()) list.Add("${index}");
            if (propName != null) list.Add(propName);

            return list.Join(".");
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
