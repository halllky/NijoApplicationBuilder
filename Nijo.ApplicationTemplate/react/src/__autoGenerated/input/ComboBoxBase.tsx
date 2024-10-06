import React from "react"
import { ChevronUpDownIcon } from "@heroicons/react/24/solid"
import { useIMEOpened, useMsgContext, useRefArray } from "../util"
import { TextInputBase, TextInputBaseAdditionalRef } from "./TextInputBase"
import { ComboAdditionalRef, ComboProps, CustomComponentProps, CustomComponentRef, defineCustomComponent } from "./InputBase"
import useEvent from "react-use-event-hook"
import { DialogOrPopupContents, useDialogContext } from "../collection"

/** コンボボックス基底クラス */
export const ComboBoxBase = defineCustomComponent(<TOption, TValue = TOption>(
  props: CustomComponentProps<TValue, ComboProps<TOption, TValue>>,
  ref: React.ForwardedRef<CustomComponentRef<TValue> & ComboAdditionalRef>
) => {

  // 選択確定
  const handleSelectOption = useEvent((opt: TOption | undefined) => {
    props.onChange?.(opt ? props.getValueFromOption(opt) : undefined)
  })

  // 絞り込み
  const [keyword, setKeyword] = React.useState<string | undefined>()
  const executeFiltering = useLazyQuery(props.onFilter, props.waitTimeMS ?? 0)
  const handleKeywordChange = useEvent(async (k: string | undefined) => {
    setKeyword(k)
    const options = await executeFiltering(k)
    if (options) openDropdown(options)
  })

  // テキストボックスに表示するテキスト
  const displayText = React.useMemo(() => {
    if (keyword !== undefined) return keyword
    if (props.value !== undefined) return props.getValueText(props.value)
    return ''
  }, [keyword, props.value, props.getValueText])

  // ドロップダウン
  const textBaseRef = React.useRef<CustomComponentRef<string> & TextInputBaseAdditionalRef>(null)
  const dropdownRef = React.useRef<DropdownListRef<TOption>>(null)
  const [isOpened, setIsOpened] = React.useState(false)
  const [highlightIndex, setHighlightIndex] = React.useState<number | undefined>()
  const [{ popupElementRef }, dispatchPopup] = useDialogContext()
  const openDropdown = useEvent((options: TOption[]) => {
    // カスタマイズ処理でfalseが返されたら展開中止
    const openingEventResult = props.onDropdownOpening?.()
    if (openingEventResult === false) return

    setIsOpened(true)
    dispatchPopup(state => state.openPopup(
      textBaseRef.current?.element,
      createDropdownList(
        options,
        props.getOptionText,
        setHighlightIndex,
        handleSelectOption,
        dropdownRef),
      () => setIsOpened(false),
    ))
  })

  // キーボード操作
  const [{ isImeOpen }] = useIMEOpened()
  const handleKeyDown: React.KeyboardEventHandler = useEvent(async e => {
    // ドロップダウン内のハイライトの上下移動
    if (e.key === 'ArrowUp' || e.key === 'ArrowDown') {
      if (!isImeOpen && !isOpened) {
        // ドロップダウンを開く
        setHighlightIndex(undefined)
        const options = await executeFiltering(undefined)
        if (options) openDropdown(options)
        e.preventDefault()

      } else if (e.key === 'ArrowUp') {
        // 一つ上を選択
        dropdownRef.current?.highlightAbove()
        e.preventDefault()

      } else {
        // 一つ下を選択
        dropdownRef.current?.highlightBelow()
        e.preventDefault()
      }
      return
    }

    // ドロップダウン中のハイライトが当たっている要素の選択を確定する
    if (isOpened && !isImeOpen && e.key === 'Enter') {
      if (highlightIndex === undefined) {
        // 選択肢がハイライトされていない状態でEnterが押されたらクリア
        handleSelectOption(undefined)
        dropdownRef.current?.close()
        e.preventDefault()
        return
      }
      const opt = dropdownRef.current?.options?.[highlightIndex]
      if (!opt) return
      handleSelectOption(opt)
      setKeyword(undefined)
      setHighlightIndex(undefined)
      dropdownRef.current.close()
      e.preventDefault()
    }
  })

  // ドロップダウン横ボタン
  const handleSideButtonClick = useEvent(async () => {
    const options = await executeFiltering(undefined)
    if (options) openDropdown(options)
  })

  /** 現在のUIの状態を総合的に考慮して値を確定させる */
  const getCurrentValue = useEvent((): TValue | undefined => {
    if (dropdownRef.current && highlightIndex !== undefined) {
      // ハイライトが当たっている選択肢がある場合はそれを返す
      const item = dropdownRef.current.options[highlightIndex]
      return item ? props.getValueFromOption(item) : undefined
    } else if (keyword !== undefined) {
      // キーワード入力中にもかかわらずハイライトが当たっている選択肢がない場合はクリア
      return undefined
    } else {
      // 編集に関する操作が行われていないので現在の値をそのまま返す
      return props.value
    }
  })

  // ref
  React.useImperativeHandle(ref, () => ({
    focus: () => textBaseRef.current?.focus(),
    getValue: getCurrentValue,
    closeDropdown: () => dropdownRef.current?.close(),
  }), [textBaseRef, getCurrentValue, dropdownRef])

  // フォーカス離脱時
  const handleBlur: React.FocusEventHandler = useEvent(e => {
    setKeyword(undefined)

    // ドロップダウンリスト内の要素がクリックされた場合、
    // クリック処理側での値確定処理を優先するため、ここではonChange処理を呼ばない
    if (popupElementRef.current?.contains(e.relatedTarget)) return

    props.onChange?.(getCurrentValue())
    dropdownRef.current?.close()
  })

  return (
    <TextInputBase
      ref={textBaseRef}
      readOnly={props.readOnly}
      value={displayText}
      onOneCharChanged={handleKeywordChange}
      onKeyDown={handleKeyDown}
      onBlur={handleBlur}
      AtEnd={<DropdownButton onClick={handleSideButtonClick} />}
    />
  )
})

