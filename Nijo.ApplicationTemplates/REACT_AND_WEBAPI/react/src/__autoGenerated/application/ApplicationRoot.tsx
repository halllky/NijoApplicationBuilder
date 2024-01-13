import React from 'react';
import { BrowserRouter, Route, Routes } from 'react-router-dom';
import { Panel, PanelGroup, PanelResizeHandle } from "react-resizable-panels";
import { QueryClient, QueryClientProvider } from 'react-query';
import { UserSettingContextProvider } from './UserSetting';
import { ServerSettingScreen } from './ServerSetting'
import { SideMenu } from './SideMenu';
import { routes } from '..';
import { MsgContextProvider, Toast, InlineMessageList } from "../util"

export function ApplicationRoot({ children }: {
  children?: React.ReactNode
}) {

  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <MsgContextProvider>
          <UserSettingContextProvider>

            <PanelGroup direction='horizontal' autoSaveId="LOCAL_STORAGE_KEY.SIDEBAR_SIZE_X" className='bg-color-base text-color-12'>
              <Panel defaultSize={20}>
                <SideMenu />
              </Panel>

              <PanelResizeHandle className='w-1' />

              <Panel className="flex flex-col [&>:first-child]:flex-1 pr-1 pt-1 pb-1">
                <Routes>
                  <Route path='/' element={<></>} />
                  <Route path='/settings' element={<ServerSettingScreen />} />
                  {routes.map(route =>
                    <Route key={route.url} path={route.url} element={route.el} />
                  )}
                  {children}
                  <Route path='*' element={<p>Not found.</p>} />
                </Routes>

                <InlineMessageList />
              </Panel>
            </PanelGroup>

            <Toast />

          </UserSettingContextProvider>
        </MsgContextProvider>
      </BrowserRouter>
    </QueryClientProvider >
  );
}

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: false,
      refetchOnWindowFocus: false,
    },
  },
})
