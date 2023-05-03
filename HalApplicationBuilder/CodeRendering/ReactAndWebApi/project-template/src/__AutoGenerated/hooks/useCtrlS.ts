import { useEffect, useRef } from "react";

export const useCtrlS = (callMethodIfCtrlS: () => void) => {

    const fnRef = useRef(callMethodIfCtrlS)
    fnRef.current = callMethodIfCtrlS

    useEffect(() => {
        const keyDown = (event: KeyboardEvent) => {
            if (event.key.toLowerCase() === "s" && (event.ctrlKey || event.metaKey)) {
                event.preventDefault()
                fnRef.current()
            }
        }
        document.addEventListener("keydown", keyDown)

        return () => {
            document.removeEventListener("keydown", keyDown)
        }
    }, [])
}
