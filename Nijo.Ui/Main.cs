using Nijo.Ui.Views;
using System.ComponentModel;
using System.Text.Json;

namespace Nijo.Ui {
    public partial class Main : Form {
        private ProjectForm? _currentFolderForm;
        private ProjectFormViewModel? _viewModel;
        private readonly RecentFoldersManager _recentFoldersManager;

        public Main() {
            InitializeComponent();

            // RecentFoldersManagerの初期化
            _recentFoldersManager = new RecentFoldersManager();

            // 最近開いたフォルダリストを表示
            UpdateRecentFoldersListUI();

            // フォーム終了時のイベントハンドラ
            this.FormClosing += Main_FormClosing;
        }

        private void Main_Load(object sender, EventArgs e) {
            // 先頭のプロジェクトを選択
            if (_recentFoldersListBox.Items.Count > 0) {
                _recentFoldersListBox.Select();
                _recentFoldersListBox.SelectedIndex = 0;
            }
        }

        private void Main_FormClosing(object? sender, FormClosingEventArgs e) {
            _recentFoldersManager.SaveRecentFolders();
        }

        private void OpenFolderMenuItem_Click(object? sender, EventArgs e) {
            using (var folderDialog = new FolderBrowserDialog()) {
                if (folderDialog.ShowDialog() == DialogResult.OK) {
                    OpenFolderView(folderDialog.SelectedPath);
                }
            }
        }

        private void OpenFolderView(string folderPath) {
            // ViewModelを使ってフォルダを開く
            if (!GeneratedProject.TryOpen(folderPath, out var project, out var errorMessage)) {
                MessageBox.Show(errorMessage);
                return;
            }

            // 最近開いたフォルダのリストを更新
            _recentFoldersManager.UpdateRecentFolders(folderPath);

            // すでに開いているフォルダがあれば閉じる
            CloseFolderView();

            // UIの更新
            UpdateRecentFoldersListUI();

            // 新しいフォルダを開く
            _viewModel = new ProjectFormViewModel(project);
            _currentFolderForm = new ProjectForm(_viewModel);
            _currentFolderForm.FolderClosed += FolderViewForm_FolderClosed;

            // メインフォームを非表示にする
            this.Hide();

            // フォルダフォームを表示
            _currentFolderForm.Show();
        }

        private void CloseFolderView() {
            _viewModel = null;

            if (_currentFolderForm != null) {
                _currentFolderForm.FolderClosed -= FolderViewForm_FolderClosed;
                _currentFolderForm.Close();
                _currentFolderForm = null;
            }

            // メインフォームを表示する
            this.Show();
        }

        private void FolderViewForm_FolderClosed(object? sender, EventArgs e) {
            // フォームが閉じられたときの処理
            _currentFolderForm = null;

            // メインフォームを表示する
            this.Show();
        }

        private void CloseFolderMenuItem_Click(object? sender, EventArgs e) {
            CloseFolderView();
        }

        /// <summary>
        /// 最近開いたフォルダリストをUIに表示
        /// </summary>
        private void UpdateRecentFoldersListUI() {
            _recentFoldersListBox.Items.Clear();

            foreach (var folderPath in _recentFoldersManager.RecentFolders) {
                string displayName = Path.GetFileName(folderPath);
                if (string.IsNullOrEmpty(displayName)) {
                    displayName = folderPath; // ルートドライブの場合はそのまま表示
                }

                // 項目にフルパスを保持しつつ、表示名はフォルダ名のみに
                _recentFoldersListBox.Items.Add(new RecentFolderItem(folderPath, displayName));
            }
        }

        /// <summary>
        /// 最近開いたフォルダのリストからフォルダを開く
        /// </summary>
        private void RecentFoldersListBox_DoubleClick(object sender, EventArgs e) {
            if (_recentFoldersListBox.SelectedItem is RecentFolderItem selectedItem) {
                OpenFolderView(selectedItem.FullPath);
            }
        }

        /// <summary>
        /// 最近開いたフォルダのリストからフォルダを開く（Enter or Space）
        /// </summary>
        private void RecentFoldersListBox_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space) {
                if (_recentFoldersListBox.SelectedItem is RecentFolderItem selectedItem) {
                    OpenFolderView(selectedItem.FullPath);
                }
            }
        }
    }

    /// <summary>
    /// 最近開いたフォルダの表示用アイテムクラス
    /// </summary>
    public class RecentFolderItem {
        public string FullPath { get; }
        public string DisplayName { get; }

        public RecentFolderItem(string fullPath, string displayName) {
            FullPath = fullPath;
            DisplayName = displayName;
        }

        public override string ToString() {
            return DisplayName;
        }
    }
}
