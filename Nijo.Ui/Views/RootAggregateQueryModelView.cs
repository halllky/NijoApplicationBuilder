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

namespace Nijo.Ui.Views {

    /// <summary>
    /// ルート集約1件と対応するUIコンポーネント（QueryModel）
    /// </summary>
    public partial class RootAggregateQueryModelView : UserControl {

        public RootAggregateQueryModelView() {
            InitializeComponent();

            _aggregateDetailPanel.BackColor = SystemColors.Control;
        }

        /// <summary>
        /// モデルの詳細を表示
        /// </summary>
        public void DisplayRootAggregateInfo(XElement rootAggregateElement, SchemaParseContext schemaParseContext) {
            // AggregateMemberDataGridViewにデータを表示
            var model = schemaParseContext.Models.Values.OfType<QueryModel>().Single();
            aggregateMemberDataGridView1.DisplayMembers(rootAggregateElement, model, schemaParseContext);

            // ルート集約は削除不可に設定（デフォルト）
            // ※ DisplayMembersが自動的に設定するため不要
        }
    }
}
