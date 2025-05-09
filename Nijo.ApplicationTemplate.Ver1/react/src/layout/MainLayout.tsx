import React from 'react'
import { Outlet } from 'react-router-dom'
import Sidebar from '../components/Sidebar'

const MainLayout: React.FC = () => {
  return (
    <div className="flex h-full">
      <Sidebar />
      <main className="flex-1 overflow-auto bg-gray-100">
        <Outlet />
      </main>
    </div>
  )
}

export default MainLayout
