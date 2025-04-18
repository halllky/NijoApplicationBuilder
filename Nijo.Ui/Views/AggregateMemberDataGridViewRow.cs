using Nijo.SchemaParsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.Ui.Views;

/// <summary>
/// <see cref="AggregateMemberDataGridView"/> のグリッドの行1行分と対応するプレーンなオブジェクト
/// </summary>
public class AggregateMemberDataGridViewRow {

    /// <summary>
    /// 引数のXElementとその子孫を再帰的に <see cref="AggregateMemberDataGridViewRow"/> のインスタンスに変換します。
    /// </summary>
    public static IEnumerable<AggregateMemberDataGridViewRow> FromXElementRecursively(XElement el, int indent, SchemaParseContext schemaParseContext) {
        yield return new AggregateMemberDataGridViewRow {
            Indent = indent,
            PhysicalName = schemaParseContext.GetPhysicalName(el),
            Attributes = el
                .Attributes()
                .ToDictionary(attr => attr.Name.LocalName, attr => attr.Value),
        };

        foreach (var child in el.Elements()) {
            foreach (var childRow in FromXElementRecursively(child, indent + 1, schemaParseContext)) {
                yield return childRow;
            }
        }
    }

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
