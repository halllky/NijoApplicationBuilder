import React, { InputHTMLAttributes, forwardRef, useCallback, useImperativeHandle, useMemo, useRef, useState } from "react"
import { FormProvider, SubmitHandler } from "react-hook-form"
import { ColDef } from "ag-grid-community"
import '../__autoGenerated/halapp.css'
import { AppContextProvider, useAppContext } from "../__autoGenerated/hooks/AppContext"
import * as Input from "../__autoGenerated/user-input"
import * as Layout from "../__autoGenerated/layout"
import { UUID } from "uuidjs"

const VForm = Layout.VerticalForm

type TestDataRoot = TestData & {
  gridRows?: TestData[]
}
type TestData = {
  word?: string
  description?: string
  bbb?: string
  num?: string
  date?: string
  ym?: string
  combo?: Input.SelectionItem
  check?: boolean
  checkWithoutLabel?: boolean
  radio?: Input.SelectionItem
}
const radioOptions: Input.SelectionItem[] = [
  { value: '1', text: 'ひとつ' },
  { value: '2', text: 'ふたつ' },
  { value: '3', text: 'みっつ' },
  { value: '4', text: 'よっつ' },
  { value: '5', text: 'いつつ' },
]
const comboOptions: Input.SelectionItem[] = [
  { value: '1', text: 'ひとつ' },
  { value: '2', text: 'ふたつ' },
  { value: '3', text: 'みっつ' },
  { value: '4', text: 'よっつ' },
  { value: '5', text: 'いつつ' },
  { value: '6', text: 'むっつ' },
]

const booleanOptions: Input.SelectionItem[] = [
  { value: 'T', text: '○' },
  { value: 'F', text: '-' },
]

const BooleanComboBox = forwardRef<Input.CustomComponentRef<boolean>, Input.CustomComponentProps<boolean, InputHTMLAttributes<HTMLInputElement>>>((props, ref) => {
  const [selected, setSelected] = useState<Input.SelectionItem | undefined>()

  useImperativeHandle(ref, () => ({
    getValue: () => comboRef.current?.getValue() === booleanOptions[0],
    focus: () => comboRef.current?.focus(),
  }), [])

  const comboRef = useRef<Input.CustomComponentRef<Input.SelectionItem>>(null)

  return (
    <Input.ComboBox
      ref={comboRef}
      {...props}
      options={booleanOptions}
      value={selected}
      onChange={setSelected}
    />
  )
})



const Option6ComboBox = Input.defineCustomComponent<Input.SelectionItem>((props, ref) => {
  return (
    <Input.ComboBox ref={ref} {...props} options={comboOptions} />
  )
})

const createSampleData = (): TestData => ({
  num: '123',
  check: true,
  checkWithoutLabel: true,
  word: 'あいうえお',
  description: 'When a React component is instantiated the grid will make the grid APIs, \na number of utility methods as well as the cell & row values available to you via props.',
  ym: '1234-12',
  date: '1911-08-07',
  radio: radioOptions[radioOptions.length - 3],
  combo: comboOptions[comboOptions.length - 3],
})
const defaultValues: TestDataRoot = {
  gridRows: [
    createSampleData(),
    createSampleData(),
    createSampleData(),
  ],
}

