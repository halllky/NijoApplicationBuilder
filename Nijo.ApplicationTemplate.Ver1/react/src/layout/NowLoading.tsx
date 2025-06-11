/**
 * 読み込み中を表すくるくる。
 * 表示中はこの裏にあるUIが操作不能になる。
 * どの範囲に覆いかぶさるかは、CSSの position:absolute で制御している。
 */
export const NowLoading = () => {
  return (
    <div className="absolute inset-0 flex justify-center items-center" aria-label="読み込み中">
      {/* 半透明のシェード */}
      <div className="absolute inset-0 bg-color-1 opacity-25"></div>
      {/* くるくる */}
      <div className="animate-spin h-6 w-6 border-4 border-color-6 rounded-full border-t-transparent"></div>
    </div>
  )
}
