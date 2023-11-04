import { PropsWithoutRef, forwardRef } from "react"

export * from "./useHttpRequest"


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


/**
 * ツリー構造をもつオブジェクトに対して再帰的に処理を行います。
 */
export const visitObject = (obj: object, callback: (obj: object) => void): void => {
  if (obj == null) return
  if (typeof obj !== 'object') return

  if (Array.isArray(obj)) {
    for (const item of obj) {
      callback(item)
      visitObject(item, callback)
    }
    return
  }

  callback(obj)

  for (const key in obj) {
    if (!Object.prototype.hasOwnProperty.call(obj, key)) continue
    const prop = obj[key as keyof typeof obj]
    visitObject(prop as object, callback)
  }
}
