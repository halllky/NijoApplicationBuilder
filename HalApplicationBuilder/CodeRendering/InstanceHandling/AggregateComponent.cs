using HalApplicationBuilder.Core;
using HalApplicationBuilder.CodeRendering.WebClient;
using HalApplicationBuilder.DotnetEx;
using static HalApplicationBuilder.CodeRendering.TemplateTextHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.InstanceHandling {

    internal class AggregateComponent {
        internal AggregateComponent(GraphNode<Aggregate> aggregate, SingleView.E_Type type) {
            _aggregate = aggregate;
            _mode = type;
        }

        private readonly GraphNode<Aggregate> _aggregate;
        private readonly SingleView.E_Type _mode;

        private string ComponentName => $"{_aggregate.Item.TypeScriptTypeName}View";

        internal string RenderCaller() {
            var args = GetArguments()
                .Select(arg => $" {arg}={{{arg}}}")
                .Join(string.Empty);
            return $"<{ComponentName}{args} />";
        }

        internal string Render() {
            var layout = new AggregateDetail(_aggregate).GetAggregateDetailMembers().SelectTextTemplate(prop => prop switch {
                AggregateMember.Schalar x => RenderProperty(x),
                AggregateMember.Ref x => RenderProperty(x),
                AggregateMember.Child x => RenderProperty(x),
                AggregateMember.VariationItem x => RenderProperty(x),
                AggregateMember.Variation x => RenderProperty(x),
                AggregateMember.Children x => RenderProperty(x),
                _ => throw new NotImplementedException(),
            });

            if (!_aggregate.IsChildrenMember()) {

                // Childrenでない集約のレンダリング
                var args = GetArguments().ToArray();
                return $$"""
                    const {{ComponentName}} = ({ {{args.Join(", ")}} }: {
                    {{args.SelectTextTemplate(arg => $$"""
                      {{arg}}: number
                    """)}}
                    }) => {
                      const [{ },] = usePageContext()
                      const { register, watch, getValues } = useFormContext<AggregateType.{{_aggregate.GetRoot().Item.TypeScriptTypeName}}>()
                      const item = getValues({{GetRegisterName()}})
                    
                      return (
                        <>
                          {{WithIndent(layout, "      ")}}
                        </>
                      )
                    }
                    """;
            } else {
                // Childrenのレンダリング
                var args = GetArguments().ToArray();
                var loopVar = $"index_{args.Length}";
                var createNewChildrenItem = new AggregateInstanceInitializerFunction(_aggregate).FunctionName;

                return $$"""
                    const {{ComponentName}} = ({ {{args.Join(", ")}} }: {
                    {{args.SelectTextTemplate(arg => $$"""
                      {{arg}}: number
                    """)}}
                    }) => {
                      const [{ },] = usePageContext()
                      const { register, watch, control } = useFormContext<AggregateType.{{_aggregate.GetRoot().Item.TypeScriptTypeName}}>()
                      const { fields, append, remove } = useFieldArray({
                        control,
                        name: {{GetRegisterName()}},
                      })
                      const onAdd = useCallback((e: React.MouseEvent) => {
                        append(AggregateType.{{createNewChildrenItem}}())
                        e.preventDefault()
                      }, [append])
                      const onRemove = useCallback((index: number) => {
                        return (e: React.MouseEvent) => {
                          remove(index)
                          e.preventDefault()
                        }
                      }, [remove])
                    
                      return (
                        <>
                          {fields.map((item, {{loopVar}}) => (
                            <div key={item.{{AggregateDetail.OBJECT_ID}}} className="flex flex-col space-y-1 p-1 border border-neutral-400">
                              {{WithIndent(layout, "          ")}}
                    {{If(_mode != SingleView.E_Type.View, () => $$"""
                              <Components.IconButton
                                underline
                                icon={XMarkIcon}
                                onClick={onRemove({{loopVar}})}
                                className="self-start">
                                削除
                              </Components.IconButton>
                    """)}}
                            </div>
                          ))}
                    {{If(_mode != SingleView.E_Type.View, () => $$"""
                          <Components.IconButton
                            underline
                            icon={PlusIcon}
                            onClick={onAdd}
                            className="self-start">
                            追加
                          </Components.IconButton>
                    """)}}
                        </>
                      )
                    }
                    """;
            }
        }

        #region SCHALAR PROPERTY
        private string RenderProperty(AggregateMember.Schalar schalar) {
            if (schalar.Options.InvisibleInGui) {
                return $$"""
                    <input type="hidden" {...register({{GetRegisterName(schalar)}})} />
                    """;

            } else {
                var renderer = new ReactForm(this, schalar, _mode);
                return $$"""
                    <div className="flex">
                      <div className="{{PropNameWidth}}">
                        <span className="text-sm select-none opacity-80">
                          {{schalar.MemberName}}
                        </span>
                      </div>
                      <div className="flex-1">
                        {{WithIndent(schalar.Options.MemberType.RenderUI(renderer), "    ")}}
                      </div>
                    </div>
                    """;
            }
        }
        private class ReactForm : IGuiFormRenderer {
            internal ReactForm(AggregateComponent component, AggregateMember.Schalar prop, SingleView.E_Type mode) {
                _component = component;
                _prop = prop;
                _mode = mode;
            }
            private readonly AggregateComponent _component;
            private readonly AggregateMember.Schalar _prop;
            private readonly SingleView.E_Type _mode;

            /// <summary>
            /// Createビュー兼シングルビュー: テキストボックス
            /// </summary>
            public string TextBox(bool multiline = false) {
                var name = _component.GetRegisterName(_prop);
                var readOnly = _component.IfReadOnly("readOnly", _prop);

                if (multiline) {
                    return $$"""
                        <Components.Description {...register({{name}})} className="{{INPUT_WIDTH}}" {{readOnly}} />
                        """;
                } else {
                    return $$"""
                        <Components.Word {...register({{name}})} className="{{INPUT_WIDTH}}" {{readOnly}} />
                        """;
                }
            }

            /// <summary>
            /// Createビュー兼シングルビュー: トグル
            /// </summary>
            public string Toggle() {
                var registerName = _component.GetRegisterName(_prop);
                var disabled = _component.IfReadOnly("disabled", _prop);
                return $$"""
                    <input type="checkbox" {...register({{registerName}})} {{disabled}} />
                    """;
            }

            /// <summary>
            /// Createビュー兼シングルビュー: 選択肢（コード自動生成時に要素が確定しているもの）
            /// </summary>
            public string Selection(IEnumerable<KeyValuePair<string, string>> options) {
                var name = _component.GetRegisterName(_prop);
                var input = $$"""
                    <Components.Word {...register({{name}})} readOnly />
                    """;
                var select = $$"""
                    <select {...register({{name}})} className="{{INPUT_WIDTH}}">
                      <option value="">
                      </option>
                    {{options.SelectTextTemplate(option => $$"""
                      <option value="{{option.Key}}">
                        {{option.Value}}
                      </option>
                    """)}}
                    </select>
                    """;

                return _mode switch {
                    SingleView.E_Type.Create => select,
                    SingleView.E_Type.View => input,
                    SingleView.E_Type.Edit => _prop.Options.IsKey
                        ? $$"""
                            {(item?.{{AggregateDetail.IS_LOADED}})
                              ? {{WithIndent(input, "    ")}}
                              : {{WithIndent(select, "    ")}}}
                            """
                        : select,
                    _ => throw new NotImplementedException(),
                };
            }

            public string HiddenField() {
                var registerName = _component.GetRegisterName(_prop);
                return $$"""
                    <input type="hidden" {...register({{registerName}})} />
                    """;
            }
        }
        #endregion SCHALAR PROPERTY

        private string RenderProperty(AggregateMember.Ref refProperty) {
            var combobox = new KeywordSearching.ComboBox(refProperty.MemberAggregate);
            var registerName = GetRegisterName(refProperty);
            var callCombobox = _mode switch {
                SingleView.E_Type.Create => combobox.RenderCaller(registerName, readOnly: false),
                SingleView.E_Type.View => combobox.RenderCaller(registerName, readOnly: true),
                SingleView.E_Type.Edit => combobox.RenderCaller(registerName, $"item?.{AggregateDetail.IS_LOADED}"),
                _ => throw new NotImplementedException(),
            };

            return $$"""
                <div className="flex">
                  <div className="{{PropNameWidth}}">
                    <span className="text-sm select-none opacity-80">
                      {{refProperty.MemberName}}
                    </span>
                  </div>
                  <div className="flex-1">
                    {{WithIndent(callCombobox, "    ")}}
                  </div>
                </div>
                """;
        }
        private string RenderProperty(AggregateMember.Child child) {
            var childComponent = new AggregateComponent(child.MemberAggregate, _mode);
            return $$"""
                <div className="py-2">
                  <span className="text-sm select-none opacity-80">
                    {{child.MemberName}}
                  </span>
                  <div className="flex flex-col space-y-1 p-1 border border-neutral-400">
                    {{WithIndent(childComponent.RenderCaller(), "    ")}}
                  </div>
                </div>
                """;
        }
        private string RenderProperty(AggregateMember.VariationItem variation) {
            var switchProp = GetRegisterName(variation.Group);
            var childComponent = new AggregateComponent(variation.MemberAggregate, _mode);

            return $$"""
                <div className={`flex flex-col space-y-1 p-1 border border-neutral-400 ${(watch({{switchProp}}) !== '{{variation.Key}}' ? 'hidden' : '')}`}>
                  {{WithIndent(childComponent.RenderCaller(), "  ")}}
                </div>
                """;
        }
        private string RenderProperty(AggregateMember.Variation variationSwitch) {
            var switchProp = GetRegisterName(variationSwitch);
            var disabled = IfReadOnly("disabled", variationSwitch);

            return $$"""
                <div className="flex">
                  <div className="{{PropNameWidth}}">
                  <span className="text-sm select-none opacity-80">
                    {{variationSwitch.MemberName}}
                  </span>
                  </div>
                  <div className="flex-1 flex gap-2 flex-wrap">
                {{variationSwitch.GetGroupItems().SelectTextTemplate(variation => $$"""
                    <label>
                      <input type="radio" value="{{variation.Key}}" {{disabled}} {...register({{switchProp}})} />
                      {{variation.MemberName}}
                    </label>
                """)}}
                  </div>
                </div>
                """;
        }
        private string RenderProperty(AggregateMember.Children children) {
            var childrenComponent = new AggregateComponent(children.MemberAggregate, _mode);

            return $$"""
                <div className="py-2">
                  <span className="text-sm select-none opacity-80">
                    {{children.MemberName}}
                  </span>
                  <div className="flex flex-col space-y-1">
                    {{WithIndent(childrenComponent.RenderCaller(), "    ")}}
                  </div>
                </div>
                """;
        }


        #region ラベル列の横幅
        internal const string INPUT_WIDTH = "w-80";
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
        private string PropNameWidth => GetPropNameFlexBasis(new AggregateDetail(_aggregate).GetAggregateDetailMembers().Select(p => p.MemberName));
        #endregion ラベル列の横幅

        private string GetRegisterName(AggregateMember.AggregateMemberBase? prop = null) {
            var path = new List<string>();
            var i = 0;
            foreach (var edge in _aggregate.PathFromEntry()) {
                path.Add(edge.RelationName);

                if (edge.Terminal.IsChildrenMember()) {
                    if (edge.Terminal != _aggregate) {
                        // 祖先の中にChildrenがあるので配列番号を加える
                        path.Add("${index_" + i.ToString() + "}");
                        i++;
                    } else if (edge.Terminal == _aggregate && prop != null) {
                        // このコンポーネント自身がChildrenのとき
                        // - propがnull: useArrayFieldの登録名の作成なので配列番号を加えない
                        // - propがnullでない: mapの中のプロパティのレンダリングなので配列番号を加える
                        path.Add("${index_" + i.ToString() + "}");
                        i++;
                    }
                }
            }
            if (prop != null) {
                path.Add(prop.MemberName);
            }
            var name = path.Join(".");
            return string.IsNullOrEmpty(name) ? string.Empty : $"`{name}`";
        }

        private IEnumerable<string> GetArguments() {
            // 祖先コンポーネントの中に含まれるChildrenの数だけ、
            // このコンポーネントのその配列中でのインデックスが特定されている必要があるので、引数で渡す
            var args = _aggregate
                .PathFromEntry()
                .Where(edge => edge.Terminal != _aggregate
                            && edge.Terminal.IsChildrenMember())
                .Select((_, i) => $"index_{i}");
            return args;
        }

        private string IfReadOnly(string readOnly, AggregateMember.ValueMember prop) {
            return _mode switch {
                SingleView.E_Type.Create => "",
                SingleView.E_Type.View => readOnly,
                SingleView.E_Type.Edit => prop.IsKey
                    ? $"{readOnly}={{item?.{AggregateDetail.IS_LOADED}}}"
                    : $"",
                _ => throw new NotImplementedException(),
            };
        }
    }
}
