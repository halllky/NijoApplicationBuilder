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
            foreach (var element in _schemaContext.Document.Root.Elements()) {
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
                _aggregateDetailView.DataSource = null;
                _aggregateDetailLabel.Text = $"{element.Name.LocalName} (未対応のモデルタイプ: {typeName})";
            }
        }

        /// <summary>
        /// データモデルの詳細を表示
        /// </summary>
        private void DisplayDataModelDetail(XElement element) {
            var dataTable = new DataTable(element.Name.LocalName);

            // 列の定義
            dataTable.Columns.Add("項目定義", typeof(string));
            dataTable.Columns.Add("種類", typeof(string));
            dataTable.Columns.Add("物理名", typeof(string));
            dataTable.Columns.Add("DB名", typeof(string));
            dataTable.Columns.Add("キー", typeof(string));
            dataTable.Columns.Add("必須", typeof(string));
            dataTable.Columns.Add("MaxLength", typeof(string));
            dataTable.Columns.Add("文字種", typeof(string));
            dataTable.Columns.Add("トータル桁数", typeof(string));
            dataTable.Columns.Add("小数部桁数", typeof(string));
            dataTable.Columns.Add("添付可能な拡張子", typeof(string));

            // モデル自身の行を追加
            var modelRow = dataTable.NewRow();
            modelRow["項目定義"] = element.Name.LocalName;
            modelRow["種類"] = "DataModel";
            modelRow["物理名"] = "-";
            modelRow["DB名"] = element.Attribute("DbName")?.Value ?? element.Name.LocalName;
            modelRow["キー"] = "-";
            modelRow["必須"] = "-";
            dataTable.Rows.Add(modelRow);

            // 各メンバーの行を追加
            foreach (var member in element.Elements()) {
                var row = dataTable.NewRow();
                row["項目定義"] = member.Name.LocalName;
                row["種類"] = member.Attribute("Type")?.Value ?? "-";
                row["物理名"] = member.Attribute("PhysicalName")?.Value ?? "-";
                row["DB名"] = member.Attribute("DbName")?.Value ?? member.Name.LocalName;
                row["キー"] = GetBoolAttributeValue(member, "IsKey");
                row["必須"] = GetBoolAttributeValue(member, "IsRequired");
                row["MaxLength"] = member.Attribute("MaxLength")?.Value ?? "-";
                row["文字種"] = member.Attribute("CharacterType")?.Value ?? "-";
                row["トータル桁数"] = member.Attribute("TotalDigit")?.Value ?? "-";
                row["小数部桁数"] = member.Attribute("DecimalPlace")?.Value ?? "-";
                dataTable.Rows.Add(row);

                // Childrenタイプの場合は子要素も追加
                if (member.Attribute("Type")?.Value == "children" || member.Attribute("Type")?.Value == "child") {
                    foreach (var childMember in member.Elements()) {
                        var childRow = dataTable.NewRow();
                        childRow["項目定義"] = "    " + childMember.Name.LocalName;
                        childRow["種類"] = childMember.Attribute("Type")?.Value ?? "-";
                        childRow["物理名"] = childMember.Attribute("PhysicalName")?.Value ?? "-";
                        childRow["DB名"] = childMember.Attribute("DbName")?.Value ?? childMember.Name.LocalName;
                        childRow["キー"] = GetBoolAttributeValue(childMember, "IsKey");
                        childRow["必須"] = GetBoolAttributeValue(childMember, "IsRequired");
                        childRow["MaxLength"] = childMember.Attribute("MaxLength")?.Value ?? "-";
                        childRow["文字種"] = childMember.Attribute("CharacterType")?.Value ?? "-";
                        childRow["トータル桁数"] = childMember.Attribute("TotalDigit")?.Value ?? "-";
                        childRow["小数部桁数"] = childMember.Attribute("DecimalPlace")?.Value ?? "-";
                        dataTable.Rows.Add(childRow);
                    }
                }
            }

            // DataGridViewに設定
            _aggregateDetailView.DataSource = dataTable;
            _aggregateDetailLabel.Text = $"{element.Name.LocalName} (データモデル)";
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
        public event EventHandler FolderClosed;
    }
}
