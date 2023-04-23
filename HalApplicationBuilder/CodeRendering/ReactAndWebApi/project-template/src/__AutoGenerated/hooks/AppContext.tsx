import moment from "moment";
import React, { useContext, useReducer } from "react";
import { UUID } from "uuidjs";
import { Toast, ToastMessage } from "../components/Toast";

export type AppState = {
    popupMessages: ToastMessage[]
}
type Action
    = { type: 'pushMsg', msg: string }
    | { type: 'delMessage', id: string }

const createDefaultAppState = (): AppState => ({
    popupMessages: [],
})
const initialState = createDefaultAppState()
const reducer: React.Reducer<AppState, Action> = (state, action) => {
    switch (action.type) {
        case 'pushMsg': {
            const id = UUID.generate()
            const popupTime = moment().format('YYYY-MM-DD hh:mm:ss')
            const popupMessages = [...state.popupMessages, { id, msg: action.msg, popupTime }]
            return { ...state, popupMessages }
        }
        case 'delMessage': {
            const popupMessages = state.popupMessages.filter(m => m.id !== action.id)
            return { ...state, popupMessages }
        }
    }
}

const AppContext = React.createContext<[AppState, React.Dispatch<Action>]>([undefined as any, undefined as any])

export const AppContextProvider = ({ children }: { children?: React.ReactNode }) => {

    const value = useReducer(reducer, initialState)

    return (
        <AppContext.Provider value={value}>

            {children}

            {/* TOAST MESSAGE */}
            <div className="fixed bottom-3 right-3" style={{ zIndex: 9999 }}>
                {value[0].popupMessages.map(msg => <Toast key={msg.id} item={msg} />)}
            </div>
        </AppContext.Provider>
    )
}

export const useAppContext = () => {
    return useContext(AppContext)
}
