import * as ReactHookForm from "react-hook-form"

/**
 * 安全にフィールドパスから値を取得する関数
 */
export const getValueByPath = (obj: ReactHookForm.FieldValues, path: string): unknown => {
  // react-hook-form で公開されているget関数のロジックを流用する
  return ReactHookForm.get(obj, path)
}

/**
 * オブジェクトの指定されたパスに値を設定するユーティリティ関数
 */
export function setValueByPath(obj: ReactHookForm.FieldValues, path: string, value: unknown): void {
  // react-hook-form で公開されているset関数のロジックを流用する
  ReactHookForm.set(obj, path, value)
}
