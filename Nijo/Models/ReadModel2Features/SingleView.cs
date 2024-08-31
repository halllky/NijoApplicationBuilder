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

        string IReactPage.Url {
            get {
                // React Router は全角文字非対応なので key0, key1, ... をURLに使う。
                // 新規作成モードでは key0, key1, ... は無視されるため、オプショナルとして定義する。
                var urlKeys = _aggregate
                    .GetKeys()
                    .OfType<AggregateMember.ValueMember>()
                    .Select((_, i) => $"/:key{i}?");
                return $"/{UrlSubDomain}/:{MODE}{urlKeys.Join("")}";
            }
        }
        private string UrlSubDomain => _aggregate.Item.UniqueId;
        private const string URL_NEW = "new";
        private const string URL_DETAIL = "detail";
        private const string URL_EDIT = "edit";

        private const string MODE = "mode";

        /// <summary>
        /// このページのURLを返します。
        /// </summary>
        private string GetUrl() {
            if (_type == E_Type.New) {
                return $"/{UrlSubDomain}/{URL_NEW}";

            } else if (_type == E_Type.ReadOnly) {
                return $"/{UrlSubDomain}/{URL_DETAIL}";

            } else if (_type == E_Type.Edit) {
                return $"/{UrlSubDomain}/{URL_EDIT}";
            } else {
                throw new InvalidOperationException($"SingleViewの種類が不正: {_aggregate.Item}");
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

        internal string FrameHookName => $"use{_aggregate.Item.PhysicalName}SingleViewFrame";
        /// <summary>
        /// 画面表示時のデータの読み込み、保存ボタン押下時の保存処理、ページの枠、をやってくれるフック
        /// </summary>
        internal string RenderPageFrameComponent(CodeRenderingContext context) {
            var dataClass = new DataClassForDisplay(_aggregate);
            var searchCondition = new SearchCondition(_aggregate);
            var loadFeature = new LoadMethod(_aggregate);
            var detailView = new SingleView(_aggregate, E_Type.ReadOnly);

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

            return $$"""
                export const {{FrameHookName}} = () => {
                  const [, dispatchMsg] = Util.useMsgContext()

                  // 表示データ
                  const reactHookFormMethods = Util.useFormEx<Types.{{dataClass.TsTypeName}}>({ criteriaMode: 'all' })
                  const { register, registerEx, getValues, setValue, setError, reset, formState: { defaultValues }, control } = reactHookFormMethods
                  const [displayName, setDisplayName] = React.useState('')

                  // 画面表示時
                  const { search } = ReactRouter.useLocation() // URLからクエリパラメータを受け取る
                  const { {{new[] { $"{MODE}: modeInUrl" }.Concat(urlKeysWithMember.Values).Join(", ")}} } = ReactRouter.useParams() // URLから画面モードと表示データのキーを受け取る
                  const { {{LoadMethod.LOAD}}: load{{_aggregate.Item.PhysicalName}} } = {{loadFeature.ReactHookName}}()
                  const mode = React.useMemo(() => {
                    if (modeInUrl === '{{URL_NEW}}') return 'new' as const
                    if (modeInUrl === '{{URL_DETAIL}}') return 'detail' as const
                    if (modeInUrl === '{{URL_EDIT}}') return 'edit' as const
                    return undefined
                  }, [modeInUrl])
                  const [loadState, setLoadState] = React.useState<'loading' | 'ready' | 'error' | undefined>()
                  const reload = useEvent(async () => {
                    if (loadState === 'loading') return
                    setLoadState('loading')
                    try {
                      if ({{MODE}} === 'new') {
                        // ------ 画面表示時処理: 新規作成モードの場合 ------
                        setDisplayName('新規作成')
                        const queryParameter = new URLSearchParams(search)
                        const initValueJson = queryParameter.get('{{NEW_MODE_INITVALUE}}')
                        if (initValueJson != null) {
                          // クエリパラメータで画面初期値が指定されている場合はそれを初期表示する
                          const initValueObject: Types.{{dataClass.TsTypeName}} = JSON.parse(initValueJson)
                          reset(initValueObject)
                        } else {
                          // 通常の初期値
                          reset(Types.{{dataClass.TsNewObjectFunction}}())
                        }
                        setLoadState('ready')

                      } else if ({{MODE}} === 'detail' || {{MODE}} === 'edit') {
                        // ------ 画面表示時処理: 閲覧モードまたは編集モードの場合 ------
                {{urlKeysWithMember.Values.SelectTextTemplate(key => $$"""
                        if ({{key}} === undefined) return
                """)}}

                        // URLで指定されたキーで検索をかける。1件だけヒットするはずなのでそれを画面に初期表示する
                        const searchCondition = Types.{{searchCondition.CreateNewObjectFnName}}()
                {{urlKeysWithMember.SelectTextTemplate(kv => $$"""
                        searchCondition.{{kv.Key.Declared.GetFullPathAsSearchConditionFilter(E_CsTs.TypeScript).Join(".")}} = {{ConvertUrlParamToSearchConditionValue(kv.Key, kv.Value)}}
                """)}}

                        const searchResult = await load{{_aggregate.Item.PhysicalName}}(searchCondition)
                        if (searchResult.length === 0) {
                          dispatchMsg(msg => msg.warn(`表示対象のデータが見つかりません。（{{urlKeysWithMember.Select(kv => $"{kv.Key.MemberName}: ${{{kv.Value}}}").Join(", ")}}）`))
                          setLoadState('error')
                          return
                        }
                        const loadedValue = searchResult[0]
                        setDisplayName(`{{names.Select(n => $"${{loadedValue.{n.Join("?.")}}}").Join("")}}`)
                        reset(loadedValue)
                      
                        // 編集モードの場合、遷移前の画面からクエリパラメータで画面初期値が指定されていることがあるため、その値で画面の値を上書きする
                        if ({{MODE}} === 'edit') {
                          try {
                            const queryParameter = new URLSearchParams(search)
                            const initValueJson = queryParameter.get('{{EDIT_MODE_INITVALUE}}')
                            if (initValueJson != null) {
                              const queryParameterValue: Types.{{dataClass.TsTypeName}} = JSON.parse(initValueJson)
                              reset(queryParameterValue, { keepDefaultValues: true }) // あくまで手で入力した場合と同じ扱いとするためdefaultValuesはキープする
                            }
                          } catch {
                            dispatchMsg(msg => msg.warn('遷移前画面で指定されたパラメータが不正です。データベースから読み込んだ値を表示します。'))
                          }
                        }
                        setLoadState('ready')
                      }
                    } catch {
                      setLoadState('error')
                      dispatchMsg(msg => msg.warn('データの読み込みに失敗しました。'))
                    }
                  })
                  React.useEffect(() => {
                    reload()
                  }, [])

                  // 保存時
                  const { {{BatchUpdateReadModel.HOOK_NAME}} } = {{BatchUpdateWriteModel.HOOK_NAME}}()
                  const navigateToDetailPage = Util.{{detailView.NavigateFnName}}()
                  const save = useEvent(async () => {
                    // 閲覧モードでは保存不可
                    if ({{MODE}} === 'detail') {
                      dispatchMsg(msg => msg.warn('閲覧モードで表示中のためデータを更新することができません。'))
                      return
                    }
                    // React hook form が持っている画面表示時（reset時）の値と最新の状態をディープイコールで比較し、変更がなければ保存処理中断
                    const currentValues = getValues()
                    if ({{MODE}} === 'edit' && defaultValues) {
                      const changed = Types.{{dataClass.CheckChangesFunction}}({
                        defaultValues: defaultValues as Types.{{dataClass.TsTypeName}},
                        currentValues,
                      })
                      if (!changed) {
                        alert('変更された内容がありません。')
                        return
                      }
                    }
                    // 一括更新APIを呼ぶ
                    const response = await {{BatchUpdateReadModel.HOOK_NAME}}({
                      dataType: '{{BatchUpdateReadModel.GetDataTypeLiteral(_aggregate)}}',
                      values: currentValues,
                    })
                    // 処理失敗の場合、入力エラーを画面に表示
                    if (!response.ok) {
                      const errors = response.errors as [ReactHookForm.FieldPath<Types.{{dataClass.TsTypeName}}>, { types: { [key: string]: string } }][][]
                      for (const [name, error] of errors[0]) {
                        setError(name, error)
                      }
                      return
                    }
                    // 処理成功の場合、詳細画面（読み取り専用）へ遷移
                    navigateToDetailPage(currentValues, 'readonly')
                  })

                  // ページの外枠
                  const SingleViewPageFrame = ({ children, header, footer }: {
                    children?: React.ReactNode
                    header?: React.ReactNode
                    footer?: React.ReactNode
                  }) => {
                    return (
                      <ReactHookForm.FormProvider {...reactHookFormMethods}>
                        <Layout.PageFrame
                          nowLoading={loadState === undefined || loadState === 'loading'}
                          header={<>
                            <Layout.PageTitle>
                              {{_aggregate.Item.DisplayName}}&nbsp;{displayName}
                            </Layout.PageTitle>
                            <div className="flex-1"></div>
                            {header}
                          </>}
                          footer={footer}
                        >
                          {loadState === 'ready' && (
                            children
                          )}
                          {loadState === 'error' && (
                            <div className="m-auto h-full flex justify-center items-center">
                              <Input.IconButton onClick={reload} fill>再読み込み</Input.IconButton>
                            </div>
                          )}
                        </Layout.PageFrame>
                      </ReactHookForm.FormProvider>
                    )
                  }

                  return {
                    /** ページの外枠 */
                    SingleViewPageFrame,
                    /** データの読み込み状態 */
                    loadState,
                    /** 画面モード */
                    {{MODE}},
                    /** React hook form のメソッド群。これを通すか、またはuseFormContextを通すことで画面表示中データにアクセスします。 */
                    reactHookFormMethods,
                    /** 画面データを読み込みなおします。通常これを使うことはないはず。 */
                    reload,
                    /** 画面の内容で保存処理を実行します。 */
                    save,
                  }
                }
                """;
        }

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
            FileName = "single-view.tsx",
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
                    import { {{AutoGeneratedCustomizer.USE_CONTEXT}} } from '../../autogenerated-customizer'

                    const VForm2 = Layout.VForm2

                    export default function () {
                      const { SingleViewPageFrame, reactHookFormMethods, loadState, mode, save } = AggregateHook.{{FrameHookName}}()
                      const { register, registerEx, getValues, setValue, setError, reset, formState: { defaultValues }, control } = reactHookFormMethods

                      // 編集画面への遷移
                      const navigateToEditPage = Util.{{editView.NavigateFnName}}()
                      const handleStartEditing = useEvent(() => {
                        navigateToEditPage(getValues(), 'edit')
                      })

                      return (
                        <SingleViewPageFrame
                          header={<>
                            {loadState === 'ready' && (mode === 'new' || mode === 'edit') && (
                              <Input.IconButton fill onClick={save}>保存</Input.IconButton>
                            )}
                            {loadState === 'ready' && mode === 'detail' && (
                              <Input.IconButton fill onClick={handleStartEditing}>編集開始</Input.IconButton>
                            )}
                          </>}
                        >
                          {{WithIndent(rootAggregateComponent.RenderCaller(), "      ")}}
                        </SingleViewPageFrame>
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
                          navigate('{{GetUrl()}}')
                        } else {
                          const queryString = new URLSearchParams({ {{NEW_MODE_INITVALUE}}: JSON.stringify(initValue) }).toString()
                          navigate(`{{GetUrl()}}?${queryString}`)
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
                          navigate(`{{readView.GetUrl()}}/{{keys.Select((_, i) => $"${{window.encodeURI(`${{key{i}}}`)}}").Join("/")}}`)
                        } else {
                          const queryString = overwrite
                            ? `?${new URLSearchParams({ {{EDIT_MODE_INITVALUE}}: JSON.stringify(obj) }).toString()}`
                            : ''
                          navigate(`{{editView.GetUrl()}}/{{keys.Select((_, i) => $"${{window.encodeURI(`${{key{i}}}`)}}").Join("/")}}${queryString}`)
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
                    IsString = vm.Options.MemberType.GetCSharpTypeName() == "string",
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
                        var key{{i}} = displayData.{{k.Path.Join("?.")}}{{(k.IsString ? "" : "?.ToString()")}};
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
