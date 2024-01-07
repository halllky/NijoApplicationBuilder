import GraphView from './Graph'
import { Messaging, StorageUtil } from './util'
import { TauriApiContextProvider } from './TauriApi'

function App() {
  return (
    <Messaging.ErrorMessageContextProvider>
      <StorageUtil.LocalStorageContextProvider>
        <TauriApiContextProvider>
          <GraphView />
        </TauriApiContextProvider>
      </StorageUtil.LocalStorageContextProvider>
    </Messaging.ErrorMessageContextProvider>
  )
}

// function AppInsideContext() {

//   const { getFileSpecifiedCliArgs } = useTauriApi()
//   useEffect(() => {
//     (async () => {
//       console.log(await getFileSpecifiedCliArgs())
//     })()
//   }, [])

//   const queryPages = GraphView.usePages()
//   const appSettingPages = AppSetting.usePages()
//   const sideMenuItems = useMemo(() => [
//     ...queryPages.menuItems,
//     ...appSettingPages.menuItems,
//   ], [queryPages.menuItems, appSettingPages.menuItems])

//   // サイドメニュー表示非表示
//   const [{ showSideMenu }] = SideMenu.useSideMenuContext()

//   return (
//     <PanelGroup direction="horizontal">

//       <Panel defaultSize={20} className={`
//         flex [&>*]:flex-1
//         ${showSideMenu ? '' : 'hidden'}`}>
//         <SideMenu.Explorer sections={sideMenuItems} />
//       </Panel>

//       {showSideMenu && (
//         <PanelResizeHandle className="w-2 bg-zinc-100" />
//       )}

//       <Panel className={`
//         flex flex-col
//         [&>*:first-child]:flex-1 [&>*:first-child]:min-h-0 py-2
//         ${showSideMenu ? 'pr-2' : 'px-2'}
//         bg-zinc-100`}>
//         <Routes>
//           {queryPages.Routes()}
//           {appSettingPages.Routes()}
//         </Routes>
//         <Messaging.InlineMessageList />
//       </Panel>

//       <Messaging.Toast />
//     </PanelGroup>
//   )
// }

export default App
