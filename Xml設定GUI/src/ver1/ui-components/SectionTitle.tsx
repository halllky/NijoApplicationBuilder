
/** セクションタイトル */
export const SectionTitle = ({ children, className }: {
  children?: React.ReactNode
  className?: string
}) => {
  return (
    <h3 className={`text-lg font-bold ${className ?? ''}`}>
      {children}
    </h3>
  )
}
