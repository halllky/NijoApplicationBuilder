import { UUID } from "uuidjs"
import { EditableDbRecord } from "../types"

/**
 * 新しいEditableDbRecordインスタンスを作成する共通関数
 * @param tableName テーブル名
 * @returns 新しいEditableDbRecordインスタンス
 */
export const createNewInstance = (tableName: string): EditableDbRecord => {
  return {
    uniqueId: UUID.generate(),
    tableName,
    values: {},
    existsInDb: false,
    changed: false,
    deleted: false,
  }
}
