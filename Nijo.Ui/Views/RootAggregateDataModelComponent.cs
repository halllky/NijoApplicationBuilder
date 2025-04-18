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
    /// ルート集約1件と対応するUIコンポーネント（DataModel）
    /// </summary>
    public partial class RootAggregateDataModelComponent : UserControl {

        public RootAggregateDataModelComponent() {
            InitializeComponent();
        }

        /// <summary>
        /// モデルの詳細を表示
        /// </summary>
        public void DisplayRootAggregateInfo(XElement rootAggregateElement, SchemaParseContext schemaParseContext) {
            // AggregateMemberDataGridViewにデータを表示
            var model = schemaParseContext.Models.Values.OfType<DataModel>().Single();
            aggregateMemberDataGridView1.DisplayMembers(rootAggregateElement, model, schemaParseContext);

            _aggregateDetailLabel.Text = schemaParseContext.GetDisplayName(rootAggregateElement);
        }
    }
}
