import { XMarkIcon } from '@heroicons/react/24/outline'
import { useCallback } from 'react'
import { IconButton } from './IconButton'

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
                <li key={msg.uuid} className="flex justify-between p-0.5 bg-rose-100 border border-rose-200">
                    <span className="text-sm text-rose-700">{msg.text}</span>
                    <IconButton icon={XMarkIcon} className="text-rose-700" onClick={() => handleDelete(msg)} />
                </li>
            )}
        </ul>
    )
}