import React from "react";
import { ValidationError, ValidationErrorKey } from "./types";

const ValidationErrorContext = React.createContext<ValidationError>({})

export const ValidationErrorContextProvider = ValidationErrorContext.Provider

/** エラーメッセージを undefind または string[] のいずれかで取得できるフック */
export const useValidationErrorContext = (
  /** 未指定の場合、戻り値は常にundefined */
  uniqueId: string | undefined,
  /** 未指定の場合は該当のユニークIDに対するエラーすべてを列挙する */
  key?: ValidationErrorKey
) => {

  const allErrors = React.useContext(ValidationErrorContext)

  // エラーが無い場合はundefined、ある場合はstring[] を返す
  if (!uniqueId) return undefined
  const errorsByUniqueId = allErrors[uniqueId]
  if (errorsByUniqueId === undefined) return undefined
  if (Object.keys(errorsByUniqueId).length === 0) return undefined
  if (key === undefined) {
    // キー未指定の場合は該当のユニークIDに対するエラーすべてを列挙する
    return Object.values(errorsByUniqueId).flat()
  } else {
    return key in errorsByUniqueId
      ? errorsByUniqueId[key as keyof typeof errorsByUniqueId]
      : undefined
  }
}
