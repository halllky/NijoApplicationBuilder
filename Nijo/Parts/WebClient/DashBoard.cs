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
                    import { useCallback, useState } from 'react'
                    import { useEvent } from 'react-use-event-hook'
                    import * as Util from '../util'
                    import * as Input from '../input'
                    import { VerticalForm as VForm } from '../collection'
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
                      const { post } = Util.useHttpRequest()
                      const [withDummyData, setWithDummyData] = useState<boolean | undefined>(true)
                      const [genDummyVer1, setGenDummyVer1] = useState<boolean | undefined>(false)
                      const genereateDummyData1 = Util.useDummyDataGenerator()
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
                            const success = genDummyVer1
                              ? await genereateDummyData1()
                              : await genereateDummyData2()
                            if (!success) {
                              dispatchMsg(msg => msg.error('DBを再作成しましたがダミーデータ作成に失敗しました。'))
                              return
                            }
                          }
                          dispatchToast(msg => msg.info('DBを再作成しました。'))
                        }
                      })

                      return (
                        <div className="page-content-root gap-4 p-1">
                          <div className="flex gap-1">
                            <Util.SideMenuCollapseButton />
                            <span className="font-bold">{{context.Config.ApplicationName}}</span>
                          </div>

                          <Util.InlineMessageList />

                          {import.meta.env.DEV && (
                            <VForm.Container label="デバッグ用コマンド ※この欄は開発環境でのみ表示されます">
                              <VForm.Item label="データベース">
                                <Input.Button outlined onClick={recreateDatabase}>DBを再作成する</Input.Button>
                                <Input.CheckBox value={withDummyData} onChange={setWithDummyData}>ダミーデータも併せて作成する</Input.CheckBox>
                                <Input.CheckBox value={genDummyVer1} onChange={setGenDummyVer1}>旧版のダミー生成処理を使用する</Input.CheckBox>
                              </VForm.Item>
                            </VForm.Container>
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
