import React, { useState } from "react"

export default function () {

  const [itemCount, setItemCount] = useState(13)
  const maxIndent = 3

  return <>
    <style>
      {/* ソース自動生成時にデフォルトのCSSファイルにこのスタイルを生成して書き出す想定 */}
      {`
        @container (max-width: 799px) {
          .vform-container {
            grid-template-columns: ${(6 + maxIndent)}rem 1fr;
          }
          .vform-vertical-2-items {
            grid-template-rows: repeat(2, auto);
          }
          .vform-vertical-5-items {
            grid-template-rows: repeat(5, auto);
          }
          .vform-vertical-13-items {
            grid-template-rows: repeat(13, auto);
          }
        }
        @container (min-width: 800px) ${(itemCount > 2 ? 'and (max-width: 1199px)' : '')} {
          .vform-container {
            grid-template-columns: ${(6 + maxIndent)}rem 1fr 6rem 1fr;
          }
          .vform-vertical-2-items {
            grid-template-rows: repeat(1, auto);
          }
          .vform-vertical-5-items {
            grid-template-rows: repeat(3, auto);
          }
          .vform-vertical-13-items {
            grid-template-rows: repeat(7, auto);
          }
        }
      `}
      {itemCount > 2 && `
        @container (min-width: 1200px) and (max-width: 1599px) {
          .vform-container {
            grid-template-columns: ${(6 + maxIndent)}rem 1fr 6rem 1fr 6rem 1fr;
          }
          .vform-vertical-2-items {
            grid-template-rows: repeat(1, auto);
          }
          .vform-vertical-5-items {
            grid-template-rows: repeat(2, auto);
          }
          .vform-vertical-13-items {
            grid-template-rows: repeat(5, auto);
          }
        }
        @container (min-width: 1600px) {
          .vform-container {
            grid-template-columns: ${(6 + maxIndent)}rem 1fr 6rem 1fr 6rem 1fr 6rem 1fr;
          }
          .vform-vertical-2-items {
            grid-template-rows: repeat(1, auto);
          }
          .vform-vertical-5-items {
            grid-template-rows: repeat(2, auto);
          }
          .vform-vertical-13-items {
            grid-template-rows: repeat(4, auto);
          }
        }
      `}
    </style>
    <div className="p-4 min-w-full min-h-full bg-stone-100" style={{ fontFamily: '"Cascadia Mono","BIZ UDGothic"' }}>
      <div className="flex gap-1 py-1">
        <button type="button" onClick={() => setItemCount(2)} className="px-1 border border-color-8">2個にする</button>
        <button type="button" onClick={() => setItemCount(5)} className="px-1 border border-color-8">5個にする</button>
        <button type="button" onClick={() => setItemCount(13)} className="px-1 border border-color-8">13個にする</button>
      </div>
      <VFromEx.Root>
        <VFromEx.Vertical count={itemCount}>
          {Array.from({ length: itemCount }).map((_, i) => (
            <VFromEx.LabelAndItem key={i} index={i} />
          ))}
        </VFromEx.Vertical>
        <VFromEx.WideItem />
        <VFromEx.Vertical count={itemCount}>
          {Array.from({ length: itemCount }).map((_, i) => (
            <VFromEx.LabelAndItem key={i} index={i} />
          ))}
        </VFromEx.Vertical>
        <VFromEx.Section label="子ブロックその1">
          <VFromEx.Vertical count={itemCount}>
            {Array.from({ length: itemCount }).map((_, i) => (
              <VFromEx.LabelAndItem key={i} index={i} />
            ))}
          </VFromEx.Vertical>
          <VFromEx.Section label="孫ブロック1-1">
            <VFromEx.Vertical count={itemCount}>
              {Array.from({ length: itemCount }).map((_, i) => (
                <VFromEx.LabelAndItem key={i} index={i} />
              ))}
            </VFromEx.Vertical>
          </VFromEx.Section>
          <VFromEx.Section label="孫ブロック1-2">
            <VFromEx.Vertical count={itemCount}>
              {Array.from({ length: itemCount }).map((_, i) => (
                <VFromEx.LabelAndItem key={i} index={i} />
              ))}
            </VFromEx.Vertical>
            <VFromEx.WideItem />
            <VFromEx.Vertical count={itemCount}>
              {Array.from({ length: itemCount }).map((_, i) => (
                <VFromEx.LabelAndItem key={i} index={i} />
              ))}
            </VFromEx.Vertical>
          </VFromEx.Section>
        </VFromEx.Section>
        <VFromEx.Section label="子ブロックその2">
          <VFromEx.Vertical count={itemCount}>
            {Array.from({ length: itemCount }).map((_, i) => (
              <VFromEx.LabelAndItem key={i} index={i} />
            ))}
          </VFromEx.Vertical>
        </VFromEx.Section>
      </VFromEx.Root>

    </div>
  </>
}

