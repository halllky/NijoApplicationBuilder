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

            // データモデル詳細表示用コントロールの初期化
            _dataModelView = projectFormDataModelView1;
            _dataModelView.Dock = DockStyle.Fill;

            // 初期化
            Text = Path.GetFileName(FolderPath);
            InitializeSchemaExplorer();
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

            // データモデルの場合はデータモデル詳細表示用コントロールで表示
            if (typeName == "data-model") {
                _dataModelView.DisplayDataModelDetail(element, _schemaContext);
            } else {
                // 他のモデルタイプは未実装
                _dataModelView.DisplayNonDataModelInfo(element.Name.LocalName, typeName);
            }
        }

        private void FolderViewForm_FormClosing(object sender, FormClosingEventArgs e) {
            // フォームが閉じられたときにイベントを発火する
            FolderClosed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// フォームが閉じられたときに発火するイベント
        /// </summary>
        public event EventHandler? FolderClosed;

        /// <summary>
        /// データモデル詳細表示用コントロール
        /// </summary>
        private ProjectFormDataModelView _dataModelView;
    }
}