// ------------------------------------------------------

/** ドロップダウン開閉ボタン */
const DropdownButton = ({ onClick }: {
  onClick?: () => void
}) => {
  return (
    <ChevronUpDownIcon
      className="w-6 text-color-5 border-l border-color-5 cursor-pointer"
      onClick={onClick}

      // TextInputBaseのblurイベントでフォーカス移動先がTextInputBaseの外か中かの判定をしているので
      // このアイコンをフォーカス可能に指定する必要がある
      tabIndex={0}
    />
  )
}

/** ドロップダウンリストのref */
type DropdownListRef<TOption> = {
  options: TOption[]
  /** 現在選択されている選択肢の一つ上の選択肢を選択します。 */
  highlightAbove: () => void
  /** 現在選択されている選択肢の一つ下の選択肢を選択します。 */
  highlightBelow: () => void
  close: () => void
}

/** ドロップダウンリストを生成して返します。 */
const createDropdownList = <TOption,>(
  /** 選択肢 */
  options: TOption[],
  /** 画面に表示する文字列を取得する関数 */
  getOptionText: (opt: TOption) => string,
  /** カーソルが当たっているインデックスが変わったタイミングのイベント */
  onHighlightIndexChanged: (i: number) => void,
  /** 選択確定 */
  onSelect: (opt: TOption) => void,
  /** ref */
  ref: React.Ref<DropdownListRef<TOption>>,
): DialogOrPopupContents => {

  return ({ closeDialog }) => {
    const [highlightIndex, setHighlightIndex] = React.useState<number | undefined>()
    const listItemRefs = useRefArray<HTMLLIElement>(options.length)

    /** 一つ上の選択肢にハイライトをあてる */
    const highlightAbove = useEvent(() => {
      const newIndex = highlightIndex === undefined
        ? 0
        : Math.max(0, highlightIndex - 1)
      if (newIndex !== highlightIndex) onHighlightIndexChanged(newIndex)
      setHighlightIndex(newIndex)
      listItemRefs.current?.[newIndex]?.current?.scrollIntoView({ block: 'nearest' })
    })
    /** 一つ下の選択肢にハイライトをあてる */
    const highlightBelow = useEvent(() => {
      const newIndex = highlightIndex === undefined
        ? 0
        : Math.min(options.length - 1, highlightIndex + 1)
      if (newIndex !== highlightIndex) onHighlightIndexChanged(newIndex)
      setHighlightIndex(newIndex)
      listItemRefs.current?.[newIndex]?.current?.scrollIntoView({ block: 'nearest' })
    })
    /** 要素クリック */
    const handleListItemClick = useEvent((index: number) => {
      const item = options[index]
      if (item) onSelect(item)
      closeDialog()
    })

    // ref
    React.useImperativeHandle(ref, () => ({
      options,
      highlightAbove,
      highlightBelow,
      close: closeDialog,
    }), [options, highlightAbove, highlightBelow, closeDialog])

    return (
      <ul>
        {options.length === 0 && (
          <li className="text-color-6">
            データなし
          </li>
        )}
        {options.map((item, index) => (
          <ListItem
            key={index}
            index={index}
            text={getOptionText(item)}
            onClick={handleListItemClick}
            highlight={index === highlightIndex}
            liRef={listItemRefs.current[index]}
          />
        ))}
      </ul>
    )
  }
}

/** ドロップダウンのリストの要素 */
const ListItem = React.memo(({ index, text, onClick, highlight, className, liRef }: {
  index?: number
  text: string
  onClick?: (index: number) => void
  highlight?: boolean
  className?: string
  liRef: React.RefObject<HTMLLIElement>
}) => {

  return (
    <li
      ref={liRef}
      className={`cursor-pointer relative ${(highlight ? 'bg-color-4' : '')} ${className}`}
      onClick={(index !== undefined ? (() => onClick?.(index)) : undefined)}
    >
      {/* ホバー時の背景色 */}
      <div className="bg-color-4 absolute inset-0 opacity-0 hover:opacity-25"></div>

      {text}&nbsp;
    </li>
  )
})

// ------------------------------------------------------

/**
 * 非同期コンボボックスのための遅延クエリ発行。
 * 短時間で連続してクエリが発行されることでサーバーに負荷がかかるのを防ぐため、
 * キーワード入力から一定時間経過しないとクエリ発行が実行されないようにするための仕組み。
 */
const useLazyQuery = <TOption,>(queryFunction: (keyword: string | undefined) => Promise<TOption[]>, waitTime: number) => {
  const [, dispatchMsg] = useMsgContext()
  const timeoutHandle = React.useRef<NodeJS.Timeout | undefined>(undefined)

  return useEvent((keyword: string | undefined): Promise<TOption[] | false> => {
    // 直前に処理の予約がある場合はそれをキャンセルする
    if (timeoutHandle.current !== undefined) clearTimeout(timeoutHandle.current)

    // クエリ実行処理の予約を行う
    return new Promise<TOption[] | false>(resolve => {
      timeoutHandle.current = setTimeout(async () => {
        // 一定時間内に処理がキャンセルされなかった場合のみここの処理が実行される
        try {
          const result = await queryFunction(keyword)
          resolve(result)
        } catch (error) {
          dispatchMsg(msg => msg.error(`データ取得に失敗しました: ${error}`))
          resolve(false)
        }
      }, waitTime)
    })
  })
}
