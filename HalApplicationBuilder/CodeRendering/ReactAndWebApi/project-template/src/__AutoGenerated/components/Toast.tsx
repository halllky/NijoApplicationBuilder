import { XMarkIcon } from "@heroicons/react/24/outline"
import { useAppContext } from "../hooks/AppContext"

export type ToastMessage = {
    id: string
    msg: string
    popupTime: string
}

export const Toast = ({ item }: {
    item: ToastMessage
}) => {
    const [, dispatch] = useAppContext()

    return (
        <div className="flex items-start p-2 mb-4 mr-4 select-none text-sky-100 bg-sky-800 dark:bg-sky-50 dark:text-sky-800" role="alert">
            <span className="sr-only">Info</span>
            <div className="text-sm font-medium">
                {item.msg}<br />{item.popupTime}
            </div>
            <XMarkIcon
                aria-label="Close"
                className="h-4 w-4 cursor-pointer"
                onClick={() => dispatch({ type: 'delMessage', id: item.id })}
            />
        </div>
    )
}
