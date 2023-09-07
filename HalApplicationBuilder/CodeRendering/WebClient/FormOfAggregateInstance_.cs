using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.WebClient {
    partial class FormOfAggregateInstance : TemplateBase {
        internal FormOfAggregateInstance(GraphNode<Aggregate> aggregate, CodeRenderingContext ctx)
            : this(aggregate.GetInstanceClass().AsEntry(), ctx) { }
        internal FormOfAggregateInstance(GraphNode<IAggregateInstance> aggregateInstance, CodeRenderingContext ctx) {
            _ctx = ctx;
            _instance = aggregateInstance;

            PropNameWidth = GetPropNameFlexBasis(_instance
                .GetProperties(ctx.Config)
                .Select(p => p.PropertyName));
        }
        private readonly CodeRenderingContext _ctx;
        private readonly GraphNode<IAggregateInstance> _instance;

        private string TypesImport => $"../../{Path.GetFileNameWithoutExtension(new types(_ctx).FileName)}";

        public override string FileName => "components.tsx";

        internal IEnumerable<string> EnumerateComponentNames() {
            return _instance
                .EnumerateThisAndDescendants()
                .Select(x => new Component(x).ComponentName);
        }

        private string PropNameWidth { get; }


        #region SCHALAR PROPERTY
        private string RenderSchalarProperty(GraphNode<IAggregateInstance> instance, IAggregateInstance.SchalarProperty prop, string indent) {
            var renderer = new ReactForm(instance, prop);
            return TemplateTextHelper.WithIndent(prop.CorrespondingDbColumn.MemberType.RenderUI(renderer), indent);
        }
        private class ReactForm : IGuiFormRenderer {
            internal ReactForm(GraphNode<IAggregateInstance> instance, IAggregateInstance.SchalarProperty prop) {
                _instance = instance;
                _prop = prop;
            }
            private readonly GraphNode<IAggregateInstance> _instance;
            private readonly IAggregateInstance.SchalarProperty _prop;

            /// <summary>
            /// Createビュー兼シングルビュー: テキストボックス
            /// </summary>
            public string TextBox(bool multiline = false) {
                var name = GetRegisterName(_instance, _prop).Value;
                if (multiline)
                    return $$"""
                        <textarea
                            {...register(`{{name}}`)}
                            className="{{INPUT_WIDTH}}"
                            readOnly={pageIsReadOnly}
                            spellCheck="false"
                            autoComplete="off">
                        </textarea>
                        """;
                else
                    return $$"""
                        <input
                            type="text"
                            {...register(`{{name}}`)}
                            className="{{INPUT_WIDTH}}"
                            readOnly={pageIsReadOnly}
                            spellCheck="false"
                            autoComplete="off"
                        />
                        """;
            }

            /// <summary>
            /// Createビュー兼シングルビュー: トグル
            /// </summary>
            public string Toggle() {
                var name = GetRegisterName(_instance, _prop).Value;
                return $$"""
                    <input type="checkbox" {...register(`{{name}}`)} disabled={pageIsReadOnly} />
                    """;
            }

            /// <summary>
            /// Createビュー兼シングルビュー: 選択肢（コード自動生成時に要素が確定しているもの）
            /// </summary>
            public string Selection(IEnumerable<KeyValuePair<string, string>> options) {
                return $$"""
                    <select className="border" {...register(`{{GetRegisterName(_instance)}}`)}>
                    {{options.SelectTextTemplate(option => $$"""
                        <option value="{{option.Key}}">
                        {{option.Value}}
                        </option>
                    """)}}
                    </select>
                    """;
            }
        }
        #endregion SCHALAR PROPERTY


        internal class Component {
            internal Component(GraphNode<IAggregateInstance> instance) {
                AggregateInstance = instance;
            }
            internal GraphNode<IAggregateInstance> AggregateInstance { get; }

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
        /// <summary>
        /// Childrenはコード自動生成の都合上各要素のコンポーネントと配列のコンポーネントの2個あり、これは前者
        /// </summary>
        private class ComponentOfChildrenListItem : Component {
            internal ComponentOfChildrenListItem(GraphNode<IAggregateInstance> instance) : base(instance) { }

            internal override string ComponentName => $"{AggregateInstance.Item.TypeScriptTypeName}ListItemView";
        }


        #region STATIC
        internal const string INPUT_WIDTH = "w-80";
        private static RegisterName GetRegisterName(GraphNode<IAggregateInstance> instance, IAggregateInstance.Property? prop = null) {
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
            internal required IAggregateInstance.Property Property { get; init; }
            public string Name => Property.PropertyName;
        }

        internal static IReadOnlyDictionary<GraphEdge, string> GetArguments(GraphNode<IAggregateInstance> instance) {
            // 祖先コンポーネントの中に含まれるChildrenの数だけ、
            // このコンポーネントのその配列中でのインデックスが特定されている必要があるので、引数で渡す
            var args = GetRegisterName(instance).Path
                .Where(path => path is ArrayIndex)
                .Cast<ArrayIndex>()
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
