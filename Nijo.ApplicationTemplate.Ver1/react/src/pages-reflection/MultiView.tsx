import * as React from "react"
import * as ReactRouter from "react-router-dom"
import * as ReactHookForm from "react-hook-form"
import * as Layout from "../layout"
import * as Input from "../input"
import * as Util from "../util"
import * as AutoGenerated from "../__autoGenerated"
import { MetadataForPage } from "../__autoGenerated/util"
import useEvent from "react-use-event-hook"
import { Panel, PanelGroup, PanelResizeHandle } from "react-resizable-panels"
import { ReflectionForm } from "./Parts.Form"
import { useReflectionGridColumnDefs } from "./Parts.Grid"
import { MetadataSchema, getSearchConditionEntries } from "./getSchema"
import { UUID } from "uuidjs"
import dayjs from "dayjs"
import customParseFormat from 'dayjs/plugin/customParseFormat'
import { MagnifyingGlassIcon } from '@heroicons/react/24/outline'
dayjs.extend(customParseFormat)

// 検索条件の型 (TSearchConditionの具体的な型は呼び出し元で解決される)
type TSearchConditionGeneric = AutoGenerated.SearchCondition.TypeMap[AutoGenerated.QueryModelType];

// 検索条件オブジェクトを URLSearchParams に変換
const serializeSearchCondition = (
  condition: TSearchConditionGeneric,
  metadata: MetadataForPage.StructureMetadata,
  schema: MetadataSchema
): URLSearchParams => {
  const params = new URLSearchParams();
  if (!condition || !condition.filter) return params;

  const entries = getSearchConditionEntries(metadata, schema);
  const filterObject = condition.filter;

  for (const entry of entries) {
    const pathString = entry.path.join('.');
    const memberMeta = entry.member;
    const value = ReactHookForm.get(filterObject, pathString);

    if (value === undefined || value === null) {
      continue;
    }

    if (memberMeta.type === 'RootAggregate' || memberMeta.type === 'ChildAggregate' || memberMeta.type === 'ChildrenAggregate') {
      continue;
    }

    const valueMeta = memberMeta as MetadataForPage.ValueMetadata | MetadataForPage.RefMetadata;

    if (typeof value === 'object' && value !== null) {
      // from/to 構造を持つオブジェクト (日付範囲、数値範囲など)
      if ((valueMeta.type === 'datetime' || valueMeta.type === 'date' || valueMeta.type === 'int' || valueMeta.type === 'decimal' || valueMeta.type === 'yearmonth' || valueMeta.type === 'year')) {
        const fromIsEmpty = value.from === undefined || value.from === null || value.from === '';
        const toIsEmpty = value.to === undefined || value.to === null || value.to === '';
        if (!fromIsEmpty || !toIsEmpty) {
          params.set(pathString, JSON.stringify(value));
        }
        // 列挙型またはブール型の複数選択オブジェクト
      } else if (valueMeta.type !== 'ref-to' && (valueMeta.enumType || valueMeta.type === 'bool')) {
        // オブジェクト内に一つでもtrueの値があるか、または何かしら選択されているかを確認
        const hasTrueValue = Object.values(value).some(v => v === true || (typeof v === 'string' && v !== ''));
        if (hasTrueValue) {
          params.set(pathString, JSON.stringify(value));
        }
      }
    } else if (valueMeta.type === 'datetime' || valueMeta.type === 'date') {
      if (value instanceof Date) {
        params.set(pathString, dayjs(value).format('YYYY-MM-DD'));
      } else if (typeof value === 'string' && value) {
        if (dayjs(value, 'YYYY-MM-DD', true).isValid()) {
          params.set(pathString, value);
        }
      }
    } else if (value !== '') {
      params.set(pathString, String(value));
    }
  }
  return params;
};

