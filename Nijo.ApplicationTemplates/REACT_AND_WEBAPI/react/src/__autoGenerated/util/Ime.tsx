import { createContext, useCallback, useContext, useEffect, useState } from "react"

const ImeCheckerContext = createContext({ isImeOpen: false })
export const ImeContextProvider = <T extends HTMLElement>({ elementRef, children }: {
  elementRef: React.RefObject<T>
  children?: React.ReactNode
}) => {
  const [isImeOpen, setIsImeOpen] = useState(false)
  const onCompositionStart = useCallback(() => setIsImeOpen(true), [])
  const onCompositionEnd = useCallback(() => setIsImeOpen(false), [])
  useEffect(() => {
    elementRef.current?.addEventListener('compositionstart', onCompositionStart)
    elementRef.current?.addEventListener('compositionend', onCompositionEnd)
    return () => {
      elementRef.current?.removeEventListener('compositionstart', onCompositionStart)
      elementRef.current?.removeEventListener('compositionend', onCompositionEnd)
    }
  }, [elementRef.current])

  return (
    <ImeCheckerContext.Provider value={{ isImeOpen }}>
      {children}
    </ImeCheckerContext.Provider>
  )
}

export const useIMEOpened = () => {
  const { isImeOpen } = useContext(ImeCheckerContext)
  return isImeOpen
}
