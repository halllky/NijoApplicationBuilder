import React from "react"

/** 説明用の箇条書き */
export const HelpText = {
  Container: ({ children, className }: { children?: React.ReactNode, className?: string }) => (
    <ul className={`list-disc pl-4 ${className ?? ''}`}>
      {children}
    </ul>
  ),
  Item: ({ children, className }: { children?: React.ReactNode, className?: string }) => (
    <li className={`text-sm text-color-5 ${className ?? ''}`}>
      {children}
    </li>
  ),
}
