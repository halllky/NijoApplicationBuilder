using System.ComponentModel;

namespace Nijo.Ui {
    public partial class ProjectForm : Form {
        private string _folderPath;

        /// <summary>
        /// 表示しているフォルダのパス
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string FolderPath {
            get => _folderPath;
            private set {
                _folderPath = value;
                UpdateTitle();
            }
        }

        public ProjectForm(string folderPath) {
            _folderPath = folderPath; // 初期化時に直接設定
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
