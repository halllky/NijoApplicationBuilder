import * as React from "react"
import * as ReactHookForm from "react-hook-form"
import * as ReactResizablePanels from "react-resizable-panels"
import * as Icon from "@heroicons/react/24/solid"
import { UUID } from "uuidjs"
import * as Layout from "../../layout"
import * as Input from "../../input"
import useEvent from "react-use-event-hook"
import { ApplicationState, ATTR_TYPE, TYPE_COMMAND_MODEL, TYPE_DATA_MODEL, TYPE_QUERY_MODEL, TYPE_STATIC_ENUM_MODEL, TYPE_VALUE_OBJECT_MODEL, XmlElementItem } from "./types"

export const NijoUiSideMenu = ({ formMethods, onSelected, selectedRootAggregateId }: {
  formMethods: ReactHookForm.UseFormReturn<ApplicationState>
  onSelected: (rootAggregateIndex: number) => void
  selectedRootAggregateId: string | undefined
}) => {
  const { control } = formMethods
  const { fields, append } = ReactHookForm.useFieldArray({ control, name: 'xmlElementTrees' })

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
      const type = rootAggregate.attributes?.get(ATTR_TYPE)
      if (type === TYPE_STATIC_ENUM_MODEL || type === TYPE_VALUE_OBJECT_MODEL) {
        // 列挙体 or 値オブジェクト。「属性種類定義」が折り畳みされていたら表示しない
        if (!collapsedItems.has('member-types')) {
          enumOrValueObjectTypes.push({
            id: rootAggregate.id,
            displayName: rootAggregate.localName!,
            aggregateTree: tree.xmlElements,
            indent: 1,
            rootAggregateIndex: i,
          })
        }
      } else {
        // Data, Query, Command のルート集約
        dataQueryCommandTypes.push({
          id: rootAggregate.id,
          displayName: rootAggregate.localName!,
          aggregateTree: tree.xmlElements,
          indent: 0,
          rootAggregateIndex: i,
        })
      }
    }
    return [
      memberTypes,
      ...enumOrValueObjectTypes,
      ...dataQueryCommandTypes,
    ]
  }, [fields, collapsedItems])

  // ---------------------------------

  // 新しいルート集約を追加する。名前の入力は必須。
  const handleNewRootAggregate = useEvent(() => {
    const localName = prompt('ルート集約名を入力してください。')
    if (!localName) return
    append({ xmlElements: [{ id: UUID.generate(), indent: 0, localName }] })
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

  return (
    <div className="h-full flex flex-col gap-1 bg-gray-200">
      {/* ツールアイコン */}
      <div className="flex justify-end gap-1 px-1 py-px">
        <Input.IconButton icon={Icon.PlusIcon} outline mini hideText onClick={handleNewRootAggregate}>
          追加
        </Input.IconButton>
      </div>

      {/* 集約ツリーの一覧 */}
      <ul className="flex-1 flex flex-col gap-1 overflow-y-auto">
        {menuItems.map(menuItem => (
          <li
            key={menuItem.id}
            onClick={() => handleSelected(menuItem)}
            className={`flex items-center gap-px cursor-pointer ${selectedRootAggregateId === menuItem.id ? 'bg-gray-100' : ''}`}
          >
            <div style={{ flexBasis: `${menuItem.indent * 1.2}rem` }}></div>
            <SideMenuItemIcon menuItem={menuItem} collapsedItems={collapsedItems} />
            <SideMenuItemLabel>{menuItem.displayName}</SideMenuItemLabel>
          </li>
        ))}
      </ul>
    </div>
  )
}

/**
 * サイドメニューの要素のアイコン
 */
const SideMenuItemIcon = ({ menuItem, collapsedItems }: {
  menuItem: SideMenuItem
  collapsedItems: Set<string>
}): React.ReactNode => {
  const className = 'w-4 h-4 min-w-4 min-h-4'

  // コンテナなら折り畳み/展開のアイコンを表示
  if (menuItem.isContainer) {
    if (collapsedItems.has(menuItem.id))
      return <Icon.ChevronDownIcon className={`${className} text-gray-500`} />
    else
      return <Icon.ChevronRightIcon className={`${className} text-gray-500`} />
  }

  // ルート集約なら集約ごとのアイコンを表示
  const modelType = menuItem.aggregateTree[0].attributes?.get(ATTR_TYPE)
  if (modelType === TYPE_DATA_MODEL) return <Icon.CircleStackIcon className={`${className} text-orange-500`} />
  if (modelType === TYPE_QUERY_MODEL) return <Icon.MagnifyingGlassIcon className={`${className} text-green-500`} />
  if (modelType === TYPE_COMMAND_MODEL) return <Icon.ArrowPathIcon className={`${className} text-red-500`} />
  if (modelType === TYPE_STATIC_ENUM_MODEL) return <Icon.ListBulletIcon className={`${className} text-blue-500`} />
  if (modelType === TYPE_VALUE_OBJECT_MODEL) return <Icon.CubeTransparentIcon className={`${className} text-purple-500`} />

  // 不明な種類のルート集約
  return <Icon.DocumentTextIcon className={`${className} text-gray-500`} />
}

const SideMenuItemLabel = ({ children, onClick }: { children?: React.ReactNode, onClick?: () => void }) => {
  return (
    <div className="flex-1 text-sm text-gray-600 pl-1 select-none truncate" onClick={onClick}>
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
