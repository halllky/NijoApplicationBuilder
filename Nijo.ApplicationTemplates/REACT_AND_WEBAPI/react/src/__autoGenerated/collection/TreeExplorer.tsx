import React, { useCallback, useMemo, useReducer, useState } from 'react'
import * as Icon from '@heroicons/react/24/outline'
import * as Util from '../util'
import * as Tree from '../util'
import * as Input from '../input'

export type ExplorerArgs<T> = Tree.ToTreeArgs<T> & {
  data?: T[]
  getLabel: (t: T) => string
  className?: string
}

export const TreeExplorer = <T,>(props: ExplorerArgs<T>) => {
  const [filter, setFilter] = useState<string | undefined>('')
  const [expanded, dispatchExpand] = useReducer(expandCollapseReducer, undefined, () => new Set<string>())

  const dataAsTree = useMemo(() => {
    if (!props.data) return []
    const flatten = Tree.flatten(Tree.toTree(props.data, props))
    let visible: typeof flatten
    if (filter) {
      visible = flatten.filter(node => props
        .getLabel(node.item)
        .includes(filter))
    } else {
      visible = flatten.filter(node => Tree
        .getAncestors(node)
        .every(a => expanded.has(props.getId(a.item))))
    }
    return visible
  }, [props.data, expanded, filter])

  const liRefs = Util.useRefArray<HTMLLIElement>(dataAsTree)
  const {
    activeItemIndex,
    isSelected,
    dispatchSelection,
    handleKeyNavigation,
  } = Util.useListSelection<HTMLLIElement>(dataAsTree, liRefs)

  const handleKeyDown: React.KeyboardEventHandler<HTMLDivElement> = useCallback(e => {
    handleKeyNavigation(e)
    if (e.key === ' ') {
      if (activeItemIndex !== undefined) {
        const id = props.getId(dataAsTree[activeItemIndex].item)
        dispatchExpand(x => x.toggle(id))
        e.preventDefault()
      }
    }
  }, [handleKeyNavigation, activeItemIndex, dataAsTree])

  return (
    <div
      className={`flex flex-col select-none overflow-x-hidden outline-none ${props.className}`}
      tabIndex={0}
      onKeyDown={handleKeyDown}
    >
      {/* ツールバー */}
      <div className="flex items-center">
        <Input.Button
          icon={Icon.MinusIcon} iconOnly small outlined className="m-1"
          onClick={() => dispatchExpand(x => x.collapseAll())}>
          全て折りたたむ
        </Input.Button>
        <Input.Word
          value={filter} onChange={setFilter}
          className="flex-1 min-w-0"
        />
      </div>

      {/* エクスプローラ */}
      <ul className="overflow-y-auto">
        {dataAsTree.map((node, ix) => (
          <li
            ref={liRefs.current[ix]}
            key={props.getId(node.item)}
            className={'flex items-center gap-1 '
              + (isSelected(ix) ? 'bg-color-selected' : '')}
            onClick={() => dispatchSelection(x => x.selectOne(ix))}
          >
            {!filter && (
              <div style={{ width: node.depth * 20 }}></div>
            )}
            <Input.Button
              icon={expanded.has(props.getId(node.item)) ? Icon.ChevronDownIcon : Icon.ChevronRightIcon}
              iconOnly small
              className={(node.children.length === 0 || filter)
                ? 'invisible'
                : undefined}
              onClick={() => dispatchExpand(x => x.toggle(props.getId(node.item)))}
            >
              折りたたむ
            </Input.Button>
            <span className="flex-1 whitespace-nowrap overflow-x-hidden">
              {props.getLabel(node.item)}
            </span>
          </li>
        ))}
      </ul>

    </div>
  )
}

// ---------------------------------
const expandCollapseReducer = Util.defineReducer((state: Set<string>) => ({
  toggle: (id: string) => {
    const newState = new Set(state)
    if (newState.has(id)) newState.delete(id)
    else newState.add(id)
    return newState
  },
  collapseAll: () => {
    return new Set<string>()
  },
}))
