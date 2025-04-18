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
using Nijo.SchemaParsing;
using Nijo.CodeGenerating;
using System.Xml;

namespace Nijo.Ui.Views {

    /// <summary>
    /// ルート集約1件と対応するUIコンポーネント
    /// </summary>
    public partial class RootAggregateView : UserControl {

        public RootAggregateView() {
            InitializeComponent();

            _aggregateDetailPanel.BackColor = SystemColors.Control;
        }

        /// <summary>
        /// モデルの詳細を表示
        /// </summary>
        public void DisplayRootAggregateInfo(XElement rootAggregateElement, IModel model, SchemaParseContext schemaParseContext) {

            // 行データを作成
            var rows = AggregateMemberDataGridViewRow
                .FromXElementRecursively(rootAggregateElement, 0, schemaParseContext)
                .ToList();

            // ルート集約行は削除不可に設定
            if (rows.Count > 0) {
                rows[0].CannotDelete = true;
            }

            // コマンドモデルの場合は引数と戻り値も削除不可に設定
            if (model is CommandModel) {
                // コマンドモデルの物理名を取得
                var rootAggregateName = schemaParseContext.GetPhysicalName(rootAggregateElement);
                if (!string.IsNullOrEmpty(rootAggregateName)) {
                    // 引数型と戻り値型の物理名
                    var parameterName = $"{rootAggregateName}Parameter";
                    var returnValueName = $"{rootAggregateName}ReturnValue";

                    // 引数型と戻り値型の行を削除不可に設定
                    foreach (var row in rows) {
                        if (row.PhysicalName == parameterName || row.PhysicalName == returnValueName) {
                            row.CannotDelete = true;
                        }
                    }
                }
            }

            // AggregateMemberDataGridViewにデータを表示
            aggregateMemberDataGridView1.DisplayMembers(rows, model, schemaParseContext);
        }
    }
}
