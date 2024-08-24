import { defineReducer } from './ReactUtil'

type PageState = { pageIndex: number, loaded?: boolean }

/** 一覧検索などのページャの操作 */
export const pagingReducer = defineReducer((state: PageState) => ({
  loadComplete: () => ({ pageIndex: state.pageIndex, loaded: true }),
  nextPage: () => ({ pageIndex: state.pageIndex + 1, loaded: false }),
  prevPage: () => ({ pageIndex: Math.max(0, state.pageIndex - 1), loaded: false }),
  moveTo: (pageIndex: number) => ({ pageIndex, loaded: false }),
}))
