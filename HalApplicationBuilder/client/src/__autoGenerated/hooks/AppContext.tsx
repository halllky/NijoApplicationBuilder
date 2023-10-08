import moment from "moment";
import React, { useCallback, useContext, useMemo, useReducer } from "react";
import { UUID } from "uuidjs";
import { Toast, ToastMessage } from "../components/Toast";
import * as GlobalFocus from '../hooks/GlobalFocus'
import { LOCAL_STORAGE_KEYS } from "./localStorageKeys";

export type AppState = {
  popupMessages?: ToastMessage[]
  apiDomain?: string
  darkMode?: boolean
}
type Action
  = { type: 'pushMsg', id?: string, msg: string }
  | { type: 'delMessage', id: string }
  | { type: 'changeDomain', value: string }
  | { type: 'toggleDark' }

const createDefaultAppState = (): AppState => {
  const json = localStorage.getItem(LOCAL_STORAGE_KEYS.APPCONTEXT)
  return json
    ? JSON.parse(json) as AppState
    : {}
}
const initialState = createDefaultAppState()
const reducer: React.Reducer<AppState, Action> = (state, action) => {
  const updated = { ...state }
  switch (action.type) {
    case 'pushMsg': {
      const id = action.id ?? UUID.generate()
      const popupTime = moment().format('YYYY-MM-DD hh:mm:ss')
      const message: ToastMessage = { id, msg: action.msg, popupTime }
      if (Array.isArray(updated.popupMessages)) {
        updated.popupMessages = [...updated.popupMessages, message]
      } else {
        updated.popupMessages = [message]
      }
      break
    }
    case 'delMessage': {
      updated.popupMessages = updated.popupMessages?.filter(m => m.id !== action.id)
      break
    }
    case 'changeDomain': {
      updated.apiDomain = action.value
      break
    }
    case 'toggleDark': {
      updated.darkMode = !updated.darkMode
      break
    }
  }
  localStorage.setItem(LOCAL_STORAGE_KEYS.APPCONTEXT, JSON.stringify(updated))
  return updated
}

const AppContext = React.createContext<[AppState, React.Dispatch<Action>]>(undefined as any)

export const AppContextProvider = ({ children }: { children?: React.ReactNode }) => {

  const [state, dispatch] = useReducer(reducer, initialState)

  const dispatchWithSetTimeout = useCallback<typeof dispatch>(action => {
    if (action.type === 'pushMsg') {
      const id = UUID.generate()
      dispatch({ ...action, id })
      window.setTimeout(() => dispatch({ type: 'delMessage', id }), 5000)
    } else {
      dispatch(action)
    }
  }, [dispatch])

  const rootCss = useMemo(() => {
    return state.darkMode
      ? 'w-full h-full dark'
      : 'w-full h-full'
  }, [state.darkMode])

  return (
    <AppContext.Provider value={[state, dispatchWithSetTimeout]}>
      <div className={rootCss}>
        <GlobalFocus.GlobalFocusPage>

          {children}

          {/* TOAST MESSAGE */}
          <div className="fixed bottom-3 right-3" style={{ zIndex: 9999 }}>
            {state.popupMessages?.map(msg => <Toast key={msg.id} item={msg} />)}
          </div>
        </GlobalFocus.GlobalFocusPage>
      </div>
    </AppContext.Provider>
  )
}

export const useAppContext = () => {
  return useContext(AppContext)
}
