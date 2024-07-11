import React from 'react'
import { ContainerProps, VerticalForm as VForm } from './VerticalForm'

/** サーバーから返された匿名型のJSONオブジェクトなど、型が不明な何らかの変数の値を画面上で閲覧するためのコンポーネント */
export const UnknownObjectViewer = ({ value, ...rest }: { value: unknown } & Omit<ContainerProps, 'children'>) => {
  const type = typeof value
  let content: React.ReactNode

  if (type === 'undefined' || value === undefined) {
    content = (
      <VForm.Item>undefined</VForm.Item>
    )
  } else if (value === null) {
    content = (
      <VForm.Item>null</VForm.Item>
    )
  } else if (type === 'bigint' || type === 'boolean' || type === 'number' || type === 'string' || type === 'function' || type === 'symbol') {
    content = (
      <VForm.Item>{value.toString()}</VForm.Item>
    )
  } else if (Array.isArray(value)) {
    content = value.map((item, index) => (
      <UnknownObjectViewer key={index}
        value={item}
        labelPosition="left"
        label={(
          <div className="overflow-hidden w-[2rem] px-1">
            <VForm.LabelText>{index}</VForm.LabelText>
          </div>
        )}
      />
    ))
  } else {
    content = (
      Object.entries(value).map(([k, v]) => (
        <PropertyView key={k} propName={k} propValue={v} />
      ))
    )
  }

  return (
    <VForm.Container {...rest}>
      {content}
    </VForm.Container>
  )
}

const PropertyView = ({ propName, propValue }: {
  propName: React.ReactNode
  propValue: unknown
}) => {
  const type = typeof propValue

  if (type === 'undefined' || propValue === undefined) {
    return (
      <VForm.Item label={propName}>
        <PlainText>undefined</PlainText>
      </VForm.Item>
    )
  }

  if (propValue === null) {
    return (
      <VForm.Item label={propName}>
        <PlainText>null</PlainText>
      </VForm.Item>
    )
  }

  if (type === 'bigint' || type === 'boolean' || type === 'number' || type === 'string' || type === 'function' || type === 'symbol') {
    return (
      <VForm.Item label={propName}>
        <PlainText>{propValue.toString()}</PlainText>
      </VForm.Item>
    )
  }

  if (Array.isArray(propValue)) {
    return (
      <VForm.Container label={propName}>
        {propValue.map((item, index) => (
          <UnknownObjectViewer key={index}
            value={item}
            labelPosition="left"
            label={(
              <div className="overflow-hidden w-[2rem] px-1">
                <VForm.LabelText>{index}</VForm.LabelText>
              </div>
            )}
          />
        ))}
      </VForm.Container>
    )
  }

  return (
    <VForm.Container label={propName}>
      {Object.entries(propValue).map(([k, v]) => (
        <PropertyView key={k} propName={k} propValue={v} />
      ))}
    </VForm.Container>
  )
}

const PlainText = ({ children }: { children?: React.ReactNode }) => {
  return (
    <span className="block overflow-hidden text-ellipsis">
      {children}
    </span>
  )
}
