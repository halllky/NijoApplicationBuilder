using System.ComponentModel;

namespace Nijo.Ui {
    public partial class ProjectForm : Form {
        /// <summary>
        /// 開かれているプロジェクト
        /// </summary>
        private readonly GeneratedProject _project;

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
            UpdateTitle(); // 初期化後にタイトルを更新
        }

        private void UpdateTitle() {
            if (_folderPathLabel != null) {
                Text = $"フォルダ: {Path.GetFileName(FolderPath)}";
                _folderPathLabel.Text = FolderPath;
            }
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
