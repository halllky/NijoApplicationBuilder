using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ui.Views;

/// <summary>
/// <see cref="AggregateMemberDataGridView"/> のグリッドの行1行分と対応するプレーンなオブジェクト
/// </summary>
public class AggregateMemberDataGridViewModel {
    /// <summary>
    /// XML要素の深さ
    /// </summary>
    public int? Indent { get; set; }
    /// <summary>
    /// XML要素の名前
    /// </summary>
    public string? PhysicalName { get; set; }
    /// <summary>
    /// XML要素の属性と値。
    /// キーは <see cref="SchemaParsing.NodeOption.AttributeName"/> と対応する。
    /// 値はXML要素の属性の値と対応する。
    /// なお1つのXML要素に同じ名前の属性が複数定義されることは無い前提。
    /// </summary>
    public Dictionary<string, string> Attributes { get; set; } = [];
}
