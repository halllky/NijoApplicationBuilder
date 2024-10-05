import React, { useMemo } from 'react'
import * as Icon from '@heroicons/react/24/solid'
import * as Util from '../__autoGenerated/util'
import * as Input from '../__autoGenerated/input'
import * as Layout from '../__autoGenerated/collection'
import useEvent from 'react-use-event-hook'

export default function () {

  const [text, setText] = React.useState<string | undefined>('これはボタンのサンプルです')

  const [fill, setFill] = React.useState<boolean | undefined>(false)
  const [outline, setOutline] = React.useState<boolean | undefined>(true)
  const [hideText, setHideText] = React.useState<boolean | undefined>(false)
  const [underline, setUnderline] = React.useState<boolean | undefined>(false)
  const [mini, setMini] = React.useState<boolean | undefined>(false)
  const [loading, setLoading] = React.useState<boolean | undefined>(false)
  const [showIcon, setShowIcon] = React.useState<boolean | undefined>(true)

  const handleClick = useEvent(() => {
    alert('ボタンが押されました')
  })

  return (
    <div className="flex p-32 gap-1 max-w-screen-sm m-auto">
      <div className="border w-[20rem] p-2 border-color-4">
        <Input.IconButton
          fill={fill}
          outline={outline}
          hideText={hideText}
          underline={underline}
          mini={mini}
          loading={loading}
          icon={(showIcon ? Icon.Battery50Icon : undefined)}
          onClick={handleClick}
        >
          {text}
        </Input.IconButton>
      </div>
      <div className="flex-1 flex flex-col gap-2 p-2 border border-color-4">
        <Input.CheckBox label="fill" value={fill} onChange={setFill} />
        <Input.CheckBox label="outline" value={outline} onChange={setOutline} />
        <Input.CheckBox label="hideText" value={hideText} onChange={setHideText} />
        <Input.CheckBox label="underline" value={underline} onChange={setUnderline} />
        <Input.CheckBox label="mini" value={mini} onChange={setMini} />
        <Input.CheckBox label="loading" value={loading} onChange={setLoading} />
        <Input.CheckBox label="iconあり" value={showIcon} onChange={setShowIcon} />
        <label>
          <span className="text-xs">表示テキスト</span>
          <Input.Word value={text} onChange={setText} />
        </label>
      </div>
    </div>
  )
}
