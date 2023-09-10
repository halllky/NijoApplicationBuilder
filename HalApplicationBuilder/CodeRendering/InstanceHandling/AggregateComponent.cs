using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using static HalApplicationBuilder.CodeRendering.TemplateTextHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HalApplicationBuilder.CodeRendering.WebClient;

namespace HalApplicationBuilder.CodeRendering.InstanceHandling {
    partial class FormOfAggregateInstance {
        internal FormOfAggregateInstance(GraphNode<Aggregate> aggregateInstance, CodeRenderingContext ctx) {
            _ctx = ctx;
            _instance = aggregateInstance;
        }
        private readonly CodeRenderingContext _ctx;
        private readonly GraphNode<Aggregate> _instance;

        public string FileName => "components.tsx";

        internal class AggregateComponent {
            internal AggregateComponent(GraphNode<Aggregate> aggregate, CodeRenderingContext ctx) {
                _aggregate = aggregate;
                _ctx = ctx;
            }

            private readonly GraphNode<Aggregate> _aggregate;
            private readonly CodeRenderingContext _ctx;

            internal virtual string ComponentName => $"{_aggregate.Item.TypeScriptTypeName}View";
            private string PropNameWidth => GetPropNameFlexBasis(_aggregate.GetMembers().Select(p => p.PropertyName));
            private string GetUseFieldArrayName() {
                var path = new List<string>();
                var args = GetArguments(_aggregate);
                var ancestors = _aggregate.PathFromEntry().ToArray();

                foreach (var ancestor in ancestors) {
                    path.Add(ancestor.RelationName);
                    if (ancestor != ancestors.Last() && ancestor.Terminal.IsChildrenMember()) path.Add($"${{{args[ancestor]}}}");
                }

                return path.Join(".");
            }
            internal string RenderCaller() {
                var args = GetArguments(_aggregate)
                    .SkipLast(1)
                    .Select(x => $" {x.Value}={{{x.Value}}}")
                    .Join(string.Empty);
                return $"<{ComponentName}{args} />";
            }

            internal string Render() {
                var layout = _aggregate.GetMembers().SelectTextTemplate(prop => prop switch {
                    AggregateMember.ParentPK => string.Empty,
                    AggregateMember.RefTargetMember => string.Empty,
                    AggregateMember.Schalar x => RenderProperty(x),
                    AggregateMember.Ref x => RenderProperty(x),
                    AggregateMember.Child x => RenderProperty(x),
                    AggregateMember.VariationItem x => RenderProperty(x),
                    AggregateMember.Variation x => RenderProperty(x),
                    AggregateMember.Children x => RenderProperty(x),
                    _ => throw new NotImplementedException(),
                });

                if (!_aggregate.IsChildrenMember()) {
                    var args = GetArguments(_aggregate).Values;

                    return $$"""
                        const {{ComponentName}} = ({ {{args.Join(", ")}} }: {
                        {{args.SelectTextTemplate(arg => $$"""
                          {{arg}}: number
                        """)}}
                        }) => {
                          const [{ singleViewPageMode },] = usePageContext()
                          const { register, watch } = useFormContext<AggregateType.{{_aggregate.GetRoot().Item.TypeScriptTypeName}}>()
                        
                          return (
                            <>
                              {{WithIndent(layout, "      ")}}
                            </>
                          )
                        }
                        """;

                } else {
                    var argsAndIndex = GetArguments(_aggregate).Values;
                    var args = argsAndIndex.SkipLast(1);
                    var index = argsAndIndex.Last();
                    var createNewChildrenItem = new types.AggregateInstanceInitializerFunction(_aggregate).FunctionName;

                    return $$"""
                        const {{ComponentName}} = ({ {{args.Join(", ")}} }: {
                        {{args.SelectTextTemplate(arg => $$"""
                          {{arg}}: number
                        """)}}
                        }) => {
                          const [{ singleViewPageMode },] = usePageContext()
                          const { register, watch, control } = useFormContext<AggregateType.{{_aggregate.GetRoot().Item.TypeScriptTypeName}}>()
                          const { fields, append, remove } = useFieldArray({
                            control,
                            name: `{{GetUseFieldArrayName()}}`,
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
                              {fields.map((_, {{index}}) => (
                                <div key={{{index}}} className="flex flex-col space-y-1 p-1 border border-neutral-400">
                                  {{WithIndent(layout, "          ")}}
                                  {singleViewPageMode !== 'view' &&
                                    <Components.IconButton
                                      underline
                                      icon={XMarkIcon}
                                      onClick={onRemove({{index}})}
                                      className="self-start">
                                      削除
                                    </Components.IconButton>}
                                </div>
                              ))}
                              {singleViewPageMode !== 'view' &&
                                <Components.IconButton
                                  underline
                                  icon={PlusIcon}
                                  onClick={onAdd}
                                  className="self-start">
                                  追加
                                </Components.IconButton>}
                            </>
                          )
                        }
                        """;
                }
            }


