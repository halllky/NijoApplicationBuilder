import React from 'react';
import { useForm, SubmitHandler, FormProvider } from 'react-hook-form';
import { Word, NumberInput } from '../input/文字数値系';
import { DateInput } from '../input/日付時刻系';
import { VForm3 } from '../layout/VForm3';
import { IconButton } from '../input/IconButton';
import { ClientSideValidatorContext } from '../input/FieldErrorView';

// 簡易的なClientSideValidatorContextプロバイダー
const SimpleClientSideValidator: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  // バリデーションメッセージを保持するstate
  const [validationMessages, setValidationMessages] = React.useState<Record<string, { message: string, type: 'error' | 'warning' | 'info' }[]>>({});

  // バリデーションコンテキストの値
  const contextValue = React.useMemo(() => ({
    getValidationMessages: (name: string) => {
      return validationMessages[name] || [];
    },
    validate: (name: string, value: any) => {
      // 簡易的なバリデーション例（実際のアプリケーションではより複雑なロジックになるはず）
      const messages: { message: string, type: 'error' | 'warning' | 'info' }[] = [];

      if (name === 'age' && value !== undefined && value < 0) {
        messages.push({ message: '年齢は0以上の値を入力してください', type: 'error' });
      }

      setValidationMessages(prev => ({ ...prev, [name]: messages }));
    }
  }), [validationMessages]);

  return (
    <ClientSideValidatorContext.Provider value={contextValue}>
      {children}
    </ClientSideValidatorContext.Provider>
  );
};

// シンプルなエラーメッセージコンポーネント（FieldErrorViewの代わりに使用）
const SimpleErrorMessage: React.FC<{ name: string }> = ({ name }) => {
  const { formState: { errors } } = useForm();
  const error = errors[name];

  if (!error) return null;

  return <span className="text-red-500 text-sm">{error.message as string}</span>;
};

type FormValues = {
  name?: string;
  age?: number;
  birthDate?: string;
};

/**
 * 文字数値系.tsxと日付時刻系.tsxのコンポーネントの
 * 使い方を示す実装例画面
 */
export const 基本的入力フォームの実装例: React.FC = () => {
  const methods = useForm<FormValues>({
    defaultValues: {
      name: '',
      age: undefined,
      birthDate: undefined,
    },
    mode: 'onBlur',
  });

  const { control, handleSubmit, formState: { errors } } = methods;

  const onSubmit: SubmitHandler<FormValues> = (data) => {
    console.log('Form Submitted:', data);
    alert(`送信データ:\n${JSON.stringify(data, null, 2)}`);
  };

  return (
    <SimpleClientSideValidator>
      <FormProvider {...methods}>
        <div className="p-4">
          <h1 className="text-xl font-bold mb-4">基本的入力フォーム 実装例</h1>
          <form onSubmit={handleSubmit(onSubmit)}>
            <VForm3.Root labelWidth="8rem">
              <VForm3.BreakPoint>
                <VForm3.Item label="名前" required>
                  <Word
                    control={control}
                    name="name"
                  />
                  {errors.name && <span className="text-red-500 text-sm">{errors.name.message}</span>}
                </VForm3.Item>
                <VForm3.Item label="年齢">
                  <NumberInput
                    control={control}
                    name="age"
                  />
                  {errors.age && <span className="text-red-500 text-sm">{errors.age.message}</span>}
                </VForm3.Item>
                <VForm3.Item label="生年月日">
                  <DateInput
                    control={control}
                    name="birthDate"
                  />
                  {errors.birthDate && <span className="text-red-500 text-sm">{errors.birthDate.message}</span>}
                </VForm3.Item>
              </VForm3.BreakPoint>
            </VForm3.Root>

            <div className="mt-4">
              <IconButton submit fill>
                送信
              </IconButton>
            </div>
          </form>

          <div className="mt-6 p-4 border rounded bg-gray-50">
            <h2 className="text-lg font-semibold mb-2">説明</h2>
            <ul className="list-disc pl-5 space-y-1">
              <li><code>Word</code>: 単一行のテキスト入力用コンポーネントです。改行なしの単語や文を入力するのに適しています。</li>
              <li><code>NumberInput</code>: 数値入力専用コンポーネントです。フォーカスアウト時に数値型へ変換されます。</li>
              <li><code>DateInput</code>: 日付入力専用コンポーネントです。カレンダーピッカーが表示されます。</li>
              <li>各コンポーネントは <code>react-hook-form</code> の <code>control</code> と <code>name</code> プロパティを必須で受け取ります。</li>
              <li>バリデーションは <code>ClientSideValidatorContext</code> を通じて行われ、エラーメッセージは各コンポーネント内の <code>FieldErrorView</code> で表示されます。</li>
              <li>各コンポーネントは <code>className</code> プロパティを受け取り、スタイリングをカスタマイズできます。</li>
              <li><code>VForm3</code> レイアウトコンポーネントと組み合わせることで、整然としたフォームレイアウトを実現できます。</li>
            </ul>
          </div>
        </div>
      </FormProvider>
    </SimpleClientSideValidator>
  );
};