// URLSearchParams を検索条件オブジェクトに変換
const deserializeSearchCondition = <TSearchCond extends TSearchConditionGeneric>(
  params: URLSearchParams,
  metadata: MetadataForPage.StructureMetadata,
  schema: MetadataSchema,
  queryModelType: AutoGenerated.QueryModelType
): TSearchCond => {
  const newCondition = AutoGenerated.SearchCondition.create[queryModelType]() as TSearchCond;
  const entries = getSearchConditionEntries(metadata, schema);

  if (!newCondition.filter || typeof newCondition.filter !== 'object') {
    newCondition.filter = {};
  }
  const filterObject = newCondition.filter;

  // URLSearchParams を列挙し、対応するメタデータエントリを見つけて型変換
  for (const [pathString, paramValue] of params) {
    // pathStringに対応するメタデータエントリを見つける
    const entry = entries.find(e => e.path.join('.') === pathString);
    if (!entry) continue; // 対応するメタデータが見つからない場合はスキップ

    const memberMeta = entry.member;
    if (memberMeta.type === 'RootAggregate' || memberMeta.type === 'ChildAggregate' || memberMeta.type === 'ChildrenAggregate') {
      continue;
    }
    const valueMeta = memberMeta as MetadataForPage.ValueMetadata | MetadataForPage.RefMetadata;

    if (paramValue !== null && paramValue !== undefined && paramValue !== '') {
      let deserializedValue: unknown;

      // 範囲検索型、列挙型オブジェクト、ブール型オブジェクトの可能性
      if (valueMeta.type !== 'ref-to' && (valueMeta.type === 'datetime' || valueMeta.type === 'date' || valueMeta.type === 'int' || valueMeta.type === 'decimal' || valueMeta.type === 'yearmonth' || valueMeta.type === 'year' || valueMeta.enumType || valueMeta.type === 'bool')) {
        try {
          const parsed = JSON.parse(paramValue);
          // from/to 構造か、列挙型/ブール型のオブジェクト形式かを判定
          if (typeof parsed === 'object' && parsed !== null) {
            if ((valueMeta.type === 'int' || valueMeta.type === 'decimal')) {
              if (parsed.from !== undefined && parsed.from !== null && parsed.from !== '') {
                const numFrom = Number(parsed.from);
                parsed.from = isNaN(numFrom) ? '' : numFrom;
              } else {
                parsed.from = '';
              }
              if (parsed.to !== undefined && parsed.to !== null && parsed.to !== '') {
                const numTo = Number(parsed.to);
                parsed.to = isNaN(numTo) ? '' : numTo;
              } else {
                parsed.to = '';
              }
            }
            deserializedValue = parsed; // from/to 構造、または列挙型/ブール型オブジェクト
          } else if (valueMeta.type === 'datetime' || valueMeta.type === 'date') {
            if (dayjs(paramValue, 'YYYY-MM-DD', true).isValid()) {
              deserializedValue = paramValue;
            } else {
              deserializedValue = '';
            }
          } else if (valueMeta.type === 'bool') { // JSONパースがオブジェクトでないがブール型の場合 (単一ブール値)
            deserializedValue = paramValue.toLowerCase() === 'true';
          } else {
            deserializedValue = paramValue;
          }
        } catch (e) { // JSON.parseに失敗した場合 (単純な値として扱う)
          if (valueMeta.type === 'datetime' || valueMeta.type === 'date') {
            if (dayjs(paramValue, 'YYYY-MM-DD', true).isValid()) {
              deserializedValue = paramValue;
            } else {
              deserializedValue = '';
            }
          } else if (valueMeta.type === 'int' || valueMeta.type === 'decimal') {
            const num = Number(paramValue);
            deserializedValue = isNaN(num) ? '' : num;
          } else if (valueMeta.type === 'bool') {
            deserializedValue = paramValue.toLowerCase() === 'true';
          } else {
            deserializedValue = paramValue;
          }
        }
      } else { // 上記以外の単純な型
        deserializedValue = paramValue;
      }

      ReactHookForm.set(filterObject, pathString, deserializedValue);
    }
  }
  return newCondition;
};

