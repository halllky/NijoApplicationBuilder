using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApp.Core.Util;

/// <summary>
/// DbContextの拡張メソッド
/// </summary>
public static class DbContextExtensions {
    /// <summary>
    /// データベースを作成し、指定されたフォルダ内のSQLスクリプトを実行します
    /// </summary>
    /// <param name="context">DbContextのインスタンス</param>
    /// <param name="runtimeSetting">実行時設定</param>
    /// <returns>データベースが新規作成されたかどうか</returns>
    public static async Task<bool> EnsureCreatedAsyncEx<T>(this T context, RuntimeSetting runtimeSetting) where T : DbContext {
        // データベースを作成
        bool created = await context.Database.EnsureCreatedAsync();

        // SQLスクリプトのフォルダパスを取得
        string scriptFolder = Path.GetFullPath(runtimeSetting.MigrationsScriptFolder);
        if (string.IsNullOrEmpty(scriptFolder) || !Directory.Exists(scriptFolder)) {
            // フォルダが存在しない場合は処理を中断
            return created;
        }

        // .sqlファイルをファイル名の昇順で取得
        var sqlFiles = Directory.GetFiles(scriptFolder, "*.sql")
            .OrderBy(file => Path.GetFileName(file))
            .ToList();

        // 各SQLファイルを実行
        foreach (var sqlFile in sqlFiles) {
            // ファイルの内容を読み込む（エンコード：BOMなしUTF-8）
            string sql = await File.ReadAllTextAsync(sqlFile, new UTF8Encoding(false));

            // SQLを実行
            await context.Database.ExecuteSqlRawAsync(sql);
        }

        return created;
    }
}
