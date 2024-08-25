using Nijo.Core;
using Nijo.Models.WriteModel2Features;
using Nijo.Parts.Utility;
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
        private string UrlSubDomain => _aggregate.Item.UniqueId;
        private const string URL_NEW = "new";
        private const string URL_DETAIL = "detail";
        private const string URL_EDIT = "edit";

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
                return $"/{UrlSubDomain}/{URL_NEW}";

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
                    return $"/{UrlSubDomain}/{URL_DETAIL}{urlParams}";

                } else if (_type == E_Type.Edit) {
                    return $"/{UrlSubDomain}/{URL_EDIT}{urlParams}";
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
        /// <summary>
        /// 編集モードで開くとき、この名前のクエリパラメータにJSONで初期値を設定すると
        /// 画面初期値にそれが入った状態になる。JSONパースに失敗した場合は警告
        /// </summary>
        private const string EDIT_MODE_INITVALUE = "init";

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

                var detailView = new SingleView(_aggregate, E_Type.ReadOnly);
                var editView = new SingleView(_aggregate, E_Type.Edit);

                return $$"""
                    import React, { useState, useEffect, useCallback, useMemo, useReducer, useRef, useId, useContext, createContext } from 'react'
                    import useEvent from 'react-use-event-hook'
                    import { Link, useParams, useNavigate, useLocation } from 'react-router-dom'
                    import { SubmitHandler, useForm, FormProvider, useFormContext, useFieldArray, useWatch, FieldPath } from 'react-hook-form'
                    import * as Icon from '@heroicons/react/24/outline'
                    import dayjs from 'dayjs'
                    import * as Input from '../../input'
                    import * as Layout from '../../collection'
                    import * as Util from '../../util'
                    import * as AggregateType from '../../autogenerated-types'
                    import * as AggregateHook from '../../autogenerated-hooks'
                    import * as AggregateComponent from '../../autogenerated-components'

                    const VForm2 = Layout.VForm2

                    export default function () {
                      const [, dispatchMsg] = Util.useMsgContext()

                      // 表示データ
                      const reactHookFormMethods = Util.useFormEx<AggregateType.{{dataClass.TsTypeName}}>({ criteriaMode: 'all' })
                      const { register, registerEx, getValues, setValue, setError, reset, formState: { defaultValues }, control } = reactHookFormMethods
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
                      const { search } = useLocation()
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
                          const loadedValue = searchResult[0]
                          setDisplayName(`{{names.Select(n => $"${{loadedValue.{n.Join("?.")}}}").Join("")}}`)
                          reset(loadedValue)
                    {{If(_type == E_Type.Edit, () => $$"""

                          // 編集モードの場合、遷移前の画面からクエリパラメータで画面初期値が指定されていることがあるため、その値で画面の値を上書きする
                          try {
                            const queryParameter = new URLSearchParams(search)
                            const initValueJson = queryParameter.get('{{EDIT_MODE_INITVALUE}}')
                            if (initValueJson != null) {
                              const queryParameterValue: AggregateType.{{dataClass.TsTypeName}} = JSON.parse(initValueJson)
                              reset(queryParameterValue, { keepDefaultValues: true }) // あくまで手で入力した場合と同じ扱いとするためdefaultValuesはキープする
                            }
                          } catch {
                            dispatchMsg(msg => msg.warn('画面初期表示に失敗しました。'))
                          }
                    """)}}
                        })
                      }, [{{urlKeysWithMember.Values.Join(", ")}}, load{{_aggregate.Item.PhysicalName}}])

                    """)}}
                    {{If(_type == E_Type.New || _type == E_Type.Edit, () => $$"""
                      // 保存時
                      const { {{BatchUpdateReadModel.HOOK_NAME}} } = AggregateHook.{{BatchUpdateWriteModel.HOOK_NAME}}()
                      const navigateToDetailPage = Util.{{detailView.NavigateFnName}}()
                      const handleSave = useEvent(async () => {
                        const currentValues = getValues()
                    {{If(_type == E_Type.Edit, () => $$"""
                        if (defaultValues) {
                          const changed = AggregateType.{{dataClass.CheckChangesFunction}}({
                            defaultValues: defaultValues as AggregateType.{{dataClass.TsTypeName}},
                            currentValues,
                          })
                          if (!changed) {
                            alert('変更された内容がありません。')
                            return
                          }
                        }
                    """)}}
                        const response = await {{BatchUpdateReadModel.HOOK_NAME}}({
                          dataType: '{{BatchUpdateReadModel.GetDataTypeLiteral(_aggregate)}}',
                          values: currentValues,
                        })
                        if (!response.ok) {
                          // 入力エラーを画面に表示
                          const errors = response.errors as [FieldPath<AggregateType.{{dataClass.TsTypeName}}>, { types: { [key: string]: string } }][][]
                          for (const [name, error] of errors[0]) {
                            setError(name, error)
                          }
                          return
                        }
                        // 詳細画面（読み取り専用）へ遷移
                        navigateToDetailPage(currentValues, 'readonly')
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
                    {{rootAggregateComponent.EnumerateThisAndDescendantsRecursively().SelectTextTemplate(component => $$"""

                    {{component.RenderDeclaring(ctx, _type == E_Type.ReadOnly)}}
                    """)}}
                    """;
            },
        };

        #region この画面へ遷移する処理＠クライアント側
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

                      return React.useCallback((
                        obj: Types.{{dataClass.TsTypeName}},
                        /** 閲覧画面へ遷移するか編集画面へ遷移するか */
                        to: 'readonly' | 'edit',
                        /** 編集画面への遷移でのみ有効。このパラメータがtrueならば、初期表示時、サーバーから最新データを取得したあと、遷移前に指定した値で上書きされる。 */
                        overwrite?: boolean
                      ) => {
                    {{keys.SelectTextTemplate((k, i) => $$"""
                        const key{{i}} = obj.{{k.Path.Join("?.")}}
                    """)}}
                    {{keys.SelectTextTemplate((k, i) => $$"""
                        if (key{{i}} === undefined) throw new Error('{{k.MemberName}}が指定されていません。')
                    """)}}

                        if (to === 'readonly') {
                          navigate(`{{readView.GetUrl(false)}}/{{keys.Select((_, i) => $"${{window.encodeURI(`${{key{i}}}`)}}").Join("/")}}`)
                        } else {
                          const queryString = overwrite
                            ? `?${new URLSearchParams({ {{EDIT_MODE_INITVALUE}}: JSON.stringify(obj) }).toString()}`
                            : ''
                          navigate(`{{editView.GetUrl(false)}}/{{keys.Select((_, i) => $"${{window.encodeURI(`${{key{i}}}`)}}").Join("/")}}${queryString}`)
                        }
                      }, [navigate])
                    }
                    """;
            }
        }
        #endregion この画面へ遷移する処理＠クライアント側

        #region この画面へ遷移する処理＠サーバー側
        /// <summary>新規 or 編集 or 閲覧</summary>
        internal const string E_SINGLE_VIEW_TYPE = "E_SingleViewType";
        /// <summary>初期表示時に最新データを取得しなおすか、サーバー側で組み立てた値を初期表示するか</summary>
        internal const string E_REFETCH_TYPE = "E_RefetchType";
        internal static string RenderSingleViewNavigationEnums() {
            return $$"""
                /// <summary>詳細画面のモード</summary>
                public enum {{E_SINGLE_VIEW_TYPE}} {
                    /// <summary>新規データ作成モード</summary>
                    New,
                    /// <summary>既存データの編集モード</summary>
                    Edit,
                    /// <summary>既存データの閲覧モード</summary>
                    ReadOnly,
                }
                /// <summary>
                /// 初期表示時に最新データを取得しなおすか、
                /// サーバー側で組み立てた値を初期表示するか
                /// </summary>
                public enum {{E_REFETCH_TYPE}} {
                    /// <summary>画面遷移後、初期表示時は最新データを取得しなおす</summary>
                    Refetch,
                    /// <summary>
                    /// 画面遷移後、初期表示時に最新データを取得しなおした後、遷移前に指定した値で上書きする。
                    /// <see cref="{{E_SINGLE_VIEW_TYPE}}.Edit"/> でのみ有効。
                    /// </summary>
                    Overwrite,
                }
                """;
        }
        internal const string GET_URL_FROM_DISPLAY_DATA = "GetSingleViewUrlFromDisplayData";
        /// <summary>
        /// 画面表示用データを渡したらパラメータつきでその画面のURLを貰えるApplicationServiceのメソッド
        /// </summary>
        internal string RenderAppSrvGetUrlMethod() {
            var displayData = new DataClassForDisplay(_aggregate);
            var keys = _aggregate
                .GetKeys()
                .OfType<AggregateMember.ValueMember>()
                .Select(vm => new {
                    vm.MemberName,
                    Path = vm.Declared.GetFullPathAsDataClassForDisplay(E_CsTs.CSharp),
                })
                .ToArray();

            return $$"""
                /// <summary>
                /// 詳細画面を引数のオブジェクトで表示するためのURLを作成して返します。
                /// URLにドメイン部分は含まれません。
                /// </summary>
                /// <param name="displayData">遷移先データ</param>
                /// <param name="mode">画面モード。新規か閲覧か編集か</param>
                /// <param name="refetchType">初期表示時に最新データを取得しなおしたあと、遷移前に指定した値での上書きを行うかどうか</param>
                public virtual string {{GET_URL_FROM_DISPLAY_DATA}}({{displayData.CsClassName}} displayData, {{E_SINGLE_VIEW_TYPE}} mode, {{E_REFETCH_TYPE}} refetchType) {

                    if (mode == {{E_SINGLE_VIEW_TYPE}}.New) {
                        var encodedJson = System.Net.WebUtility.UrlEncode(displayData.{{UtilityClass.TO_JSON}}());
                        return $"/{{UrlSubDomain}}/{{URL_NEW}}?{{NEW_MODE_INITVALUE}}={encodedJson}";

                    } else {
                {{keys.SelectTextTemplate((k, i) => $$"""
                        var key{{i}} = displayData.{{k.Path.Join("?.")}};
                """)}}
                {{keys.SelectTextTemplate((k, i) => $$"""
                        if (key{{i}} == null) throw new ArgumentException($"{{k.MemberName}}が指定されていません。");
                """)}}
                {{keys.SelectTextTemplate((k, i) => $$"""
                        key{{i}} = System.Net.WebUtility.UrlEncode(key{{i}});
                """)}}

                        var subdomain = mode == {{E_SINGLE_VIEW_TYPE}}.Edit
                            ? "{{URL_EDIT}}"
                            : "{{URL_DETAIL}}";
                        var queryString = refetchType == {{E_REFETCH_TYPE}}.Overwrite
                            ? $"?{{EDIT_MODE_INITVALUE}}={System.Net.WebUtility.UrlEncode(displayData.{{UtilityClass.TO_JSON}}())}"
                            : string.Empty;

                        return $"/{{UrlSubDomain}}/{subdomain}{{keys.Select((_, i) => $"/{{key{i}}}").Join("")}}{queryString}";
                    }
                }
                """;
        }
        #endregion この画面へ遷移する処理＠サーバー側

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
