using Nijo.CodeGenerating;
using Nijo.Models;
using Nijo.SchemaParsing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.Ui.Views;

/// <summary>
/// 集約のメンバーを表示・編集するグリッドのUI。
/// </summary>
public partial class AggregateMemberDataGridView : UserControl {
    /// <summary>
    /// データグリッド用のBindingSource
    /// </summary>
    private readonly BindingSource _bindingSource = new BindingSource();

    public AggregateMemberDataGridView() {
        InitializeComponent();

        // DataGridViewの初期設定
        dataGridView1.DataSource = _bindingSource;
        dataGridView1.AutoGenerateColumns = false;
        dataGridView1.AllowUserToResizeRows = false;
        dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        dataGridView1.CellFormatting += DataGridView1_CellFormatting;
        // データバインディング完了時のイベントを追加
        dataGridView1.DataBindingComplete += DataGridView1_DataBindingComplete;
        // 編集開始時のイベントを追加
        dataGridView1.CellBeginEdit += DataGridView1_CellBeginEdit;

        // コンボボックスのデータソースに無い値が指定されていたとしても例外を出さない
        dataGridView1.DataError += DataGridView1_DataError;
    }

    /// <summary>
    /// セル編集開始時のイベントハンドラ
    /// </summary>
    private void DataGridView1_CellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e) {
        // Type列のみ処理
        if (dataGridView1.Columns[e.ColumnIndex].Name == "Type" && e.RowIndex >= 0) {
            var list = _bindingSource.DataSource as List<AggregateMemberDataGridViewRow>;
            if (list != null && e.RowIndex < list.Count) {
                var row = list[e.RowIndex];
                // 削除不可の行は種類の変更を禁止
                if (row.CannotDelete) {
                    e.Cancel = true;
                }
            }
        }
    }

    /// <summary>
    /// セルの書式設定イベントハンドラ
    /// </summary>
    private void DataGridView1_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e) {
        if (e.RowIndex >= 0) {
            var list = _bindingSource.DataSource as List<AggregateMemberDataGridViewRow>;
            if (list != null && e.RowIndex < list.Count) {
                var row = list[e.RowIndex];

                // PhysicalName列の処理
                if (dataGridView1.Columns[e.ColumnIndex].Name == "PhysicalName" && row.Indent.HasValue) {
                    // インデントに基づいてパディングを設定
                    int indentSize = row.Indent.Value * 20; // インデント1つあたり20ピクセル
                    e.CellStyle.Padding = new Padding(indentSize, 0, 0, 0);
                }
            }
        }
    }

    /// <summary>
    /// データバインディング完了時のイベントハンドラ
    /// </summary>
    private void DataGridView1_DataBindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e) {
        // CannotDeleteプロパティに基づいてスタイルを適用
        var list = _bindingSource.DataSource as List<AggregateMemberDataGridViewRow>;
        if (list != null) {
            for (int i = 0; i < dataGridView1.Rows.Count && i < list.Count; i++) {
                if (list[i].CannotDelete) {
                    // 行全体のスタイルを適用
                    dataGridView1.Rows[i].DefaultCellStyle.BackColor = Color.LightGray;
                    dataGridView1.Rows[i].DefaultCellStyle.Font = new Font(dataGridView1.Font, FontStyle.Bold);

                    // Type列を読み取り専用に設定
                    int typeColumnIndex = dataGridView1.Columns["Type"].Index;
                    dataGridView1.Rows[i].Cells[typeColumnIndex].ReadOnly = true;
                }
            }
        }
    }

    private void DataGridView1_DataError(object? sender, DataGridViewDataErrorEventArgs e) {
        // コンボボックスの値エラーを無視
        e.ThrowException = false;
    }

    /// <summary>
    /// 「行挿入」ボタンのクリックイベントハンドラ
    /// </summary>
    private void InsertRowButton_Click(object sender, EventArgs e) {
        InsertRowsAtSelection();
    }

    /// <summary>
    /// 「下挿入」ボタンのクリックイベントハンドラ
    /// </summary>
    private void InsertBelowButton_Click(object sender, EventArgs e) {
        InsertRowsBelowSelection();
    }

    /// <summary>
    /// 「インデントを下げる」ボタンのクリックイベントハンドラ
    /// </summary>
    private void DecreaseIndentButton_Click(object sender, EventArgs e) {
        ChangeIndentForSelectedRows(-1);
    }

    /// <summary>
    /// 「インデントを上げる」ボタンのクリックイベントハンドラ
    /// </summary>
    private void IncreaseIndentButton_Click(object sender, EventArgs e) {
        ChangeIndentForSelectedRows(1);
    }

    /// <summary>
    /// 選択された行のインデントを変更する
    /// </summary>
    /// <param name="delta">インデント変更量</param>
    private void ChangeIndentForSelectedRows(int delta) {
        if (dataGridView1.SelectedRows.Count == 0 && dataGridView1.SelectedCells.Count == 0) {
            return;
        }

        var rows = GetSelectedRows();
        if (rows.Count == 0) {
            return;
        }

        var list = _bindingSource.DataSource as List<AggregateMemberDataGridViewRow>;
        if (list == null) {
            return;
        }

        // 選択された各行のインデントを変更
        foreach (var row in rows) {
            if (row.Index < list.Count) {
                var item = list[row.Index];
                // インデントは0以上の値を維持する
                item.Indent = Math.Max(0, (item.Indent ?? 0) + delta);
            }
        }

        // DataGridViewを更新
        _bindingSource.ResetBindings(false);
    }

    /// <summary>
    /// 選択した行の位置に新しい行を挿入する
    /// </summary>
    private void InsertRowsAtSelection() {
        if (dataGridView1.SelectedRows.Count == 0 && dataGridView1.SelectedCells.Count == 0) {
            return;
        }

        var rows = GetSelectedRows();
        if (rows.Count == 0) {
            return;
        }

        // 選択された最初の行のインデックスを取得
        int insertIndex = rows.Min(r => r.Index);

        // 選択された行の数だけ新しい行を追加
        InsertNewRows(insertIndex, rows.Count);
    }

    /// <summary>
    /// 選択した行の下に新しい行を挿入する
    /// </summary>
    private void InsertRowsBelowSelection() {
        if (dataGridView1.SelectedRows.Count == 0 && dataGridView1.SelectedCells.Count == 0) {
            return;
        }

        var rows = GetSelectedRows();
        if (rows.Count == 0) {
            return;
        }

        // 選択された最後の行のインデックス+1の位置に挿入
        int insertIndex = rows.Max(r => r.Index) + 1;

        // 選択された行の数だけ新しい行を追加
        InsertNewRows(insertIndex, rows.Count);
    }

    /// <summary>
    /// 新しい行を指定された位置に挿入する
    /// </summary>
    /// <param name="insertIndex">挿入位置のインデックス</param>
    /// <param name="count">挿入する行数</param>
    private void InsertNewRows(int insertIndex, int count) {
        var list = _bindingSource.DataSource as List<AggregateMemberDataGridViewRow>;
        if (list == null) {
            return;
        }

        // 挿入する行のインデントを決定（前の行があればそのインデントを継承）
        int indent = 0;
        if (insertIndex > 0 && insertIndex <= list.Count) {
            indent = list[insertIndex - 1].Indent ?? 0;
        }

        // 新しい行を作成して挿入
        for (int i = 0; i < count; i++) {
            var newRow = new AggregateMemberDataGridViewRow {
                Indent = indent,
                PhysicalName = "",
                Attributes = new Dictionary<string, string>(),
                CannotDelete = false
            };
            list.Insert(insertIndex + i, newRow);
        }

        // DataGridViewを更新
        _bindingSource.ResetBindings(false);
    }

    /// <summary>
    /// 選択されている行のリストを取得する
    /// </summary>
    private List<DataGridViewRow> GetSelectedRows() {
        var rows = new List<DataGridViewRow>();

        if (dataGridView1.SelectedRows.Count > 0) {
            // 行選択モードの場合
            foreach (DataGridViewRow row in dataGridView1.SelectedRows) {
                rows.Add(row);
            }
        } else if (dataGridView1.SelectedCells.Count > 0) {
            // セル選択モードの場合、選択されたセルの行を抽出（重複なし）
            foreach (DataGridViewCell cell in dataGridView1.SelectedCells) {
                if (!rows.Any(r => r.Index == cell.RowIndex)) {
                    rows.Add(dataGridView1.Rows[cell.RowIndex]);
                }
            }
        }

        return rows;
    }

    /// <summary>
    /// モデルの詳細を表示
    /// </summary>
    public void DisplayMembers(List<AggregateMemberDataGridViewRow> rows, IModel model, SchemaParseContext schemaParseContext) {
        // BindingSourceを使用してDataGridViewに設定
        _bindingSource.DataSource = rows;

        // 列設定
        var options = schemaParseContext.GetAvailableOptionsFor(model);

        // optionsを使用して dataGridView1.Columns に列定義を追加する
        // 既存の列をクリア
        dataGridView1.Columns.Clear();

        // 物理名の列を追加
        var physicalNameColumn = new DataGridViewTextBoxColumn {
            Name = "PhysicalName",
            DataPropertyName = "PhysicalName",
            HeaderText = "物理名",
            Width = 200,
            Frozen = true,
        };
        dataGridView1.Columns.Add(physicalNameColumn);

        // 種類の列を追加（コンボボックスとして）
        var typeColumn = new DataGridViewComboBoxColumn {
            Name = "Type",
            DataPropertyName = "Type",
            HeaderText = "種類",
            Width = 150,
            ValueMember = "Key",
            DisplayMember = "Value",
            FlatStyle = FlatStyle.Flat, // フラットスタイルに設定して視覚効果を強化
        };
        typeColumn.Items.AddRange(EnumerateTypeComboSource(model, schemaParseContext)
            .Select(kvp => new { kvp.Key, kvp.Value })
            .ToArray());

        dataGridView1.Columns.Add(typeColumn);

        // オプション属性を使って列を追加
        foreach (var option in options) {
            DataGridViewColumn column;

            // 型に応じて列の設定を変更
            if (option.Type == E_NodeOptionType.Boolean) {
                column = new DataGridViewCheckBoxColumn {
                    Name = option.AttributeName,
                    DataPropertyName = $"Attributes[{option.AttributeName}]",
                    HeaderText = option.DisplayName,
                    ToolTipText = option.HelpText,
                    Width = 80
                };
            } else {
                column = new DataGridViewTextBoxColumn {
                    Name = option.AttributeName,
                    DataPropertyName = $"Attributes[{option.AttributeName}]",
                    HeaderText = option.DisplayName,
                    Width = 120,
                    ToolTipText = option.HelpText
                };
            }

            dataGridView1.Columns.Add(column);
        }

        foreach (DataGridViewColumn column in dataGridView1.Columns) {
            // 列のソートを無効化
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            // レンダリングのパフォーマンス改善
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet;
        }

        // 先頭行の外観を変更するため、DataGridViewの更新を強制
        dataGridView1.Refresh();
    }

    /// <summary>
    /// 種類コンボボックスのデータソースを列挙する
    /// </summary>
    /// <param name="schemaParseContext"></param>
    /// <returns></returns>
    private IEnumerable<KeyValuePair<string, string>> EnumerateTypeComboSource(IModel gridOwnerModel, SchemaParseContext schemaParseContext) {
        // 値型の種類
        var vmTypes = schemaParseContext.GetValueMemberTypes();
        foreach (var vmType in vmTypes) {
            yield return KeyValuePair.Create(vmType.SchemaTypeName, vmType.TypePhysicalName);
        }

        // Child, Children
        yield return KeyValuePair.Create(SchemaParseContext.NODE_TYPE_CHILD, "Child");
        yield return KeyValuePair.Create(SchemaParseContext.NODE_TYPE_CHILDREN, "Children");

        // ref-to
        var refToAvailableAggregates = schemaParseContext.EnumerateModelElements(gridOwnerModel.SchemaName);
        foreach (var refToAggregate in refToAvailableAggregates) {
            var type = $"{SchemaParseContext.ATTR_NODE_TYPE}:{schemaParseContext.GetPhysicalName(refToAggregate)}";
            var displayName = $"ref-to:{schemaParseContext.GetDisplayName(refToAggregate)}";
            yield return KeyValuePair.Create(type, displayName);
        }
    }
    /// <summary>
    /// DataGridViewのプロパティ
    /// </summary>
    public DataGridView DataGridView => dataGridView1;
}


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
            Type = el.Attribute(SchemaParseContext.ATTR_NODE_TYPE)?.Value,
            Attributes = el
                .Attributes()
                .ToDictionary(attr => attr.Name.LocalName, attr => attr.Value),
            CannotDelete = false,
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
    /// XML要素の種類
    /// </summary>
    public string? Type { get; set; }
    /// <summary>
    /// XML要素の属性と値。
    /// キーは <see cref="SchemaParsing.NodeOption.AttributeName"/> と対応する。
    /// 値はXML要素の属性の値と対応する。
    /// なお1つのXML要素に同じ名前の属性が複数定義されることは無い前提。
    /// </summary>
    public Dictionary<string, string> Attributes { get; set; } = [];
    /// <summary>
    /// この行が削除不可かどうか。削除不可の場合、表示時に太字・灰色で表示される。
    /// </summary>
    public bool CannotDelete { get; set; }
}
