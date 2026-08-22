import * as React from "react"
import * as ReactRouter from "react-router-dom"
import DynamicFormDebugging from "./DynamicFormDebugging"
import _001_セクションと配列の基本的組み合わせ from "@nijo/ui-components/layout/DynamicForm/__tests__/001_セクションと配列の基本的組み合わせ"
import _002_値メンバーの各種型 from "@nijo/ui-components/layout/DynamicForm/__tests__/002_値メンバーの各種型"
import _003_カスタムレンダリング from "@nijo/ui-components/layout/DynamicForm/__tests__/003_カスタムレンダリング"
import _004_カスタムレンダリング2 from "@nijo/ui-components/layout/DynamicForm/__tests__/004_カスタムレンダリング2"
import _005_複雑なネスト構造 from "@nijo/ui-components/layout/DynamicForm/__tests__/005_複雑なネスト構造"
import _006_空データ構造 from "@nijo/ui-components/layout/DynamicForm/__tests__/006_空データ構造"
import _007_グリッド機能 from "@nijo/ui-components/layout/DynamicForm/__tests__/007_グリッド機能"
import FormLayoutPatternsDebugging from "./FormLayoutPatternsDebugging"
import EditableGridDebugging from "./EditableGridDebugging"
import EditableGridHookTest from "./EditableGridHookTest"
import GraphView2Debugging from "./GraphView2Debugging"
import ReactContextDebugging from "./ReactContextDebugging"

export default function () {

  const pathname = ReactRouter.useLocation().pathname
  const isHereIndex = React.useMemo(() => {
    return pathname === '/'
  }, [pathname])

  const currentPage = React.useMemo(() => {
    return getDebuggingPages()
      .flatMap(group => group.links)
      .find(link => link.path === pathname)
  }, [pathname])

  const menuItems = React.useMemo(() => {
    return getDebuggingPages()
  }, [])

  return (
    <div className="w-full h-full flex flex-col gap-2 p-4">

      <h1 className="text-xl font-bold flex items-center gap-2">
        <ReactRouter.Link to="/">
          UIデバッグ画面
        </ReactRouter.Link>
        {currentPage && (<>
          <span className="text-gray-500">&gt;</span>
          <span className="">{currentPage.label}</span>
        </>)}
      </h1>

      <hr className="border-t border-gray-300" />

      {isHereIndex && menuItems.map((group, index) => (
        <div key={index} className="flex flex-col gap-2">

          <h2 className="font-bold">{group.groupName}</h2>

          <div className="flex flex-col gap-2 pl-4">
            {group.links.map((link, index) => (
              <ReactRouter.Link key={index} to={link.path ?? ''} className="text-sky-600 underline">
                {link.label}
              </ReactRouter.Link>
            ))}
          </div>
        </div>
      ))}

      <ReactRouter.Outlet />
    </div>
  )
}

// -------------------------------------

export const getDebuggingPages = (): { groupName: string, links: (ReactRouter.RouteObject & { label: string })[] }[] => [
  {
    groupName: '刷新後',
    links: [
      {
        path: '/graph-view-2/001',
        label: 'GraphView2 基本機能',
        element: <GraphView2Debugging />,
      },
      {
        path: '/react-context/001',
        label: 'React Context レンダリング調査',
        element: <ReactContextDebugging />,
      }
    ]
  },
  {
    groupName: 'FormLayoutのデバッグ',
    links: [
      {
        path: '/form-layout/001',
        label: 'FormLayout レイアウト構造パターン集',
        element: <FormLayoutPatternsDebugging />,
      }
    ],
  },
  {
    groupName: 'EditableGrid(旧)のデバッグ',
    links: [
      {
        path: '/editable-grid/001',
        label: 'EditableGrid 基本機能',
        element: <EditableGridDebugging mode="basic" />,
      },
      {
        path: '/editable-grid/002',
        label: 'EditableGrid 高度な機能',
        element: <EditableGridDebugging mode="advanced" />,
      },
      {
        path: '/editable-grid/003',
        label: 'EditableGrid 読み取り専用',
        element: <EditableGridDebugging mode="readonly" />,
      },
      {
        path: '/editable-grid/004',
        label: 'EditableGrid カスタムレンダリング',
        element: <EditableGridDebugging mode="custom" />,
      },
      {
        path: '/editable-grid/005',
        label: 'EditableGrid キーボード操作',
        element: <EditableGridDebugging mode="keyboard" />,
      },
      {
        path: '/editable-grid/006',
        label: 'EditableGrid 行選択機能',
        element: <EditableGridDebugging mode="selection" />,
      },
      {
        path: '/editable-grid/007',
        label: 'EditableGrid 列グループ混在',
        element: <EditableGridDebugging mode="mixed-groups" />,
      },
      {
        path: '/editable-grid/008',
        label: 'EditableGrid フック使用テスト',
        element: <EditableGridHookTest />,
      },
    ],
  },
  {
    groupName: 'DynamicFormのデバッグ',
    links: [
      {
        path: '/dynamic-form/001',
        label: '001_セクションと配列の基本的組み合わせ',
        element: <DynamicFormDebugging getSchema={_001_セクションと配列の基本的組み合わせ} />,
      },
      {
        path: '/dynamic-form/002',
        label: '002_値メンバーの各種型',
        element: <DynamicFormDebugging getSchema={_002_値メンバーの各種型} />,
      },
      {
        path: '/dynamic-form/003',
        label: '003_カスタムレンダリング',
        element: <DynamicFormDebugging getSchema={_003_カスタムレンダリング} />,
      },
      {
        path: '/dynamic-form/004',
        label: '004_カスタムレンダリング2',
        element: <DynamicFormDebugging getSchema={_004_カスタムレンダリング2} />,
      },
      {
        path: '/dynamic-form/005',
        label: '005_複雑なネスト構造',
        element: <DynamicFormDebugging getSchema={_005_複雑なネスト構造} />,
      },
      {
        path: '/dynamic-form/006',
        label: '006_空データ構造',
        element: <DynamicFormDebugging getSchema={_006_空データ構造} />,
      },
      {
        path: '/dynamic-form/007',
        label: '007_グリッド機能',
        element: <DynamicFormDebugging getSchema={_007_グリッド機能} />,
      },
    ],
  },
]
