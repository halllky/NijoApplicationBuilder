import React from "react"
import * as ReactHookForm from "react-hook-form"
import * as Icon from "@heroicons/react/24/outline"
import * as Input from "@nijo/ui-components/input"
import * as EG2 from "@nijo/ui-components/layout/EditableGrid2"
import FormLayout from "@nijo/ui-components/layout/FormLayout"
import useEvent from "react-use-event-hook"
import { ApplicationState, PreviewProcessSetting } from "../../types"
import * as UI from "../../UI"
import { usePreview } from "./usePreview"

/**
 * プレビュー（生成後アプリのデバッグプロセス）設定セクション。
 * nijo.preview.json の内容の閲覧・編集と、実プロセスの起動・停止・ログ表示を行う。
 * nijo.preview.json 自体の保存は、このセクション固有のボタンではなく
 * 画面共通の保存ボタン（Ctrl+S）に相乗りする。
 */
export const PreviewSection: React.FC<{
  formMethods: ReactHookForm.UseFormReturn<ApplicationState>
  projectDir: string | null
}> = ({ formMethods, projectDir }) => {
  const { control, register, getValues, setValue } = formMethods

  // プレビュープロセスの起動・停止・ログ取得
  const { processes, logs, start, stop, isBusy, error } = usePreview(projectDir)

  // concurrently配列の編集グリッド
  const looseControl = control as unknown as ReactHookForm.Control<ReactHookForm.FieldValues>
  const looseSetValue = setValue as unknown as ReactHookForm.UseFormSetValue<ReactHookForm.FieldValues>
  const {
    gridRef,
    editableGrid2Props,
    fieldArrayReturn: { append, remove },
  } = UI.useFieldArrayForEditableGrid2({
    name: "previewSetting.concurrently",
    control,
    getValues,
    setValue,
  }, helper => [
    helper.text("名前", "name", { defaultWidth: 90 }),
    helper.text("作業ディレクトリ", "process.cwd", { defaultWidth: 100 }),
    helper.text("コマンド", "process.filename", { defaultWidth: 90 }),
    helper.text("引数", "process.args", { defaultWidth: 140 }),
    helper.text("標準出力ログ", "log.stdout", { defaultWidth: 150 }),
    helper.text("標準エラーログ", "log.stderr", { defaultWidth: 150 }),
    createBooleanColumn(looseControl, looseSetValue, "追記", "log.appendStdout"),
    createBooleanColumn(looseControl, looseSetValue, "追記(エラー)", "log.appendStderr"),
    createBooleanColumn(looseControl, looseSetValue, "生成時再起動", "restartOnGenerateCode"),
  ], [looseControl, looseSetValue])

  const handleAddRow = useEvent(() => {
    const newProcess: PreviewProcessSetting = {
      name: '',
      process: { cwd: '', filename: '', args: '' },
      log: { stdout: '', stderr: '', appendStdout: true, appendStderr: false },
      restartOnGenerateCode: false,
    }
    append(newProcess)
  })
  const handleDeleteRow = useEvent(() => {
    const selection = gridRef.current?.getSelectedRows()
    if (!selection || selection.length === 0) return
    remove(selection.map(x => x.rowIndex))
  })

  return (
    <FormLayout.Section labelEnd={(
      <div id="preview" className="flex flex-col scroll-mt-2">
        <h2 className="text-lg font-bold">プレビュー</h2>
        <span className="text-xs text-gray-600">
          生成後アプリのデバッグプロセスをここから起動・停止できます
        </span>
      </div>
    )}>
      {/* 起動・停止ボタンと稼働状態 */}
      <FormLayout.Field label="操作">
        <div className="flex flex-col gap-1 my-1">
          <div className="flex items-center gap-2">
            <UI.Button outline icon={Icon.PlayIcon} onClick={start} loading={isBusy}>起動</UI.Button>
            <UI.Button outline icon={Icon.StopIcon} onClick={stop} loading={isBusy}>停止</UI.Button>
            <span className="text-xs text-gray-600">
              {processes.length === 0
                ? '停止中'
                : processes.map(p => `${p.name}: ${p.isRunning ? '稼働中' : `停止 (ExitCode=${p.exitCode ?? '?'})`}`).join(' / ')}
            </span>
          </div>
          {error && <div className="text-rose-500 text-sm">{error}</div>}
        </div>
      </FormLayout.Field>

      {/* 起動完了時に開くURL */}
      <FormLayout.Field label="browser">
        <div className="flex items-center gap-2 my-1">
          <UI.WordTextBox {...register("previewSetting.browser")} className="basis-80" />
          <span className="text-xs text-gray-500">起動完了時に開くURL。空なら開かない</span>
        </div>
      </FormLayout.Field>

      {/* nijo serve 起動時の自動起動 */}
      <FormLayout.Field label="startOnNijoServe">
        <div className="flex items-center gap-2 my-1">
          <input type="checkbox" {...register("previewSetting.startOnNijoServe")} className="h-4 w-4" />
          <span className="text-xs text-gray-500">nijo serve の起動と同時にプロセス群を自動起動する</span>
        </div>
      </FormLayout.Field>

      {/* concurrently配列の編集グリッド */}
      <FormLayout.Field fullWidth label="concurrently" labelEnd={(
        <div className="flex gap-1">
          <Input.IconButton outline mini icon={Icon.PlusIcon} onClick={handleAddRow}>追加</Input.IconButton>
          <Input.IconButton outline mini icon={Icon.TrashIcon} onClick={handleDeleteRow}>削除</Input.IconButton>
        </div>
      )}>
        <EG2.EditableGrid2
          {...editableGrid2Props}
          className="flex-1 h-[200px] resize-y border border-gray-600"
        />
      </FormLayout.Field>

      {/* 稼働中プロセスのログ */}
      {processes.map(p => (
        <FormLayout.Field key={p.name} fullWidth label={`ログ: ${p.name}`}>
          <div className="flex flex-col gap-1">
            <LogPane text={logs[p.name]?.stdout ?? ''} />
            <LogPane text={logs[p.name]?.stderr ?? ''} isError />
          </div>
        </FormLayout.Field>
      ))}
    </FormLayout.Section>
  )
}

