import React, { forwardRef, useCallback, useEffect, useImperativeHandle, useMemo, useRef } from "react"
import { FieldArrayPath, FieldArrayWithId, FieldValues } from "react-hook-form"
import { AgGridReact, AgGridReactProps } from "ag-grid-react"
import { ColDef, GridReadyEvent, ICellEditorParams, ValueFormatterFunc } from "ag-grid-community"
import * as Util from "../util"
import 'ag-grid-community/styles/ag-grid.css'
import 'ag-grid-community/styles/ag-theme-alpine.css'

// TODO
// - カスタムセルに関するロジックを1カ所に集約する。
//   その際、Num等のロジックが通常用とCellEditor用の2か所にばらけてはならない。
//   - Input.tsx 内のコンポーネント全てに共通するAPIを策定するためにコンボボックスの型定義を精査
//   - Input.tsx 内のコンポーネント全てに共通するAPIを策定する
//     - For react hook form
//       - {...register('...')} できるようにする。
//         ただ最低限nameさえあればなんとかなるのと、propsをHTMLAttributesで指定すればよい
//       - { value?: T } ※forwardRefで定義。register設定先の必須項目。またおそらくblur時に参照されている
//     - For ag-grid
//       - { getValue: () => T | undefined } ※propsではなくrefで定義しないといけない
//       - className?: string // w-full h-full border-none のために必要
//       - elementRef?: React.RefObject<HTMLElement> // グリッドでの編集開始時にフォーカスを当てるのに必要
//   - CellEditorはそのAPIに依存するようにする
//   - createColDefの引数が以下となるようにする
//     1. カラムと対応するプロパティ名
//     2. 関数コンポーネント（通常のInput.Num, Input.Date など）
// - できればvalueFormatterも上記APIが持つようにしたい。
//   値から文字列への変換のロジックがInput内の各コンポーネント内にまとまるので

export const AgGridWrapper = forwardRef(<T,>(props: AgGridReactProps<T>, ref: React.ForwardedRef<AgGridReact<T>>) => {
  const { data: {
    darkMode,
    fontFamily,
  } } = Util.useUserSetting()

  const divRef = useRef<HTMLDivElement>(null)
  const gridRef = useRef<AgGridReact<T>>(null)
  useImperativeHandle(ref, () => gridRef.current!)

  // 画面表示時
  const onGridReady = useCallback((e: GridReadyEvent<T>) => {
    // グリッドが非表示のタブ中にあるなど見えていない状態でなければ、自動列幅調整をする
    if (divRef.current?.offsetParent != null) {
      gridRef.current?.columnApi.autoSizeAllColumns()
    }
    props.onGridReady?.(e)
  }, [props.onGridReady])

  const {
    className,
    ...gridProps
  } = props

  return (
    <div ref={divRef}
      className={`ag-theme-alpine compact ${(darkMode ? 'dark' : '')} ${className}`}
      style={{ fontFamily: fontFamily ?? Util.DEFAULT_FONT_FAMILY }}
    >
      <AgGridReact
        ref={gridRef}
        {...gridProps}
        rowData={props.rowData}
        multiSortKey={props.multiSortKey ?? 'ctrl'}
        rowSelection={props.rowSelection ?? 'multiple'}
        undoRedoCellEditing={props.undoRedoCellEditing ?? true}
        undoRedoCellEditingLimit={props.undoRedoCellEditingLimit ?? 20}
        onGridReady={onGridReady}
      >
      </AgGridReact>
    </div>
  )
}) as <T>(p: AgGridReactProps<T> & { ref?: React.Ref<AgGridReact<T>> }) => JSX.Element;


export const createColumnDefinitions = <
  TRoot extends FieldValues = FieldValues,
  TFieldArrayName extends FieldArrayPath<TRoot> = FieldArrayPath<TRoot>,
  TRow extends FieldArrayWithId<TRoot, TFieldArrayName, "id"> = FieldArrayWithId<TRoot, TFieldArrayName, "id">
>(
  arrayPath: TFieldArrayName,
  fn: (addColumn: (
    field: ColDef<TRow>['field'],
    editor: any,
    editorProp?: {},
    valueFormatter?: ValueFormatterFunc<TRow>,
  ) => ColDef<TRow>) => ColDef<TRow>[]
) => {
  const defaultValueFormatter: ValueFormatterFunc<TRow> = item => {
    switch (typeof item.value) {
      case 'boolean':
        return item.value ? '○' : '-'
      case 'undefined':
        return ''
      case 'object':
        if (item.value === null) return ''
        const asOpt = item.value as { text: string } | undefined
        if (typeof asOpt?.text === 'string') return asOpt.text
        return JSON.stringify(item.value) // Unexpected object
      default:
        return item.value as string
    }
  }

  return fn((
    field: ColDef<TRow>['field'],
    editor: any,
    editorProp?: {},
    valueFormatter?: ValueFormatterFunc<TRow>,
  ): ColDef<TRow> => ({
    field,
    cellDataType: false, // セル型の自動推論を無効にする
    resizable: true,
    editable: true,
    valueFormatter: valueFormatter ?? defaultValueFormatter,
    cellEditor: generateCellEditor(arrayPath, editor, editorProp),
    cellEditorParams: editorProp,
    cellEditorPopup: true,
  }))
}

export const generateCellEditor = (arrayPath: string, editor: any, editorProp?: {}) => {
  return React.memo(forwardRef<ICellEditorRef, ICellEditorParams>((props, ref) => {

    // 編集開始時にフォーカス
    const customRef = useRef<Util.CustomComponentRef>(null)
    useEffect(() => {
      customRef.current?.focus()
    }, [])

    // for react hook form
    const { registerEx } = Util.useFormContextEx()
    const name = useMemo(() => {
      return `${arrayPath}.[${props.rowIndex}].${props.colDef.field}`
    }, [props.rowIndex, props.colDef.field])
    const { value, onChange } = registerEx(name)

    useImperativeHandle(ref, () => ({
      // ag-gridのカスタムセルではこの名前の関数で編集後の値をセルに戻す
      getValue: () => {
        const currentValue = customRef.current?.getValue()
        // getValueはblurより前に実行されるので、
        // TextInputBaseの値をこのタイミングで反映するためにonChangeを呼ぶ
        onChange?.(currentValue)
        // TODO: ここでreact hook form にsetValueしないといけない
        return currentValue
      },
      isCancelBeforeStart: () => false,
      isCancelAfterEnd: () => false,
    }), [onChange])

    return React.createElement(editor, {
      ref: customRef,
      className: 'border-none',
      value,
      onChange,
      ...editorProp,
    })
  }))
}

// ----------------------------------

type ICellEditorRef = {
  /** ag-gridは編集終了時にこの名前の関数を呼んでエディターから通常セルに値を渡す */
  getValue: () => any
}
