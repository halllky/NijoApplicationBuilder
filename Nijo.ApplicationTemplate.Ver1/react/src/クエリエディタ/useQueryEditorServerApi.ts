import React from "react"
import { EditableDbRecord, DbTableEditor, UseQueryEditorServerApiReturn } from "./types"

const BACKEND_API = import.meta.env.VITE_BACKEND_API

export default function useQueryEditorServerApi(): UseQueryEditorServerApiReturn {

  const executeQuery: UseQueryEditorServerApiReturn["executeQuery"] = React.useCallback(async (sql: string) => {
    const response = await fetch(`${BACKEND_API}api/query-editor/execute-query`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(sql),
    })
    if (response.ok) {
      const data = await response.json()
      return { ok: true, records: data }
    } else {
      return { ok: false, error: await response.text() }
    }
  }, [])

  const getTableMetadata: UseQueryEditorServerApiReturn["getTableMetadata"] = React.useCallback(async () => {
    const response = await fetch(`${BACKEND_API}api/query-editor/get-table-metadata`)
    if (response.ok) {
      const data = await response.json()
      return { ok: true, data }
    } else {
      return { ok: false, error: await response.text() }
    }
  }, [])

  const getDbRecords: UseQueryEditorServerApiReturn["getDbRecords"] = React.useCallback(async (query: DbTableEditor) => {
    const response = await fetch(`${BACKEND_API}api/query-editor/get-db-records`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(query),
    })
    if (response.ok) {
      const data = await response.json()
      return { ok: true, data }
    } else {
      return { ok: false, error: await response.text() }
    }
  }, [])

  const batchUpdate: UseQueryEditorServerApiReturn["batchUpdate"] = React.useCallback(async (records: EditableDbRecord[]) => {
    const response = await fetch(`${BACKEND_API}api/query-editor/batch-update`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(records),
    })
    if (response.ok) {
      return { ok: true }
    } else {
      return { ok: false, error: await response.text() }
    }
  }, [])

  return {
    executeQuery,
    getTableMetadata,
    getDbRecords,
    batchUpdate,
  }
}
