using System.ComponentModel;
using System.Data;
using System.Xml.Linq;

namespace Nijo.Ui.Views {

    /// <summary>
    /// NijoApplicationBuilderで構築されるプロジェクト1個を表す画面のUI
    /// </summary>
    public partial class ProjectForm : Form {
        /// <summary>
        /// ViewModel
        /// </summary>
        private readonly ProjectFormViewModel _viewModel;

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
            // メニュー項目をTreeViewに表示
            _schemaExplorer.Nodes.Clear();
            foreach (var rootNode in _viewModel.GetMenuItems()) {
                _schemaExplorer.Nodes.Add(rootNode);
            }

            // ツリーを展開
            if (_schemaExplorer.Nodes.Count > 0) {
                _schemaExplorer.Nodes[0].Expand();

                // 最初の子ノードがあれば選択
                if (_schemaExplorer.Nodes[0].Nodes.Count > 0) {
                    _schemaExplorer.SelectedNode = _schemaExplorer.Nodes[0].Nodes[0];
                } else {
                    _schemaExplorer.SelectedNode = _schemaExplorer.Nodes[0];
                }
            }
        }

        /// <summary>
        /// スキーマ定義エクスプローラーで選択が変更されたときの処理
        /// </summary>
        private void SchemaExplorer_AfterSelect(object sender, TreeViewEventArgs e) {
            if (e.Node != null) {
                SuspendLayout();

                // 既存のコントロールを明示的にDisposeする
                foreach (Control control in _splitContainer.Panel2.Controls) {
                    control.Dispose();
                }

                // 既存のコントロールをクリア
                _splitContainer.Panel2.Controls.Clear();

                // 選択されたノードに対応する画面を取得
                var dataModelView = _viewModel.ChangeSelectedElement(e.Node);
                dataModelView.Dock = DockStyle.Fill;

                // パネルに追加
                _splitContainer.Panel2.Controls.Add(dataModelView);

                ResumeLayout();
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
    }
}
