import React from "react"
import useEvent from "react-use-event-hook"
import * as ReactRouter from "react-router-dom"
import * as ReactHookForm from "react-hook-form"
import * as Icon from "@heroicons/react/24/solid"
import * as Input from "../input"
import * as Layout from "../layout"
import * as Util from "../util"
import { Panel, PanelGroup, PanelResizeHandle } from "react-resizable-panels"

/**
 * 詳細画面のテンプレート
 */
export const SingleViewTemplate = () => {
  return (
    <Layout.PageFrame>
      {/* ここにコンテンツを配置してください。 */}
    </Layout.PageFrame>
  )
}