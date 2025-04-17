using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
        }

        /// <summary>
        /// モデルの詳細を表示
        /// </summary>
        public void DisplayModel(DataTable dataTable) {
            // BindingSourceを使用してDataGridViewに設定
            _bindingSource.DataSource = dataTable;

            // 列ごとの設定
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
