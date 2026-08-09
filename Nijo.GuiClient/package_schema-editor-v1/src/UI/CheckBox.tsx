import React from "react"
import * as ReactHookForm from "react-hook-form"

type CheckBoxProps<TFieldValues extends ReactHookForm.FieldValues = ReactHookForm.FieldValues> =
  Omit<React.InputHTMLAttributes<HTMLInputElement>, 'value' | 'checked' | 'onChange'> & {
    control: ReactHookForm.Control<TFieldValues>
    name: string
  }

/**
 * react-hook-form の boolean フィールドと連動するチェックボックス
 */
export const CheckBox = React.forwardRef(CheckBoxInner) as <TFieldValues extends ReactHookForm.FieldValues = ReactHookForm.FieldValues>(
  props: CheckBoxProps<TFieldValues> & React.RefAttributes<HTMLInputElement>
) => React.ReactElement | null

function CheckBoxInner<TFieldValues extends ReactHookForm.FieldValues>(
  { control, name, className, ...props }: CheckBoxProps<TFieldValues>,
  ref: React.ForwardedRef<HTMLInputElement>
) {
  return (
    <ReactHookForm.Controller
      control={control}
      name={name as ReactHookForm.Path<TFieldValues>}
      render={({ field: { onChange, value, ref: fieldRef, ...fieldRest } }) => (
        <input
          type="checkbox"
          className={`h-4 w-4 ${className ?? ''}`}
          checked={!!value}
          onChange={e => onChange(e.target.checked)}
          ref={(e) => {
            fieldRef(e)
            if (typeof ref === 'function') ref(e)
            else if (ref) ref.current = e
          }}
          {...fieldRest}
          {...props}
        />
      )}
    />
  )
}
