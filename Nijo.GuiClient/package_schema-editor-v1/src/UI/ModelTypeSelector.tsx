import React from "react"
import { MODEL_DATA, MODEL_QUERY, MODEL_COMMAND, MODEL_STRUCTURE } from "../backend"
import { DropdownSelector } from "@nijo/ui-components"

/** モデル種類の選択肢 */
type ModelTypeOption = {
  value: string
  displayName: string
  description: string
  nameTextColor: string
  descriptionTextColor: string
}

/** モデル種類の選択肢定義 */
const MODEL_TYPE_OPTIONS: ModelTypeOption[] = [
  {
    value: MODEL_DATA,
    displayName: "Data Model",
    description: "永続化されるデータ。EFCoreの構造定義、自動生成可能なエラーチェック、楽観的排他制御の基本機能が自動生成されます。",
    nameTextColor: "text-orange-600",
    descriptionTextColor: "text-orange-500",
  },
  {
    value: MODEL_QUERY,
    displayName: "Query Model",
    description: "データの検索や照会に特化したモデル。一覧検索処理が自動生成されます。",
    nameTextColor: "text-emerald-600",
    descriptionTextColor: "text-emerald-500",
  },
  {
    value: MODEL_COMMAND,
    displayName: "Command Model",
    description: "引数を受け取り戻り値を返す処理。Webサーバー・クライアント間で常に同期された型定義を提供します。",
    nameTextColor: "text-sky-600",
    descriptionTextColor: "text-sky-500",
  },
  {
    value: MODEL_STRUCTURE,
    displayName: "Structure Model",
    description: "構造体。Webサーバー・クライアント間で常に同期されているべき構造を定義します。",
    nameTextColor: "text-gray-600",
    descriptionTextColor: "text-gray-500",
  }
]

type ModelTypeSelectorForSchemaProps = {
  value?: string
  onChange: (value: string) => void
  className?: string
  autoFocus?: boolean
  onKeyDown?: React.KeyboardEventHandler<HTMLInputElement>
}

/**
 * ルート集約のモデルの型を選択するドロップダウン
 */
export function ModelTypeSelector({
  value,
  onChange,
  className
}: ModelTypeSelectorForSchemaProps) {
  return (
    <DropdownSelector
      value={value}
      onChange={onChange}
      className={className}
    >
      {MODEL_TYPE_OPTIONS.map(option => [
        option.value,
        (
          <span className={`select-none ${option.nameTextColor}`}>
            {option.displayName}
          </span>
        ),
        (
          <div key={option.value} className="p-1">
            <div className={`font-semibold mb-1 ${option.nameTextColor}`}>
              {option.displayName}
            </div>
            <div className={`text-xs leading-relaxed ${option.descriptionTextColor}`}>
              {option.description}
            </div>
          </div>
        )
      ])}
    </DropdownSelector>
  )
}

/**
 * ルート集約のモデルの型を選択するラジオボタン
 */
export function ModelTypeRadioButtonGroup({
  value,
  onChange,
  autoFocus,
  className,
  onKeyDown,
}: ModelTypeSelectorForSchemaProps) {

  React.useEffect(() => {
    if (autoFocus) {
      const firstRadio = document.querySelector<HTMLInputElement>('input[name="modelType"]')
      firstRadio?.focus()
    }
  }, [autoFocus])

  return (
    <div className={`flex flex-col gap-2 ${className ?? ''}`}>
      {MODEL_TYPE_OPTIONS.map(option => (
        <label
          key={option.value}
          className={`
            flex items-start gap-3 p-3 rounded border cursor-pointer transition-colors
            ${value === option.value ? 'bg-blue-50 border-blue-400 ring-1 ring-blue-400' : 'bg-white border-gray-200 hover:bg-gray-50'}
          `}
        >
          <input
            type="radio"
            name="modelType"
            value={option.value}
            checked={value === option.value}
            onChange={() => onChange(option.value)}
            onKeyDown={onKeyDown}
            className="mt-1"
          />
          <div>
            <div className={`font-bold ${option.nameTextColor}`}>
              {option.displayName}
            </div>
            <div className={`text-xs mt-1 leading-snug ${option.descriptionTextColor}`}>
              {option.description}
            </div>
          </div>
        </label>
      ))}
    </div>
  )
}
