import { useCallback } from 'react'
import { useHttpRequest } from './Http'

export const useDummyDataGenerator = () => {
  const { post } = useHttpRequest()

  return useCallback(async () => {
    let hasError = false

    return hasError
  }, [post])
}