export const UserInputテスト = () => {
  const formRef = useRef<HTMLFormElement>(null)
  const [readOnly, setReadOnly] = useState(false)
  const { registerEx, ...formMethdos } = Input.useFormEx<TestDataRoot>({ defaultValues })

  const onSubmit: SubmitHandler<TestDataRoot> = useCallback(e => {
    console.log(e)
  }, [])

  return (
    // darkMode用
    <AppContextProvider>

      {/* コンボボックス内でEnter押したときの挙動の制御で使用 */}
      <Input.ImeContextProvider elementRef={formRef}>

        {/* react-hook-form */}
        <FormProvider {...formMethdos}>

          <form
            ref={formRef}
            className={`text-color-12 flex flex-col items-start gap-2 bg-color-ridge p-4 min-h-[100vh]`}
            onSubmit={formMethdos.handleSubmit(onSubmit)}
          >
            <label className="flex items-center gap-2">
              <input type="checkbox" checked={readOnly} onChange={e => setReadOnly(e.target.checked)} />
              <span>readOnly</span>
            </label>
            <DarkModeToggle />
            <hr className="w-full border border-color-4 my-2" />

            <div className="w-full flex flex-wrap justify-start gap-4">

              <table className="text-left w-fit max-w-lg">
                <tbody>
                  <tr>
                    <th>Word</th>
                    <td><Input.Word className="w-32" {...registerEx(`word`)} readOnly={readOnly} /></td>
                  </tr>
                  <tr>
                    <th>Description</th>
                    <td><Input.Description {...registerEx(`description`)} readOnly={readOnly} /></td>
                  </tr>
                  <tr>
                    <th>Num</th>
                    <td><Input.Num {...registerEx(`num`)} readOnly={readOnly} /></td>
                  </tr>
                  <tr>
                    <th>Date</th>
                    <td><Input.Date {...registerEx(`date`)} readOnly={readOnly} /></td>
                  </tr>
                  <tr>
                    <th>YearMonth</th>
                    <td><Input.YearMonth {...registerEx(`ym`)} readOnly={readOnly} /></td>
                  </tr>
                  <tr>
                    <th>radio</th>
                    <td><Input.Selection {...registerEx(`radio`)} readOnly={readOnly} options={radioOptions} /></td>
                  </tr>
                  <tr>
                    <th>ComboBox</th>
                    <td><Input.Selection {...registerEx(`combo`)} readOnly={readOnly} options={comboOptions} /></td>
                  </tr>
                  <tr>
                    <th>Check</th>
                    <td><Input.CheckBox {...registerEx(`check`)} readOnly={readOnly}>ラベル</Input.CheckBox></td>
                  </tr>
                  <tr>
                    <th>Check(ラベルなし)</th>
                    <td><Input.CheckBox {...registerEx(`checkWithoutLabel`)} readOnly={readOnly} /></td>
                  </tr>
                </tbody>
              </table>

              <Input.AgGridWrapper
                name='gridRows'
                columnDefs={useMemo<ColDef<TestData>[]>(() => [
                  Input.createColDef('gridRows', 'check', BooleanComboBox),
                  Input.createColDef('gridRows', 'num', Input.Num),
                  Input.createColDef('gridRows', 'date', Input.Date),
                  Input.createColDef('gridRows', 'word', Input.Word),
                  Input.createColDef('gridRows', 'ym', Input.YearMonth),
                  Input.createColDef('gridRows', 'combo', Option6ComboBox),
                  Input.createColDef('gridRows', 'description', Input.Description),
                  // { field: 'bbb' },
                  // { field: 'date' },
                  // { field: 'ym' },
                  // { field: 'checkWithoutLabel' },
                  // { field: 'radio' },
                ], [])}
                className="w-96 h-96"
              />
            </div>
            <hr className="w-full border border-color-4 my-2" />
            <button className="bg-color-button text-color-0 px-2">submit</button>


            <Layoutのテスト />


          </form>
        </FormProvider>
      </Input.ImeContextProvider>
    </AppContextProvider >
  )
}

// --------------------------------------------------------------------------------------------


