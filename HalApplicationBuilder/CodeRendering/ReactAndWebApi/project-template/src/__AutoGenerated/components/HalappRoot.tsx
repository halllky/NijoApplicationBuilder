import { BrowserRouter, Route, Routes } from 'react-router-dom';
import { Panel, PanelGroup, PanelResizeHandle } from "react-resizable-panels";
import { AppContextProvider } from '../hooks/AppContext'
import { Dashboard } from './Dashboard'
import { MyAccount } from './MyAccount'
import { SettingsScreen } from './Settings'
import { SideMenu } from './SideMenu';
import { UnCommitChanges } from './UnCommitChanges'
import { routes } from '..';
import { QueryClient, QueryClientProvider } from 'react-query';
import React from 'react';

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

  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <AppContextProvider>
          <PanelGroup direction='horizontal'>
            <Panel defaultSize={20}>
              <SideMenu />
            </Panel>
            <PanelResizeHandle className='w-1' />
            <Panel className="flex [&>*]:flex-1 items-stretch pr-1 pt-1 pb-1">
              <Routes>
                <Route path='/' element={<Dashboard />} />
                <Route path='/changes' element={<UnCommitChanges />} />
                <Route path='/settings' element={<SettingsScreen />} />
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
