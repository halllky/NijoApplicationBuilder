import { useCallback } from 'react'
import { useHttpRequest } from './Http'
import { BarMessage } from '..'

export const useDummyDataGenerator = (setErrorMessages: (msgs: BarMessage[]) => void) => {
  const { post } = useHttpRequest()

  return useCallback(async () => {
    let hasError = false

    return hasError
  }, [post, setErrorMessages])
}
