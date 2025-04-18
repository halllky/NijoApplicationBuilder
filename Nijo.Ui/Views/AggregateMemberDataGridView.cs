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
using System.Windows.Forms;
using System.Xml.Linq;

namespace Nijo.Ui.Views {
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
        }

        /// <summary>
        /// セルの書式設定イベントハンドラ
        /// </summary>
        private void DataGridView1_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e) {
            // PhysicalName列のみ処理
            if (dataGridView1.Columns[e.ColumnIndex].Name == "PhysicalName" && e.RowIndex >= 0) {
                var row = dataGridView1.Rows[e.RowIndex].DataBoundItem as AggregateMemberDataGridViewRow;
                if (row != null && row.Indent.HasValue) {
                    // インデントに基づいてパディングを設定
                    int indentSize = row.Indent.Value * 20; // インデント1つあたり20ピクセル
                    e.CellStyle.Padding = new Padding(indentSize, 0, 0, 0);
                }
            }
        }

        /// <summary>
        /// 「行挿入」ボタンのクリックイベントハンドラ
        /// </summary>
        private void insertRowButton_Click(object sender, EventArgs e) {
            InsertRowsAtSelection();
        }

        /// <summary>
        /// 「下挿入」ボタンのクリックイベントハンドラ
        /// </summary>
        private void insertBelowButton_Click(object sender, EventArgs e) {
            InsertRowsBelowSelection();
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
                    Attributes = new Dictionary<string, string>()
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
        public void DisplayMembers(XElement rootAggregateElement, IModel model, SchemaParseContext schemaParseContext) {

            // BindingSourceを使用してDataGridViewに設定
            var list = AggregateMemberDataGridViewRow
                .FromXElementRecursively(rootAggregateElement, 0, schemaParseContext)
                .ToList();
            _bindingSource.DataSource = list;

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
        }

        /// <summary>
        /// DataGridViewのプロパティ
        /// </summary>
        public DataGridView DataGridView => dataGridView1;
    }
}
