import { useCallback } from "react"
import { useAppContext } from "./AppContext"

export const usePopupAlert = (msg: string) => {
    const [, dispatch] = useAppContext()
    const popup = useCallback(() => {
        dispatch({ type: 'pushMsg', msg })
    }, [msg])
    return popup
}
