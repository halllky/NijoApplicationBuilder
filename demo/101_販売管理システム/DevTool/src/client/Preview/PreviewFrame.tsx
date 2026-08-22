import React from "react"

/**
 * デバッグ対象アプリ（生成後アプリの client）を全画面表示するiframe。
 *
 * key を reloadCount に紐付けているのは、クロスオリジンのiframeを同一URLのまま
 * 再読み込みする確実な手段が他にないため（contentWindow.location.reload() は
 * 同一オリジンポリシーで呼べず、src への同値代入も再読み込みを保証しない）。
 * URL自体が変わる場合は src の変化で通常どおり遷移するので key の変更は不要。
 */
export const PreviewFrame: React.FC<{ url: string, reloadKey: number }> = ({ url, reloadKey }) => {
  return (
    <iframe
      key={reloadKey}
      src={url}
      className="w-full h-full border-0"
      title="デモ101"
    />
  )
}
