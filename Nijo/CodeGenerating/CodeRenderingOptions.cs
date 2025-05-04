using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.CodeGenerating;

/// <summary>
/// コード自動生成のコマンド実行時に指定できるオプション。
/// </summary>
public class CodeRenderingOptions {
    /// <summary>
    /// QueryModelのデータ構造定義など、必ず実装しなければならないメソッドは通常abstractでレンダリングされるが、
    /// コンパイルエラーの確認などのためにあえてvirtualでレンダリングするなど、未実装を許す設定。
    /// </summary>
    public required bool AllowNotImplemented { get; init; }
}
