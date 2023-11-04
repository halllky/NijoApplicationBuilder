import { XMarkIcon } from "@heroicons/react/24/outline"
import { useAppContext } from "../application/AppContext"

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
    <div className="flex items-start max-w-sm p-1 mb-2 mr-2 gap-2 text-sky-100 bg-sky-800 dark:bg-sky-50 dark:text-sky-800 drop-shadow-[0_6px_6px_rgba(0,0,0,.5)]" role="alert">
      <span className="sr-only">Info</span>
      <div className="flex-1 flex flex-col gap-2 text-xs overflow-hidden">
        <span title={item.msg} className="whitespace-nowrap text-ellipsis overflow-hidden">
          {item.msg}
        </span>
        <span className="opacity-60">
          {item.popupTime}
        </span>
      </div>
      <XMarkIcon
        aria-label="Close"
        className="h-4 w-4 cursor-pointer"
        onClick={() => dispatch({ type: 'delMessage', id: item.id })}
      />
    </div>
  )
}
