import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';

interface IMEContextType {
  /** 現在IMEが有効かどうか */
  isComposing: boolean;
}

/** IME の状態を管理するコンテキスト */
const IMEContext = createContext<IMEContextType | undefined>(undefined);

/** IME の状態を管理するプロバイダー */
export const IMEProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [isComposing, setIsComposing] = useState(false);

  useEffect(() => {
    const handleCompositionStart = () => {
      setIsComposing(true);
    };
    const handleCompositionEnd = () => {
      setIsComposing(false);
    };

    document.addEventListener('compositionstart', handleCompositionStart);
    document.addEventListener('compositionend', handleCompositionEnd);

    return () => {
      document.removeEventListener('compositionstart', handleCompositionStart);
      document.removeEventListener('compositionend', handleCompositionEnd);
    };
  }, []);

  return (
    <IMEContext.Provider value={{ isComposing }}>
      {children}
    </IMEContext.Provider>
  );
};

/** IME の状態を取得するカスタムフック */
export const useIME = (): IMEContextType => {
  const context = useContext(IMEContext);
  if (context === undefined) {
    throw new Error('useIME must be used within an IMEProvider');
  }
  return context;
};