            #region SCHALAR PROPERTY
            private string RenderProperty(AggregateMember.Schalar schalar) {
                var renderer = new ReactForm(_aggregate, schalar);
                return $$"""
                    <div className="flex">
                      <div className="{{PropNameWidth}}">
                        <span className="text-sm select-none opacity-80">
                          {{schalar.PropertyName}}
                        </span>
                      </div>
                      <div className="flex-1">
                        {{WithIndent(schalar.MemberType.RenderUI(renderer), "    ")}}
                      </div>
                    </div>
                    """;
            }
            private class ReactForm : IGuiFormRenderer {
                internal ReactForm(GraphNode<Aggregate> instance, AggregateMember.Schalar prop) {
                    _instance = instance;
                    _prop = prop;
                }
                private readonly GraphNode<Aggregate> _instance;
                private readonly AggregateMember.Schalar _prop;

                private string ReadOnlyWhere() {
                    return _prop.IsPrimary
                        ? "singleViewPageMode === 'view' || singleViewPageMode === 'edit'"
                        : "singleViewPageMode === 'view'";
                }

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
                              readOnly={{{ReadOnlyWhere()}}}
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
                              readOnly={{{ReadOnlyWhere()}}}
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
                        <input type="checkbox" {...register(`{{name}}`)} disabled={{{ReadOnlyWhere()}}} />
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


            private string RenderProperty(AggregateMember.Ref refProperty) {
                var combobox = new KeywordSearching.ComboBox(refProperty.MemberAggregate, _ctx);
                var registerName = GetRegisterName(_aggregate, refProperty).Value;
                return $$"""
                    <div className="flex">
                      <div className="{{PropNameWidth}}">
                        <span className="text-sm select-none opacity-80">
                          {{refProperty.PropertyName}}
                        </span>
                      </div>
                      <div className="flex-1">
                        {{WithIndent(combobox.RenderCaller(registerName), "    ")}}
                      </div>
                    </div>
                    """;
            }
            private string RenderProperty(AggregateMember.Child child) {
                var childComponent = new AggregateComponent(child.MemberAggregate, _ctx);
                return $$"""
                    <div className="py-2">
                      <span className="text-sm select-none opacity-80">
                      {{child.PropertyName}}
                      </span>
                      <div className="flex flex-col space-y-1 p-1 border border-neutral-400">
                        {{WithIndent(childComponent.RenderCaller(), "    ")}}
                      </div>
                    </div>
                    """;
            }
            private string RenderProperty(AggregateMember.VariationItem variation) {
                var childComponent = new AggregateComponent(variation.MemberAggregate, _ctx);
                var switchProp = GetRegisterName(_aggregate, variation.Group).Value;
                return $$"""
                    <div className={`flex flex-col space-y-1 p-1 border border-neutral-400 ${(watch(`{{switchProp}}`) !== '{{variation.Key}}' ? 'hidden' : '')}`}>
                      {{WithIndent(childComponent.RenderCaller(), "  ")}}
                    </div>
                    """;
            }
            private string RenderProperty(AggregateMember.Variation variationSwitch) {
                var switchProp = GetRegisterName(_aggregate, variationSwitch).Value;
                return $$"""
                    <div className="flex">
                      <div className="{{PropNameWidth}}">
                      <span className="text-sm select-none opacity-80">
                        {{variationSwitch.PropertyName}}
                      </span>
                      </div>
                      <div className="flex-1 flex gap-2 flex-wrap">
                    {{variationSwitch.GetGroupItems().SelectTextTemplate(variation => $$"""
                        <label>
                          <input type="radio" value="{{variation.Key}}" disabled={singleViewPageMode === 'view'} {...register(`{{switchProp}}`)} />
                          {{variation.PropertyName}}
                        </label>
                    """)}}
                      </div>
                    </div>
                    """;
            }
            private string RenderProperty(AggregateMember.Children children) {
                var childrenComponent = new AggregateComponent(children.MemberAggregate, _ctx);
                return $$"""
                    <div className="py-2">
                      <span className="text-sm select-none opacity-80">
                        {{children.PropertyName}}
                      </span>
                      <div className="flex flex-col space-y-1">
                        {{WithIndent(childrenComponent.RenderCaller(), "    ")}}
                      </div>
                    </div>
                    """;
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
