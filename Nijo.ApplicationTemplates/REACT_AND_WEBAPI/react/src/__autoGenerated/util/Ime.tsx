import { useCallback, useEffect, useReducer, useState } from "react"
import { defineContext } from "./ReactUtil"

export const [ImeContextProvider, useIMEOpened] = defineContext(
  () => ({ isImeOpen: false, }),
  () => ({ setValue: (v: boolean) => ({ isImeOpen: v }) }),
  (Context, reducer) => ({ elementRef, children }: {
    elementRef: React.RefObject<HTMLElement>
    children?: React.ReactNode
  }) => {
    const ctxValue = useReducer(reducer, { isImeOpen: false })
    const onCompositionStart = useCallback(() => ctxValue[1](state => state.setValue(true)), [])
    const onCompositionEnd = useCallback(() => ctxValue[1](state => state.setValue(false)), [])
    useEffect(() => {
      elementRef.current?.addEventListener('compositionstart', onCompositionStart)
      elementRef.current?.addEventListener('compositionend', onCompositionEnd)
      return () => {
        elementRef.current?.removeEventListener('compositionstart', onCompositionStart)
        elementRef.current?.removeEventListener('compositionend', onCompositionEnd)
      }
    }, [elementRef.current])

    return (
      <Context.Provider value={ctxValue}>
        {children}
      </Context.Provider>
    )
  }
)