const VFromEx = {
  /** ルート要素 */
  Root: ({ children }: { children?: React.ReactNode }) => {
    return (
      <div style={{ containerType: 'inline-size' }}>
        <div className="vform-container" style={{
          width: '100%',
          height: '100%',
          display: 'grid',
          // containerType: 'inline-size',
          // gridTemplateColumns: 'repeat(auto-fit, minmax(320px, 1fr))',
          gap: '1px',
        }}>
          {children}
        </div>
      </div>
    )
  },
  /** 縦並びのコンテナ */
  Vertical: ({ count, children }: { count: number, children?: React.ReactNode }) => {
    return (
      <div className={`vform-vertical-${count}-items`} style={{
        display: 'grid',
        gridAutoFlow: 'column',
        gridTemplateColumns: 'subgrid',
        gridColumn: '1 / -1',
        gap: '1px',
      }}>
        {children}
      </div>
    )
  },
  /** 1つインデントが下がる子要素コンテナ */
  Section: ({ label, children }: { label?: React.ReactNode, children?: React.ReactNode }) => {
    return (
      <div className="bg-color-3" style={{
        display: 'grid',
        gridTemplateColumns: 'subgrid',
        gridColumn: '1 / -1',
        ...SHADOWBORDER2,
      }}>
        {label && (
          <div className="px-1" style={{ gridColumn: '1 / -1' }}>{label}</div>
        )}
        <div style={{
          display: 'grid',
          gridTemplateColumns: 'subgrid',
          gridColumn: '1 / -1',
          paddingLeft: '1.5rem',
          gap: '1px',
        }}>
          {children}
        </div>
      </div>
    )
  },
  /** 要素（not WIDE） */
  LabelAndItem: ({ index, children }: { index?: number, children?: React.ReactNode }) => {
    return (
      <div style={{ display: 'grid', gridAutoFlow: 'row', gridTemplateColumns: 'subgrid', gridColumn: 'span 2' }}>
        <div className="px-1 bg-color-3" style={SHADOWBORDER2}>
          ラベル{index}
          {index === 5 && <>
            ラベル{index}
            ラベル{index}
            ラベル{index}
          </>}
        </div>
        <div className="px-1 bg-color-2 flex-1 min-w-0" style={SHADOWBORDER2}>
          要素{index}
          {index === 2 && (
            <textarea className="max-w-full" defaultValue="ああああああああああああああああああああああああああああああああああああ"></textarea>
          )}
          {children}
        </div>
      </div>
    )
  },
  /** 要素（WIDE） */
  WideItem: ({ index }: { index?: number }) => {
    return <>
      <div className="px-1 bg-color-3 basis-[6em]" style={{ gridColumn: '1 / -1', ...SHADOWBORDER2 }}>
        横長要素のラベル{index}
      </div>
      <div className="px-1 bg-color-2" style={{ gridColumn: '1 / -1', ...SHADOWBORDER2 }}>
        横長要素{index}
        {index === 2 && (
          <textarea className="max-w-full" defaultValue="ああああああああああああああああああああああああああああああああああああ"></textarea>
        )}
      </div>
    </>
  },
}

