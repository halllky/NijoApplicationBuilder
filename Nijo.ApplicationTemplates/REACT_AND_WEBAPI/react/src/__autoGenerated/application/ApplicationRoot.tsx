import React from 'react'
import { BrowserRouter, Route, Routes } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from 'react-query'
import { Panel, PanelGroup, PanelResizeHandle } from 'react-resizable-panels'
import { SideMenu } from './SideMenu'
import { routes } from '..'
import * as Util from '../util'

function ApplicationRootInContext({ children }: {
  children?: React.ReactNode
}) {
  const { data: { darkMode } } = Util.useUserSetting()

  return (
    <PanelGroup
      direction='horizontal'
      autoSaveId="LOCAL_STORAGE_KEY.SIDEBAR_SIZE_X"
      className={`bg-color-base text-color-12 ${darkMode && 'dark'}`}>

      <Panel defaultSize={20}>
        <SideMenu />
      </Panel>

      <PanelResizeHandle className='w-1' />

      <Panel className="flex flex-col [&>:first-child]:flex-1 pr-1 pt-1 pb-1">
        <Routes>
          <Route path='/' element={<></>} />
          <Route path='/settings' element={<Util.ServerSettingScreen />} />
          {routes.map(route =>
            <Route key={route.url} path={route.url} element={route.el} />
          )}
          {children}
          <Route path='*' element={<p>Not found.</p>} />
        </Routes>

        <Util.InlineMessageList />
      </Panel>
    </PanelGroup>
  )
}

export function ApplicationRoot({ children }: {
  children?: React.ReactNode
}) {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Util.MsgContextProvider>
          <Util.UserSettingContextProvider>
            <ApplicationRootInContext>
              {children}
            </ApplicationRootInContext>
            <Util.Toast />
          </Util.UserSettingContextProvider>
        </Util.MsgContextProvider>
      </BrowserRouter>
    </QueryClientProvider >
  )
}

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: false,
      refetchOnWindowFocus: false,
    },
  },
})
