import * as React from "react"
import * as ReactRouter from "react-router-dom"
import * as ReactHookForm from "react-hook-form"
import * as ReactResizablePanels from "react-resizable-panels"
import * as Icon from "@heroicons/react/24/solid"
import { UUID } from "uuidjs"
import * as Layout from "../layout"
import * as Input from "../input"
import useEvent from "react-use-event-hook"
import { ApplicationState, ATTR_GENERATE_DEFAULT_QUERY_MODEL, ATTR_TYPE, TYPE_COMMAND_MODEL, TYPE_DATA_MODEL, TYPE_QUERY_MODEL, TYPE_STATIC_ENUM_MODEL, TYPE_VALUE_OBJECT_MODEL, XmlElementItem } from "./types"
import { getNavigationUrl } from "./index"

export const NijoUiSideMenu = ({ onSave, formMethods, onSelected, selectedRootAggregateId }: {
  onSave: (applicationState: ApplicationState) => void
  formMethods: ReactHookForm.UseFormReturn<ApplicationState>
  onSelected: (rootAggregateIndex: number) => void
  selectedRootAggregateId: string | undefined
}) => {
  const { control } = formMethods
  const { fields, append, remove } = ReactHookForm.useFieldArray({ control, name: 'xmlElementTrees' })
  const navigate = ReactRouter.useNavigate()

  const [collapsedItems, setCollapsedItems] = React.useState<Set<string>>(new Set())

  // 集約ツリーを構成する。
  // 折り畳みされていないメニュー項目のみを返す。
  const menuItems = React.useMemo((): SideMenuItem[] => {

    // Enum, ValueOjbect は入れ子になった「属性種類定義」メニューの下に表示
    const memberTypes: SideMenuContainerItem = {
      id: 'member-types',
      indent: 0,
      displayName: '属性種類定義',
      isContainer: true,
    }
    const enumOrValueObjectTypes: SideMenuLeafItem[] = []

    // Data, Query, Command は入れ子にせずトップの階層に表示
    const dataQueryCommandTypes: SideMenuLeafItem[] = []

    for (let i = 0; i < fields.length; i++) {
      const tree = fields[i]
      const rootAggregate = tree.xmlElements[0]
      const type = rootAggregate.attributes[ATTR_TYPE]
      if (type === TYPE_STATIC_ENUM_MODEL || type === TYPE_VALUE_OBJECT_MODEL) {
        // 列挙体 or 値オブジェクト。「属性種類定義」が折り畳みされていたら表示しない
        if (!collapsedItems.has('member-types')) {
          enumOrValueObjectTypes.push({
            id: rootAggregate.uniqueId,
            displayName: rootAggregate.localName!,
            aggregateTree: tree.xmlElements,
            indent: 1,
            rootAggregateIndex: i,
          })
        }
      } else {
        // Data, Query, Command のルート集約
        dataQueryCommandTypes.push({
          id: rootAggregate.uniqueId,
          displayName: rootAggregate.localName!,
          aggregateTree: tree.xmlElements,
          indent: 0,
          rootAggregateIndex: i,
        })
      }
    }
    return [
      ...dataQueryCommandTypes,
      memberTypes,
      ...enumOrValueObjectTypes,
    ]
  }, [fields, collapsedItems])

  // ---------------------------------

  // 保存処理
  const handleSave = useEvent(() => {
    onSave(formMethods.getValues())
  })

  // 新しいルート集約を追加する。名前の入力は必須。
  const handleNewRootAggregate = useEvent(() => {
    const localName = prompt('ルート集約名を入力してください。')
    if (!localName) return
    append({ xmlElements: [{ uniqueId: UUID.generate(), indent: 0, localName, attributes: {} }] })
  })

  // 集約ツリーを選択したときの処理
  const handleSelected = useEvent((menuItem: SideMenuItem) => {
    if (menuItem.isContainer) {
      // 折り畳み/展開の状態を変更
      setCollapsedItems(prev => {
        const newSet = new Set(prev)
        if (newSet.has(menuItem.id)) newSet.delete(menuItem.id)
        else newSet.add(menuItem.id)
        return newSet
      })
    } else {
      // 集約ツリーを選択した旨を親に通知
      onSelected(menuItem.rootAggregateIndex)
    }
  })

  // ルート集約を削除する
  const handleDeleteRootAggregate = useEvent((menuItem: SideMenuItem, e: React.MouseEvent<Element>) => {
    e.stopPropagation()
    if (window.confirm(`${menuItem.displayName}を削除しますか？`)) {
      const index = fields.findIndex(field => field.xmlElements[0].uniqueId === menuItem.id)
      remove(index)
    }
  })

  return (
    <div className="h-full flex flex-col bg-gray-200 border-b border-gray-300">
      {/* アプリケーション名 & ツールアイコン */}
      <div className="flex items-center gap-1 px-1 py-1 border-r border-gray-300">
        <ReactRouter.Link to={getNavigationUrl()} className="text-sm font-bold truncate">
          {formMethods.getValues('applicationName')}
        </ReactRouter.Link>
        <div className="flex-1"></div>
        <Input.IconButton icon={Icon.FolderArrowDownIcon} outline mini hideText onClick={handleSave}>
          変更をnijo.xmlに保存する
        </Input.IconButton>
        <Input.IconButton icon={Icon.PlusIcon} outline mini hideText onClick={handleNewRootAggregate}>
          新しいルート集約を追加する
        </Input.IconButton>
      </div>

      {/* メニュー */}
      <ul className="flex-1 flex flex-col overflow-y-auto">
        {menuItems.map((menuItem, index) => (
          <SideMenuItem
            key={menuItem.id}
            isActive={selectedRootAggregateId === menuItem.id}
            onClick={() => handleSelected(menuItem)}
          >
            <div style={{ flexBasis: `${menuItem.indent * 1.2}rem` }}></div>
            <SideMenuItemIcon menuItem={menuItem} collapsedItems={collapsedItems} />
            <SideMenuItemLabel>
              {menuItem.displayName}
            </SideMenuItemLabel>
            {selectedRootAggregateId === menuItem.id && (
              <Input.IconButton icon={Icon.TrashIcon} mini hideText onClick={e => handleDeleteRootAggregate(menuItem, e)}>
                削除
              </Input.IconButton>
            )}
          </SideMenuItem>
        ))}
        <li className="flex-1 border-r border-gray-300"></li>
      </ul>

      {/* ボトム部分 */}
      <ul className="flex flex-col">
        <li className="basis-2 border-r border-gray-300"></li>
        <ReactRouter.NavLink to={getNavigationUrl({ page: 'debug-menu' })}>
          {({ isActive }) => (
            <SideMenuItem isActive={isActive} onClick={() => navigate(getNavigationUrl({ page: 'debug-menu' }))}>
              <>
                <Icon.PlayIcon className={`${SIDEMENU_ICON_CLASSNAME} text-emerald-600`} />
                <SideMenuItemLabel>
                  デバッグメニュー
                </SideMenuItemLabel>
              </>
            </SideMenuItem>
          )}
        </ReactRouter.NavLink>
      </ul>
    </div>
  )
}

