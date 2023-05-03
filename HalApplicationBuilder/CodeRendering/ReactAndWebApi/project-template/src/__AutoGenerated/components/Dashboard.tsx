import { useCallback } from 'react';
import { useAppContext } from '../hooks/AppContext';
import { IconButton } from './IconButton';

export const Dashboard = () => {

  const [{ apiDomain }, dispatch] = useAppContext()

  const recreateDatabase = useCallback(() => {
    if (window.confirm('DBを再作成します。データは全て削除されます。よろしいですか？')) {
      fetch(`${apiDomain}/HalappDebug/recreate-database`, {
        method: 'PUT',
      }).then(async response => {
        dispatch({ type: 'pushMsg', msg: await response.text() })
      })
    }
  }, [apiDomain, dispatch])

  return (
    <div>
      <label className="flex flex-row space-x-2">
        <span className="select-none opacity-50">
          API DOMAIN
        </span>
        <input type="text" value={apiDomain} onChange={e => dispatch({ type: 'changeDomain', value: e.target.value })} className="border" />
      </label>
      <IconButton onClick={recreateDatabase}>DB再作成</IconButton>
    </div>
  )
}
