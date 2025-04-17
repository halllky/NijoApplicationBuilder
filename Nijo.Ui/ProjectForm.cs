using System.ComponentModel;
using System.Data;
using System.Xml.Linq;
using Nijo.SchemaParsing;

namespace Nijo.Ui {
    public partial class ProjectForm : Form {
        /// <summary>
        /// 開かれているプロジェクト
        /// </summary>
        private readonly GeneratedProject _project;

        /// <summary>
        /// スキーマ定義解釈コンテキスト
        /// </summary>
        private SchemaParseContext _schemaContext;

        /// <summary>
        /// 現在選択されている集約ノード
        /// </summary>
        private XElement? _selectedAggregateNode;

        /// <summary>
        /// データグリッド用のBindingSource
        /// </summary>
        private readonly BindingSource _bindingSource = new BindingSource();

        /// <summary>
        /// 表示しているフォルダのパス
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string FolderPath {
            get => _project.ProjectRoot;
            private set {
            }
        }

        public ProjectForm(GeneratedProject project) {
            _project = project;
            InitializeComponent();

            // スキーマ定義のロード
            var rule = SchemaParseRule.Default();
            _schemaContext = new SchemaParseContext(XDocument.Load(_project.SchemaXmlPath), rule);

            // DataGridViewの初期設定
            _aggregateDetailView.DataSource = _bindingSource;

            // 初期化
            UpdateTitle();
            InitializeSchemaExplorer();
        }

        private void UpdateTitle() {
            if (_folderPathLabel != null) {
                Text = $"フォルダ: {Path.GetFileName(FolderPath)}";
                _folderPathLabel.Text = FolderPath;
            }
        }

        /// <summary>
        /// スキーマ定義エクスプローラーの初期化
        /// </summary>
        private void InitializeSchemaExplorer() {
            // スキーマ定義のルート集約をTreeViewに表示
            _schemaExplorer.Nodes.Clear();
            foreach (var element in _schemaContext.Document.Root?.Elements() ?? []) {
                // ルート集約を判定（Type属性の値で判断）
                var typeAttr = element.Attribute("Type")?.Value;
                if (typeAttr == "data-model" || typeAttr == "command-model" || typeAttr == "query-model" || typeAttr == "enum") {
                    var node = new TreeNode(element.Name.LocalName);
                    node.Tag = element;
                    _schemaExplorer.Nodes.Add(node);
                }
            }

            // 最初の集約を選択
            if (_schemaExplorer.Nodes.Count > 0) {
                _schemaExplorer.SelectedNode = _schemaExplorer.Nodes[0];
            }
        }

        /// <summary>
        /// スキーマ定義エクスプローラーで選択が変更されたときの処理
        /// </summary>
        private void SchemaExplorer_AfterSelect(object sender, TreeViewEventArgs e) {
            if (e.Node?.Tag is XElement element) {
                _selectedAggregateNode = element;
                DisplayAggregateDetail(element);
            }
        }

        /// <summary>
        /// 選択された集約の詳細を表示
        /// </summary>
        private void DisplayAggregateDetail(XElement element) {
            string typeName = element.Attribute("Type")?.Value ?? string.Empty;

            // データモデルの場合はDataGridViewで表示
            if (typeName == "data-model") {
                DisplayDataModelDetail(element);
            } else {
                // 他のモデルタイプは未実装
                _bindingSource.DataSource = null;
                _aggregateDetailLabel.Text = $"{element.Name.LocalName} (未対応のモデルタイプ: {typeName})";
            }
        }

        /// <summary>
        /// データモデルの詳細を表示
        /// </summary>
        private void DisplayDataModelDetail(XElement element) {
            var dataTable = new DataTable(element.Name.LocalName);

            // 基本列の定義
            dataTable.Columns.Add("項目定義", typeof(string));
            dataTable.Columns.Add("種類", typeof(string));
            dataTable.Columns.Add("物理名", typeof(string));

            // SchemaParseRuleからNodeOptionsを取得し、データモデルに適用可能な属性のみをフィルタリング
            // "data-model"に対応するモデルを取得
            var dataModelType = _schemaContext.Models
                .FirstOrDefault(m => m.Value.SchemaName == "data-model");

            var availableOptions = _schemaContext
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

            // 列のソートを無効化
            foreach (DataGridViewColumn column in _aggregateDetailView.Columns) {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
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

        private void FolderViewForm_FormClosing(object sender, FormClosingEventArgs e) {
            // フォームが閉じられたときにイベントを発火する
            FolderClosed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// フォームが閉じられたときに発火するイベント
        /// </summary>
        public event EventHandler? FolderClosed;
    }
}
