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
