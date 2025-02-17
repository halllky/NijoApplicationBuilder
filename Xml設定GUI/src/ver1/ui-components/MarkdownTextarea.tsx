import SimpleMdeReact from "react-simplemde-editor"
import useEvent from "react-use-event-hook"

export const MarkdownTextarea = (props: {
  index: number
  value: string | undefined
  onChange: (index: number, value: string) => void
}) => {

  const handleChange = useEvent((value: string) => {
    props.onChange(props.index, value)
  })

  return (
    <SimpleMdeReact
      value={props.value}
      onChange={handleChange}
      spellCheck="false"
      options={SIMPLE_MDE_OPTIONS}
      placeholder="注釈を書いてください"
      className="flex-1"
    />
  )
}

const SIMPLE_MDE_OPTIONS: EasyMDE.Options = {
  spellChecker: false, // trueだと日本語の部分が全部チェックに引っかかってしまう
  toolbar: [], // ツールバー全部非表示
  status: false, // フッター非表示
  minHeight: '3rem',
}
