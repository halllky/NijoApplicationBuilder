import React from 'react'
import { useForm, FormProvider } from 'react-hook-form'
import * as Input from '../input'
import * as Layout from '../layout'

export function NumberInputExample() {
  const methods = useForm()
  return (
    <FormProvider {...methods}>
      <Layout.PageFrame>
        <h1>数値入力コンポーネント</h1>
        <label htmlFor="age">年齢</label>
        <Input.NumberInput control={methods.control} name="age" />
      </Layout.PageFrame>
    </FormProvider>
  )
}
