/**
 * 画面ヘッダ部分に表示されるページタイトルコンポーネント
 */
export function PageTitle(props: {
  children?: React.ReactNode
  className?: string
}) {
  return (
    <h1 className={`text-lg font-bold select-none ${props.className ?? ''}`}>
      {props.children}
    </h1>
  )
}
