/**
 * このファイルはソース自動生成によって上書きされます。
 */

import { RouteObject } from 'react-router-dom'

export const THIS_APPLICATION_NAME = 'サンプルアプリケーション' as const
export const routes: RouteObject[] = []
export const menuItems: SideMenuItem[] = []
export const SHOW_LOCAL_REPOSITORY_MENU = true

export type SideMenuItem = {
  url: string
  text: React.ReactNode
  icon?: React.ElementType
  children?: SideMenuItem[]
}