const SideMenuItem = ({ isActive, onClick, children }: {
  isActive: boolean
  onClick: () => void
  children: React.ReactNode
}) => {
  return (
    <li className={`flex items-center gap-px py-px cursor-pointer border-y ${isActive
      ? 'bg-white border-gray-300'
      : 'border-r border-r-gray-300 border-y-transparent'}`} onClick={onClick}>
      {children}
    </li>
  )
}

/**
 * サイドメニューの要素のアイコン
 */
const SideMenuItemIcon = ({ menuItem, collapsedItems }: {
  menuItem: SideMenuItem
  collapsedItems: Set<string>
}): React.ReactNode => {

  // コンテナなら折り畳み/展開のアイコンを表示
  if (menuItem.isContainer) {
    if (collapsedItems.has(menuItem.id))
      return <Icon.ChevronDownIcon className={`${SIDEMENU_ICON_CLASSNAME} text-gray-500`} />
    else
      return <Icon.ChevronRightIcon className={`${SIDEMENU_ICON_CLASSNAME} text-gray-500`} />
  }

  // ルート集約なら集約ごとのアイコンを表示
  const modelType = menuItem.aggregateTree[0].attributes[ATTR_TYPE]
  if (modelType === TYPE_DATA_MODEL) {
    if (menuItem.aggregateTree[0].attributes[ATTR_GENERATE_DEFAULT_QUERY_MODEL] === 'True') {
      // DataModelの場合、QueryModelを生成するのであればQueryModelのアイコンも併記する
      return <DataAndQueryModelIcon className={SIDEMENU_ICON_CLASSNAME} />
    } else {
      return <Icon.CircleStackIcon className={`${SIDEMENU_ICON_CLASSNAME} text-orange-600`} />
    }
  }
  if (modelType === TYPE_QUERY_MODEL) return <Icon.TableCellsIcon className={`${SIDEMENU_ICON_CLASSNAME} text-emerald-600`} />
  if (modelType === TYPE_COMMAND_MODEL) return <Icon.CommandLineIcon className={`${SIDEMENU_ICON_CLASSNAME} text-sky-600`} />
  if (modelType === TYPE_STATIC_ENUM_MODEL) return <Icon.ListBulletIcon className={`${SIDEMENU_ICON_CLASSNAME} text-blue-500`} />
  if (modelType === TYPE_VALUE_OBJECT_MODEL) return <Icon.CubeTransparentIcon className={`${SIDEMENU_ICON_CLASSNAME} text-purple-500`} />

  // 不明な種類のルート集約
  return <Icon.QuestionMarkCircleIcon className={`${SIDEMENU_ICON_CLASSNAME} text-gray-500`} />
}
/** サイドメニューのアイコンのクラス名 */
const SIDEMENU_ICON_CLASSNAME = 'w-4 h-4 min-w-4 min-h-4'

/** DataModelとQueryModelを混ぜたもの */
export const DataAndQueryModelIcon = ({ className }: { className?: string }) => {
  return (
    <div className={`${className} relative`}>
      <Icon.TableCellsIcon className={`w-[12px] h-[12px] text-emerald-600 absolute top-0 left-0`} />
      <Icon.CircleStackIcon className={`w-[12px] h-[12px] text-orange-600 absolute bottom-0 right-0`} />
    </div>
  )
}

/**
 * サイドメニューの集約要素のラベル
 */
const SideMenuItemLabel = ({ onClick, children }: {
  onClick?: () => void
  children: React.ReactNode
}) => {
  return (
    <div className="flex-1 flex items-center gap-1 text-sm text-gray-600 pl-1 select-none truncate" onClick={onClick}>
      {children}
    </div>
  )
}

/** サイドメニューの要素 */
type SideMenuItem = SideMenuContainerItem | SideMenuLeafItem

type SideMenuContainerItem = {
  id: string
  indent: number
  displayName: string
  isContainer: true
}
type SideMenuLeafItem = {
  id: string
  indent: number
  displayName: string
  aggregateTree: XmlElementItem[]
  rootAggregateIndex: number
  isContainer?: never
}
