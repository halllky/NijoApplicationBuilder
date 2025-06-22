import React from "react"
import { DiagramView, DiagramItem } from "./index"
import { UUID } from "uuidjs"

// カスタムアイテム型を定義
interface ExampleItem extends DiagramItem {
  title: string
  content: string
  color: string
}

/**
 * DiagramViewの使用例
 */
export default function ExampleUsage() {
  const [items, setItems] = React.useState<ExampleItem[]>([
    {
      id: UUID.generate(),
      title: "アイテム1",
      content: "これはサンプルです。ドラッグハンドルを使って移動できます。",
      color: "bg-blue-100",
      layout: { x: 100, y: 100, width: 300, height: 200 }
    },
    {
      id: UUID.generate(),
      title: "アイテム2",
      content: "別のアイテムです。リサイズも可能です。",
      color: "bg-green-100",
      layout: { x: 450, y: 150, width: 280, height: 180 }
    }
  ])

  const handleUpdateItem = (index: number, item: ExampleItem) => {
    const newItems = [...items]
    newItems[index] = item
    setItems(newItems)
  }

  const handleRemoveItem = (index: number) => {
    if (!window.confirm("アイテムを削除しますか？")) return
    const newItems = items.filter((_, i) => i !== index)
    setItems(newItems)
  }

  const handleAddItem = () => {
    const title = window.prompt("アイテムのタイトルを入力してください")
    if (!title) return

    const colors = ["bg-blue-100", "bg-green-100", "bg-yellow-100", "bg-red-100", "bg-purple-100"]
    const randomColor = colors[Math.floor(Math.random() * colors.length)]

    const newItem: ExampleItem = {
      id: UUID.generate(),
      title,
      content: "新しく追加されたアイテムです。",
      color: randomColor,
      layout: {
        x: Math.random() * 400,
        y: Math.random() * 300,
        width: 300,
        height: 200
      }
    }

    setItems([...items, newItem])
  }

  const renderItem = (item: ExampleItem, index: number, { onRemove, zoom, DragHandle }: {
    onUpdateLayout: (layout: any) => void
    onRemove: () => void
    zoom: number
    DragHandle: React.ReactNode
    handleMouseDown: React.MouseEventHandler<Element>
  }) => (
    <div className={`border rounded shadow-md ${item.color}`}>
      <div className="flex items-center bg-gray-50 p-2 rounded-t border-b">
        {DragHandle}
        <h3 className="font-bold flex-1 text-sm">{item.title}</h3>
        <button
          onClick={onRemove}
          className="text-red-500 hover:text-red-700 px-2 py-1 text-sm"
        >
          ×
        </button>
      </div>
      <div className="p-3">
        <p className="text-sm text-gray-700">{item.content}</p>
        <div className="mt-2 text-xs text-gray-500">
          位置: ({Math.round(item.layout.x)}, {Math.round(item.layout.y)})
          <br />
          サイズ: {Math.round(item.layout.width)} × {Math.round(item.layout.height)}
          <br />
          ズーム: {Math.round(zoom * 100)}%
        </div>
      </div>
    </div>
  )

  return (
    <DiagramView
      items={items}
      onUpdateItem={handleUpdateItem}
      onRemoveItem={handleRemoveItem}
      renderItem={renderItem}
      className="w-full h-screen"
    >
      {/* カスタムUIを追加 */}
      <div className="absolute top-4 right-4 flex flex-col gap-2">
        <button
          onClick={handleAddItem}
          className="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 text-sm"
        >
          アイテム追加
        </button>
        <div className="bg-white border rounded p-2 text-xs text-gray-600">
          <div className="font-bold mb-1">操作方法:</div>
          <div>• ハンドル部分をドラッグして移動</div>
          <div>• 端をドラッグしてリサイズ</div>
          <div>• 背景をドラッグしてパン</div>
          <div>• 右下でズーム操作</div>
        </div>
      </div>
    </DiagramView>
  )
}
