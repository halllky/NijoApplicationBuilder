import React from "react"
import { ArrowTopRightOnSquareIcon, TrashIcon } from "@heroicons/react/24/outline"
import type { ChatSessionSummary } from "../../shared/devtool-api"

/**
 * チャットセッションの一覧。行をクリックするとそのセッションを選択状態にする。
 * 一覧の増減（新規作成）はこの画面では行わない。
 */
export const SessionList: React.FC<{
  sessions: ChatSessionSummary[]
  isLoading: boolean
  selectedId: string | undefined
  onSelect: (id: string) => void
  /** その行のセッションをフローティングウィンドウとして開くために呼ぶ */
  onOpenFloating: (id: string) => void
  onRemove: (id: string) => void
}> = ({ sessions, isLoading, selectedId, onSelect, onOpenFloating, onRemove }) => {

  return (
    <div className="w-[280px] shrink-0 border-r border-gray-200 overflow-y-auto p-3 flex flex-col gap-2">

      {isLoading && sessions.length === 0 && (
        <p className="text-xs text-gray-500">読み込み中...</p>
      )}
      {!isLoading && sessions.length === 0 && (
        <p className="text-xs text-gray-500">セッションはまだありません。</p>
      )}

      {sessions.map(session => (
        <SessionRow
          key={session.id}
          session={session}
          selected={selectedId === session.id}
          onSelect={() => onSelect(session.id)}
          onOpenFloating={() => onOpenFloating(session.id)}
          onRemove={() => onRemove(session.id)}
        />
      ))}
    </div>
  )
}

/** 1件のセッション。見出し・作成日時・変更計画の状態と、行単位の操作ボタンを持つ。 */
const SessionRow: React.FC<{
  session: ChatSessionSummary
  selected: boolean
  onSelect: () => void
  onOpenFloating: () => void
  onRemove: () => void
}> = ({ session, selected, onSelect, onOpenFloating, onRemove }) => {

  return (
    <div className={`border rounded ${selected ? 'border-gray-800' : 'border-gray-200'}`}>

      {/* 見出し・作成日時・変更計画の状態 */}
      <button
        type="button"
        onClick={onSelect}
        className="w-full text-left px-2 py-1.5 flex flex-col gap-0.5 hover:bg-gray-50 cursor-pointer"
      >
        <span className="text-sm font-medium truncate">{session.title}</span>
        <span className="text-xs text-gray-500">
          {new Date(session.createdAt).toLocaleString()}
          {session.changePlanSummary && ` ・ 変更計画: ${session.changePlanSummary.status}`}
        </span>
      </button>

      {/* 行単位の操作 */}
      <div className="flex justify-end gap-1 px-1 pb-1">
        <button
          type="button"
          onClick={onOpenFloating}
          className="p-1 text-gray-500 hover:text-gray-800 cursor-pointer"
          title="別ウィンドウで開く"
        >
          <ArrowTopRightOnSquareIcon className="w-4 h-4" />
        </button>
        <button
          type="button"
          onClick={onRemove}
          className="p-1 text-gray-500 hover:text-rose-600 cursor-pointer"
          title="このセッションを削除"
        >
          <TrashIcon className="w-4 h-4" />
        </button>
      </div>
    </div>
  )
}