const Layoutのテスト = () => {

  const [arr, setArr] = useState([
    { id: '1111', 概要: '○○したい', 回答希望日: '' },
    { id: '2222', 概要: '××したい', 回答希望日: '' },
    { id: '3333', 概要: '▽▽したい', 回答希望日: '' },
  ])

  return <>
    <hr className="w-full border border-color-4 my-2" />
    <div className="w-full p-4 bg-color-4">
      <VForm.Root
        label="受注契約 新規登録"
        className="my-4 mx-auto p-2 max-w-[640px] bg-color-ridge"
        indentSizePx={26}
        leftColumnWidth="160px"
      >
        <VForm.Row label="取引ID">
          <input type="text" defaultValue="aaa" className="border border-1 border-color-5" />
        </VForm.Row>
        <VForm.Row label="取引時刻">
          <input type="text" defaultValue="aaa" className="border border-1 border-color-5" />
        </VForm.Row>

        <VForm.Spacer />

        <VForm.Section label="基本情報" table>
          <VForm.Row label="氏名">
            <input type="text" defaultValue="aaa" className="border border-1 border-color-5" />
          </VForm.Row>
          <VForm.Row label="生年月日">
            <input type="text" defaultValue="aaa" className="border border-1 border-color-5" />
          </VForm.Row>

          <VForm.Section label="連絡先" table>
            <VForm.Section label="自宅">
              <VForm.Row label="住所">
                <input type="text" defaultValue="aaa" className="border border-1 border-color-5" />
              </VForm.Row>
              <VForm.Row label="電話番号">
                <input type="text" defaultValue="aaa" className="border border-1 border-color-5" />
              </VForm.Row>
            </VForm.Section>
            <VForm.Section label="勤務先">
              <VForm.Row label="住所">
                <input type="text" defaultValue="aaa" className="border border-1 border-color-5" />
              </VForm.Row>
              <VForm.Row label="電話番号">
                <input type="text" defaultValue="aaa" className="border border-1 border-color-5" />
              </VForm.Row>
            </VForm.Section>
          </VForm.Section>
          <VForm.Row label="連絡先備考" fullWidth>
            <Input.AgGridWrapper
              name='gridRows'
              columnDefs={useMemo<ColDef<TestData>[]>(() => [
                Input.createColDef('gridRows', 'word', Input.Word),
              ], [])}
              className="h-32"
            />
          </VForm.Row>
        </VForm.Section>

        <VForm.Spacer />

        <VForm.Row label="過去取引履歴" fullWidth>
          <Input.AgGridWrapper
            name='gridRows'
            columnDefs={useMemo<ColDef<TestData>[]>(() => [
              Input.createColDef('gridRows', 'word', Input.Word),
            ], [])}
            className="h-32"
          />
        </VForm.Row>

        <VForm.Spacer />

        <VForm.Section label="依頼内容">
          <VForm.Row label="格納先フォルダ">
            <input type="text" defaultValue="aaa" className="border border-1 border-color-5" />
          </VForm.Row>
        </VForm.Section>

        <VForm.Spacer />

        <VForm.Row label="依頼内容" fullWidth>
          <Layout.TabGroup
            items={arr}
            keySelector={item => item.id}
            onCreate={() => setArr([...arr, { id: UUID.generate(), 概要: '', 回答希望日: '' }])}
          >
            {({ item }) => <>
              <VForm.Root leftColumnWidth="132px">
                <VForm.Section>
                  <VForm.Row label="概要" fullWidth>
                    <textarea rows={2} className="w-full border border-1 border-color-5" value={item.概要} onChange={() => { }}></textarea>
                  </VForm.Row>
                  <VForm.Row label="回答希望日">
                    <input type="text" value={item.回答希望日} onChange={() => { }} className="border border-1 border-color-5" />
                  </VForm.Row>
                  <VForm.Section label="関連依頼" table>
                    <VForm.Row fullWidth>
                      <Input.AgGridWrapper
                        name='gridRows'
                        columnDefs={[
                          Input.createColDef('gridRows', 'word', Input.Word),
                        ]}
                        className="h-32"
                      />
                    </VForm.Row>
                  </VForm.Section>
                  <VForm.Section label="関連資料" table>
                    <VForm.Row fullWidth>
                      <Input.AgGridWrapper
                        name='gridRows'
                        columnDefs={[
                          Input.createColDef('gridRows', 'word', Input.Word),
                        ]}
                        className="h-32"
                      />
                    </VForm.Row>
                    <VForm.Row label="格納先フォルダ">
                      <input type="text" defaultValue="aaa" className="border border-1 border-color-5" />
                    </VForm.Row>
                  </VForm.Section>
                </VForm.Section>
              </VForm.Root>
            </>}

          </Layout.TabGroup>
        </VForm.Row>

        <VForm.Row label="備考" fullWidth>
          <input type="text" defaultValue="aaa" className="border border-1 border-color-5" />
        </VForm.Row>
      </VForm.Root>

    </div>
  </>
}


// ----------------------------------


// ----------------------------------

const DarkModeToggle = () => {
  const [{ darkMode }, dispatch] = useAppContext()
  return (
    <label className="flex items-center gap-2">
      <input type="checkbox" checked={darkMode} onChange={e => dispatch({ type: 'toggleDark' })} />
      <span>darkMode</span>
    </label>
  )
}

