using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.WebClient {
    partial class AggregateInstanceFormBody : ITemplate {
        internal AggregateInstanceFormBody(GraphNode<AggregateInstance> instance, CodeRenderingContext ctx) {
            _ctx = ctx;
            _instance = instance;
            _dbEntity = instance.GetDbEntity();

            PropNameWidth = GetPropNameFlexBasis(instance
                .GetProperties(ctx.Config)
                .Select(p => p.PropertyName));
        }

        private readonly CodeRenderingContext _ctx;
        private readonly GraphNode<AggregateInstance> _instance;
        private readonly GraphNode<EFCoreEntity> _dbEntity;

        private string PropNameWidth { get; }
        public string FileName => throw new NotImplementedException("このテンプレートは他のテンプレートの一部としてレンダリングされるためファイル名はありません。");

        private string GetRegisterName(string propName) {
            var component = new DescencantForms.Component(_instance);
            var path = component.GetUseFieldArrayName();

            var list = new List<string>();
            if (!string.IsNullOrWhiteSpace(path)) list.Add(path);
            if (_instance.GetParent()?.IsChildren() == true) list.Add("${index}");
            list.Add(propName);

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

        #region SCHALAR PROPERTY
        private void RenderSchalarProperty(AggregateInstance.SchalarProperty prop) {
            var renderer = new FormRenderer(prop, this);
            foreach (var line in prop.CorrespondingDbColumn.MemberType.RenderUI(renderer)) {
                WriteLine(line);
            }
        }
        private class FormRenderer : IGuiFormRenderer {
            internal FormRenderer(AggregateInstance.SchalarProperty prop, AggregateInstanceFormBody owner) {
                _prop = prop;
                _owner = owner;
            }
            private readonly AggregateInstance.SchalarProperty _prop;
            private readonly AggregateInstanceFormBody _owner;

            private const string INPUT_WIDTH = "w-80";

            /// <summary>
            /// Createビュー兼シングルビュー: テキストボックス
            /// </summary>
            public IEnumerable<string> TextBox(bool multiline = false) {
                var name = _owner.GetRegisterName(_prop.PropertyName);
                if (multiline)
                    yield return $"<textarea {{...register(`{name}`)}} className=\"{INPUT_WIDTH}\"></textarea>";
                else
                    yield return $"<input type=\"text\" {{...register(`{name}`)}} className=\"{INPUT_WIDTH}\" />";
            }

            /// <summary>
            /// Createビュー兼シングルビュー: トグル
            /// </summary>
            public IEnumerable<string> Toggle() {
                var name = _owner.GetRegisterName(_prop.PropertyName);
                yield return $"<input type=\"checkbox\" {{...register(`{name}`)}} />";
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
            var registerName = GetRegisterName(refProperty.PropertyName);
            component.RenderCaller(this, registerName);
        }
        private void RenderChildrenAggregateBody(AggregateInstance.ChildrenProperty childrenProperty) {
            var component = new DescencantForms.Component(childrenProperty.ChildAggregateInstance);
            component.RenderCaller(this);
        }
        private void RenderChildAggregateBody(AggregateInstance.ChildProperty childProperty) {
            var component = new DescencantForms.Component(childProperty.ChildAggregateInstance);
            component.RenderCaller(this);
        }
        private void RenderVariationAggregateBody(GraphNode<AggregateInstance> variationAggregateInstance) {
            var component = new DescencantForms.Component(variationAggregateInstance);
            component.RenderCaller(this);
        }

        internal class VariationSwitchState {
            internal VariationSwitchState(VariationGroup<AggregateInstance> variationGroup) {
                _variationGroup = variationGroup;
            }
            private readonly VariationGroup<AggregateInstance> _variationGroup;

            internal string StateName {
                get {
                    var name = _variationGroup.Initial
                        .PathFromEntry()
                        .Select(edge => edge.RelationName)
                        .Concat(new[] { _variationGroup.GroupName })
                        .Join("_")
                        .ToCSharpSafe();
                    return $"selected{name}";
                }
            }
            internal string DispatcherName {
                get {
                    var name = _variationGroup.Initial
                        .PathFromEntry()
                        .Select(edge => edge.RelationName)
                        .Concat(new[] { _variationGroup.GroupName })
                        .Join("_")
                        .ToCSharpSafe();
                    return $"change{name}";
                }
            }

            internal void RenderHook(ITemplate template) {
                template.WriteLine($"const [{StateName}, {DispatcherName}] = useState(0)");
            }
        }
        #endregion DESCENDANT AGGREGATES
    }
}
