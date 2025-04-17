using System.ComponentModel;

namespace Nijo.Ui {
    public partial class Main : Form {
        private string? _currentFolderPath;
        private ProjectForm? _currentFolderForm;

        /// <summary>
        /// 現在開いているフォルダのパス
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string? CurrentFolderPath {
            get => _currentFolderPath;
            private set {
                _currentFolderPath = value;
                UpdatePathLabel();
            }
        }

        public Main() {
            InitializeComponent();

            // 初期状態
            CurrentFolderPath = string.Empty;
        }

        private void OpenFolderMenuItem_Click(object? sender, EventArgs e) {
            using (var folderDialog = new FolderBrowserDialog()) {
                if (folderDialog.ShowDialog() == DialogResult.OK) {
                    OpenFolderView(folderDialog.SelectedPath);
                }
            }
        }

        private void OpenFolderView(string folderPath) {
            // すでに開いているフォルダがあれば閉じる
            CloseFolderView();

            // 新しいフォルダを開く
            CurrentFolderPath = folderPath;
            EnableCloseFolderMenuItem(true);

            // フォルダ表示用のフォームを開く
            _currentFolderForm = new ProjectForm(folderPath);
            _currentFolderForm.FolderClosed += FolderViewForm_FolderClosed;

            // メインフォームを非表示にする
            this.Hide();

            // フォルダフォームを表示
            _currentFolderForm.Show();
        }

        private void CloseFolderView() {
            if (_currentFolderForm != null) {
                _currentFolderForm.FolderClosed -= FolderViewForm_FolderClosed;
                _currentFolderForm.Close();
                _currentFolderForm = null;
            }

            CurrentFolderPath = string.Empty;
            EnableCloseFolderMenuItem(false);

            // メインフォームを表示する
            this.Show();
        }

        private void FolderViewForm_FolderClosed(object? sender, EventArgs e) {
            // フォームが閉じられたときの処理
            _currentFolderForm = null;
            CurrentFolderPath = string.Empty;
            EnableCloseFolderMenuItem(false);

            // メインフォームを表示する
            this.Show();
        }

        private void CloseFolderMenuItem_Click(object? sender, EventArgs e) {
            CloseFolderView();
        }

        private void UpdatePathLabel() {
            _pathLabel.Text = string.IsNullOrEmpty(CurrentFolderPath)
                ? "フォルダが開かれていません"
                : $"現在のフォルダ: {CurrentFolderPath}";
        }

        private void EnableCloseFolderMenuItem(bool enable) {
            _closeFolderMenuItem.Enabled = enable;
        }
    }
}
