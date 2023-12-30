import cytoscape from 'cytoscape'
import { useCallback, useState } from 'react'
import Layout from './Extension.Layout'

export const Toolbar = ({ cy, className }: {
  cy?: cytoscape.Core
  className?: string
}) => {
  const [locked, setLocked] = useState(false)

  const handleLockChanged: React.ChangeEventHandler<HTMLInputElement> = useCallback(e => {
    if (!cy) { setLocked(false); return }
    setLocked(e.target.checked)
    cy.autolock(e.target.checked)
  }, [cy, locked])

  const handlePositionReset = useCallback(() => {
    if (!cy) return
    cy.layout(Layout.OPTIONS)?.run()
    cy.resize().fit().reset()
  }, [cy])
  const handleExpandAll = useCallback(() => {
    (cy as any)?.expandCollapse('get').expandAll()
  }, [cy])
  const handleCollapseAll = useCallback(() => {
    (cy as any)?.expandCollapse('get').collapseAll()
  }, [cy])

  return (
    <div className={`flex content-start items-center gap-3 ${className}`}>
      <button onClick={handlePositionReset}>位置リセット</button>
      <label>
        <input type="checkbox" checked={locked} onChange={handleLockChanged} />
        ノード位置固定
      </label>
      <button onClick={handleExpandAll}>すべて展開</button>
      <button onClick={handleCollapseAll}>すべて折りたたむ</button>
    </div>
  )
}
