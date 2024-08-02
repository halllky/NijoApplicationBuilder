import * as RT from '@tanstack/react-table'

export const TABLE_ZINDEX = {
  CELLEDITOR: 30 as const,
  ROWHEADER_THEAD: 21 as const,
  THEAD: 20 as const,
  ROWHEADER_SELECTION: 11 as const,
  ROWHEADER: 10 as const,
  SELECTION: 1 as const,
}

export type CellPosition = {
  rowIndex: number
  colIndex: number
}

export type CellEditorRef<T> = {
  focus: () => void
  startEditing: (cell: RT.Cell<T, unknown>) => void
}
