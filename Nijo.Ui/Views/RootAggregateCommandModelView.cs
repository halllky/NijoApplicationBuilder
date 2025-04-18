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
using Nijo.Models;
using Nijo.Models.CommandModelModules;
using Nijo.SchemaParsing;

namespace Nijo.Ui.Views {

    /// <summary>
    /// ルート集約1件と対応するUIコンポーネント（CommandModel）
    /// </summary>
    public partial class RootAggregateCommandModelView : UserControl {

        public RootAggregateCommandModelView() {
            InitializeComponent();

            _aggregateDetailPanel.BackColor = SystemColors.Control;
        }

        /// <summary>
        /// モデルの詳細を表示
        /// </summary>
        public void DisplayRootAggregateInfo(XElement rootAggregateElement, SchemaParseContext schemaParseContext) {
            // AggregateMemberDataGridViewにデータを表示
            var model = schemaParseContext.Models.Values.OfType<CommandModel>().Single();
            aggregateMemberDataGridView1.DisplayMembers(rootAggregateElement, model, schemaParseContext);

            // 引数型と戻り値型のハイライト設定
            MarkParameterAndReturnValueAsCannotDelete(rootAggregateElement, schemaParseContext);
        }

        /// <summary>
        /// 引数型と戻り値型の行を削除不可に設定
        /// </summary>
        private void MarkParameterAndReturnValueAsCannotDelete(XElement rootAggregateElement, SchemaParseContext schemaParseContext) {
            // コマンドモデルの物理名を取得
            var rootAggregateName = schemaParseContext.GetPhysicalName(rootAggregateElement);
            if (string.IsNullOrEmpty(rootAggregateName)) return;

            // 引数型と戻り値型の物理名
            var parameterName = $"{rootAggregateName}Parameter";
            var returnValueName = $"{rootAggregateName}ReturnValue";

            // 各行をチェックして引数型と戻り値型の行を削除不可に設定
            var list = aggregateMemberDataGridView1.DataGridView.DataSource as BindingSource;
            if (list == null) return;

            var rows = list.DataSource as List<AggregateMemberDataGridViewRow>;
            if (rows == null) return;

            foreach (var row in rows) {
                if (row.PhysicalName == parameterName || row.PhysicalName == returnValueName) {
                    row.CannotDelete = true;
                }
            }

            // グリッドを更新
            list.ResetBindings(false);
        }
    }
}
