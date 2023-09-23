import React, { forwardRef, ForwardedRef, useState, useCallback, useMemo } from "react"
import { useQuery } from "react-query"
import { Combobox } from "@headlessui/react"
import { ChevronUpDownIcon } from "@heroicons/react/24/outline"
import { NowLoading } from "./NowLoading"
import { useAppContext } from "../hooks/AppContext"
import { Word } from "./InputForms"

type ComboBoxProps<T> = {
  selectedItem: T | null | undefined
  onSelectedItemChanged: (item: T | null | undefined) => void
  keySelector: (item: T) => string
  textSelector: (item: T) => string
  readOnly?: boolean
  className?: string
}
type SyncComboBoxProps<T> = ComboBoxProps<T> & {
  data: T[]
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


export const SyncComboBox = <T,>(props: SyncComboBoxProps<T>) => {
  const [keyword, setKeyword] = useState('')
  const filteredData = useMemo(() => {
    return props.data.filter(item => {
      if (props.keySelector(item).includes(keyword)) return true
      if (props.textSelector(item).includes(keyword)) return true
      return false
    })
  }, [keyword, props.data, props.keySelector, props.textSelector])

  const onChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    setKeyword(e.target.value)
  }, [setKeyword])

  return (
    <ComboBoxBase
      {...props}
      data={filteredData}
      nowLoading={false}
      onChangeKeyword={onChange}
    />
  )
}


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


const ComboBoxBase = forwardRef(<T,>(props: ComboBoxBaseProps<T>, ref: ForwardedRef<HTMLElement>) => {
  const className = `relative ${props.className}`

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
    <Combobox ref={ref} value={props.selectedItem || null} onChange={props.onSelectedItemChanged} nullable>
      <div className={className}>
        <Combobox.Input
          displayValue={props.textSelector}
          onChange={props.onChangeKeyword}
          onBlur={props.onBlurKeyword}
          className="w-full border border-neutral-400"
          spellCheck="false"
          autoComplete="off"
        />
        {!props.readOnly &&
          <Combobox.Button className="absolute inset-y-0 right-0 flex items-center pr-2">
            <ChevronUpDownIcon className="h-5 w-5 text-gray-400" aria-hidden="true" />
          </Combobox.Button>}
        <Combobox.Options className="absolute mt-1 w-full overflow-auto bg-white py-1 shadow-lg focus:outline-none">
          {props.nowLoading &&
            <NowLoading />}
          {!props.nowLoading && props.data.length === 0 &&
            <span className="p-1 text-sm select-none opacity-50">データなし</span>}
          {!props.nowLoading && props.data.map(item => (
            <Combobox.Option key={props.keySelector(item)} value={item}>
              {({ active }) => (
                <div className={active ? 'bg-neutral-200' : ''}>
                  {props.textSelector(item)}
                </div>
              )}
            </Combobox.Option>
          ))}
        </Combobox.Options>
      </div>
    </Combobox>
  )
}) as <T>(props: ComboBoxBaseProps<T>) => JSX.Element
