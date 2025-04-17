using System.ComponentModel;
using System.Text.Json;

namespace Nijo.Ui {
    public partial class Main : Form {
        private string? _currentFolderPath;
        private ProjectForm? _currentFolderForm;
        private readonly List<string> _recentFolders = new List<string>();
        private const int MaxRecentFolders = 10;
        private const string RecentFoldersFilePath = "recent_folders.json";

        public Main() {
            InitializeComponent();

            // 最近開いたフォルダのリストを読み込む
            LoadRecentFolders();

            // 最近開いたフォルダリストを表示
            UpdateRecentFoldersList();

            // フォーム終了時のイベントハンドラ
            this.FormClosing += Main_FormClosing;
        }

        private void Main_FormClosing(object? sender, FormClosingEventArgs e) {
            SaveRecentFolders();
        }

        private void OpenFolderMenuItem_Click(object? sender, EventArgs e) {
            using (var folderDialog = new FolderBrowserDialog()) {
                if (folderDialog.ShowDialog() == DialogResult.OK) {
                    OpenFolderView(folderDialog.SelectedPath);
                }
            }
        }

        private void OpenFolderView(string folderPath) {
            // パスの検査
            if (!GeneratedProject.TryOpen(folderPath, out var project, out var error)) {
                MessageBox.Show(error);
                return;
            }

            // すでに開いているフォルダがあれば閉じる
            CloseFolderView();

            // 最近開いたフォルダのリストを更新
            UpdateRecentFolders(folderPath);

            // 新しいフォルダを開く
            _currentFolderForm = new ProjectForm(project);
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
        /// 最近開いたフォルダのリストを更新
        /// </summary>
        private void UpdateRecentFolders(string folderPath) {
            // すでにリストに存在する場合は削除して先頭に追加
            _recentFolders.Remove(folderPath);
            _recentFolders.Insert(0, folderPath);

            // 最大数を超えた場合は古いものを削除
            if (_recentFolders.Count > MaxRecentFolders) {
                _recentFolders.RemoveAt(_recentFolders.Count - 1);
            }

            // リストを保存
            SaveRecentFolders();

            // UI更新
            UpdateRecentFoldersList();
        }

        /// <summary>
        /// 最近開いたフォルダリストをUIに表示
        /// </summary>
        private void UpdateRecentFoldersList() {
            _recentFoldersListBox.Items.Clear();

            foreach (var folderPath in _recentFolders) {
                string displayName = Path.GetFileName(folderPath);
                if (string.IsNullOrEmpty(displayName)) {
                    displayName = folderPath; // ルートドライブの場合はそのまま表示
                }

                // 項目にフルパスを保持しつつ、表示名はフォルダ名のみに
                _recentFoldersListBox.Items.Add(new RecentFolderItem(folderPath, displayName));
            }
        }

        /// <summary>
        /// 最近開いたフォルダのリストを読み込む
        /// </summary>
        private void LoadRecentFolders() {
            try {
                if (File.Exists(RecentFoldersFilePath)) {
                    string json = File.ReadAllText(RecentFoldersFilePath);
                    var folders = JsonSerializer.Deserialize<List<string>>(json);
                    if (folders != null) {
                        _recentFolders.Clear();
                        _recentFolders.AddRange(folders);
                    }
                }
            } catch (Exception ex) {
                // 読み込みに失敗した場合はエラーメッセージを表示
                MessageBox.Show($"最近開いたフォルダのリストの読み込みに失敗しました: {ex.Message}");
            }
        }

        /// <summary>
        /// 最近開いたフォルダのリストを保存
        /// </summary>
        private void SaveRecentFolders() {
            try {
                string json = JsonSerializer.Serialize(_recentFolders);
                File.WriteAllText(RecentFoldersFilePath, json);
            } catch (Exception ex) {
                // 保存に失敗した場合はエラーメッセージを表示
                MessageBox.Show($"最近開いたフォルダのリストの保存に失敗しました: {ex.Message}");
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
