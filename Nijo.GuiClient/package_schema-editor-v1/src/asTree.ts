export type TreeHelper<TFlatItem extends { indent: number }, TKey> = ReturnType<typeof asTree<TFlatItem, TKey>>

/**
 * インデント付きのフラット配列（EditingRootAggregate.members など）をツリー構造として扱うための汎用ユーティリティ。
 * 配列自体の並び順とインデントの大小関係だけから親子関係を再構築する。
 */
export const asTree = <TFlatItem extends { indent: number }, TKey>(flat: TFlatItem[], keySelector: (item: TFlatItem) => TKey) => {
  return {

    /** 指定された要素のルートを取得する。配列内にインデント0の要素が存在する場合のみ使用できる。 */
    getRoot: (el: TFlatItem): TFlatItem => {
      // 引数のエレメント以前の位置にある、インデント0の要素のうち直近のものがルート
      const elKey = keySelector(el)
      const previousElementsAndThis = flat.slice(0, flat.findIndex(x => keySelector(x) === elKey) + 1)
      const root = previousElementsAndThis.reverse().find(x => x.indent === 0)
      if (!root) throw new Error('root not found') // 必ずルート集約はあるはず
      return root
    },

    /** 指定された要素の親を取得する。 */
    getParent: (el: TFlatItem): TFlatItem | undefined => {
      // 引数のエレメントより前の位置にあり、
      // インデントが引数のエレメントより小さいもののうち、直近にあるのが親。
      // インデントは必ずしも1小さいとは限らない。
      const elKey = keySelector(el)
      const previousElements = flat.slice(0, flat.findIndex(x => keySelector(x) === elKey))
      const parent = previousElements.reverse().find(y => y.indent < el.indent)
      return parent
    },

    /** 指定された要素の子を取得する。 */
    getChildren: (el: TFlatItem): TFlatItem[] => {
      // 引数のエレメントより後ろの位置にあり、
      // インデントが引数のエレメントより大きいもののうち、
      // そのエレメントと引数のエレメントの間にインデントが挟まるものがないものが子。
      // 例えば以下の場合、b, d, f, gはaの子。cはbの子。eはdの子。
      // - a
      //       - b
      //         - c
      //     - d
      //       - e
      //     - f
      //   - g

      const elKey = keySelector(el)
      const elIndex = flat.findIndex(x => keySelector(x) === elKey)
      // 要素が配列内に見つからないという状況は設計上起こりえないかもしれないが、念のためチェック
      if (elIndex === -1) {
        return []
      }

      const children: TFlatItem[] = []
      const stack: TFlatItem[] = []

      // el の次の要素から走査を開始
      for (let i = elIndex + 1; i < flat.length; i++) {
        const potentialChild = flat[i]

        // 注目している要素のインデントが走査開始時点のインデント以下になった場合、
        // それはもはや現在注目している親の子ではない（兄弟か、より上位の階層の要素）。
        // それ以降の要素も子ではないため、探索を終了する。
        if (potentialChild.indent <= el.indent) {
          break
        }

        if (stack.length === 0) {
          children.push(potentialChild)
          stack.push(potentialChild)
          continue
        }

        const peek = stack[stack.length - 1]

        // elとこの要素の間に挟まるインデントの要素がある場合、この要素はelの直下の子ではない
        if (peek.indent < potentialChild.indent) {
          continue
        }

        // elとこの要素の間に挟まるインデントの要素がなく、
        // この要素のインデントがelと同じ場合、この要素はelの直下の子。
        if (peek.indent >= potentialChild.indent) {
          children.push(potentialChild)
          stack.pop()
          stack.push(potentialChild)
          continue
        }
      }
      return children
    },

    /** 指定された要素の祖先を取得する。よりルート集約に近いほうが先。 */
    getAncestors: (el: TFlatItem): TFlatItem[] => {
      // 引数のエレメントより前の方向に辿っていき、
      // インデントが現在のエレメントより小さいものを集める。
      // よりルート集約に近いほうが先なので、最後に配列を逆転させてreturnする。
      const elKey = keySelector(el)
      const previousElements = flat.slice(0, flat.findIndex(x => keySelector(x) === elKey))
      let currentIndent = el.indent
      const ancestors: TFlatItem[] = []
      for (const y of previousElements) {
        if (y.indent < currentIndent) {
          ancestors.push(y)
          currentIndent = y.indent
        }
        // ルート集約（インデント0）に到達したら探索を打ち切る
        if (y.indent === 0) {
          break
        }
      }
      return ancestors.reverse()
    },

    /** 指定された要素の子孫を取得する。 */
    getDescendants: (el: TFlatItem): TFlatItem[] => {
      // getChildrenのロジックのうち「直下の子」という条件を外したものが子孫。
      const elKey = keySelector(el)
      const belowElements = flat.slice(flat.findIndex(x => keySelector(x) === elKey) + 1)
      const descendants: TFlatItem[] = []
      for (const y of belowElements) {
        // 引数のエレメント以下のインデントの要素が登場したら探索を打ち切る
        if (y.indent <= el.indent) {
          break
        } else {
          descendants.push(y)
        }
      }
      return descendants
    },
  }
}
