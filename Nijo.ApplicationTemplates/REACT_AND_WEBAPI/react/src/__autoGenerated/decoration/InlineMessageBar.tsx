import { XMarkIcon } from '@heroicons/react/24/outline'
import { useCallback } from 'react'
import { IconButton } from '../user-input'

export type BarMessage = {
  uuid: string
  text: string
}

export const InlineMessageBar = ({ value, onChange }: {
  value?: BarMessage[]
  onChange?: (v: BarMessage[]) => void
}) => {

  const handleDelete = useCallback((msg: BarMessage) => {
    onChange?.(value?.filter(m => m.uuid !== msg.uuid) || [])
  }, [value, onChange])

  return (
    <ul className="w-full flex flex-col items-stretch">
      {value?.map(msg =>
        <li key={msg.uuid} className="flex justify-between p-0.5 bg-rose-50 border border-rose-100">
          <span className="text-xs text-rose-700 whitespace-pre-wrap overflow-hidden">{msg.text}</span>
          <IconButton icon={XMarkIcon} className="text-rose-700 items-start" onClick={() => handleDelete(msg)} />
        </li>
      )}
    </ul>
  )
}
