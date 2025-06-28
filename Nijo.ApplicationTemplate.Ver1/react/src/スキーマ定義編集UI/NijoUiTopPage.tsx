import React from "react"
import { Link, useOutletContext } from "react-router-dom"
import { SchemaDefinitionOutletContextType } from "./スキーマ定義編集/types"
import useEvent from "react-use-event-hook";
import { AppSettingsForDisplay, AppSettingsForSave } from "./types";
import { Perspective } from "./型つきドキュメント/types";
import { UUID } from "uuidjs";
import * as Input from "../input"
import * as Layout from "../layout"
import * as Icon from "@heroicons/react/24/solid"
import { getNavigationUrl } from "../routes";
import { AppSettingsEditDialog, AppSettingsEditDialogProps } from "./AppSettingsEditDialog";
import { PersonalSettingsEditDialog } from "./PersonalSettings";
import { PageFrame } from "./PageFrame";

export const NijoUiTopPage = () => {

  const {
    typedDoc: {
      createPerspective,
      loadAppSettings,
      saveAppSettings,
      savePerspective,
      loadPerspectivePageData,
    },
  } = useOutletContext<SchemaDefinitionOutletContextType>()

  // アプリケーション全体の設定
  const [appSettings, setAppSettings] = React.useState<AppSettingsForDisplay>({
    applicationName: "",
    entityTypeList: [],
    dataPreviewList: [],
  })

  React.useEffect(() => {
    loadAppSettings().then(setAppSettings)
  }, [loadAppSettings])

  // アプリケーション設定編集
  const [appSettingsDialogProps, setAppSettingsDialogProps] = React.useState<AppSettingsEditDialogProps | undefined>(undefined);
  const handleClickSettings = useEvent(() => {
    const defaultValues: AppSettingsForSave = {
      applicationName: appSettings.applicationName,
      entityTypeOrder: appSettings.entityTypeList.map(entityType => entityType.entityTypeId),
    }
    setAppSettingsDialogProps({
      defaultValues,
      entityTypeList: appSettings.entityTypeList,
      onSave: async (values: AppSettingsForSave, newPerspectives: Perspective[], entityNames: Record<string, string>) => {
        // 既存ドキュメントの名前を更新
        for (const [perspectiveId, newName] of Object.entries(entityNames)) {
          const latest = await loadPerspectivePageData(perspectiveId)
          if (!latest) continue
          const success = await savePerspective({
            perspective: {
              ...latest.perspective,
              name: newName,
            },
          })
          if (!success) {
            alert('ドキュメントの名前を更新できませんでした。')
            return // 処理中断
          }
        }

        // 新たに追加されたドキュメントのJSONファイルを作成
        for (const perspective of newPerspectives) {
          await createPerspective(perspective)
        }

        // settings.json を更新
        const success = await saveAppSettings(values)
        if (!success) {
          alert('アプリケーション設定を保存できませんでした。')
          return // 処理中断
        }

        // 再読み込み。JSON保存のタイムラグがあるので0.5秒待つ
        await new Promise(resolve => setTimeout(resolve, 500))
        const appSettings = await loadAppSettings()
        setAppSettings(appSettings)
      },
      onCancel: () => setAppSettingsDialogProps(undefined),
    })
  })

  // 個人設定
  const [openPersonalSettingsDialog, setOpenPersonalSettingsDialog] = React.useState(false);
  const handleClickPersonalSettings = useEvent(() => {
    setOpenPersonalSettingsDialog(true)
  })
  const handleClosePersonalSettingsDialog = useEvent(() => {
    setOpenPersonalSettingsDialog(false)
  })

  return (
    <PageFrame>
      <div className="h-full w-full flex justify-center items-center">
        <div className="flex flex-col items-start px-4 py-2">

          <div className="flex justify-between items-center">
            <h1 className="text-xl font-bold whitespace-nowrap">
              {appSettings.applicationName}
            </h1>
            <div className="min-w-16"></div>
            <Input.IconButton icon={Icon.PencilSquareIcon} mini onClick={handleClickSettings}>
              編集
            </Input.IconButton>
          </div>

          <div className="basis-4"></div>

          {appSettings.entityTypeList.map(entityType => (
            <MenuItem
              key={entityType.entityTypeId}
              icon={Icon.TableCellsIcon}
              link={getNavigationUrl({ page: 'typed-document-perspective', perspectiveId: entityType.entityTypeId })}
            >
              {entityType.entityTypeName}
            </MenuItem>
          ))}

          <hr className="self-stretch border-t border-gray-300 mt-4 mb-2" />

          <MenuItem icon={Icon.ShareIcon} link={getNavigationUrl({ page: 'schema' })}>
            ソースコード自動生成設定
          </MenuItem>

          <MenuItem icon={Icon.PlayCircleIcon} link={getNavigationUrl({ page: 'debug-menu' })}>
            デバッグメニュー
          </MenuItem>

          {/* データプレビュー */}
          {appSettings.dataPreviewList.map(dataPreview => (
            <MenuItem key={dataPreview.id} icon={Icon.ChartBarIcon} link={getNavigationUrl({ page: 'data-preview', dataPreviewId: dataPreview.id })}>
              {dataPreview.title}
            </MenuItem>
          ))}

          <MenuItem icon={Icon.Cog6ToothIcon} onClick={handleClickPersonalSettings}>
            個人設定
          </MenuItem>
        </div>

        {/* アプリケーション設定編集ダイアログ */}
        {appSettingsDialogProps && (
          <AppSettingsEditDialog {...appSettingsDialogProps} />
        )}

        {/* 個人設定ダイアログ */}
        {openPersonalSettingsDialog && (
          <PersonalSettingsEditDialog onClose={handleClosePersonalSettingsDialog} />
        )}
      </div>
    </PageFrame>
  )
}

/** アイコンつきリンクボタン */
const MenuItem = ({ icon, children, link, onClick }: {
  icon: React.ElementType,
  children: React.ReactNode,
  link?: string,
  onClick?: () => void,
}) => {
  return link ? (
    <Link to={link} className="self-stretch flex items-center gap-2 hover:bg-gray-200 py-1">
      {React.createElement(icon, { className: "w-4 h-4" })}
      {children}
    </Link>
  ) : (
    <div className="self-stretch flex items-center gap-2 hover:bg-gray-200 py-1 cursor-pointer" onClick={onClick}>
      {React.createElement(icon, { className: "w-4 h-4" })}
      {children}
    </div>
  )
}
