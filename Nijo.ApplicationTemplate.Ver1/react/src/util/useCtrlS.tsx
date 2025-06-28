import * as React from "react"

type SaveHandler = () => void

const CtrlSContext = React.createContext<{
  registerSaveHandler: (handler: SaveHandler) => void
  unregisterSaveHandler: (handler: SaveHandler) => void
} | null>(null)

/**
 * Ctrl+S保存処理を管理するプロバイダー
 */
export const CtrlSProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const saveHandlersRef = React.useRef<Set<SaveHandler>>(new Set())

  const registerSaveHandler = React.useCallback((handler: SaveHandler) => {
    saveHandlersRef.current.add(handler)
  }, [])

  const unregisterSaveHandler = React.useCallback((handler: SaveHandler) => {
    saveHandlersRef.current.delete(handler)
  }, [])

  const contextValue = React.useMemo(() => ({
    registerSaveHandler,
    unregisterSaveHandler,
  }), [registerSaveHandler, unregisterSaveHandler])

  // グローバルなキーボードイベントリスナー
  React.useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if ((event.ctrlKey || event.metaKey) && event.key === 's') {
        event.preventDefault()
        // 登録されているすべての保存ハンドラーを実行
        saveHandlersRef.current.forEach(handler => {
          handler()
        })
      }
    }

    document.addEventListener('keydown', handleKeyDown)
    return () => {
      document.removeEventListener('keydown', handleKeyDown)
    }
  }, [])

  return (
    <CtrlSContext.Provider value={contextValue}>
      {children}
    </CtrlSContext.Provider>
  )
}

/**
 * Ctrl+S保存処理を登録するフック
 * @param saveHandler 保存処理関数
 */
export const useCtrlS = (saveHandler: SaveHandler) => {
  const context = React.useContext(CtrlSContext)

  if (!context) {
    throw new Error('useCtrlS must be used within a CtrlSProvider')
  }

  React.useEffect(() => {
    context.registerSaveHandler(saveHandler)
    return () => {
      context.unregisterSaveHandler(saveHandler)
    }
  }, [context, saveHandler])
}
