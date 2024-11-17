import React from "react"

/** 新規追加・変更・削除のいずれかの状態を表すセル */
export const AddModDelStateCell = React.memo(({ noSideLine, state }: {
  noSideLine?: boolean
  state?: 'ADD' | 'MOD' | 'DEL' | 'NONE'
}) => {
  let bgColor: string
  let textColor: string
  let text: string
  if (state === 'ADD') {
    bgColor = 'bg-emerald-500'
    textColor = 'text-emerald-500'
    text = '追加'

  } else if (state === 'MOD') {
    bgColor = 'bg-sky-500'
    textColor = 'text-sky-500'
    text = '変更'

  } else if (state === 'DEL') {
    bgColor = 'bg-rose-500'
    textColor = 'text-rose-500'
    text = '削除'

  } else {
    bgColor = ''
    textColor = ''
    text = ''
  }

  return noSideLine ? (
    <span className={`text-sm font-bold ${textColor}`}>
      {text}
    </span>
  ) : (
    <div className="flex select-none gap-1 text-sm font-bold whitespace-nowrap">
      <div className={`basis-1 self-stretch ${bgColor}`}></div>
      <span className={textColor}>{text}</span>
    </div>
  )
})
