export const groupBy = <TItem, TKey>(arr: TItem[], fn: (t: TItem) => TKey): Map<TKey, TItem[]> => {
  return arr.reduce((map, curr) => {
    const key = fn(curr)
    const group = map.get(key)
    if (group) {
      group.push(curr)
    } else {
      map.set(key, [curr])
    }
    return map
  }, new Map<TKey, TItem[]>())
}

export const crossJoin = <T1, T2, TKey>(
  left: T1[], getKeyLeft: (t: T1) => TKey,
  right: T2[], getKeyRight: (t: T2) => TKey
): CrossJoinResult<T1, T2, TKey>[] => {

  const sortedLeft = [...left]
  sortedLeft.sort((a, b) => {
    const keyA = getKeyLeft(a)
    const keyB = getKeyLeft(b)
    if (keyA < keyB) return -1
    if (keyA > keyB) return 1
    return 0
  })
  const sortedRight = [...right]
  sortedRight.sort((a, b) => {
    const keyA = getKeyRight(a)
    const keyB = getKeyRight(b)
    if (keyA < keyB) return -1
    if (keyA > keyB) return 1
    return 0
  })
  const result: CrossJoinResult<T1, T2, TKey>[] = []
  let cursorLeft = 0
  let cursorRight = 0
  while (true) {
    const left = sortedLeft[cursorLeft]
    const right = sortedRight[cursorRight]
    if (left === undefined && right === undefined) {
      break
    }
    if (left === undefined && right !== undefined) {
      result.push({ key: getKeyRight(right), right })
      cursorRight++
      continue
    }
    if (left !== undefined && right === undefined) {
      result.push({ key: getKeyLeft(left), left })
      cursorLeft++
      continue
    }
    const keyLeft = getKeyLeft(left)
    const keyRight = getKeyRight(right)
    if (keyLeft === keyRight) {
      result.push({ key: keyLeft, left, right })
      cursorLeft++
      cursorRight++
    } else if (keyLeft < keyRight) {
      result.push({ key: keyLeft, left })
      cursorLeft++
    } else if (keyLeft > keyRight) {
      result.push({ key: keyRight, right })
      cursorRight++
    }
  }
  return result
}
type CrossJoinResult<T1, T2, TKey>
  = { key: TKey, left: T1, right: T2 }
  | { key: TKey, left: T1, right?: never }
  | { key: TKey, left?: never, right: T2 }
