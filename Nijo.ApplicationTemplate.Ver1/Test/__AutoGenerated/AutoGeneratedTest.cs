using NUnit;
using NUnit.Framework;

namespace MyApp;

/// <summary>
/// データベースなどの入出力を伴うため並列実行できないテスト。
/// 具体的なテスト内容は別ファイルで記載
/// </summary>
[NonParallelizable]
public partial class DB等入出力あり {
}