// ---------------------------------------

function 横並びかつインデント込みでも親子の縦がきれいにそろうgrid() {
  return (
    <div className="p-4 min-w-full min-h-full bg-stone-200" style={{ fontFamily: '"Cascadia Mono","BIZ UDGothic"' }}>

      {/* 案1 */}
      {/* <div style={{
        display: 'grid',
        gridTemplateColumns: '24px repeat(auto-fill, minmax(200px, 1fr))',
      }}>
        <Indent />
        <div style={{
          display: 'grid',
          gridTemplateColumns: 'subgrid',
          gridColumn: '2 / -1',
          // gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))',
        }}>
          {Array.from({ length: 13 }).map((_, i) => (
            <LabelAndItem key={i} index={i} />
          ))}
        </div>
      </div> */}

      {/* 案2: 幅24pxの列を大量に作って span で微調整する => インデントで横1ブロック使うところ、子孫が親と1ブロックずつずれてしまう */}
      {/* <div style={{
        display: 'grid',
        gridTemplateColumns: 'repeat(auto-fill, minmax(24px, 1fr))',
      }}>
        <Indent />
        <div style={{ display: 'grid', gridTemplateColumns: 'subgrid', gridColumn: '2 / -1' }}>
          {Array.from({ length: 13 }).map((_, i) => (
            <LabelAndItem key={i} index={i} style={{ gridColumn: 'span 8' }} />
          ))}

          <div style={{ display: 'grid', gridTemplateColumns: 'subgrid', gridColumn: '1 / -1' }}>
            <Indent />
            {Array.from({ length: 13 }).map((_, i) => (
              <LabelAndItem key={i} index={i} style={{ gridColumn: 'span 7' }} />
            ))}
          </div>
        </div>
      </div> */}

      {/* 案3: padding-left => 成功 */}
      <div className="border-vform" style={{
        display: 'grid',
        gridTemplateColumns: 'repeat(auto-fill, minmax(16rem, 1fr))',
      }}>
        {Array.from({ length: 13 }).map((_, i) => (
          <LabelAndItem key={i} index={i} />
        ))}

        <div style={{ gridColumn: '1 / -1', paddingTop: '12px' }}>子ブロック</div>
        <div style={{ display: 'grid', gridTemplateColumns: 'subgrid', gridColumn: '1 / -1', paddingLeft: '24px' }}>
          {Array.from({ length: 13 }).map((_, i) => (
            <LabelAndItem key={i + 13} index={i + 13} />
          ))}

          <div style={{ gridColumn: '1 / -1', paddingTop: '12px' }}>孫ブロック</div>
          <div style={{ display: 'grid', gridTemplateColumns: 'subgrid', gridColumn: '1 / -1', paddingLeft: '24px' }}>
            {Array.from({ length: 13 }).map((_, i) => (
              <LabelAndItem key={i + 26} index={i + 26} />
            ))}
          </div>
        </div>
      </div>

    </div>
  )
}

const Indent = () => {
  return (
    <div className="bg-color-4" style={{
      gridRow: '1 / -1',
      gridColumn: '1',
    }}></div>
  )
}
const LabelAndItem = ({ index, style }: { index?: number, style?: React.CSSProperties }) => {
  return (
    <div className="flex border-vform" style={style}>
      <div className="px-1 basis-[5em]">
        ラベル{index}
      </div>
      <div className="px-1 flex-1">
        要素{index}
      </div>
    </div>
  )
}

// ---------------------------------

