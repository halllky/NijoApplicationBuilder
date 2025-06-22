# DiagramView

ノードを自由に配置でき、パン操作やズームを行うことができるダイアグラムビューコンポーネントです。

## 機能

- **ドラッグ可能なウィンドウ**: 各アイテムをドラッグして位置を変更
- **リサイズ**: ウィンドウのサイズを変更
- **パン操作**: 背景をドラッグしてキャンバス全体を移動
- **ズーム**: ズームイン・ズームアウト・リセット機能
- **汎用的な設計**: 任意のアイテム型に対応

## 基本的な使用方法

```tsx
import React from "react"
import { DiagramView, DiagramItem } from "../layout/DiagramView"

// カスタムアイテム型を定義
interface MyItem extends DiagramItem {
  title: string
  content: string
}

function MyDiagramComponent() {
  const [items, setItems] = React.useState<MyItem[]>([
    {
      id: "1",
      title: "アイテム1",
      content: "これはサンプルです",
      layout: { x: 100, y: 100, width: 300, height: 200 }
    }
  ])

  const handleUpdateItem = (index: number, item: MyItem) => {
    const newItems = [...items]
    newItems[index] = item
    setItems(newItems)
  }

  const handleRemoveItem = (index: number) => {
    const newItems = items.filter((_, i) => i !== index)
    setItems(newItems)
  }

  const renderItem = (item: MyItem, index: number, { onRemove, zoom, DragHandle }) => (
    <div className="bg-white border rounded shadow-md">
      <div className="flex items-center bg-gray-100 p-2 rounded-t">
        {DragHandle}
        <h3 className="font-bold flex-1">{item.title}</h3>
        <button onClick={onRemove} className="text-red-500">×</button>
      </div>
      <div className="p-4">
        <p>{item.content}</p>
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
      <div className="absolute top-4 right-4">
        <button onClick={() => {/* 新しいアイテムを追加 */}}>
          アイテム追加
        </button>
      </div>
    </DiagramView>
  )
}
```

## 型定義

### DiagramItem
```tsx
interface DiagramItem {
  id: string
  layout: DiagramItemLayout
}
```

### DiagramItemLayout
```tsx
interface DiagramItemLayout {
  x: number
  y: number
  width: number
  height: number
}
```

### DiagramViewProps
```tsx
interface DiagramViewProps<T extends DiagramItem> {
  items: T[]
  onUpdateItem: (index: number, item: T) => void
  onRemoveItem: (index: number) => void
  renderItem: (item: T, index: number, props: {
    onUpdateLayout: (layout: DiagramItemLayout) => void
    onRemove: () => void
    zoom: number
    DragHandle: React.ReactNode
    handleMouseDown: React.MouseEventHandler<Element>
  }) => React.ReactNode
  className?: string
  children?: React.ReactNode
}
```

## 個別コンポーネント

### DraggableWindow
ドラッグ可能なウィンドウコンポーネント。独立して使用することも可能です。

### ZoomControls
ズーム操作のUIコントロール。

### usePanAndZoom
パンとズーム機能を提供するカスタムフック。

## 操作方法

- **アイテムの移動**: アイテムのハンドル部分（バーアイコン）をドラッグ
- **アイテムのリサイズ**: アイテムの端をドラッグ
- **パン操作**: 背景をドラッグ
- **ズーム**: 右下のズームコントロールを使用
- **位置リセット**: 右下の「位置リセット」ボタンでパン位置をリセット
