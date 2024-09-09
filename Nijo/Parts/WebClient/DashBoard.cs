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
                    import { useCallback, useState, useEffect } from 'react'
                    import { useEvent } from 'react-use-event-hook'
                    import * as Icon from '@heroicons/react/24/outline'
                    import * as Util from '../util'
                    import * as Input from '../input'
                    import { VForm2 } from '../collection'
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

                      // デバッグ用DB再作成コマンド
                      const [, dispatchMsg] = Util.useMsgContext()
                      const [, dispatchToast] = Util.useToastContext()
                      const { get, post } = Util.useHttpRequest()
                      const [withDummyData, setWithDummyData] = useState<boolean | undefined>(true)
                      const genereateDummyData2 = Util.useDummyDataGenerator2()
                      const { reset: resetLocalRepository } = Util.useLocalRepositoryChangeList()

                      const recreateDatabase = useEvent(async () => {
                        if (window.confirm('DBを再作成します。データは全て削除されます。よろしいですか？')) {
                          try {
                            await resetLocalRepository()
                          } catch (error) {
                            dispatchMsg(msg => msg.error(`ローカルリポジトリの初期化に失敗しました: ${error}`))
                          }

                          const response = await post('/WebDebugger/recreate-database')
                          if (!response.ok) { return }
                          if (withDummyData) {
                            const success = await genereateDummyData2()
                            if (!success) {
                              dispatchMsg(msg => msg.error('DBを再作成しましたがダミーデータ作成に失敗しました。'))
                              return
                            }
                          }
                          dispatchToast(msg => msg.info('DBを再作成しました。'))
                        }
                      })

                      // 現在の接続先DB
                      const [currentDb, setCurrentDb] = useState<{ name?: string, connStr?: string } | undefined>()
                      useEffect(() => {
                        get<{ currentDb?: string, db?: { name?: string, connStr?: string }[] }>('/WebDebugger/secret-settings').then(res => {
                          if (!res.ok) return
                          const currentDbDetail = res.data.db?.find(x => x.name === res.data.currentDb)
                          if (currentDbDetail) {
                            setCurrentDb(currentDbDetail)
                          } else {
                            setCurrentDb({ name: res.data?.currentDb })
                          }
                        })
                      }, [])
                      const [visible, toggleVisigle] = Util.useToggle()
                      const handleToggle = useEvent(() => {
                        toggleVisigle(x => x.toggle())
                      })

                      return (
                        <div className="page-content-root gap-4 p-1">
                          <div className="flex gap-1">
                            <Util.SideMenuCollapseButton />
                            <span className="font-bold">{{context.Config.ApplicationName}}</span>
                          </div>

                          <Util.InlineMessageList />

                          {import.meta.env.DEV && (
                            <VForm2.Root label="デバッグ用コマンド ※この欄は開発環境でのみ表示されます" estimatedLabelWidth="8rem">
                              <VForm2.Indent label="データベース">
                                <VForm2.Item label="再作成" wideValue>
                                  <Input.IconButton onClick={recreateDatabase} fill>DBを再作成する</Input.IconButton>
                                </VForm2.Item>
                                <VForm2.Item wideValue>
                                  <Input.CheckBox value={withDummyData} onChange={setWithDummyData}>ダミーデータも併せて作成する</Input.CheckBox>
                                </VForm2.Item>
                                <VForm2.Indent label="現在の接続先データベース（{{RuntimeSettings.JSON_FILE_NAME}} で設定されています）">
                                  <VForm2.Item label="設定名" wideValue>
                                    {currentDb?.name}
                                  </VForm2.Item>
                                  <VForm2.Item label="接続文字列" wideValue>
                                    <div className="flex justify-start gap-1">
                                      <Input.IconButton icon={(visible ? Icon.EyeIcon : Icon.EyeSlashIcon)} onClick={handleToggle} outline mini>表示</Input.IconButton>
                                      <span>
                                        {(visible ? currentDb?.connStr : '*********************************')}
                                      </span>
                                    </div>
                                  </VForm2.Item>
                                </VForm2.Indent>
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
