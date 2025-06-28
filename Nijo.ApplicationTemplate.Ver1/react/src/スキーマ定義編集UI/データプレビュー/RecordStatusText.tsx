import { EditableDbRecord } from "./types"

export const RecordStatusText = ({ record, className }: {
  record: EditableDbRecord | null | undefined
  className?: string
}) => {
  if (!record) {
    return undefined
  }
  if (!record.existsInDb) {
    return (
      <span className={`text-green-500 ${className ?? ""}`}>
        新規
      </span>
    )
  }
  if (record.deleted) {
    return (
      <span className={`text-red-500 ${className ?? ""}`}>
        削除
      </span>
    )
  }
  if (record.changed) {
    return (
      <span className={`text-blue-500 ${className ?? ""}`}>
        更新
      </span>
    )
  }

  return undefined
}