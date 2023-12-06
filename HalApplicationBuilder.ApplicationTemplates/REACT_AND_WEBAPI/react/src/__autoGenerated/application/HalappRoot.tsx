import React from 'react';
import { BrowserRouter, Route, Routes } from 'react-router-dom';
import { Panel, PanelGroup, PanelResizeHandle } from "react-resizable-panels";
import { QueryClient, QueryClientProvider } from 'react-query';
import { AppContextProvider } from './AppContext'
import { ServerSettingScreen } from './ServerSetting'
import { SideMenu } from './SideMenu';
import { routes } from '..';
import { LOCAL_STORAGE_KEYS } from './localStorageKeys';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: false,
      refetchOnWindowFocus: false,
    },
  },
})

export function HalappRoot({ children }: {
  children?: React.ReactNode
}) {

  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <AppContextProvider>
          <PanelGroup direction='horizontal' autoSaveId={LOCAL_STORAGE_KEYS.SIDEBAR_SIZE_X} className='bg-color-base text-color-12'>
            <Panel defaultSize={20}>
              <SideMenu />
            </Panel>
            <PanelResizeHandle className='w-1' />
            <Panel className="flex [&>*]:flex-1 items-stretch pr-1 pt-1 pb-1">
              <Routes>
                <Route path='/' element={<></>} />
                <Route path='/settings' element={<ServerSettingScreen />} />
                {routes.map(route =>
                  <Route key={route.url} path={route.url} element={route.el} />
                )}
                {children}
                <Route path='*' element={<p>Not found.</p>} />
              </Routes>
            </Panel>
          </PanelGroup>
        </AppContextProvider>
      </BrowserRouter>
    </QueryClientProvider>
  );
}
