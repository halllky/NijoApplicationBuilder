import { BrowserRouter, Route, Routes } from 'react-router-dom';
import { Panel, PanelGroup, PanelResizeHandle } from "react-resizable-panels";
import { AppContextProvider, useAppContext } from '../hooks/AppContext'
import { Dashboard } from './Dashboard'
import { MyAccount } from './MyAccount'
import { ServerSettingScreen } from './ServerSetting'
import { SideMenu } from './SideMenu';
import { UnCommitChanges } from './UnCommitChanges'
import BackgroundTaskList from '../pages/BackgroundTask/list'
import { routes } from '..';
import { QueryClient, QueryClientProvider } from 'react-query';
import React, { useMemo } from 'react';
import { LOCAL_STORAGE_KEYS } from '../hooks/localStorageKeys';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: false,
      refetchOnWindowFocus: false,
    },
  },
})

function HalappRoot({ children }: {
  children?: React.ReactNode
}) {
  const [{ darkMode }] = useAppContext()
  const className = useMemo(() => {
    return darkMode
      ? 'bg-color-0 text-color-12 dark'
      : 'bg-color-0 text-color-12'
  }, [darkMode])

  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <AppContextProvider>
          <PanelGroup direction='horizontal' autoSaveId={LOCAL_STORAGE_KEYS.SIDEBAR_SIZE_X} className={className}>
            <Panel defaultSize={20}>
              <SideMenu />
            </Panel>
            <PanelResizeHandle className='w-1' />
            <Panel className="flex [&>*]:flex-1 items-stretch pr-1 pt-1 pb-1">
              <Routes>
                <Route path='/' element={<Dashboard />} />
                <Route path='/changes' element={<UnCommitChanges />} />
                <Route path='/bagkground-tasks' element={<BackgroundTaskList />} />
                <Route path='/settings' element={<ServerSettingScreen />} />
                <Route path='/account' element={<MyAccount />} />
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

export default HalappRoot
