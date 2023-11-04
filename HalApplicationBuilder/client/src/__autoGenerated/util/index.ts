import { PropsWithoutRef, forwardRef } from "react"

/**
 * forwardRefの戻り値の型定義がややこしいので単純化するためのラッピング関数
 */
export const forwardRefEx = <TRef, TProps>(
  fn: (props: TProps, ref: React.ForwardedRef<TRef>) => React.ReactNode
) => {
  return forwardRef(fn) as (
    (props: PropsWithoutRef<TProps> & { ref?: React.Ref<TRef> }) => React.ReactNode
  )
}
