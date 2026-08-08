/**
 * 読み込み中を表すくるくる。
 * 表示中はこの裏にあるUIが操作不能になる。
 * どの範囲に覆いかぶさるかは、CSSの position:absolute で制御している。
 */
export const NowLoading = ({ className, opacity }: {
  className?: string
  opacity?: number
}) => {
  return (
    // Allotment のセパレータより手前に表示させるために z-index を指定している
    <div className={`absolute z-50 inset-0 flex justify-center items-center ${className ?? ''}`} aria-label="読み込み中">
      {/* 半透明のシェード */}
      <div className="absolute inset-0 bg-white" style={{ opacity: opacity ?? 0.5 }}></div>
      {/* くるくる */}
      <div className="animate-spin h-6 w-6 border-4 border-sky-500 rounded-full border-t-transparent"></div>
    </div>
  )
}
