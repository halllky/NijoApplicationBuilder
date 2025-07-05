import React from "react";
import * as Layout from "../../layout"
import * as Input from "../../input"
import * as Util from "../../util"
import * as Icon from "@heroicons/react/24/solid"
import useEvent from "react-use-event-hook";

/** 設定ダイアログのプロパティ */
export type SettingDialogProps = {
  /** trueの場合、保存せずにキャンセルしようとすると警告ダイアログが表示される */
  isDirty: boolean;
  /** ダイアログの編集内容確定時のコールバック */
  onApply: () => void;
  /** キャンセル時 */
  onCancel: () => void;
  /** ダイアログの枠に適用されるクラス名 */
  className?: string;
  /** ダイアログのタイトル */
  title?: string;
  children?: React.ReactNode;
};

/**
 * 設定画面ダイアログ。
 * 通常のモーダルダイアログに加え下記の性質を持つ。
 *
 * * データのライフサイクルが呼び出し元画面と異なる。
 *   * 開く瞬間に呼び出し元画面から初期値を受け取る。
 *   * 閉じる時に呼び出し元画面に編集後の値を返す。
 *   * キャンセルされた場合、呼び出し元画面が持っているデータには影響を与えず、ダイアログが閉じられる。
 * * ダイアログの中身は、呼び出し元画面のデータを編集するためのフォームと、保存ボタンとキャンセルボタンからなる。
 * * Ctrl + Enter で保存、Esc でキャンセル。
 * * isDirtyな状態でキャンセルしようとすると警告ダイアログが表示される。
 */
export const SettingDialog = (props: SettingDialogProps) => {

  const handleApply = useEvent(() => {
    props.onApply()
  })

  const handleCancel = useEvent(() => {
    // isDirtyな状態でキャンセルしようとすると警告
    if (props.isDirty && !window.confirm('キャンセルしますか？')) return;
    props.onCancel()
  })

  // キーボードショートカット
  const handleKeyDown = useEvent((e: React.KeyboardEvent) => {
    if ((e.ctrlKey || e.metaKey) && e.key === 'Enter') {
      e.preventDefault()
      handleApply()

    } else if (e.key === 'Escape') {
      e.preventDefault()
      handleCancel()
    }
  })

  return (
    <Layout.ModalDialog
      open
      className={`relative bg-white flex flex-col gap-1 relative border border-gray-400 ${props.className ?? 'w-lg h-[80vh]'}`}
      onOutsideClick={handleCancel}
    >
      <div onKeyDown={handleKeyDown} tabIndex={-1} className="flex flex-col h-full outline-none">
        {/* タイトル */}
        {props.title && (
          <div className="px-4 py-2 border-b border-gray-200">
            <h1 className="font-bold text-gray-700">{props.title}</h1>
          </div>
        )}

        {/* コンテンツ */}
        <div className="flex-1 overflow-y-auto">
          {props.children}
        </div>

        {/* ボタン */}
        <div className="flex justify-end items-center gap-2 px-4 py-2 border-t border-gray-200">
          <Input.IconButton
            icon={Icon.XMarkIcon}
            outline
            onClick={handleCancel}
          >
            キャンセル(Esc)
          </Input.IconButton>
          <Input.IconButton
            icon={Icon.CheckIcon}
            fill
            onClick={handleApply}
          >
            保存(Ctrl + Enter)
          </Input.IconButton>
        </div>
      </div>
    </Layout.ModalDialog>
  )
};