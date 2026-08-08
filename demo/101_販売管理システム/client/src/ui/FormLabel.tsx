/**
 * 入力フォームのラベルコンポーネント。
 */
export function FormLabel(props: {
  /** 対応する入力要素のID */
  htmlFor?: string
  className?: string
  children?: React.ReactNode
}) {
  return (
    <label htmlFor={props.htmlFor} className={`inline-block p-px text-gray-700 select-none ${props.className ?? ''}`}>
      {props.children}
    </label>
  )
}
