import React from "react"
import useEvent from "react-use-event-hook"
import { XMarkIcon } from "@heroicons/react/24/outline"
import { defineCustomComponent } from "./InputBase"
import { IconButton } from "./IconButton"

/** 添付ファイルを表す型 */
export type FileAttachment = {
  /** 新しくファイルを添付しようとしているときはここにファイルが入る */
  file?: FileList
  /** 永続化されたファイルのメタデータ。ファイルが永続化されていない場合はundefined */
  metadata?: FileAttachmentMetadata
  /** 永続化されたファイルを削除しようとしている場合はtrue */
  willDetach?: boolean
}
/** 永続化されたファイルのメタデータ */
export type FileAttachmentMetadata = {
  /** 画面上に表示するファイル名 */
  displayFileName: string
  /** aタグのリンク */
  href: string
  /** リンククリック時にダウンロードするかどうか */
  download?: boolean
}

/** 添付ファイル追加UI。または永続化された添付ファイルへのリンク。 */
export const FileAttachmentView = defineCustomComponent<FileAttachment>((props, ref) => {

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
    onChange?.({ ...value, willDetach: true })
  })

  const divRef = React.useRef<HTMLDivElement>(null)
  React.useImperativeHandle(ref, () => ({
    getValue: () => value,
    focus: () => divRef.current?.focus(),
  }), [value, divRef])

  return (
    <div {...rest} className={`inline-flex gap-1 items-center ${className ?? ''}`}>
      {!readOnly && (!value?.metadata || value.willDetach) && (
        <input type="file" className="flex-1" onChange={handleChange} />
      )}
      {value?.metadata && !value.willDetach && (
        <a
          href={value.metadata.href}
          download={value.metadata.download}
          target="_blank"
          className="text-color-link"
        >
          {value.metadata.displayFileName}
        </a>
      )}
      {!readOnly && value?.metadata && !value.willDetach && (
        <IconButton icon={XMarkIcon} onClick={handleDetach} hideText>解除</IconButton>
      )}
    </div>
  )
})
