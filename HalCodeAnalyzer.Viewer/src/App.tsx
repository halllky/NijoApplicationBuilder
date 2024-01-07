import GraphView from './Graph'
import { Messaging, StorageUtil } from './util'

function App() {
  return (
    <Messaging.ErrorMessageContextProvider>
      <StorageUtil.LocalStorageContextProvider>
        <GraphView />
      </StorageUtil.LocalStorageContextProvider>
    </Messaging.ErrorMessageContextProvider>
  )
}

export default App
