import * as ReactHookForm from "react-hook-form"
import * as React from "react"
import { useContext } from "react"

type ValidationMessage = {
  message: string;
  type: "error" | "warning" | "info";
}

// クライアント側バリデーションコンテキストの型定義
export interface ClientSideValidationContext {
  getValidationMessages: (name: string) => ValidationMessage[];
  validate?: (name: string, value: any) => void;
}

// バリデーションコンテキスト
export const ClientSideValidatorContext = React.createContext<ClientSideValidationContext>({
  getValidationMessages: () => []
});

/**
 * 特定のフィールドに生じたエラーメッセージを表示するコンポーネント。
 * エラーは赤字、警告は黄色、インフォメーションは青字で表示する。
 */
export const FieldErrorView = (props: {
  /** 画面全体からこのフィールドへのパス。 */
  name: string
  /** レイアウトの微調整に使用するクラス名。 */
  className?: string
}) => {
  // サーバー側エラーを取得
  const { errors } = ReactHookForm.useFormState();
  // クライアント側エラーを取得
  const clientSideValidator = useContext(ClientSideValidatorContext);

  // 対象フィールドのサーバー側エラー取得
  const fieldError = errors[props.name];

  // クライアント側エラーを取得
  const clientSideErrors = clientSideValidator.getValidationMessages(props.name);

  // 表示するメッセージがない場合はundefinedを返す
  if (!fieldError && (!clientSideErrors || clientSideErrors.length === 0)) {
    return undefined;
  }

  // エラーメッセージの配列を作成
  const messages: { message: string, className: string }[] = [];

  // サーバー側エラーの処理
  if (fieldError) {
    // 通常のエラーメッセージ
    if (fieldError.message) {
      messages.push({
        message: fieldError.message as string,
        className: "text-red-500" // エラーは赤字
      });
    }

    // typesの中のエラーを処理
    if (fieldError.types) {
      Object.entries(fieldError.types).forEach(([key, value]) => {
        if (key.startsWith('ERROR-')) {
          messages.push({ message: value as string, className: "text-red-500" });
        } else if (key.startsWith('WARN-')) {
          messages.push({ message: value as string, className: "text-yellow-500" });
        } else if (key.startsWith('INFO-')) {
          messages.push({ message: value as string, className: "text-blue-500" });
        } else {
          messages.push({ message: value as string, className: "" });
        }
      });
    }
  }

  // クライアント側エラーの処理
  clientSideErrors.forEach(error => {
    let className = "text-red-500";
    if (error.type === "warning") {
      className = "text-yellow-500";
    } else if (error.type === "info") {
      className = "text-blue-500";
    }

    messages.push({
      message: error.message,
      className
    });
  });

  return (
    <div className={`flex flex-col ${props.className ?? ''}`}>
      {messages.map((msg, index) => (
        <div key={index} className={msg.className}>
          {msg.message}
        </div>
      ))}
    </div>
  )
}