function CssGrid挙動調査() {

  return (
    <div className="p-4 min-w-full min-h-full bg-stone-200" style={{ fontFamily: '"Cascadia Mono","BIZ UDGothic"' }}>
      <CssGridForm>
        <FormItem />
        <FormItem label="とてもとてもとてもとても長いラベル" />
        <FormItem label="項目1" />
        <FormItem label="項目2" />
        <FormItem label="項目3" />
        <FormItem label="項目4" />
        <FormItem label="項目5" />
        <FormItem label="項目6" />
        <FormItem label="項目7" />
        <FormItem label="項目8" />
        <FormItem label="項目9" />
        <FormItem label="項目10" />
        <FormItem label="短" />
        <FormItem value="横幅いっぱい！" wide />
        <FormItem />
        <FormItem />
        <FormItem />
        <FormItem />
        <FormItem />
        <FormItem />
        <FormItem />
        <FormItem />
        <FormItem />
        <FormItem />
        <FormItem />
        <FormItem value="とてもとてもとてもとてもとてもとてもとてもとても長い値" />
        <FormItem />
        <CssGridForm depth={1}>
          <FormItem />
          <FormItem />
          <FormItem />
          <FormItem />
          <FormItem />
          <FormItem />
          <FormItem />
          <CssGridForm depth={2}>
            <FormItem />
            <FormItem />
            <FormItem />
            <FormItem />
            <FormItem />
            <CssGridForm depth={3}>
              <FormItem />
              <FormItem />
              <FormItem />
              <FormItem />
              <FormItem />
              <FormItem />
              <FormItem />
            </CssGridForm>
            <FormItem />
            <FormItem />
          </CssGridForm>
          <FormItem />
          <FormItem />
          <FormItem />
          <FormItem />
          <FormItem />
          <FormItem />
          <FormItem />
        </CssGridForm>
        <FormItem />
        <FormItem />
        <FormItem />
        <FormItem />
        <FormItem />
      </CssGridForm>
    </div>
  )
}
const CssGridForm = ({ depth, children }: { depth?: number, children?: React.ReactNode }) => {

  if (depth === undefined || depth === 0) return (
    <div className="grid gap-[1px] grid-cols-[repeat(auto-fill,minmax(16rem,1fr))]">
      {children}
    </div >
  )

  if (depth === 1) return (
    <div className="col-span-full flex flex-col">
      <div className="mt-[1rem] text-sm text-stone-500 font-semibold">
        入れ子グリッド（深さ {depth} ）
      </div>
      <div className="flex-1 mb-[1rem] grid gap-[1px] grid-cols-[repeat(auto-fill,minmax(16rem,1fr))]">
        {children}
      </div>
    </div>
  )

  return (
    <div className="col-span-full flex flex-col bg-stone-100" style={SHADOWBORDER}>
      <div className="text-stone-500 text-sm font-semibold">
        入れ子グリッド（深さ {depth} ）
      </div>
      <div className="flex-1 ml-[2rem] grid gap-[1px] grid-cols-[repeat(auto-fill,minmax(16rem,1fr))]">
        {children}
      </div>
    </div>
  )
}
const FormItem = ({ label, value, wide }: {
  label?: string
  value?: string
  wide?: boolean
}) => {
  return wide ? (
    <div className="flex flex-col col-span-full" style={SHADOWBORDER}>
      <Label>{label ?? 'ラベル'}</Label>
      <Value>{value ?? '0000-00-00'}</Value>
    </div>
  ) : (
    <div className="flex" style={SHADOWBORDER}>
      <Label flexBasis="6rem">{label ?? 'ラベル'}</Label>
      <Value>{value ?? '0000-00-00'}</Value>
    </div>
  )
}
const SHADOWBORDER: React.CSSProperties = {
  boxShadow: '0 0 0 1px #ddd'
}
const SHADOWBORDER2: React.CSSProperties = {
  boxShadow: '0 0 0 1px black'
}
const Label = ({ flexBasis, children }: { flexBasis?: string, children?: React.ReactNode }) => {
  return (
    <div className="text-stone-600 bg-stone-100 px-1 text-sm pt-[7px]" style={{ flexBasis }}>
      {children}
    </div>
  )
}
const Value = ({ children }: { children?: React.ReactNode }) => {
  return (
    <div className="bg-white flex-1 p-1">
      <span className="inline-block whitespace-nowrap px-1 border border-1 border-stone-200">
        {children}
      </span>
    </div>
  )
}
