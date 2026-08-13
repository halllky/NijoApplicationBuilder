import React from "react"
import * as EG2 from "@nijo/ui-components/layout/EditableGrid2"
import { formatNumber } from "./formatNumber"

/**
 * 読み取り専用列で指定できるオプション。
 * renderHeader / renderBody / editor / getValueForEditor / setValueFromEditor は
 * 各ビルダー関数が組み立てるため、呼び出し側からは指定できない。
 */
export type ReadOnlyColumnOptions<TRow> = Omit<
  EG2.EditableGrid2LeafColumn<TRow>,
  'renderHeader' | 'renderBody' | 'editor' | 'getValueForEditor' | 'setValueFromEditor'
>

/**
 * 文字列表示列。
 * 編集はできないが、値は getValueForEditor 経由で Ctrl+C のコピー対象になる。
 * 折り返さない場合は truncate 表示になり、title 属性でツールチップを表示する。
 */
export function textColumn<TRow>(
  header: React.ReactNode,
  getValue: (row: TRow) => string | number | null | undefined,
  options?: ReadOnlyColumnOptions<TRow>,
): EG2.EditableGrid2LeafColumn<TRow> {
  return {
    renderHeader: () => (
      <div className="px-1 py-px truncate text-gray-700">{header}</div>
    ),
    renderBody: ({ context }) => {
      const value = getValue(context.row.original) ?? ''
      return options?.wrap ? (
        <div className="w-full px-1 py-px whitespace-pre-wrap">{value}</div>
      ) : (
        <div className="w-full px-1 py-px truncate" title={String(value)}>{value}</div>
      )
    },
    getValueForEditor: ({ row }) => String(getValue(row) ?? ''),
    ...options,
  }
}

/** 数値表示列。右寄せ・3桁カンマ区切り・接尾辞つき（旧 DataTable.tsx の NumericCell 相当）。 */
export function numericColumn<TRow>(
  header: React.ReactNode,
  getValue: (row: TRow) => unknown,
  options?: ReadOnlyColumnOptions<TRow> & { suffix?: string },
): EG2.EditableGrid2LeafColumn<TRow> {
  const { suffix, ...rest } = options ?? {}
  return {
    renderHeader: () => (
      <div className="px-1 py-px truncate text-gray-700">{header}</div>
    ),
    renderBody: ({ context }) => (
      <div className="w-full px-1 py-px truncate text-right">
        {formatNumber(getValue(context.row.original), suffix)}
      </div>
    ),
    getValueForEditor: ({ row }) => formatNumber(getValue(row), suffix),
    ...rest,
  }
}

/** 任意の ReactNode を描画する読み取り専用列。 */
export function customColumn<TRow>(
  header: React.ReactNode,
  render: (row: TRow) => React.ReactNode,
  options?: ReadOnlyColumnOptions<TRow> & { getValueForCopy?: (row: TRow) => string },
): EG2.EditableGrid2LeafColumn<TRow> {
  const { getValueForCopy, ...rest } = options ?? {}
  return {
    renderHeader: () => (
      <div className="px-1 py-px truncate text-gray-700">{header}</div>
    ),
    renderBody: ({ context }) => (
      <div className="w-full px-1 py-px truncate">{render(context.row.original)}</div>
    ),
    getValueForEditor: ({ row }) => getValueForCopy?.(row) ?? '',
    ...rest,
  }
}

/**
 * リンクやボタンなど、クリック可能な要素を含む列。
 * mouseDown の伝播を止めることで、その要素をクリックしてもグリッドのセル選択が発生しないようにする
 * （EditableGrid2Column の renderBody のドキュメントコメントで要求されている作法）。
 */
export function interactiveColumn<TRow>(
  header: React.ReactNode,
  render: (row: TRow) => React.ReactNode,
  options?: ReadOnlyColumnOptions<TRow> & { getValueForCopy?: (row: TRow) => string },
): EG2.EditableGrid2LeafColumn<TRow> {
  const { getValueForCopy, ...rest } = options ?? {}
  return {
    renderHeader: () => (
      <div className="px-1 py-px truncate text-gray-700">{header}</div>
    ),
    renderBody: ({ context }) => (
      <div
        className="w-full h-full px-1 py-px flex items-center gap-1"
        onMouseDown={e => e.stopPropagation()}
      >
        {render(context.row.original)}
      </div>
    ),
    getValueForEditor: ({ row }) => getValueForCopy?.(row) ?? '',
    ...rest,
  }
}
