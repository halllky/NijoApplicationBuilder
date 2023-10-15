import React, { useState, useCallback, useMemo, useImperativeHandle, forwardRef, useRef } from "react"
import { useQuery } from "react-query"
import { Combobox } from "@headlessui/react"
import { ChevronUpDownIcon } from "@heroicons/react/24/outline"
import { NowLoading } from "./NowLoading"
import { useAppContext } from "../hooks/AppContext"
import { Word } from "./InputForms"
import { useFormContext } from "react-hook-form"
import { useFocusTarget } from "../hooks"

type ComboBoxProps<T> = {
  selectedItem: T | null | undefined
  onSelectedItemChanged: (item: T | null | undefined) => void
  keySelector: (item: T | null) => string
  textSelector: (item: T | null) => string
  readOnly?: boolean
  className?: string
}
type SyncComboBoxProps<T> = React.SelectHTMLAttributes<HTMLSelectElement> & {
  data: T[]
  onSelectedItemChanged?: (item: T | null | undefined) => void
  keySelector: (item: T | null) => string
  textSelector: (item: T | null) => string
  readOnly?: boolean
  className?: string
  // ag-grid CellEditorの場合はrowIndexと組み合わせてこのコンポーネントの中でIDを組み立てる
  reactHookFormId: string | ((rowIndex: number) => string)
  // ag-grid CellEditor用
  rowIndex?: number
}
type AsyncComboBoxProps<T> = ComboBoxProps<T> & {
  queryKey: string[]
  queryFn: (keyword: string) => Promise<T[]>
}
type ComboBoxBaseProps<T> = ComboBoxProps<T> & {
  data: T[]
  nowLoading: boolean
  onChangeKeyword: (e: React.ChangeEvent<HTMLInputElement>) => void
  onBlurKeyword?: (e: React.FocusEvent<HTMLInputElement>) => void
}


/**
 * データソースがソースコード上に存在するコンボボックス
 */
export const SyncComboBox = forwardRef(<T,>(props: SyncComboBoxProps<T>, ref: React.ForwardedRef<unknown>) => {
  // react hook form
  const { watch, setValue } = useFormContext()
  const rhfId = useMemo(() => {
    if (typeof props.reactHookFormId === 'string') {
      return props.reactHookFormId
    } else if (props.rowIndex !== undefined) {
      return props.reactHookFormId(props.rowIndex)
    } else {
      throw Error('IDが関数の場合(グリッドセルの場合)はrowIndex必須')
    }
  }, [props.reactHookFormId, props.rowIndex])
  const selectedItem = useMemo(() => {
    const selectedItemKey = watch(rhfId)
    return props.data.find(item => props.keySelector(item) === selectedItemKey) ?? null
  }, [props.data, watch(rhfId)])

  const onSelectedItemChanged = useCallback((item: T | null | undefined) => {
    setValue(rhfId, item ? props.keySelector(item) : undefined)
    props.onSelectedItemChanged?.(item)
  }, [rhfId, setValue, props.onSelectedItemChanged])

  // フィルタリング処理
  const [keyword, setKeyword] = useState('')
  const filteredData = useMemo(() => {
    const matched = props.data.filter(item => {
      if (!item) return false
      if (props.keySelector(item).includes(keyword)) return true
      if (props.textSelector(item).includes(keyword)) return true
      return false
    })
    return [null, ...matched]
  }, [keyword, props.data, props.keySelector, props.textSelector])

  const onChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    setKeyword(e.target.value)
  }, [setKeyword])

  // ag-grid CellEditor用
  useImperativeHandle(ref, () => ({
    getValue: () => selectedItem,
    isCancelBeforeStart: () => false,
    isCancelAfterEnd: () => false,
  }))

  const { data, ...rest } = props

  return (
    <ComboBoxBase<T | null>
      {...rest}
      data={filteredData}
      nowLoading={false}
      onChangeKeyword={onChange}
      selectedItem={selectedItem}
      onSelectedItemChanged={onSelectedItemChanged}
    />
  )
}) as <T>(props: SyncComboBoxProps<T>) => JSX.Element


