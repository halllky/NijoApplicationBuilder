import React from "react"
import useEvent from "react-use-event-hook"
import { XMarkIcon } from "@heroicons/react/24/outline"
import { defineCustomComponent } from "./InputBase"
import { IconButton } from "./IconButton"

/** 添付ファイルを表す型 */
export type FileAttachmentMetadata = {
  /**
   * 永続化された添付ファイルにアクセスするためのID。
   * サーバー側で発番される。この項目に値がある場合はアップロード済みであることを表す。
   */
  fileAttachmentId?: string
  /** 画面上に表示するファイル名 */
  displayFileName?: string
  /** IDと名前以外の属性 */
  otherProps?: { [key: string]: string | undefined }

  /** 新しくファイルを添付しようとしているときはここにファイルが入る */
  file?: FileList
}

/** 添付ファイル追加UI。または永続化された添付ファイルへのリンク。 */
export const FileAttachmentView = defineCustomComponent<FileAttachmentMetadata>((props, ref) => {

  const {
    value,
    onChange,
    readOnly,
    className,
    ...rest
  } = props

  const handleChange: React.ChangeEventHandler<HTMLInputElement> = useEvent(e => {
    onChange?.({ ...value, file: e.target.files! })
  })
  const handleDetach = useEvent(() => {
    onChange?.(undefined)
  })

  const divRef = React.useRef<HTMLDivElement>(null)
  React.useImperativeHandle(ref, () => ({
    getValue: () => value,
    focus: () => divRef.current?.focus(),
  }), [value, divRef])

  return (
    <div {...rest} className={`inline-flex gap-1 items-center ${className ?? ''}`}>
      {!readOnly && !value?.fileAttachmentId && (
        <input type="file" className="flex-1" onChange={handleChange} />
      )}
      {value?.fileAttachmentId && (
        <span>{value.displayFileName}</span>
      )}
      {!readOnly && value?.fileAttachmentId && (
        <IconButton icon={XMarkIcon} onClick={handleDetach} hideText>解除</IconButton>
      )}
    </div>
  )
})
