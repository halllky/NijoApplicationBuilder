import React, { useCallback, useRef } from 'react';
import { ClipboardDocumentIcon } from '@heroicons/react/24/outline'

export const CopyableText = ({ children, className }: {
    className?: string
    children?: React.ReactNode
}) => {

    const ref = useRef<HTMLSpanElement>(null)
    const copy = useCallback(() => {
        const text = ref.current?.innerText
        if (text) navigator.clipboard.writeText(text)
    }, [])

    return (
        <span ref={ref} className={`select-all ${className}`}>
            {children}
            <ClipboardDocumentIcon
                className='inline w-6 opacity-50 cursor-pointer'
                onClick={copy}
            />
        </span>
    )
}