/**
 * データソースがソースコード上に無く外部に取得しにいく必要があるコンボボックス
 */
export const AsyncComboBox = <T,>(props: AsyncComboBoxProps<T>) => {
  const [keyword, setKeyword] = useState('')
  const [, dispatch] = useAppContext()
  const { data, refetch, isFetching } = useQuery({
    queryKey: props.queryKey,
    queryFn: async () => {
      return await props.queryFn(keyword)
    },
    onError: error => {
      dispatch({ type: 'pushMsg', msg: `ERROR!: ${JSON.stringify(error)}` })
    },
  })

  const [setTimeoutHandle, setSetTimeoutHandle] = useState<NodeJS.Timeout | undefined>(undefined)
  const onChangeKeyword = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    setKeyword(e.target.value)
    if (setTimeoutHandle !== undefined) clearTimeout(setTimeoutHandle)
    setSetTimeoutHandle(setTimeout(() => {
      refetch()
      setSetTimeoutHandle(undefined)
    }, 300))
  }, [setKeyword, setTimeoutHandle, setSetTimeoutHandle, refetch])
  const onBlurKeyword = useCallback(() => {
    if (setTimeoutHandle !== undefined) clearTimeout(setTimeoutHandle)
    setSetTimeoutHandle(undefined)
    refetch()
  }, [setTimeoutHandle, setSetTimeoutHandle, refetch])

  return (
    <ComboBoxBase
      {...props}
      data={data || []}
      nowLoading={setTimeoutHandle !== undefined || isFetching}
      onChangeKeyword={onChangeKeyword}
      onBlurKeyword={onBlurKeyword}
    />
  )
}


/**
 * BASE
 */
const ComboBoxBase = <T,>(props: ComboBoxBaseProps<T>) => {
  const displayValue = useCallback((item: T) => {
    return item ? props.textSelector(item) : ''
  }, [props.textSelector])

  const zIndex = 'z-10' // すぐ下にag-gridがあるとOptionsが隠れてしまうため

  const ref = useRef(null)
  const { globalFocusEvents } = useFocusTarget(ref)

  // ComboBoxのdisabledを使って読み取り専用にするとテキスト選択ができなくなるので
  if (props.readOnly) {
    const value = props.selectedItem
      ? props.textSelector(props.selectedItem)
      : ''
    return (
      <Word value={value} className={props.className} readOnly />
    )
  }

  return (
    <Combobox value={props.selectedItem || null} onChange={props.onSelectedItemChanged} nullable>
      <div className={`relative ${props.className}`}>
        <Combobox.Input
          ref={ref}
          displayValue={displayValue}
          onChange={props.onChangeKeyword}
          onBlur={props.onBlurKeyword}
          className="bg-color-base w-full border border-color-5"
          spellCheck="false"
          autoComplete="off"
          {...globalFocusEvents}
        />
        {!props.readOnly &&
          <Combobox.Button className="absolute inset-y-0 right-0 flex items-center pr-2">
            <ChevronUpDownIcon className="h-5 w-5 text-gray-400" aria-hidden="true" />
          </Combobox.Button>}
        <Combobox.Options className={`absolute mt-1 w-full overflow-auto bg-color-base py-1 shadow-lg focus:outline-none ${zIndex}`}>
          {props.nowLoading &&
            <NowLoading />}
          {!props.nowLoading && props.data.length === 0 &&
            <span className="p-1 text-sm select-none opacity-50">データなし</span>}
          {!props.nowLoading && props.data.map((item, index) => (
            <Combobox.Option key={item ? props.keySelector(item) : index} value={item}>
              {({ active }) => (
                <div className={active ? 'bg-color-ridge' : ''}>
                  {item ? props.textSelector(item) : ''}&nbsp;
                </div>
              )}
            </Combobox.Option>
          ))}
        </Combobox.Options>
      </div>
    </Combobox>
  )
}
