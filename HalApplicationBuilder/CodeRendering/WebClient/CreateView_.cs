using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.WebClient {
    partial class CreateView : ITemplate {
        internal CreateView(GraphNode<Aggregate> aggregate, CodeRenderingContext ctx) {
            _ctx = ctx;
            _aggregate = aggregate;
            _dbEntity = aggregate.GetDbEntity().AsEntry();
            _instance = aggregate.GetInstanceClass().AsEntry();

            PropNameWidth = GetPropNameFlexBasis(_instance.GetProperties(ctx.Config));
        }

        private readonly CodeRenderingContext _ctx;
        private readonly GraphNode<Aggregate> _aggregate;
        private readonly GraphNode<EFCoreEntity> _dbEntity;
        private readonly GraphNode<AggregateInstance> _instance;

        public string FileName => "new.tsx";
        internal string Url => $"/{_aggregate.Item.UniqueId}/new";
        internal string Route => $"/{_aggregate.Item.UniqueId}/new";

        private string GetMultiViewUrl() => new MultiView(_aggregate, _ctx).Url;
        private string GetSingleViewUrl() => new SingleView(_aggregate, _ctx).Url;
        private string GetCreateCommandApi() => new AggFile.Controller(_aggregate).CreateCommandApi;

        private string PropNameWidth { get; }

        private IEnumerable<string> RenderForm(AggregateInstance.SchalarProperty prop) {
            var renderer = new FormRenderer(prop, _dbEntity);
            foreach (var line in prop.CorrespondingDbColumn.MemberType.RenderUI(renderer)) {
                yield return line;
            }
        }


        #region Formレンダリング
        internal class FormRenderer : IGuiFormRenderer {

            internal FormRenderer(AggregateInstance.SchalarProperty prop, GraphNode<EFCoreEntity> owner) {
                _prop = prop;
                _owner = owner;
            }
            private readonly AggregateInstance.SchalarProperty _prop;
            private readonly GraphNode<EFCoreEntity> _owner;

            private string GetRegisterName(string? inner = null) {
                var list = _owner
                    .PathFromEntry()
                    .Select(path => path.RelationName)
                    .ToList();
                list.Add(_prop.PropertyName);
                if (!string.IsNullOrEmpty(inner)) list.Add(inner);
                return list.Join(".");
            }

            /// <summary>
            /// Createビュー兼シングルビュー: テキストボックス
            /// </summary>
            public IEnumerable<string> TextBox(bool multiline = false) {
                var name = GetRegisterName();
                if (multiline)
                    yield return $"<textarea {{...register('{name}')}}></textarea>";
                else
                    yield return $"<input type=\"text\" {{...register('{name}')}} />";
            }

            /// <summary>
            /// Createビュー兼シングルビュー: トグル
            /// </summary>
            public IEnumerable<string> Toggle() {
                var name = GetRegisterName();
                yield return $"<input type=\"checkbox\" {{...register('{name}')}} />";
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
                    yield return $"    <option selected value=\"{opt.Key}\">";
                    yield return $"        {opt.Value}";
                    yield return $"    </option>";
                }
                yield return $"</select>";
            }
        }
        internal static string GetPropNameFlexBasis(IEnumerable<AggregateInstance.Property> props) {
            var maxCharWidth = props
                .Select(prop => prop.PropertyName.CalculateCharacterWidth())
                .DefaultIfEmpty()
                .Max();

            var a = (maxCharWidth + 1) / 2; // tailwindのbasisはrem基準（全角文字n文字）のため偶数にそろえる
            var b = a + 1; // ちょっと横幅に余裕をもたせるための +1
            var c = Math.Min(96, b * 4); // tailwindでは basis-96 が最大なので

            return $"basis-{c}";
        }
        #endregion Formレンダリング
    }
}
