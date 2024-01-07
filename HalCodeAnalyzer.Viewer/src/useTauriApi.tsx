import { useCallback } from 'react'
import { Messaging } from './util'
import { invoke } from '@tauri-apps/api'

export default function () {
  const [, dispatchMsg] = Messaging.useMsgContext()

  const getFileSpecifiedCliArgs = useCallback(async (): Promise<string> => {
    try {
      const fileContents = await invoke('get_cli_arg_file')
      if (typeof fileContents !== 'string') {
        console.error('Server doesnt return string.', fileContents)
        dispatchMsg(msg => msg.push('error', 'Server doesnt return string.'))
        console.error('')
        return ''
      }
      return fileContents as string

    } catch (error) {
      dispatchMsg(msg => msg.push('error', error))
      return ''
    }
  }, [])

  return {
    getFileSpecifiedCliArgs,
  }
}