/** QueryModelの一覧検索画面 */
export const MultiView = ({ rootAggregatePhysicalName, metadata, schema }: {
  rootAggregatePhysicalName: string
  metadata: MetadataForPage.StructureMetadata
  schema: MetadataSchema
}) => {

  const queryModelType = rootAggregatePhysicalName as AutoGenerated.QueryModelType

  // 画面上部で編集中の検索条件
  type TSearchCondition = AutoGenerated.SearchCondition.TypeMap[typeof queryModelType]
  const form = ReactHookForm.useForm<TSearchCondition>({
    defaultValues: AutoGenerated.SearchCondition.create[queryModelType](),
  })

  // 最後に検索を実行したときの検索結果
  type TDisplayData = AutoGenerated.DisplayData.TypeMap[typeof queryModelType]
  const [currentPageItems, setCurrentPageItems] = React.useState<TDisplayData[]>([])
  const [totalCount, setTotalCount] = React.useState<number>()
  const getMemberColumnDefs = useReflectionGridColumnDefs(metadata, schema)
  const getColumnDefs: Layout.GetColumnDefsFunction<ReactHookForm.FieldValues> = React.useCallback(cellType => {

    // 詳細画面へのリンクの列
    const linkColumn: Layout.EditableGridColumnDef<ReactHookForm.FieldValues> = cellType.other('', {
      defaultWidth: 64,
      renderCell: cell => {
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const keys = AutoGenerated.DisplayData.extractKeys[queryModelType](cell.row.original as any)
        const link = `/${rootAggregatePhysicalName}/detail/${keys.map(key => (key as object)?.toString()).join('/')}`
        return (
          <ReactRouter.Link to={link} className="text-sky-700 underline underline-offset-2">
            詳細
          </ReactRouter.Link>
        )
      }
    })

    return [
      linkColumn,
      ...getMemberColumnDefs(cellType),
    ]
  }, [getMemberColumnDefs, queryModelType, rootAggregatePhysicalName])

  // 検索処理。
  // 検索ボタン押下やページングなどのタイミングで検索条件がURLのクエリパラメータに反映され、
  // 検索処理はformの値ではなくURLの値を使用する。
  const navigate = ReactRouter.useNavigate()
  const { pathname, search } = ReactRouter.useLocation()
  const [searchParams, setSearchParams] = ReactRouter.useSearchParams()
  const { complexPost } = Util.useHttpRequest()
  const [isLoaded, setIsLoaded] = React.useState(false)
  const [searchTrigger, setSearchTrigger] = React.useState(UUID.generate())

  const executeSearch = useEvent(async (searchConditionFromForm: TSearchCondition) => {
    // この画面に遷移したうえで、クエリパラメータに検索条件をシリアライズして付加する。
    const newSearchParams = serializeSearchCondition(searchConditionFromForm, metadata, schema);
    setSearchParams(newSearchParams, { replace: true });
    setSearchTrigger(UUID.generate());
  })

  React.useEffect(() => {
    let isActive = true;
    setIsLoaded(false);

    // URLのクエリパラメータから検索条件を取得し、検索条件オブジェクトにアサインする
    const searchConditionFromUrl = deserializeSearchCondition<TSearchCondition>(
      searchParams,
      metadata,
      schema,
      queryModelType
    );

    // デシリアライズした検索条件をフォームに反映
    form.reset(searchConditionFromUrl);

    // APIに渡すオブジェクトは { filter: ..., sort: ... } の形式にする
    // deserializeSearchCondition がこの形式のオブジェクトを返すようになったので、そのまま使う
    const requestBody = searchConditionFromUrl;

    complexPost<AutoGenerated.LoadFeature.ReturnType[typeof queryModelType]>(
      AutoGenerated.LoadFeature.Endpoint[queryModelType],
      requestBody
    ).then(response => {
      if (!isActive) return;
      if (response) {
        setCurrentPageItems(response.currentPageItems)
        setTotalCount(response.totalCount)
      } else {
        setCurrentPageItems([])
        setTotalCount(undefined)
      }
      setIsLoaded(true)
    }).catch(error => {
      if (!isActive) return;
      console.error("Search failed:", error);
      setCurrentPageItems([]);
      setTotalCount(undefined);
      setIsLoaded(true);
    });

    return () => {
      isActive = false;
    };
  }, [searchParams, queryModelType, metadata, schema, complexPost, form, searchTrigger]);

  // 新規登録画面へ遷移する
  const handleNavigateNew = useEvent(() => {
    navigate(`/${rootAggregatePhysicalName}/new`)
  })

  return (
    <Layout.PageFrame
      headerContent={(
        <>
          <Layout.PageFrameTitle>
            {metadata.displayName}
          </Layout.PageFrameTitle>
          <div className="flex-1"></div>
          <Input.IconButton
            onClick={form.handleSubmit(executeSearch)}
            icon={MagnifyingGlassIcon}
            className="mr-2"
          >
            検索
          </Input.IconButton>
          {!metadata.isReadOnly && (
            <Input.IconButton onClick={handleNavigateNew}>新規</Input.IconButton>
          )}
        </>
      )}
      style={MULTI_VIEW_STYLE}
    >
      <PanelGroup direction="vertical">
        <Panel collapsible minSize={8}>
          <form onSubmit={e => {
            form.handleSubmit(executeSearch)(e);
          }} className="h-full overflow-y-auto p-4">
            <Layout.VForm2.LabelText>検索条件</Layout.VForm2.LabelText>
            <ReflectionForm
              mode="search-condition"
              metadataPhysicalName={rootAggregatePhysicalName}
              metadata={metadata}
              schema={schema}
              formMethods={form as unknown as ReactHookForm.UseFormReturn<ReactHookForm.FieldValues>}
            />
          </form>
        </Panel>

        <PanelResizeHandle className="h-2 w-full bg-gray-200 hover:bg-blue-500 transition-colors" />

        <Panel className="relative">
          {!isLoaded && (
            <Layout.NowLoading />
          )}
          {isLoaded && (
            <Layout.EditableGrid
              rows={currentPageItems}
              getColumnDefs={getColumnDefs}
              className="h-full"
            />
          )}
        </Panel>
      </PanelGroup>
    </Layout.PageFrame>
  )
}

const MULTI_VIEW_STYLE: React.CSSProperties = {
  fontFamily: '"ＭＳ 明朝", sans-serif',
}
