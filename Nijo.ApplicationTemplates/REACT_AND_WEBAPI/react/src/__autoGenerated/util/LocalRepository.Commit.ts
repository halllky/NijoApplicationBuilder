/**
 * このファイルはソース自動生成によって上書きされます。
 */
import { useCallback } from 'react'
import { useMsgContext } from './Notification'
import { ItemKey, LocalRepositoryContextValue } from './LocalRepository'

export const useLocalRepositoryCommitHandling = () => {
  const [, dispatchMsg] = useMsgContext()

  return useCallback(async (
    commit: LocalRepositoryContextValue['commit'],
    ...keys: { dataTypeKey: string, itemKey: ItemKey }[]
  ) => {
    const success = await commit(async localReposItem => {

      // ここに各データの種類ごとの更新APIをたたく処理を自動生成する

      dispatchMsg(msg => msg.error(`データ型 '${localReposItem.dataTypeKey}' の保存処理が定義されていません。`))
      return { commit: false }
    }, ...keys)

    dispatchMsg(msg => success
      ? msg.info('保存しました。')
      : msg.info('一部のデータの保存に失敗しました。'))
  }, [dispatchMsg])
}
