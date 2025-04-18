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
    /// 列挙体の一覧を表示・編集するコンポーネント
    /// </summary>
    public partial class EnumListView : UserControl {
        private readonly SchemaParseContext _ctx;
        // インデント値を保持する辞書（キー：行インデックス、値：インデント値）
        private readonly Dictionary<int, int> _rowIndents = new Dictionary<int, int>();
        // インデント幅のピクセル数
        private const int INDENT_SIZE = 20;

        private const string ATTR_DISPLAY_NAME = "DisplayName";
        private const string ATTR_TYPE = "Type";
        private const string ATTR_KEY = "key";
        private const string NODE_TYPE_ENUM = "enum";

        public EnumListView(SchemaParseContext ctx) {
            _ctx = ctx;
            InitializeComponent();
            InitializeDataGridView();
            LoadEnums();
        }

        /// <summary>
        /// DataGridViewの初期設定
        /// </summary>
        private void InitializeDataGridView() {
            _enumsGrid.AutoGenerateColumns = false;
            _enumsGrid.AllowUserToAddRows = false;
            _enumsGrid.AllowUserToDeleteRows = false;
            _enumsGrid.EditMode = DataGridViewEditMode.EditOnEnter;
            _enumsGrid.SelectionMode = DataGridViewSelectionMode.CellSelect;
        }

        /// <summary>
        /// 列挙体データの読み込み
        /// </summary>
        private void LoadEnums() {
            if (_ctx.Document.Root == null) return;

            // 列挙体要素を取得
            var enumElements = _ctx.Document.Root.Elements()
                .Where(el => el.Attribute(ATTR_TYPE)?.Value == NODE_TYPE_ENUM)
                .ToList();

            // データを表示用に準備
            _enumsGrid.Rows.Clear();
            _rowIndents.Clear();

            foreach (var enumElement in enumElements) {
                // 列挙体の種類を追加
                var enumTypeRow = _enumsGrid.Rows.Add();
                var enumTypeRowObj = _enumsGrid.Rows[enumTypeRow];
                enumTypeRowObj.Cells[_displayNameColumn.Index].Value = enumElement.Attribute(ATTR_DISPLAY_NAME)?.Value ?? enumElement.Name.LocalName;
                enumTypeRowObj.Cells[_physicalNameColumn.Index].Value = enumElement.Name.LocalName;
                enumTypeRowObj.Cells[_keyColumn.Index].Value = string.Empty;
                enumTypeRowObj.Tag = enumElement;

                // スタイル設定（列挙体の種類行）
                enumTypeRowObj.DefaultCellStyle.BackColor = Color.LightGray;
                enumTypeRowObj.DefaultCellStyle.Font = new Font(_enumsGrid.Font, FontStyle.Bold);

                // インデント0を設定
                _rowIndents[enumTypeRow] = 0;

                // 列挙体の値を追加
                foreach (var valueElement in enumElement.Elements()) {
                    var valueRow = _enumsGrid.Rows.Add();
                    var valueRowObj = _enumsGrid.Rows[valueRow];
                    valueRowObj.Cells[_displayNameColumn.Index].Value = valueElement.Attribute(ATTR_DISPLAY_NAME)?.Value ?? valueElement.Name.LocalName;
                    valueRowObj.Cells[_physicalNameColumn.Index].Value = valueElement.Name.LocalName;
                    valueRowObj.Cells[_keyColumn.Index].Value = valueElement.Attribute(ATTR_KEY)?.Value ?? string.Empty;
                    valueRowObj.Tag = valueElement;

                    // デフォルトでインデント1を設定
                    int indent = 1;
                    _rowIndents[valueRow] = indent;

                    // スタイル設定（値行）- インデント
                    ApplyIndentToRow(valueRowObj, indent);
                }
            }
        }

        /// <summary>
        /// 行にインデントを適用する
        /// </summary>
        private void ApplyIndentToRow(DataGridViewRow row, int indent) {
            row.DefaultCellStyle.Padding = new Padding(indent * INDENT_SIZE, 0, 0, 0);
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
            var rows = GetSelectedRows();
            if (rows.Count == 0) return;

            foreach (var row in rows) {
                // 列挙体の親行は編集不可
                if (row.Tag is XElement element && element.Parent == _ctx.Document.Root) {
                    continue;
                }

                // 現在のインデント値を取得
                if (!_rowIndents.TryGetValue(row.Index, out int currentIndent)) {
                    currentIndent = 0;
                }

                // 新しいインデント値を計算（最小値は0）
                int newIndent = Math.Max(0, currentIndent + delta);
                _rowIndents[row.Index] = newIndent;

                // 行にインデントを適用
                ApplyIndentToRow(row, newIndent);
            }
        }

        /// <summary>
        /// セルの値が変更されたときの処理
        /// </summary>
        private void EnumsGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e) {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var row = _enumsGrid.Rows[e.RowIndex];
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
                } else if (e.ColumnIndex == _keyColumn.Index) {
                    // キー値の更新（値の行のみ）
                    if (element.Parent != null && element.Parent.Attribute(ATTR_TYPE)?.Value == NODE_TYPE_ENUM) {
                        if (string.IsNullOrEmpty(newValue)) {
                            element.Attribute(ATTR_KEY)?.Remove();
                        } else {
                            var attr = element.Attribute(ATTR_KEY);
                            if (attr != null)
                                attr.Value = newValue;
                            else
                                element.Add(new XAttribute(ATTR_KEY, newValue));
                        }
                    }
                }

                // 注：物理名（要素名）の変更はXML構造を変更するため、
                // ここでは実装していません。より複雑な処理が必要です。
            }
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
        /// 選択した行の位置に新しい行を挿入する
        /// </summary>
        private void InsertRowsAtSelection() {
            if (_enumsGrid.SelectedRows.Count == 0 && _enumsGrid.SelectedCells.Count == 0) {
                return;
            }

            var rows = GetSelectedRows();
            if (rows.Count == 0) {
                return;
            }

            // 選択された最初の行のインデックスを取得
            int insertIndex = rows.Min(r => r.Index);

            // 選択された行の数だけ新しい行を追加
            InsertNewEnumValue(insertIndex);
        }

        /// <summary>
        /// 選択した行の下に新しい行を挿入する
        /// </summary>
        private void InsertRowsBelowSelection() {
            if (_enumsGrid.SelectedRows.Count == 0 && _enumsGrid.SelectedCells.Count == 0) {
                return;
            }

            var rows = GetSelectedRows();
            if (rows.Count == 0) {
                return;
            }

            // 選択された最後の行のインデックス+1の位置に挿入
            int insertIndex = rows.Max(r => r.Index) + 1;

            // 新しい行を追加
            InsertNewEnumValue(insertIndex);
        }

        /// <summary>
        /// 列挙体の新しい値を指定された位置に挿入する
        /// </summary>
        private void InsertNewEnumValue(int insertIndex) {
            if (insertIndex < 0 || insertIndex > _enumsGrid.Rows.Count) return;

            // 親となる列挙体要素を特定
            XElement? parentEnum = null;
            int parentRowIndex = -1;

            // 選択された行から親の列挙体を見つける
            for (int i = insertIndex - 1; i >= 0; i--) {
                var rowTag = _enumsGrid.Rows[i].Tag as XElement;
                if (rowTag != null && rowTag.Parent == _ctx.Document.Root) {
                    parentEnum = rowTag;
                    parentRowIndex = i;
                    break;
                }
            }

            // 親が見つからなかった場合は、次の列挙体を探す
            if (parentEnum == null) {
                for (int i = insertIndex; i < _enumsGrid.Rows.Count; i++) {
                    var rowTag = _enumsGrid.Rows[i].Tag as XElement;
                    if (rowTag != null && rowTag.Parent == _ctx.Document.Root) {
                        parentEnum = rowTag;
                        parentRowIndex = i;
                        break;
                    }
                }
            }

            // 親が見つからない場合は終了
            if (parentEnum == null) return;

            // 新しい列挙体値の名前を生成
            string baseName = "NewValue";
            int counter = 1;
            string newName = $"{baseName}{counter}";

            // 既存の値と重複しないように名前を設定
            while (parentEnum.Elements().Any(e => e.Name.LocalName == newName)) {
                counter++;
                newName = $"{baseName}{counter}";
            }

            // 新しい要素を作成
            var newElement = new XElement(newName);
            newElement.Add(new XAttribute(ATTR_KEY, GetNextAvailableKey(parentEnum)));

            // XML要素の適切な位置に挿入
            if (insertIndex > parentRowIndex + 1) {
                // 列挙体の値の間に挿入する場合
                int valueIndex = insertIndex - parentRowIndex - 1;
                var siblings = parentEnum.Elements().ToList();

                if (valueIndex >= siblings.Count) {
                    // 最後に追加
                    parentEnum.Add(newElement);
                } else {
                    // 特定の値の前に挿入
                    siblings[valueIndex].AddBeforeSelf(newElement);
                }
            } else {
                // 列挙体の最初の値として挿入
                if (parentEnum.Elements().Any()) {
                    parentEnum.Elements().First().AddBeforeSelf(newElement);
                } else {
                    parentEnum.Add(newElement);
                }
            }

            // DataGridViewを更新
            LoadEnums();

            // 新しく追加した行を選択
            for (int i = 0; i < _enumsGrid.Rows.Count; i++) {
                if (_enumsGrid.Rows[i].Tag == newElement) {
                    _enumsGrid.ClearSelection();
                    _enumsGrid.Rows[i].Selected = true;
                    _enumsGrid.CurrentCell = _enumsGrid.Rows[i].Cells[0];
                    break;
                }
            }
        }

        /// <summary>
        /// 次に使用可能なキー値を取得
        /// </summary>
        private int GetNextAvailableKey(XElement enumElement) {
            var keys = enumElement.Elements()
                .Select(e => e.Attribute(ATTR_KEY))
                .Where(a => a != null)
                .Select(a => int.TryParse(a.Value, out int k) ? k : -1)
                .Where(k => k >= 0)
                .OrderBy(k => k)
                .ToList();

            // キーが存在しない場合は1から開始
            if (keys.Count == 0) return 1;

            // 使用されていない最小の正の整数を探す
            int expectedKey = 1;
            foreach (var key in keys) {
                if (key > expectedKey) {
                    return expectedKey;
                }
                expectedKey = key + 1;
            }

            // すべてのキーが連続している場合は最大値+1
            return keys.Max() + 1;
        }

        /// <summary>
        /// 選択されている行のリストを取得する
        /// </summary>
        private List<DataGridViewRow> GetSelectedRows() {
            var rows = new List<DataGridViewRow>();

            if (_enumsGrid.SelectedRows.Count > 0) {
                // 行選択モードの場合
                foreach (DataGridViewRow row in _enumsGrid.SelectedRows) {
                    rows.Add(row);
                }
            } else if (_enumsGrid.SelectedCells.Count > 0) {
                // セル選択モードの場合、選択されたセルの行を抽出（重複なし）
                foreach (DataGridViewCell cell in _enumsGrid.SelectedCells) {
                    if (!rows.Any(r => r.Index == cell.RowIndex)) {
                        rows.Add(_enumsGrid.Rows[cell.RowIndex]);
                    }
                }
            }

            return rows;
        }
    }
}
