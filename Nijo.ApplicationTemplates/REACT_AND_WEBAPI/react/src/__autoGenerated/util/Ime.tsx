import { useEffect, useMemo, useReducer } from "react"
import { defineContext } from "./ReactUtil"

export const [ImeContextProvider, useIMEOpened] = defineContext(
  () => ({ isImeOpen: false, }),
  () => ({ setValue: (v: boolean) => ({ isImeOpen: v }) }),
  (Context, reducer) => ({ elementRef, children }: {
    elementRef: HTMLElement | null
    children?: React.ReactNode
  }) => {
    const [state, dispatch] = useReducer(reducer, { isImeOpen: false })
    const ctxValue = useMemo(() => [state, dispatch] as const, [state, dispatch])

    useEffect(() => {
      const onCompositionStart = () => dispatch(state => state.setValue(true))
      const onCompositionEnd = () => dispatch(state => state.setValue(false))
      elementRef?.addEventListener('compositionstart', onCompositionStart)
      elementRef?.addEventListener('compositionend', onCompositionEnd)
      return () => {
        elementRef?.removeEventListener('compositionstart', onCompositionStart)
        elementRef?.removeEventListener('compositionend', onCompositionEnd)
      }
    }, [elementRef, dispatch])

    return (
      <Context.Provider value={ctxValue}>
        {children}
      </Context.Provider>
    )
  }
)
