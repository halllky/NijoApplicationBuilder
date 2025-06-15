import * as ReactRouter from "react-router-dom"

/**
 * MultiViewの検索処理。
 * URLのクエリパラメータから検索条件を取得し、自動生成された検索処理を呼び出して検索を行う。
 * 検索はページングされており、このフック内部には現在表示中のページのデータのみが格納される。
 */
export const useLoader = () => {
  const [searchParams, setSearchParams] = ReactRouter.useSearchParams()

  const page = searchParams.get("page")
  const pageSize = searchParams.get("pageSize")

  const pageNumber = page ? parseInt(page) : 1
  const pageSizeNumber = pageSize ? parseInt(pageSize) : 10
}