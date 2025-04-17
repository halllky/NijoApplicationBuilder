using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace Nijo.Ui;

/// <summary>
/// 「最近開いたフォルダ」の記録と復元
/// </summary>
public class RecentFoldersManager {
    private const int MaxRecentFolders = 10;
    private const string RecentFoldersFilePath = "recent_folders.json";
    private List<string> _recentFolders = new List<string>();

    public RecentFoldersManager() {
        LoadRecentFolders();
    }

    /// <summary>
    /// 最近開いたフォルダのリスト
    /// </summary>
    public IReadOnlyList<string> RecentFolders => _recentFolders;

    /// <summary>
    /// 最近開いたフォルダのリストを更新
    /// </summary>
    public void UpdateRecentFolders(string folderPath) {
        // すでにリストに存在する場合は削除して先頭に追加
        _recentFolders.Remove(folderPath);
        _recentFolders.Insert(0, folderPath);

        // 最大数を超えた場合は古いものを削除
        if (_recentFolders.Count > MaxRecentFolders) {
            _recentFolders.RemoveAt(_recentFolders.Count - 1);
        }

        // リストを保存
        SaveRecentFolders();
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
        } catch {
            // 読み込みエラーの場合は何もしない
        }
    }

    /// <summary>
    /// 最近開いたフォルダのリストを保存
    /// </summary>
    public void SaveRecentFolders() {
        try {
            string json = JsonSerializer.Serialize(_recentFolders);
            File.WriteAllText(RecentFoldersFilePath, json);
        } catch {
            // 保存エラーの場合は何もしない
        }
    }
}
