import React from 'react'
import { useForm, FormProvider } from 'react-hook-form'
import * as Input from '../input'
import * as Layout from '../layout'

export function WordExample() {
  const methods = useForm()
  return (
    <FormProvider {...methods}>
      <Layout.PageFrame>
        <h1>単語入力コンポーネント</h1>
        <label htmlFor="name">名前</label>
        <Input.Word control={methods.control} name="name" />
      </Layout.PageFrame>
    </FormProvider>
  )
}
