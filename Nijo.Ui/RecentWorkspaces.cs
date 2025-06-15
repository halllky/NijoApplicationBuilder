using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Nijo.Ui {
    /// <summary>
    /// 最近開いたワークスペースの履歴を管理するクラス
    /// </summary>
    public class RecentWorkspaces {
        private const string RECENT_WORKSPACES_FILE = "recent_workspaces.json";
        private const int MAX_RECENT_WORKSPACES = 10;

        private List<string> _recentWorkspaces;

        public IReadOnlyList<string> Workspaces => _recentWorkspaces.AsReadOnly();

        public RecentWorkspaces() {
            _recentWorkspaces = LoadRecentWorkspaces();
        }

        /// <summary>
        /// ワークスペースを履歴に追加します
        /// </summary>
        /// <param name="path">ワークスペースのパス</param>
        public void AddWorkspace(string path) {
            // 既に存在する場合は削除してリストの先頭に追加
            _recentWorkspaces.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
            _recentWorkspaces.Insert(0, path);

            // 上限を超えた場合は古いものを削除
            if (_recentWorkspaces.Count > MAX_RECENT_WORKSPACES) {
                _recentWorkspaces.RemoveAt(_recentWorkspaces.Count - 1);
            }

            SaveRecentWorkspaces();
        }

        /// <summary>
        /// 履歴から指定されたワークスペースを削除します
        /// </summary>
        /// <param name="path">削除するワークスペースのパス</param>
        public void RemoveWorkspace(string path) {
            _recentWorkspaces.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
            SaveRecentWorkspaces();
        }

        /// <summary>
        /// 履歴をクリアします
        /// </summary>
        public void ClearWorkspaces() {
            _recentWorkspaces.Clear();
            SaveRecentWorkspaces();
        }

        /// <summary>
        /// 最近開いたワークスペースの履歴をJSONファイルから読み込みます
        /// </summary>
        private List<string> LoadRecentWorkspaces() {
            try {
                string filePath = GetRecentWorkspacesFilePath();

                if (File.Exists(filePath)) {
                    string json = File.ReadAllText(filePath);
                    var workspaces = JsonSerializer.Deserialize<List<string>>(json);
                    if (workspaces != null) {
                        // 存在しないパスをフィルタリング
                        return workspaces.Where(path => !string.IsNullOrEmpty(path) && Directory.Exists(path)).ToList();
                    }
                }
            } catch (Exception ex) {
                // ファイル読み込みエラーの場合は空のリストを返す
                System.Diagnostics.Debug.WriteLine($"最近使ったワークスペースの読み込みに失敗: {ex.Message}");
            }

            return new List<string>();
        }

        /// <summary>
        /// 最近開いたワークスペースの履歴をJSONファイルに保存します
        /// </summary>
        private void SaveRecentWorkspaces() {
            try {
                string filePath = GetRecentWorkspacesFilePath();
                string directory = Path.GetDirectoryName(filePath);

                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonSerializer.Serialize(_recentWorkspaces);
                File.WriteAllText(filePath, json);
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"最近使ったワークスペースの保存に失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 最近開いたワークスペースの履歴を保存するファイルのパスを取得します
        /// </summary>
        private string GetRecentWorkspacesFilePath() {
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Nijo");

            return Path.Combine(appDataPath, RECENT_WORKSPACES_FILE);
        }
    }
}
