using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ui;

/// <summary>
/// 実行時設定
/// </summary>
public class NIjoUiConfigJson {
    /// <summary>
    /// 最近開いたディレクトリ
    /// </summary>
    public List<string> RecentDirectories { get; set; } = new();
}
