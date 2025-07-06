import { renderHook, act } from '@testing-library/react'
import { describe, it, expect } from 'vitest'
import { useToggle } from '../useToggle'

describe('useToggle', () => {
    it('初期値がfalseで初期化される', () => {
        const { result } = renderHook(() => useToggle())

        expect(result.current.opened).toBe(false)
    })

    it('初期値を指定して初期化される', () => {
        const { result } = renderHook(() => useToggle(true))

        expect(result.current.opened).toBe(true)
    })

    it('toggleで値が切り替わる', () => {
        const { result } = renderHook(() => useToggle(false))

        act(() => {
            result.current.toggle()
        })

        expect(result.current.opened).toBe(true)
    })

    it('setOpenedで値を直接設定できる', () => {
        const { result } = renderHook(() => useToggle(false))

        act(() => {
            result.current.setOpened(true)
        })

        expect(result.current.opened).toBe(true)
    })
})
