import * as React from "react"
import * as ReactRouter from "react-router-dom"
import useEvent from "react-use-event-hook"
import { NowLoading } from "@nijo/ui-components/layout"
import { loadProject, loadSchemaRule } from "./backend/api"
import { ProjectSelector } from "./ProjectSelector/ProjectSelector"
import ProjectPage from "./ProjectPage"

/** WindowsForms埋め込みアプリまたはそのデバッグ用のルーティング */
export const getRouterForNijoUi = (): ReactRouter.RouteObject[] => {
  return [{
    path: '/',
    loader: rootLoader,
    element: <Root />,
    errorElement: <ErrorPage />,
  }]
}

/**
 * 画面全体の初期表示処理
 */
const rootLoader = async ({ request }: ReactRouter.LoaderFunctionArgs) => {
  const url = new URL(request.url)
  const projectDir = url.searchParams.get(NIJOUI_CLIENT_ROUTE_PARAMS.QUERY_PROJECT_DIR)

  // クエリパラメータでプロジェクト情報が無い場合
  if (!projectDir) {
    return null
  }

  // 読み込み（編集対象のプロジェクトの内容と、プロジェクトに依存しない固定ルールを並列取得する）
  const [projectResult, ruleResult] = await Promise.all([
    loadProject(projectDir, request.signal),
    loadSchemaRule(request.signal),
  ])

  if (!projectResult.ok) {
    throw new Error(projectResult.error ?? '不明なエラー')
  }
  if (!ruleResult.ok) {
    throw new Error(ruleResult.error ?? '不明なエラー')
  }
  return { project: projectResult.value, rule: ruleResult.value }
}

/**
 * ルート画面。クエリパラメータを基にしたデータ読み込み結果によって画面を出し分ける
 */
function Root() {
  const data = ReactRouter.useLoaderData() as Awaited<ReturnType<typeof rootLoader>>
  const navigation = ReactRouter.useNavigation()

  if (navigation.state === 'loading') return (
    <NowLoading />
  )

  if (!data) return (
    <ProjectSelector />
  )

  return (
    <ProjectPage defaultValues={data.project} schemaRule={data.rule} />
  )
}

/**
 * ルートのローダーでエラーが発生した場合に表示するコンポーネント。
 */
function ErrorPage() {
  const error = ReactRouter.useRouteError()
  const handleReload = React.useCallback(() => {
    window.location.reload()
  }, [])

  return (
    <div className="flex flex-col items-start gap-2 p-1">
      <span className="text-rose-600">
        読み込みでエラーが発生しました: {error instanceof Error ? error.message : String(error)}
      </span>
      <ReactRouter.Link to="" className="text-sky-600 underline cursor-pointer">
        プロジェクト選択へ戻る
      </ReactRouter.Link>
      <button type="button" onClick={handleReload} className="text-sky-600 underline cursor-pointer">
        再読み込み
      </button>
    </div>
  )
}

/** WindowsForms埋め込みアプリまたはそのデバッグ用のルーティングパラメーター */
export const NIJOUI_CLIENT_ROUTE_PARAMS = {
  /** nijo.xmlがあるディレクトリ（クエリパラメータ）。C#側と合わせる必要あり */
  QUERY_PROJECT_DIR: 'pj',
} as const

/** WindowsForms埋め込みアプリまたはそのデバッグ用のナビゲーション処理を取得する。 */
export const useNijoUiNavigation = () => {
  const navigate = ReactRouter.useNavigate()
  const [searchParams] = ReactRouter.useSearchParams()

  return useEvent((...args:
    | [to: 'project-selector']
    | [to: 'project', projectDir: string]
  ): void => {
    const [to, argsProjectDir] = args

    // クエリパラメータ
    const params = new URLSearchParams()
    const projectDir = searchParams.get(NIJOUI_CLIENT_ROUTE_PARAMS.QUERY_PROJECT_DIR)
    if (argsProjectDir) {
      // 新しくプロジェクトを開く
      params.set(NIJOUI_CLIENT_ROUTE_PARAMS.QUERY_PROJECT_DIR, argsProjectDir)

    } else if (projectDir) {
      // 現在開かれているプロジェクトディレクトリを引き継ぐ
      params.set(NIJOUI_CLIENT_ROUTE_PARAMS.QUERY_PROJECT_DIR, projectDir)
    }

    if (to === 'project-selector') {
      navigate(`/`)

    } else if (to === 'project') {
      navigate(`/?${params.toString()}`)

    } else {
      throw new Error(`不正なページ: ${to}`)
    }
  })
}
