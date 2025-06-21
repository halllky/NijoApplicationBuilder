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
      layout: {
        ...comment.layout,
        x: comment.layout.x + deltaX,
        y: comment.layout.y + deltaY,
      },
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
  const handleMouseDownDeleteButton = useEvent((e: React.MouseEvent<Element>) => {
    e.stopPropagation()
  })

  return (
    <DraggableWindow
      layout={comment.layout}
      onMove={handleMouseMove}
      className="bg-sky-100 border border-sky-200"
    >
      {({ handleMouseDown }) => (
        <div className="flex flex-col h-full">
          <div onMouseDown={handleMouseDown} className="flex gap-1 items-center cursor-grab bg-sky-200">
            <div className="flex-1"></div>
            <Input.IconButton icon={Icon.XMarkIcon} hideText onClick={handleDeleteWindow} onMouseDown={handleMouseDownDeleteButton}>
              削除
            </Input.IconButton>
          </div>
          <textarea
            value={comment.content}
            onChange={handleChangeContent}
            spellCheck={false}
            className="w-full h-full field-sizing-content outline-none resize-none p-1 text-sky-800"
          />
        </div>
      )}
    </DraggableWindow>
  )
}