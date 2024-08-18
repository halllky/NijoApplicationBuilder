using Nijo.Core;
using Nijo.Models.WriteModel2Features;
using Nijo.Parts.Utility;
using Nijo.Parts.WebClient;
using Nijo.Parts.WebServer;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.ReadModel2Features {
    /// <summary>
    /// 詳細画面。新規モード・閲覧モード・編集モードの3種類をもつ。
    /// </summary>
    internal class SingleView : IReactPage {
        /// <summary>
        /// 新規モード・閲覧モード・編集モードのうちいずれか
        /// </summary>
        internal enum E_Type {
            New,
            ReadOnly,
            Edit,
        }

        internal SingleView(GraphNode<Aggregate> agg, E_Type type) {
            _aggregate = agg;
            _type = type;
        }
        private readonly GraphNode<Aggregate> _aggregate;
        private readonly E_Type _type;

        string IReactPage.Url => GetUrl(true);
        /// <summary>
        /// このページのURLを返します。
        /// </summary>
        /// <param name="asReactRouterDef">
        /// trueの場合、 "/aaa/bbb/:key0/:key1" など React Router の記法に則ったパラメータ込みのURL定義を返します。
        /// falseの場合、"/aaa/bbb" などパラメータ抜きのURLを返します。
        /// </param>
        /// <returns>クォートなしの文字列を返します。</returns>
        internal string GetUrl(bool asReactRouterDef) {
            if (_type == E_Type.New) {
                return $"/{_aggregate.Item.UniqueId}/new";

            } else {
                // React Router は全角文字非対応なので key0, key1, ... をURLに使う
                var urlKeys = _aggregate
                    .GetKeys()
                    .OfType<AggregateMember.ValueMember>()
                    .Select((_, i) => $"/:key{i}");
                var urlParams = asReactRouterDef
                    ? urlKeys.Join("")
                    : "";

                if (_type == E_Type.ReadOnly) {
                    return $"/{_aggregate.Item.UniqueId}/detail{urlParams}";

                } else if (_type == E_Type.Edit) {
                    return $"/{_aggregate.Item.UniqueId}/edit{urlParams}";
                } else {
                    throw new InvalidOperationException($"SingleViewの種類が不正: {_aggregate.Item}");
                }
            }
        }

        public string DirNameInPageDir => _aggregate.Item.DisplayName.ToFileNameSafe();
        public string ComponentPhysicalName => _type switch {
            E_Type.New => $"{_aggregate.Item.DisplayName.ToCSharpSafe()}CreateView",
            E_Type.ReadOnly => $"{_aggregate.Item.DisplayName.ToCSharpSafe()}DetailView",
            E_Type.Edit => $"{_aggregate.Item.DisplayName.ToCSharpSafe()}EditView",
            _ => throw new NotImplementedException(),
        };
        public bool ShowMenu => false;
        public string? LabelInMenu => null;

        /// <summary>
        /// 新規作成モードで開くとき、この名前のクエリパラメータにJSONで初期値を設定すると
        /// 画面初期値にそれが入った状態になる。JSONパースに失敗した場合は警告
        /// </summary>
        private const string NEW_MODE_INITVALUE = "init";

        public SourceFile GetSourceFile() => new SourceFile {
            FileName = _type switch {
                E_Type.New => "new.tsx",
                E_Type.ReadOnly => "detail.tsx",
                E_Type.Edit => "edit.tsx",
                _ => throw new NotImplementedException(),
            },
            RenderContent = ctx => {
                var dataClass = new DataClassForDisplay(_aggregate);
                var searchCondition = new SearchCondition(_aggregate);
                var loadFeature = new LoadMethod(_aggregate);
                var keyArray = KeyArray.Create(_aggregate);
                var keys = _aggregate
                    .GetKeys()
                    .OfType<AggregateMember.ValueMember>()
                    .ToArray();
                var urlKeysWithMember = keys
                    .Select((vm, i) => new { ValueMember = vm, Index = i })
                    .ToDictionary(x => x.ValueMember.Declared, x => $"key{x.Index}");
                var names = _aggregate
                    .GetNames()
                    .OfType<AggregateMember.ValueMember>()
                    .Where(vm => vm.DeclaringAggregate == _aggregate)
                    .Select(vm => vm.Declared.GetFullPathAsDataClassForDisplay(E_CsTs.TypeScript).ToArray())
                    .ToArray();

                var rootAggregateComponent = new SingleViewAggregateComponent(_aggregate);

                var editView = new SingleView(_aggregate, E_Type.Edit);

                return $$"""
                    import React, { useState, useEffect, useCallback, useMemo, useReducer, useRef, useId, useContext, createContext } from 'react'
                    import useEvent from 'react-use-event-hook'
                    import { Link, useParams, useNavigate, useLocation } from 'react-router-dom'
                    import { SubmitHandler, useForm, FormProvider, useFormContext, useFieldArray, useWatch, FieldPath } from 'react-hook-form'
                    import { BookmarkSquareIcon, PencilIcon, XMarkIcon, PlusIcon, ArrowUturnLeftIcon } from '@heroicons/react/24/outline'
                    import dayjs from 'dayjs'
                    import * as Input from '../../input'
                    import * as Layout from '../../collection'
                    import * as Util from '../../util'
                    import * as AggregateType from '../../autogenerated-types'
                    import * as AggregateHook from '../../autogenerated-hooks'

                    const VForm2 = Layout.VForm2

                    export default function () {
                      const [, dispatchMsg] = Util.useMsgContext()

                      // 表示データ
                      const reactHookFormMethods = Util.useFormEx<AggregateType.{{dataClass.TsTypeName}}>({ criteriaMode: 'all' })
                      const { register, registerEx, getValues, setValue, setError, reset, control } = reactHookFormMethods
                    {{If(_type != E_Type.New, () => $$"""
                      const [displayName, setDisplayName] = useState('')
                    """)}}

                      // 画面初期表示時
                    {{If(_type == E_Type.New, () => $$"""
                      const { search } = useLocation()
                      useEffect(() => {
                        try {
                          const queryParameter = new URLSearchParams(search)
                          const initValueJson = queryParameter.get('{{NEW_MODE_INITVALUE}}')
                          if (initValueJson != null) {
                            // クエリパラメータで画面初期値が指定されている場合はそれを初期表示する
                            const initValueObject: AggregateType.{{dataClass.TsTypeName}} = JSON.parse(initValueJson)
                            reset(initValueObject)
                          } else {
                            // 通常の初期値
                            reset(AggregateType.{{dataClass.TsNewObjectFunction}}())
                          }
                        } catch {
                          dispatchMsg(msg => msg.warn('画面初期表示に失敗しました。'))
                        }
                      }, [search])

                    """).Else(() => $$"""
                      const { {{urlKeysWithMember.Values.Join(", ")}} } = useParams() // URLから表示データのキーを受け取る
                      const { {{LoadMethod.LOAD}}: load{{_aggregate.Item.PhysicalName}} } = AggregateHook.{{loadFeature.ReactHookName}}()
                      useEffect(() => {
                    {{urlKeysWithMember.Values.SelectTextTemplate(key => $$"""
                        if ({{key}} === undefined) return
                    """)}}

                        // URLで指定されたキーで検索をかける。1件だけヒットするはずなのでそれを画面に初期表示する
                        const searchCondition = AggregateType.{{searchCondition.CreateNewObjectFnName}}()
                    {{urlKeysWithMember.SelectTextTemplate(kv => $$"""
                        searchCondition.{{kv.Key.Declared.GetFullPathAsSearchConditionFilter(E_CsTs.TypeScript).Join(".")}} = {{ConvertUrlParamToSearchConditionValue(kv.Key, kv.Value)}}
                    """)}}

                        load{{_aggregate.Item.PhysicalName}}(searchCondition).then(searchResult => {
                          if (searchResult.length === 0) {
                            dispatchMsg(msg => msg.warn(`表示対象のデータが見つかりません。（{{urlKeysWithMember.Select(kv => $"{kv.Key.MemberName}: ${{{kv.Value}}}").Join(", ")}}）`))
                            return
                          }
                          const initValue = searchResult[0]
                          setDisplayName(`{{names.Select(n => $"${{initValue.{n.Join("?.")}}}").Join("")}}`)
                          reset(initValue)
                        })
                      }, [{{urlKeysWithMember.Values.Join(", ")}}, load{{_aggregate.Item.PhysicalName}}])

                    """)}}
                    {{If(_type == E_Type.New || _type == E_Type.Edit, () => $$"""
                      // 保存時
                      const { {{BatchUpdateReadModel.HOOK_NAME}} } = AggregateHook.{{BatchUpdateWriteModel.HOOK_NAME}}()
                      const handleSave = useEvent(async () => {
                        const response = await {{BatchUpdateReadModel.HOOK_NAME}}({
                          dataType: '{{BatchUpdateReadModel.GetDataTypeLiteral(_aggregate)}}',
                          values: getValues(),
                        })
                        if (!response.ok) {
                          const errors = response.errors as [FieldPath<AggregateType.{{dataClass.TsTypeName}}>, { types: { [key: string]: string } }][][]
                          for (const [name, error] of errors[0]) {
                            setError(name, error)
                          }
                        }
                      })

                    """)}}
                    {{If(_type == E_Type.ReadOnly, () => $$"""
                      // 編集画面への遷移
                      const navigateToEditPage = Util.{{editView.NavigateFnName}}()
                      const handleStartEditing = useEvent(() => {
                        navigateToEditPage(getValues(), 'edit')
                      })

                    """)}}

                      return (
                        <FormProvider {...reactHookFormMethods}>
                          <Layout.PageFrame
                            header={<>
                              <Layout.PageTitle>
                    {{If(_type == E_Type.New, () => $$"""
                                {{_aggregate.Item.DisplayName}}&nbsp;新規作成
                    """).Else(() => $$"""
                                {{_aggregate.Item.DisplayName}}&nbsp;{displayName}
                    """)}}
                              </Layout.PageTitle>
                              <div className="flex-1"></div>
                    {{If(_type == E_Type.New || _type == E_Type.Edit, () => $$"""
                              <Input.IconButton fill onClick={handleSave}>保存</Input.IconButton>
                    """)}}
                    {{If(_type == E_Type.ReadOnly, () => $$"""
                              <Input.IconButton fill onClick={handleStartEditing}>編集開始</Input.IconButton>
                    """)}}
                            </>}
                          >
                            {{WithIndent(rootAggregateComponent.RenderCaller(), "        ")}}
                          </Layout.PageFrame>
                        </FormProvider>
                      )
                    }
                    {{rootAggregateComponent.EnumerateThisAndDescendants().SelectTextTemplate(component => $$"""

                    {{component.RenderDeclaring(ctx, _type == E_Type.ReadOnly)}}
                    """)}}
                    """;
            },
        };

        /// <summary>
        /// 詳細画面へ遷移する関数の名前
        /// </summary>
        public string NavigateFnName => _type switch {
            E_Type.New => $"useNavigateTo{_aggregate.Item.PhysicalName}CreateView",
            _ => $"useNavigateTo{_aggregate.Item.PhysicalName}SingleView",
        };
        internal string RenderNavigateFn(CodeRenderingContext context) {
            if (_type == E_Type.New) {
                var dataClass = new DataClassForDisplay(_aggregate);
                return $$"""
                    /** {{_aggregate.Item.DisplayName}}の新規作成画面へ遷移する関数を返します。引数にオブジェクトを渡した場合は画面初期値になります。 */
                    export const {{NavigateFnName}} = () => {
                      const navigate = useNavigate()

                      return React.useCallback((initValue?: Types.{{dataClass.TsTypeName}}) => {
                        if (initValue === undefined) {
                          navigate('{{GetUrl(false)}}')
                        } else {
                          const queryString = new URLSearchParams({ {{NEW_MODE_INITVALUE}}: JSON.stringify(initValue) }).toString()
                          navigate(`{{GetUrl(false)}}?${queryString}`)
                        }
                      }, [navigate])
                    }
                    """;
            } else {
                var readView = new SingleView(_aggregate, E_Type.ReadOnly);
                var editView = new SingleView(_aggregate, E_Type.Edit);
                var dataClass = new DataClassForDisplay(_aggregate);
                var keys = _aggregate
                    .GetKeys()
                    .OfType<AggregateMember.ValueMember>()
                    .Select(vm => new {
                        vm.MemberName,
                        Path = vm.Declared.GetFullPathAsDataClassForDisplay(E_CsTs.TypeScript),
                    })
                    .ToArray();
                return $$"""
                    /** {{_aggregate.Item.DisplayName}}の閲覧画面または編集画面へ遷移する関数を返します。 */
                    export const {{NavigateFnName}} = () => {
                      const navigate = useNavigate()

                      return React.useCallback((obj: Types.{{dataClass.TsTypeName}}, to: 'readonly' | 'edit') => {
                    {{keys.SelectTextTemplate((k, i) => $$"""
                        const key{{i}} = obj.{{k.Path.Join("?.")}}
                    """)}}
                    {{keys.SelectTextTemplate((k, i) => $$"""
                        if (key{{i}} === undefined) throw new Error('{{k.MemberName}}が指定されていません。')
                    """)}}

                        if (to === 'readonly') {
                          navigate(`{{readView.GetUrl(false)}}/{{keys.Select((_, i) => $"${{window.encodeURI(`${{key{i}}}`)}}").Join("/")}}`)
                        } else {
                          navigate(`{{editView.GetUrl(false)}}/{{keys.Select((_, i) => $"${{window.encodeURI(`${{key{i}}}`)}}").Join("/")}}`)
                        }
                      }, [navigate])
                    }
                    """;
            }
        }

        /// <summary>
        /// 詳細画面初期表示時の検索で、URLから受け取ったパラメータを <see cref="SearchCondition"/> に設定する。
        /// この処理は理論的には AggregateMemberType に保持させるのが綺麗だが、主キーなので結局stringかnumberしかありえないことから、
        /// URLのパラメータに設定する処理に近いここに書いている。
        /// </summary>
        private static string ConvertUrlParamToSearchConditionValue(AggregateMember.ValueMember vm, string urlParam) {
            var tsType = vm.Options.MemberType.GetTypeScriptTypeName();

            if (tsType == "number") {
                // 数値の場合は範囲検索で最小値最大値両方にURLパラメータを設定する
                return $"{{ {FromTo.FROM_TS}: Number({urlParam}), {FromTo.TO_TS}: Number({urlParam}) }}";

            } else {
                // 予期しない型の主キーが登場した場合はその時考える（ここに分岐を追加するか、AggregateMemberTypeに処理を委譲する）。
                // 文字列や列挙体の場合はURLパラメータそのまま検索にかけてよいのでそのままreturn
                return urlParam;
            }
        }
    }
}