/**
 * boolean値をそのまま扱うチェックボックス列。
 * UI.useFieldArrayForEditableGrid2 の helper.checkBox はサーバー側が "True"/"" 文字列で
 * 管理するXML属性向けであり、nijo.preview.json のような素のJSON真偽値には使えないため独自定義する。
 */
function createBooleanColumn<TRow>(
  control: ReactHookForm.Control<ReactHookForm.FieldValues>,
  setValue: ReactHookForm.UseFormSetValue<ReactHookForm.FieldValues>,
  header: string,
  key: string,
): EG2.EditableGrid2LeafColumn<TRow> {
  return {
    defaultWidth: 90,
    renderHeader: () => (
      <div className="px-1 py-px truncate text-sm text-gray-700">{header}</div>
    ),
    renderBody: ({ context }) => {
      const name = `previewSetting.concurrently.${context.row.index}.${key}`
      const value = ReactHookForm.useWatch({ control, name })
      return (
        <label className="self-start block h-full w-full px-1 cursor-pointer">
          <input
            type="checkbox"
            checked={!!value}
            onChange={e => setValue(name, e.target.checked, { shouldDirty: true })}
            className="block h-6"
          />
        </label>
      )
    },
  }
}

/** ログ1本分の表示欄。追記のたびに末尾へ自動スクロールする */
const LogPane: React.FC<{ text: string, isError?: boolean }> = ({ text, isError }) => {
  const ref = React.useRef<HTMLPreElement>(null)

  // DOM要素の末尾へのスクロール位置は描画結果に依存するため、DOM操作としてeffectで行う
  React.useEffect(() => {
    const el = ref.current
    if (el) el.scrollTop = el.scrollHeight
  }, [text])

  return (
    <pre
      ref={ref}
      className={`h-[100px] overflow-auto bg-gray-900 text-xs p-2 whitespace-pre-wrap ${isError ? 'text-rose-400' : 'text-gray-100'}`}
    >
      {text}
    </pre>
  )
}
