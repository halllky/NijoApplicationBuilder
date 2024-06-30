export default function () {

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
