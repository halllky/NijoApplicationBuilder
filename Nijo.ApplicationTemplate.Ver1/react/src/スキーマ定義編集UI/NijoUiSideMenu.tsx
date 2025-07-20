import React from "react"
import { Link } from "react-router-dom"
import { NijoUiOutletContextType } from "./types"
import useEvent from "react-use-event-hook";
import { AppSettingsForSave } from "./types";
import { Perspective } from "./型つきドキュメント/types";
import { UUID } from "uuidjs";
import * as Input from "../input"
import * as Layout from "../layout"
import * as Icon from "@heroicons/react/24/solid"
import { getNavigationUrl } from "../routes";
import { AppSettingsEditDialog, AppSettingsEditDialogProps } from "./AppSettingsEditDialog";
import { PersonalSettingsEditDialog } from "./PersonalSettings";

export const NijoUiSideMenu = ({ outletContext }: {
  outletContext: NijoUiOutletContextType
}) => {

  const {
    typedDoc: {
      appSettings,
      createPerspective,
      saveAppSettings,
      savePerspective,
      loadPerspectivePageData,
    },
  } = outletContext

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

        // settings.json を更新。
        // 再読み込みはsaveAppSettingsの中で行われる
        const success = await saveAppSettings(values)
        if (!success) {
          alert('アプリケーション設定を保存できませんでした。')
          return // 処理中断
        }
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
    <>
      <div className="h-full flex flex-col p-2 bg-gray-100 overflow-y-auto">

        <div className="flex justify-between items-center">
          <h1 className="font-bold truncate">
            {appSettings.applicationName}
          </h1>
          <Input.IconButton icon={Icon.PencilSquareIcon} mini hideText onClick={handleClickSettings}>
            編集
          </Input.IconButton>
        </div>

        <div className="basis-4 min-h-4"></div>

        {appSettings.entityTypeList.map(entityType => (
          <MenuItem
            key={entityType.entityTypeId}
            icon={Icon.TableCellsIcon}
            link={getNavigationUrl({ page: 'typed-document-perspective', perspectiveId: entityType.entityTypeId })}
          >
            {entityType.entityTypeName}
          </MenuItem>
        ))}

        <SectionSeparator />

        <SectionTitle>
          ソースコード自動生成設定
        </SectionTitle>

        <MenuItem icon={Icon.ShareIcon} link={getNavigationUrl({ page: 'schema' })}>
          集約定義
        </MenuItem>

        <MenuItem icon={Icon.ShareIcon} link={getNavigationUrl({ page: 'schema-enum-definition' })}>
          区分定義
        </MenuItem>

        <SectionSeparator />

        <SectionTitle>
          デバッグ
        </SectionTitle>

        <MenuItem icon={Icon.PlayCircleIcon} link={getNavigationUrl({ page: 'debug-menu' })}>
          デバッグメニュー
        </MenuItem>

        {/* データプレビュー */}
        {appSettings.dataPreviewList.map(dataPreview => (
          <MenuItem key={dataPreview.id} icon={Icon.ChartBarIcon} link={getNavigationUrl({ page: 'data-preview', dataPreviewId: dataPreview.id })}>
            {dataPreview.title}
          </MenuItem>
        ))}

        <SectionSeparator />

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
    </>
  )
}

const SectionSeparator = () => {
  return (
    <div className="self-stretch border-t border-gray-300 mt-4 mb-2" />
  )
}

const SectionTitle = ({ children }: { children: React.ReactNode }) => {
  return (
    <span className="truncate text-xs text-gray-600 select-none mb-1">
      {children}
    </span>
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
      {React.createElement(icon, { className: "w-4 h-4 min-w-4" })}
      <span className="truncate">
        {children}
      </span>
    </Link>
  ) : (
    <div className="self-stretch flex items-center gap-2 hover:bg-gray-200 py-1 cursor-pointer" onClick={onClick}>
      {React.createElement(icon, { className: "w-4 h-4 min-w-4" })}
      <span className="truncate">
        {children}
      </span>
    </div>
  )
}
