import React from "react"
import { EditableDbRecord, tableMetadataHelper, UseQueryEditorServerApiReturn } from "./types"
import { DataModelMetadata } from "../../__autoGenerated/util"

export const QueryEditorServerApiContext = React.createContext<string>("")

export default function useQueryEditorServerApi(backendApiViaProps?: string): UseQueryEditorServerApiReturn {

  const backendApiViaContext = React.useContext(QueryEditorServerApiContext)
  const backendUrl = backendApiViaProps ?? backendApiViaContext

  const executeQuery: UseQueryEditorServerApiReturn["executeQuery"] = React.useCallback(async (sql: string) => {
    if (!backendUrl) return { ok: false, error: "backendUrl is not set" }

    const response = await fetch(`${(backendUrl.endsWith("/") ? backendUrl : backendUrl + "/")}api/query-editor/execute-query`, {
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
  }, [backendUrl])

  const getTableMetadata: UseQueryEditorServerApiReturn["getTableMetadata"] = React.useCallback(async () => {
    if (!backendUrl) return { ok: false, error: "backendUrl is not set" }

    const response = await fetch(`${(backendUrl.endsWith("/") ? backendUrl : backendUrl + "/")}api/query-editor/get-table-metadata`)
    if (response.ok) {
      const data: DataModelMetadata.Aggregate[] = await response.json()
      return { ok: true, data: tableMetadataHelper(data) }
    } else {
      return { ok: false, error: await response.text() }
    }
  }, [backendUrl])

  const getDbRecords: UseQueryEditorServerApiReturn["getDbRecords"] = React.useCallback(async query => {
    if (!backendUrl) return { ok: false, error: "backendUrl is not set" }

    const response = await fetch(`${(backendUrl.endsWith("/") ? backendUrl : backendUrl + "/")}api/query-editor/get-db-records`, {
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
  }, [backendUrl])

  const batchUpdate: UseQueryEditorServerApiReturn["batchUpdate"] = React.useCallback(async (records: EditableDbRecord[]) => {
    if (!backendUrl) return { ok: false, error: "backendUrl is not set" }

    const response = await fetch(`${(backendUrl.endsWith("/") ? backendUrl : backendUrl + "/")}api/query-editor/batch-update`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(records),
    })
    if (response.ok) {
      return { ok: true }
    } else {
      return { ok: false, error: await response.text() }
    }
  }, [backendUrl])

  const getDummyDataGenerateOptions: UseQueryEditorServerApiReturn["getDummyDataGenerateOptions"] = React.useCallback(async () => {
    if (!backendUrl) return { ok: false, error: "backendUrl is not set" }

    const response = await fetch(`${(backendUrl.endsWith("/") ? backendUrl : backendUrl + "/")}api/debug-info/dummy-data-generate-options`)
    if (response.ok) {
      const data = await response.json()
      return { ok: true, data }
    } else {
      return { ok: false, error: await response.text() }
    }
  }, [backendUrl])

  const destroyAndResetDatabase: UseQueryEditorServerApiReturn["destroyAndResetDatabase"] = React.useCallback(async (options: { [key: string]: boolean }) => {
    if (!backendUrl) return { ok: false, error: "backendUrl is not set" }

    const response = await fetch(`${(backendUrl.endsWith("/") ? backendUrl : backendUrl + "/")}api/debug-info/destroy-and-reset-database`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(options),
    })
    if (response.ok) {
      return { ok: true }
    } else {
      return { ok: false, error: await response.text() }
    }
  }, [backendUrl])

  return {
    executeQuery,
    getTableMetadata,
    getDbRecords,
    batchUpdate,
    getDummyDataGenerateOptions,
    destroyAndResetDatabase,
  }
}
