import React from "react"
import { Modal } from "../ui"
import type { ChatSessionDto } from "../../shared/devtool-api"
import type { useChatSessions } from "./useChatSessions"
import { SessionList } from "./SessionList"
import { ChangePlanView } from "./ChangePlanView"
import { ChatPane } from "./ChatPane"

/**
 * チャットセッションの一覧と、選択したセッションの会話・変更計画を並べて表示するモーダル。
 * セッションの一覧は呼び出し側が保持しているものを受け取る（どのセッションが開いているかの持ち主を1つに保つため）。
 * 閉じる操作（シェードクリック・閉じるボタン）は onClose を呼ぶだけで、
 * 実際に閉じるかどうかの判断はこのコンポーネントを呼ぶ側が行う。
 */
export const AgentPanel = ({ open, sessions, onClose, onOpenFloating, onOpenSettings }: {
  open: boolean
  sessions: ReturnType<typeof useChatSessions>
  onClose: () => void
  /** 一覧で選んだセッションをフローティングウィンドウとして開くために呼ぶ */
  onOpenFloating: (sessionId: string) => void
  /** チャットがAPIキー未設定エラーを受け取ったときに、設定画面へ切り替えるために呼ぶ */
  onOpenSettings: () => void
}) => {

  //#region 状態

  const [selectedId, setSelectedId] = React.useState<string>()
  // 変更計画は一覧の要約には本文が含まれないため、選択されたセッションを個別に取得する
  const [selected, setSelected] = React.useState<ChatSessionDto>()
  const [isLoadingSelected, setIsLoadingSelected] = React.useState(false)

  // パネルが開かれるたびにサーバー側の最新の一覧と同期する（会話が進むと見出しや変更計画の状態が変わるため）
  const { reload } = sessions
  React.useEffect(() => {
    if (open) reload()
  }, [open, reload])

  // 選択中のセッションの詳細をサーバーから取得する
  React.useEffect(() => {
    if (!selectedId) {
      setSelected(undefined)
      return
    }
    let canceled = false
    setIsLoadingSelected(true)
    fetch(`/devtool-api/sessions/${encodeURIComponent(selectedId)}`)
      .then(res => res.ok ? res.json() as Promise<ChatSessionDto> : undefined)
      .then(session => { if (!canceled) setSelected(session) })
      .finally(() => { if (!canceled) setIsLoadingSelected(false) })
    return () => { canceled = true }
  }, [selectedId])

  //#endregion 状態

  return (
    <Modal open={open} onClose={onClose} title="セッション" panelClassName="w-[1200px] max-w-[95vw] h-[720px] max-h-[85vh]">

      {/* 本体（左：セッション一覧、中央：チャット、右：そのセッションの変更計画） */}
      <div className="flex-1 flex min-h-0">

        <SessionList
          sessions={sessions.sessions}
          isLoading={sessions.isLoading}
          selectedId={selectedId}
          onSelect={setSelectedId}
          onOpenFloating={onOpenFloating}
          onRemove={id => {
            if (!window.confirm('このセッションを削除しますか？')) return;
            if (id === selectedId) setSelectedId(undefined)
            sessions.remove(id)
          }}
        />

        {/* 選択されるまではチャットを表示しない（会話の読み込み先が決まらないため） */}
        {selectedId
          ? <ChatPane key={selectedId} sessionId={selectedId} onApiKeyMissing={onOpenSettings} />
          : <p className="flex-1 min-w-0 p-3 text-sm text-gray-500">セッションを選択してください。</p>}

        <ChangePlanView changePlan={selected?.changePlan ?? null} isLoading={isLoadingSelected} />
      </div>
    </Modal>
  )
}
