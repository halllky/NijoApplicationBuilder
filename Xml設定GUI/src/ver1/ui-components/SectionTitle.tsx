
/** セクションタイトル */
export const SectionTitle = ({ children }: {
  children?: React.ReactNode
}) => {
  return (
    <h3 className="text-lg font-bold">
      {children}
    </h3>
  )
}
