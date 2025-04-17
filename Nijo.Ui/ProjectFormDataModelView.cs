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
using Nijo.SchemaParsing;

namespace Nijo.Ui {
    public partial class ProjectFormDataModelView : UserControl {
        /// <summary>
        /// データグリッド用のBindingSource
        /// </summary>
        private readonly BindingSource _bindingSource = new BindingSource();

        public ProjectFormDataModelView() {
            InitializeComponent();

            // DataGridViewの初期設定
            _aggregateDetailView.DataSource = _bindingSource;
        }

        /// <summary>
        /// データモデルの詳細を表示
        /// </summary>
        public void DisplayDataModelDetail(XElement element, SchemaParseContext schemaContext) {
            var dataTable = new DataTable(element.Name.LocalName);

            // 基本列の定義
            dataTable.Columns.Add("項目定義", typeof(string));
            dataTable.Columns.Add("種類", typeof(string));
            dataTable.Columns.Add("物理名", typeof(string));

            // SchemaParseRuleからNodeOptionsを取得し、データモデルに適用可能な属性のみをフィルタリング
            // "data-model"に対応するモデルを取得
            var dataModelType = schemaContext.Models
                .FirstOrDefault(m => m.Value.SchemaName == "data-model");

            var availableOptions = schemaContext
                .GetOptions(element)
                .Where(opt => opt.IsAvailableModel == null ||
                       (dataModelType.Value != null && opt.IsAvailableModel(dataModelType.Value)))
                .ToArray();

            // 動的にNodeOptionsの属性に対応する列を追加
            var optionColumns = new Dictionary<string, string>();
            foreach (var option in availableOptions) {
                // DisplayNameを列名として使用
                dataTable.Columns.Add(option.DisplayName, typeof(string));
                optionColumns.Add(option.AttributeName, option.DisplayName);
            }

            // 追加の列（NodeOptionsに含まれない特別な列）
            if (!dataTable.Columns.Contains("添付可能な拡張子")) {
                dataTable.Columns.Add("添付可能な拡張子", typeof(string));
            }

            // モデル自身の行を追加
            var modelRow = dataTable.NewRow();
            modelRow["項目定義"] = element.Name.LocalName;
            modelRow["種類"] = "DataModel";
            modelRow["物理名"] = "-";

            // 動的に属性値を設定
            foreach (var attr in element.Attributes()) {
                if (optionColumns.TryGetValue(attr.Name.LocalName, out var columnName)) {
                    if (attr.Value.ToLower() == "true" || attr.Value.ToLower() == "false") {
                        modelRow[columnName] = GetBoolAttributeValue(element, attr.Name.LocalName);
                    } else {
                        modelRow[columnName] = attr.Value;
                    }
                }
            }

            dataTable.Rows.Add(modelRow);

            // 各メンバーの行を追加
            foreach (var member in element.Elements()) {
                AddMemberRow(dataTable, member, "", optionColumns);

                // Childrenタイプの場合は子要素も追加
                if (member.Attribute("Type")?.Value == "children" || member.Attribute("Type")?.Value == "child") {
                    foreach (var childMember in member.Elements()) {
                        AddMemberRow(dataTable, childMember, "    ", optionColumns);
                    }
                }
            }

            // BindingSourceを使用してDataGridViewに設定
            _bindingSource.DataSource = dataTable;

            // 列ごとの設定
            foreach (DataGridViewColumn column in _aggregateDetailView.Columns) {
                // 列のソートを無効化
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
                // レンダリングのパフォーマンス改善
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet;
            }

            _aggregateDetailLabel.Text = $"{element.Name.LocalName} (データモデル)";
        }

        /// <summary>
        /// データテーブルにメンバー行を追加
        /// </summary>
        private void AddMemberRow(DataTable dataTable, XElement member, string indent, Dictionary<string, string> optionColumns) {
            var row = dataTable.NewRow();
            row["項目定義"] = indent + member.Name.LocalName;
            row["種類"] = member.Attribute("Type")?.Value ?? "-";
            row["物理名"] = member.Attribute("PhysicalName")?.Value ?? "-";

            // 動的に属性値を設定
            foreach (var attr in member.Attributes()) {
                if (optionColumns.TryGetValue(attr.Name.LocalName, out var columnName)) {
                    if (attr.Value.ToLower() == "true" || attr.Value.ToLower() == "false") {
                        row[columnName] = GetBoolAttributeValue(member, attr.Name.LocalName);
                    } else {
                        row[columnName] = attr.Value;
                    }
                }
            }

            dataTable.Rows.Add(row);
        }

        /// <summary>
        /// Boolean型の属性値を取得
        /// </summary>
        private string GetBoolAttributeValue(XElement element, string attributeName) {
            var attr = element.Attribute(attributeName);
            if (attr == null) return "-";

            return attr.Value.ToLower() == "true" ? "○" : "×";
        }

        /// <summary>
        /// 非データモデルの場合のラベル表示を設定
        /// </summary>
        public void DisplayNonDataModelInfo(string name, string typeName) {
            _bindingSource.DataSource = null;
            _aggregateDetailLabel.Text = $"{name} (未対応のモデルタイプ: {typeName})";
        }
    }
}
