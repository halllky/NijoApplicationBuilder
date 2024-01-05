import * as UUID from 'uuid'
import { StorageUtil } from './util'

export type Query = {
  queryId: string
  name: string
  queryString: string
}
export const createNewQuery = (): Query => ({
  queryId: UUID.v4(),
  name: '',
  queryString: '',
})

export const useQueryRepository = () => {
  const { data, save } = StorageUtil.useLocalStorage<Query[]>(() => ({
    storageKey: 'HALDIAGRAM::QUERIES',
    defaultValue: () => [],
    serialize: obj => {
      return JSON.stringify(obj)
    },
    deserialize: str => {
      try {
        const parsed: Partial<Query>[] = JSON.parse(str)
        if (!Array.isArray(parsed)) return { ok: false }
        const obj = parsed.map<Query>(item => ({
          queryId: item.queryId ?? '',
          name: item.name ?? '',
          queryString: item.queryString ?? '',
        }))
        return { ok: true, obj }
      } catch (error) {
        console.error(`Failure to load application settings.`, error)
        return { ok: false }
      }
    },
  }))
  return { storedQueries: data, saveQueries: save }
}
