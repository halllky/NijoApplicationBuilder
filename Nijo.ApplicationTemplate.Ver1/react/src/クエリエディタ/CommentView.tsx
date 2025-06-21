import * as Input from "../input"
import * as Icon from "@heroicons/react/24/outline"
import DraggableWindow from "./DraggableWindow"
import { SqlTextarea } from "./SqlTextarea"
import { Comment } from "./types"
import useEvent from "react-use-event-hook"

/**
 * コメントを表示するコンポーネント。
 * コメントは、テキストエリアで入力できる。
 */
export const CommentView = ({ commentIndex, comment, zoom, onChangeComment, onDeleteComment }: {
  commentIndex: number
  comment: Comment
  zoom: number
  onChangeComment: (index: number, comment: Comment) => void
  onDeleteComment: (index: number) => void
}) => {

  const handleMouseMove = useEvent((e: MouseEvent) => {
    const deltaX = e.movementX / zoom
    const deltaY = e.movementY / zoom
    onChangeComment(commentIndex, {
      ...comment,
      layout: { ...comment.layout, x: comment.layout.x + deltaX, y: comment.layout.y + deltaY },
    })
  })

  const handleChangeContent = useEvent((e: React.ChangeEvent<HTMLTextAreaElement>) => {
    onChangeComment(commentIndex, {
      ...comment,
      content: e.target.value,
    })
  })

  const handleDeleteWindow = useEvent(() => {
    onDeleteComment(commentIndex)
  })

  const handleChangeTitle = useEvent(() => {
    const newTitle = window.prompt("タイトルを入力してください。")
    const item = { ...comment }
    if (newTitle) {
      item.title = newTitle
    } else {
      delete item.title
    }
    onChangeComment(commentIndex, item)
  })

  return (
    <DraggableWindow
      layout={comment.layout}
      onMove={handleMouseMove}
      className="bg-sky-100 border border-sky-200"
    >
      {({ DragHandle, handleMouseDown }) => (
        <div className="flex flex-col h-full">
          <div className="flex gap-1 items-center">
            {DragHandle}
            <span className="select-none text-sky-600">
              {comment.title ?? "コメント"}
            </span>
            <Input.IconButton icon={Icon.PencilIcon} hideText onClick={handleChangeTitle}>
              名前を変更
            </Input.IconButton>
            <div className="flex-1"></div>
            <Input.IconButton icon={Icon.XMarkIcon} hideText onClick={handleDeleteWindow}>
              削除
            </Input.IconButton>
          </div>
          <textarea
            value={comment.content}
            onChange={handleChangeContent}
            spellCheck={false}
            className="w-full h-full field-sizing-content outline-none resize-none p-1"
          />
        </div>
      )}
    </DraggableWindow>
  )
}