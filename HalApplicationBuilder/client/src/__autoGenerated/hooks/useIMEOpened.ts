import { createContext, useContext } from 'react'

export const IMECheckerContext = createContext({ isIMEOpen: false })
export const useIMEOpened = () => {
  const { isIMEOpen } = useContext(IMECheckerContext)
  return isIMEOpen
}
