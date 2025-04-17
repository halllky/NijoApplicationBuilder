using System.ComponentModel;
using System.Data;
using System.Xml.Linq;
using Nijo.SchemaParsing;

namespace Nijo.Ui {

    /// <summary>
    /// NijoApplicationBuilderで構築されるプロジェクト1個を表す画面のUI
    /// </summary>
    public partial class ProjectForm : Form {
        /// <summary>
        /// ViewModel
        /// </summary>
        private readonly ProjectFormViewModel _viewModel;
        /// <summary>
        /// スキーマ定義エクスプローラーで選択されている要素
        /// </summary>
        private XElement? _selectedRootAggregate;

        /// <summary>
        /// 表示しているフォルダのパス
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string FolderPath => _viewModel.ProjectRoot;

        public ProjectForm(ProjectFormViewModel viewModel) {
            _viewModel = viewModel;

            InitializeComponent();

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
            foreach (var element in _viewModel.GetRootAggregates()) {
                var node = new TreeNode(element.Name.LocalName);
                node.Tag = element;
                _schemaExplorer.Nodes.Add(node);
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
                _selectedRootAggregate = element;
                DisplayAggregateDetail(element);
            }
        }

        /// <summary>
        /// 選択された集約の詳細を表示
        /// </summary>
        private void DisplayAggregateDetail(XElement element) {
            var (dataTable, label) = _viewModel.GetDataModelDetail(element);

            // 既存のコントロールを明示的にDisposeする
            foreach (Control control in _splitContainer.Panel2.Controls) {
                control.Dispose();
            }

            // 既存のコントロールをクリア
            _splitContainer.Panel2.Controls.Clear();

            // 新しいRootAggregateDataModelComponentを作成
            var dataModelView = new RootAggregateDataModelComponent();
            dataModelView.Dock = DockStyle.Fill;
            dataModelView.DisplayModel(dataTable, label);

            // パネルに追加
            _splitContainer.Panel2.Controls.Add(dataModelView);
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
