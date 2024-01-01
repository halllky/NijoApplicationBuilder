import cytoscape from 'cytoscape'
import { useCallback, useState } from 'react'
import Layout from './GraphView.Layout'

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

  const [currentLayout, setCurrentLayout] = useState(Layout.DEFAULT.name)
  const handleLayoutChanged: React.ChangeEventHandler<HTMLSelectElement> = useCallback(e => {
    setCurrentLayout(e.target.value)
  }, [])
  const handlePositionReset = useCallback(() => {
    if (!cy) return
    cy.layout(Layout.OPTION_LIST[currentLayout])?.run()
    cy.resize().fit().reset()
  }, [cy, currentLayout])
  const handleExpandAll = useCallback(() => {
    const api = (cy as any)?.expandCollapse('get')
    api.expandAll()
    api.expandAllEdges()
  }, [cy])
  const handleCollapseAll = useCallback(() => {
    const api = (cy as any)?.expandCollapse('get')
    api.collapseAll()
    api.collapseAllEdges()
  }, [cy])

  return (
    <div className={`flex content-start items-center gap-3 ${className}`}>
      <select value={currentLayout} onChange={handleLayoutChanged}>
        {Object.entries(Layout.OPTION_LIST).map(([key]) => (
          <option key={key} value={key}>
            {key}
          </option>
        ))}
      </select>
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
