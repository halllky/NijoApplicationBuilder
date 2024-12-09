using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts.WebClient {
    internal class DashBoard {
        internal static SourceFile Generate(CodeRenderingContext context) {

            return new SourceFile {
                FileName = "DashBoard.tsx",
                RenderContent = context => $$"""
                    import React, { useCallback, useState, useEffect } from 'react'
                    import { useEvent } from 'react-use-event-hook'
                    import * as Icon from '@heroicons/react/24/outline'
                    import * as Util from '../util'
                    import * as Input from '../input'
                    import { VForm2 } from '../collection'
                    import { {{UiContext.CONTEXT_NAME}} } from '../default-ui-component'
                    {{context.ReactProject.DashBoardImports.SelectTextTemplate(import => import)}}

                    /** DashBoard */
                    export default function () {
                      return (
                        <Util.MsgContextProvider>
                          <DashBoard />
                        </Util.MsgContextProvider>
                      )
                    }

                    const DashBoard = () => {

                      const { applicationName } = React.useContext({{UiContext.CONTEXT_NAME}})

                      // デバッグ用DB再作成コマンド
                      const [recreating, setRecreating] = React.useState(false)
                      const [, dispatchMsg] = Util.useMsgContext()
                      const [, dispatchToast] = Util.useToastContext()
                      const { get, post } = Util.useHttpRequest()
                      const [withDummyData, setWithDummyData] = useState<boolean | undefined>(true)
                      const { reset: resetLocalRepository } = Util.useLocalRepositoryChangeList()

                      const recreateDatabase = useEvent(async () => {
                        if (recreating) return
                        if (!window.confirm('DBを再作成します。データは全て削除されます。よろしいですか？')) return
                        try {
                          setRecreating(true)
                          try {
                            await resetLocalRepository()
                          } catch (error) {
                            dispatchMsg(msg => msg.error(`ローカルリポジトリの初期化に失敗しました: ${error}`))
                            return
                          }

                          const response = await post(`/WebDebugger/recreate-database?generateDummyData=${withDummyData}`)
                          if (response.ok) dispatchToast(msg => msg.info('DBを再作成しました。'))
                        } finally {
                          setRecreating(false)
                        }
                      })

                      // 現在の接続先DB
                      const [currentDb, setCurrentDb] = useState<{ Name?: string, ConnStr?: string } | undefined>()
                      useEffect(() => {
                        get<{ CurrentDb?: string, DbProfiles?: { Name?: string, ConnStr?: string }[] }>('/WebDebugger/secret-settings').then(res => {
                          if (!res.ok) return
                          const currentDbDetail = res.data.DbProfiles?.find(x => x.Name === res.data.CurrentDb)
                          if (currentDbDetail) {
                            setCurrentDb(currentDbDetail)
                          } else {
                            setCurrentDb({ Name: res.data?.CurrentDb })
                          }
                        })
                      }, [])
                      const { opened: visible, toggle: toggleVisigle } = Util.useToggle2()

                      return (
                        <div className="page-content-root gap-4 p-1">
                          <div className="flex gap-1">
                            <Util.SideMenuCollapseButton />
                            <span className="font-bold">{applicationName}</span>
                          </div>

                          <Util.InlineMessageList />

                          {import.meta.env.DEV && (
                            <VForm2.Root label="デバッグ用コマンド ※この欄は開発環境でのみ表示されます" estimatedLabelWidth="8rem">
                              <VForm2.Indent label="データベース">
                                <VForm2.Item label="再作成" wideValue>
                                  <Input.IconButton onClick={recreateDatabase} fill loading={recreating}>DBを再作成する</Input.IconButton>
                                </VForm2.Item>
                                <VForm2.Item wideValue>
                                  <Input.CheckBox value={withDummyData} onChange={setWithDummyData}>ダミーデータも併せて作成する</Input.CheckBox>
                                </VForm2.Item>
                                <VForm2.Indent label="現在の接続先データベース（appsettings.json の {{RuntimeSettings.APP_SETTINGS_SECTION_NAME}} セクションで変更可能です。）">
                                  <VForm2.Item label="設定名" wideValue>
                                    {currentDb?.Name}
                                  </VForm2.Item>
                                  <VForm2.Item label="接続文字列" wideValue>
                                    <div className="flex justify-start gap-1">
                                      <Input.IconButton icon={(visible ? Icon.EyeIcon : Icon.EyeSlashIcon)} onClick={toggleVisigle} outline mini>表示</Input.IconButton>
                                      <span>
                                        {(visible
                                          ? currentDb?.ConnStr
                                          : (currentDb?.ConnStr ? '*********************************' : ''))}
                                      </span>
                                    </div>
                                  </VForm2.Item>
                                </VForm2.Indent>
                              </VForm2.Indent>
                              <VForm2.Indent label="バックエンドAPIエンドポイント">
                                <VForm2.Item>
                                  <a href={`${import.meta.env.VITE_BACKEND_API}swagger`} target="_blank" className="text-color-link">Swaggerを開く</a>
                                </VForm2.Item>
                              </VForm2.Indent>
                            </VForm2.Root>
                          )}

                    {{context.ReactProject.DashBoardContents.SelectTextTemplate(source => $$"""
                          {{WithIndent(source, "      ")}}

                    """)}}
                        </div>
                      )
                    }
                    """,
            };
        }
    }
}
