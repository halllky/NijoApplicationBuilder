import React from "react"
import SimpleMdeReact from "react-simplemde-editor"

export const MarkdownTextarea = React.forwardRef((props: {
  value: string | undefined
  onChange: (value: string) => void
  placeholder?: string
  className?: string
}, ref: React.ForwardedRef<HTMLDivElement>) => {

  return (
    <div className={`[&_.CodeMirror]:border-0 [&_.CodeMirror-scroll]:px-0 ${props.className ?? ''}`}>
      <SimpleMdeReact
        ref={ref}
        value={props.value}
        onChange={props.onChange}
        spellCheck="false"
        options={SIMPLE_MDE_OPTIONS}
        placeholder={props.placeholder}
        className="w-full h-full"
      />
    </div>
  )
})

const SIMPLE_MDE_OPTIONS: EasyMDE.Options = {
  spellChecker: false, // trueだと日本語の部分が全部チェックに引っかかってしまう
  toolbar: [], // ツールバー全部非表示
  status: false, // フッター非表示
  minHeight: '3rem',
}
