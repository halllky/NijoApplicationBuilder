using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using Nijo.SchemaParsing;

namespace Nijo.Ui.Views {
    /// <summary>
    /// 値オブジェクトの一覧を表示・編集するコンポーネント
    /// </summary>
    public partial class ValueObjectListView : UserControl {
        private readonly SchemaParseContext _ctx;

        private const string ATTR_DISPLAY_NAME = "DisplayName";
        private const string ATTR_TYPE = "Type";
        private const string NODE_TYPE_VALUE_OBJECT = "value-object";

        public ValueObjectListView(SchemaParseContext ctx) {
            _ctx = ctx;
            InitializeComponent();
            InitializeDataGridView();
            LoadValueObjects();
        }

        /// <summary>
        /// DataGridViewの初期設定
        /// </summary>
        private void InitializeDataGridView() {
            _valueObjectsGrid.AutoGenerateColumns = false;
            _valueObjectsGrid.AllowUserToAddRows = false;
            _valueObjectsGrid.AllowUserToDeleteRows = false;
            _valueObjectsGrid.EditMode = DataGridViewEditMode.EditOnEnter;
            _valueObjectsGrid.SelectionMode = DataGridViewSelectionMode.CellSelect;
        }

        /// <summary>
        /// 値オブジェクトデータの読み込み
        /// </summary>
        private void LoadValueObjects() {
            if (_ctx.Document.Root == null) return;

            // 値オブジェクト要素を取得
            var valueObjectElements = _ctx.Document.Root.Elements()
                .Where(el => el.Attribute(ATTR_TYPE)?.Value == NODE_TYPE_VALUE_OBJECT)
                .ToList();

            // データを表示用に準備
            _valueObjectsGrid.Rows.Clear();

            foreach (var valueObjectElement in valueObjectElements) {
                // 値オブジェクトの情報を追加
                var row = _valueObjectsGrid.Rows.Add();
                var rowObj = _valueObjectsGrid.Rows[row];
                rowObj.Cells[_displayNameColumn.Index].Value = valueObjectElement.Attribute(ATTR_DISPLAY_NAME)?.Value ?? valueObjectElement.Name.LocalName;
                rowObj.Cells[_physicalNameColumn.Index].Value = valueObjectElement.Name.LocalName;
                rowObj.Tag = valueObjectElement;
            }
        }

        /// <summary>
        /// セルの値が変更されたときの処理
        /// </summary>
        private void ValueObjectsGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e) {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var row = _valueObjectsGrid.Rows[e.RowIndex];
            if (row.Tag is XElement element) {
                var newValue = row.Cells[e.ColumnIndex].Value?.ToString() ?? string.Empty;

                // 変更された列に応じて属性を更新
                if (e.ColumnIndex == _displayNameColumn.Index) {
                    // 表示名の更新
                    if (string.IsNullOrEmpty(newValue) || newValue == element.Name.LocalName) {
                        // デフォルト値と同じ場合は属性を削除
                        element.Attribute(ATTR_DISPLAY_NAME)?.Remove();
                    } else {
                        // 属性を設定または追加
                        var attr = element.Attribute(ATTR_DISPLAY_NAME);
                        if (attr != null)
                            attr.Value = newValue;
                        else
                            element.Add(new XAttribute(ATTR_DISPLAY_NAME, newValue));
                    }
                }

                // 注：物理名（要素名）の変更はXML構造を変更するため、
                // ここでは実装していません。より複雑な処理が必要です。
            }
        }
    }
}
