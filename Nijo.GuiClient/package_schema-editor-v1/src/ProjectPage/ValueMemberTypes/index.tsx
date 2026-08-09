import { Allotment, LayoutPriority } from "allotment"
import React from "react"
import * as ReactHookForm from "react-hook-form"
import FormLayout from "@nijo/ui-components/layout/FormLayout"
import { EditingProject } from "../../backend"
import StaticEnumGrid from "./StaticEnumGrid"
import { CustomAttributeSettings } from "./CustomAttributeSettings"

/**
 * 属性種類定義画面。
 *
 * Nijo の ValueMemberType に相当する属性種類を定義する。
 */
function ValueMemberTypes(props: {
  formMethods?: ReactHookForm.UseFormReturn<EditingProject>
}) {
  const { control } = props.formMethods ?? {}
  const staticEnums = control ? ReactHookForm.useWatch({
    control,
    name: "staticEnums"
  }) : []

  // 静的区分の一覧を取得
  const enumItems = React.useMemo(() => {
    return (staticEnums ?? []).map(root => ({
      uniqueId: root.uniqueId,
      physicalName: root.physicalName,
    }))
  }, [staticEnums])

  return (
    <Allotment proportionalLayout={false} separator={false}>

      {/* 目次 */}
      <Allotment.Pane preferredSize={200} className="border-r border-gray-400">
        <div className="h-full w-full overflow-y-auto bg-gray-50 p-3">
          <div className="flex flex-col gap-4 text-sm">
            <div>
              <SideMenuLink hash="value-types" className="font-semibold text-gray-800">
                種類
              </SideMenuLink>
              <div className="ml-2 border-l border-gray-300 pl-1 flex flex-col gap-1 pt-1">
                <SideMenuLink hash="string-values" className="pl-1 text-sm">
                  文字列型
                </SideMenuLink>
                <SideMenuLink hash="numeric-values" className="pl-1 text-sm">
                  数値型
                </SideMenuLink>
                <SideMenuLink hash="date-values" className="pl-1 text-sm">
                  日付型
                </SideMenuLink>
                <SideMenuLink hash="selection-values" className="pl-1 text-sm">
                  選択・区分型
                </SideMenuLink>
                {enumItems.length > 0 && (
                  <div className="ml-2 flex flex-col items-stretch border-l border-gray-200 gap-1 pl-1 py-1">
                    {enumItems.map(item => (
                      <button
                        type="button"
                        key={item.uniqueId}
                        onClick={() => {
                          const el = document.getElementById(`enum-def-${item.uniqueId}`)
                          if (el) el.scrollIntoView({ block: 'center', behavior: 'smooth' })
                        }}
                        className="pl-1 py-0.5 w-full text-left hover:bg-gray-200 cursor-pointer select-none"
                      >
                        <div className="text-xs truncate max-w-full" title={item.physicalName ?? undefined}>
                          {item.physicalName || '(名前なし)'}
                        </div>
                      </button>
                    ))}
                  </div>
                )}
                <SideMenuLink hash="other-values" className="pl-1 text-sm">
                  その他
                </SideMenuLink>
              </div>
            </div>

            <div>
              <SideMenuLink hash="value-options" className="font-semibold text-gray-800">
                オプション
              </SideMenuLink>
              <div className="ml-2 border-l border-gray-300 pl-1 flex flex-col gap-1 pt-1 pb-4">
                <SideMenuLink hash="custom-attributes" className="pl-1 text-sm">
                  カスタム属性
                </SideMenuLink>
              </div>
            </div>
          </div>
        </div>
      </Allotment.Pane>

      {/* コンテンツ */}
      <Allotment.Pane priority={LayoutPriority.High}>
        <div className="h-full w-full overflow-y-auto px-4 py-2">

          <section id="value-types">
            <h1 className="text-3xl font-bold">種類</h1>
            <p className="text-sm text-gray-700 mt-2">
              このページでは「データ構造」の各データの属性として利用できる種類の詳細設定を確認できます。
            </p>

            <section className="mt-6">
              <h2 id="string-values" className="text-2xl font-bold">文字列型</h2>

              <ul className="list-disc list-inside my-2 space-y-1">
                <li>
                  <strong className="mr-2">word (単語型):</strong>
                  ものの名前など改行を含まない単語を表す。
                </li>
                <li>
                  <strong className="mr-2">description (文章型):</strong>
                  コメントや備考などフリーフォーマットの文章。
                </li>
              </ul>
            </section>

            <section className="mt-8">
              <h2 id="numeric-values" className="text-2xl font-bold">数値型</h2>

              <ul className="list-disc list-inside my-2 space-y-1">
                <li>
                  <strong className="mr-2">int (整数型):</strong>
                  数量、回数、順序番号などの整数。
                </li>
                <li>
                  <strong className="mr-2">decimal (実数型):</strong>
                  金額、重量、割合などの小数を含む数値。
                </li>
                <li>
                  <strong className="mr-2">sequence (採番型):</strong>
                  DB登録時に自動採番される整数。
                </li>
              </ul>
            </section>

            <section className="mt-8">
              <h2 id="date-values" className="text-2xl font-bold">日付型</h2>

              <ul className="list-disc list-inside my-2 space-y-1">
                <li>
                  <strong className="mr-2">date (日付型):</strong>
                  年月日。時刻は含まない。
                </li>
                <li>
                  <strong className="mr-2">datetime (日時型):</strong>
                  年月日と時刻。
                </li>
                <li>
                  <strong className="mr-2">year (年型):</strong>
                  西暦年。
                </li>
                <li>
                  <strong className="mr-2">year-month (年月型):</strong>
                  年月。
                </li>
              </ul>
            </section>

            <section className="mt-8">
              <h2 id="selection-values" className="text-2xl font-bold">選択・区分型</h2>

              <ul className="list-disc list-inside my-2 space-y-1">
                <li>
                  <strong className="mr-2">bool (真偽値型):</strong>
                  はい/いいえ (True/False)。
                </li>
                <li>
                  <strong className="mr-2">静的区分:</strong>
                  モデル全体で共有する選択肢。
                </li>
              </ul>

              {props.formMethods && (
                <StaticEnumGrid formMethods={props.formMethods} />
              )}

            </section>

            <section className="mt-8">
              <h2 id="other-values" className="text-2xl font-bold">その他</h2>

              <ul className="list-disc list-inside my-2 space-y-1">
                <li>
                  <strong className="mr-2">bytearray (バイナリ型):</strong>
                  ハッシュ化されたパスワードなどのバイナリデータ。
                </li>
              </ul>
            </section>
          </section>

          <section id="value-options" className="mt-12 pb-96">
            <h1 className="text-3xl font-bold">オプション</h1>

            <section id="custom-attributes" className="mt-4">
              <h2 className="text-2xl font-bold">カスタム属性</h2>
              <p className="text-sm text-gray-700 mt-2">
                標準の型に加えて、属性へ付与するメタ情報を独自に定義します。各属性に再利用できる追加列を設計してください。
              </p>

              {props.formMethods ? (
                <div className="mt-4">
                  <FormLayout.Root labelWidthPx={160}>
                    <CustomAttributeSettings formMethods={props.formMethods} />
                  </FormLayout.Root>
                </div>
              ) : (
                <p className="text-sm text-gray-500 mt-4">フォームの読み込み後に設定できます。</p>
              )}
            </section>
          </section>
        </div>
      </Allotment.Pane>
    </Allotment>
  )
}

export default React.memo(ValueMemberTypes)

/**
 * 目次のリンク
 */
function SideMenuLink({ hash, children, className }: {
  hash: string
  children?: React.ReactNode
  className?: string
}) {

  const handleClick = () => {
    const el = document.getElementById(hash)
    if (el) el.scrollIntoView({ block: 'start', behavior: 'smooth' })
  }

  return (
    <button
      type="button"
      onClick={handleClick}
      className={`px-2 py-1 w-full text-left truncate hover:bg-gray-200 cursor-pointer select-none text-sm ${className ?? ''}`}
    >
      {children}
    </button>
  )
}
