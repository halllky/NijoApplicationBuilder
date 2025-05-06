import React from 'react'
import { useForm, FormProvider } from 'react-hook-form'
import * as Input from '../input'
import * as Layout from '../layout'

export function DateInputExample() {
  const methods = useForm()
  return (
    <FormProvider {...methods}>
      <Layout.PageFrame>
        <h1>日付入力コンポーネント</h1>
        <label htmlFor="birthday">誕生日</label>
        <Input.DateInput control={methods.control} name="birthday" />
      </Layout.PageFrame>
    </FormProvider>
  )
}
