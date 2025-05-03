/**
 * 安全にフィールドパスから値を取得する関数
 */
export const getValueByPath = (obj: any, path: string): any => {
  try {
    // 空のパスの場合は元のオブジェクトを返す
    if (!path) return obj;

    // パスを分割して各パートを順に処理
    const result = path.split('.').reduce((acc, part) => {
      // nullまたはundefinedの場合は早期リターン
      if (acc === null || acc === undefined) return acc;

      // プロパティアクセスを試みる
      return acc[part];
    }, obj);

    return result;
  } catch (e) {
    console.error(`Failed to get value at path ${path}`, e);
    return undefined;
  }
}
