import React from 'react'
import useEvent from 'react-use-event-hook'
import { PageState } from './types'

/** nijo.exe 側との通信を行います。 */
export const useBackend = () => {
  // 画面表示直後の一瞬のみfalse。通信準備完了したらtrue
  const [ready, setReady] = React.useState(false)

  // サーバー側ドメイン。
  // - nijo.exeから起動したときは '' (空文字)
  // - Nijoプロジェクトのデバッグプロファイル "nijo ui" が立ち上がっているときは https://localhost:8081 を画面から入力する
  const [backendDomain, setBackendDomain] = React.useState<string | undefined>('')
  const onChangBackendDomain = useEvent((value: string | undefined) => {
    if (value) {
      setBackendDomain(value)
      localStorage.setItem(STORAGE_KEY, value)
    } else {
      setBackendDomain(value)
      localStorage.removeItem(STORAGE_KEY)
    }
  })
  React.useLayoutEffect(() => {
    const savedValue = localStorage.getItem(STORAGE_KEY)
    if (savedValue) setBackendDomain(savedValue)
    setReady(true)
  }, [])

  const load = useEvent(async (): Promise<PageState> => {
    const response = await fetch(`${backendDomain ?? ''}/load`, {
      method: 'GET',
    })
    const responseBody = await response.json() as PageState
    return responseBody
  })

  return {
    /** 画面初期表示時のみfalse。通信準備完了したらtrue */
    ready,
    /** バックエンド側ドメイン。末尾スラッシュなし。 */
    backendDomain,
    /** バックエンド側ドメインを設定します。 */
    onChangBackendDomain,
    /** 画面初期表示時データを読み込んで返します。 */
    load,
  }
}

const STORAGE_KEY = 'NIJO-UI::BACKEND-API'
