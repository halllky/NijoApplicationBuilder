import React, { useState, useCallback } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { FieldValues, SubmitHandler, useForm, FormProvider } from 'react-hook-form';
import { useQuery } from 'react-query';
import { AgGridReact } from 'ag-grid-react';
import { ColDef } from 'ag-grid-community';
import { BookmarkIcon, ChevronDownIcon, ChevronUpIcon, MagnifyingGlassIcon, PlusIcon } from '@heroicons/react/24/outline';
import { IconButton } from './IconButton';
import { InputForms } from './InputForms';
import { useCtrlS } from '../hooks/useCtrlS';
import { useAppContext } from '../hooks/AppContext';
import { useHttpRequest } from '../hooks/useHttpRequest';

export default function () {

  const [, dispatch] = useAppContext()
  useCtrlS(() => {
    dispatch({ type: 'pushMsg', msg: '保存しました。' })
  })

  const { get } = useHttpRequest()
  const [param, setParam] = useState<SearchCondition>({})

  const reactHookFormMethods = useForm<SearchCondition>()
  const register = reactHookFormMethods.register
  const handleSubmit = reactHookFormMethods.handleSubmit
  const reset = reactHookFormMethods.reset

  const onSearch: SubmitHandler<SearchCondition> = useCallback(data => {
    setParam(data)
  }, [])
  const onClear = useCallback((e: React.MouseEvent) => {
    reset()
    e.preventDefault()
  }, [reset])
  const { data, isFetching } = useQuery({
    queryKey: ['9ad02a44902e693b9bad8f2529db056b', JSON.stringify(param)],
    queryFn: async () => {
      const response = await get<RowType[]>(`/api/BackgrorndTask/list`, { param })
      return response.ok ? response.data : []
    },
    onError: error => {
      dispatch({ type: 'pushMsg', msg: `ERROR!: ${JSON.stringify(error)}` })
    },
  })

  const [expanded, setExpanded] = useState(false)

  if (isFetching) return <></>

  return (
    <div className="page-content-root">

      <div className="flex flex-row justify-start items-center space-x-2">
        <div className='flex-1 flex flex-row items-center space-x-1 cursor-pointer' onClick={() => setExpanded(!expanded)}>
          <h1 className="text-base font-semibold select-none py-1">
            バッチ処理
          </h1>
          {expanded
            ? <ChevronDownIcon className="w-4" />
            : <ChevronUpIcon className="w-4" />}
        </div>
      </div>

      <FormProvider {...reactHookFormMethods}>
        <form className={`${expanded ? '' : 'hidden'} flex flex-col space-y-1 p-1 bg-neutral-200`} onSubmit={handleSubmit(onSearch)}>
          <div className="flex">
            <div className="basis-24">
              <span className="text-sm select-none opacity-80">
                ID
              </span>
            </div>
            <div className="flex-1">
              <InputForms.Word {...register('id')} />
            </div>
          </div>
          <div className="flex">
            <div className="basis-24">
              <span className="text-sm select-none opacity-80">
                名称
              </span>
            </div>
            <div className="flex-1">
              <InputForms.Word {...register('name')} />
            </div>
          </div>
          <div className="flex">
            <div className="basis-24">
              <span className="text-sm select-none opacity-80">
                種別
              </span>
            </div>
            <div className="flex-1">
              <label>
                <input type="checkbox" {...register('stateIsWaitForStart')} />
                起動待ち
              </label>
              <label>
                <input type="checkbox" {...register('stateIsRunning')} />
                実行中
              </label>
              <label>
                <input type="checkbox" {...register('stateIsSuccess')} />
                正常終了
              </label>
              <label>
                <input type="checkbox" {...register('stateIsFault')} />
                異常終了
              </label>
            </div>
          </div>
          <div className="flex">
            <div className="basis-24">
              <span className="text-sm select-none opacity-80">
                予約時刻
              </span>
            </div>
            <div className="flex-1">
              <InputForms.Word {...register('requestTime')} />
            </div>
          </div>
          <div className="flex">
            <div className="basis-24">
              <span className="text-sm select-none opacity-80">
                開始時刻
              </span>
            </div>
            <div className="flex-1">
              <InputForms.Word {...register('startTime')} />
            </div>
          </div>
          <div className="flex">
            <div className="basis-24">
              <span className="text-sm select-none opacity-80">
                終了時刻
              </span>
            </div>
            <div className="flex-1">
              <InputForms.Word {...register('finishTime')} />
            </div>
          </div>
          <div className='flex flex-row justify-start space-x-1'>
            <IconButton fill icon={MagnifyingGlassIcon}>検索</IconButton>
            <IconButton outline onClick={onClear}>クリア</IconButton>
            <div className='flex-1'></div>
          </div>
        </form>
      </FormProvider>

      <div className="ag-theme-alpine compact flex-1">
        <AgGridReact
          rowData={data || []}
          columnDefs={columnDefs}
          multiSortKey='ctrl'
          undoRedoCellEditing
          undoRedoCellEditingLimit={20}>
        </AgGridReact>
      </div>
    </div>
  )
}

type SearchCondition = {
  id?: string
  name?: string
  batchType?: string
  stateIsWaitForStart?: boolean
  stateIsRunning?: boolean
  stateIsSuccess?: boolean
  stateIsFault?: boolean
  requestTime?: string
  startTime?: string
  finishTime?: string
}
type RowType = {
  id: string
  name: string
  batchType: string
  parameterJson: string
  state: '起動待ち' | '実行中' | '正常終了' | '異常終了'
  requestTime: string
  startTime?: string
  finishTime?: string
}

const columnDefs: ColDef<RowType>[] = [
  { field: 'id', headerName: 'ID', resizable: true, sortable: true, editable: false },
  { field: 'batchType', headerName: '種別', resizable: true, sortable: true, editable: false },
  { field: 'name', headerName: '名称', resizable: true, sortable: true, editable: false },
  {
    resizable: true,
    headerName: 'パラメータ',
    cellRenderer: ({ data }: { data: RowType }) => {
      return data.parameterJson
        ? <IconButton className="text-blue-400">詳細</IconButton>
        : <></>
    },
  },
  { field: 'state', headerName: '状態', resizable: true, sortable: true, editable: false },
  { field: 'requestTime', headerName: '予約時刻', resizable: true, sortable: true, editable: false },
  { field: 'startTime', headerName: '開始時刻', resizable: true, sortable: true, editable: false },
  { field: 'finishTime', headerName: '完了時刻', resizable: true, sortable: true, editable: false },
]

