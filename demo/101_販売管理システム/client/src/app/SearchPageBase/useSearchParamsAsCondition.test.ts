import { describe, it, expect } from 'vitest'
import { assignDeep, removeEmptyValues, isEmptyObject, toQueryParamRecord, fromQueryParamRecord } from './useSearchParamsAsCondition'

describe('SearchPageBase で用いているURLと検索条件オブジェクトの相互変換処理のテスト', () => {
  describe('assignDeep', () => {
    it('プロパティがマージされることの確認', () => {
      const target = { a: 1, b: { c: 2 } }
      const source = { b: { d: 3 }, e: 4 }
      assignDeep(source, target)
      expect(target).toEqual({ a: 1, b: { c: 2, d: 3 }, e: 4 })
    })

    it('プリミティブ値が上書きされることの確認', () => {
      const target = { a: 1 }
      const source = { a: 2 }
      assignDeep(source, target)
      expect(target).toEqual({ a: 2 })
    })

    it('ターゲットにネストされたオブジェクトが存在しない場合の処理の確認', () => {
      const target = { a: 1 }
      const source = { b: { c: 2 } }
      assignDeep(source, target)
      expect(target).toEqual({ a: 1, b: { c: 2 } })
    })
  })

  describe('removeEmptyValues', () => {
    it('空文字列、null、undefined、falseを削除することの確認', () => {
      const obj = { a: '', b: null, c: undefined, d: false, e: 0, f: true, g: 'hello' }
      removeEmptyValues(obj)
      expect(obj).toEqual({ e: 0, f: true, g: 'hello' })
    })

    it('ネストされた空のオブジェクトを削除することの確認', () => {
      const obj = { a: { b: '' }, c: { d: 'hello' } }
      removeEmptyValues(obj)
      expect(obj).toEqual({ c: { d: 'hello' } })
    })

    it('ネストされた空のオブジェクトを削除することの確認', () => {
      const obj = { a: { b: { c: '' } } }
      removeEmptyValues(obj)
      expect(obj).toEqual({})
    })
  })

  describe('isEmptyObject', () => {
    it('空オブジェクト', () => {
      expect(isEmptyObject({})).toBe(true)
    })

    it('空なプリミティブ値のみのオブジェクト', () => {
      expect(isEmptyObject({ a: '', b: null, c: undefined, d: false })).toBe(true)
    })

    it('ネストされた空のオブジェクトを含むオブジェクトの確認', () => {
      expect(isEmptyObject({ a: { b: '' } })).toBe(true)
    })

    it('非空の値を持つオブジェクトに対してfalseを返すことの確認', () => {
      expect(isEmptyObject({ a: 0 })).toBe(false)
      expect(isEmptyObject({ a: true })).toBe(false)
      expect(isEmptyObject({ a: 'hello' })).toBe(false)
    })
  })

  describe('toQueryParamRecord', () => {
    it('ネストされたオブジェクトをフラット化することの確認', () => {
      const obj = { a: { b: 1, c: 2 }, d: 3 }
      const result = toQueryParamRecord(obj)
      expect(result).toEqual({ 'a.b': '1', 'a.c': '2', 'd': '3' })
    })

    it('空文字、nullおよびundefinedを無視することの確認', () => {
      const obj = { a: null, b: undefined, c: 1, d: '', e: 'hello' }
      const result = toQueryParamRecord(obj)
      expect(result).toEqual({ c: '1', e: 'hello' })
    })
  })

  describe('fromQueryParamRecord', () => {
    it('フラットなエントリからネストされたオブジェクトを再構築することの確認', () => {
      const params: [string, string][] = [
        ['a.b', '1'],
        ['a.c', '2'],
        ['d', '3']
      ]
      const result = fromQueryParamRecord(params)
      expect(result).toEqual({ a: { b: '1', c: '2' }, d: '3' })
    })

    it('単一レベルのオブジェクトを処理できることの確認', () => {
      const params: [string, string][] = [
        ['a', '1'],
        ['b', '2']
      ]
      const result = fromQueryParamRecord(params)
      expect(result).toEqual({ a: '1', b: '2' })
    })
  })
})
