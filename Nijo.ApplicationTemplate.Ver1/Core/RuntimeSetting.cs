using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApp.Core;

/// <summary>
/// 実行時設定。 appsettings.json から読み込まれた設定値を使う。
/// </summary>
public class RuntimeSetting {

    /// <summary>
    /// NLogのログ出力先ディレクトリ
    /// </summary>
    public string LogDirectory { get; set; } = string.Empty;

    #region データベース接続
    /// <summary>
    /// 現在接続しているデータベースのプロファイル。
    /// <see cref="DbProfiles"/> に設定されたものの名前を指定する。
    /// </summary>
    public string CurrentDbProfileName { get; set; } = string.Empty;
    /// <summary>
    /// 接続先データベース情報の一覧。
    /// 開発中はここに記載されたプロファイルを切り替えながら進める。
    /// </summary>
    public List<DbProfile> DbProfiles { get; set; } = [];

    public class DbProfile {
        /// <summary>
        /// プロファイル名
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// 接続文字列
        /// </summary>
        public string ConnStr { get; set; } = string.Empty;
    }

    /// <summary>
    /// 設定されているDB接続先情報を返します。
    /// 設定値が不正な場合は例外を送出します。
    /// </summary>
    public DbProfile GetCurrentProfile() {
        if (string.IsNullOrWhiteSpace(CurrentDbProfileName)) {
            throw new InvalidOperationException($"{nameof(CurrentDbProfileName)}が指定されていません。");
        }
        var profile = DbProfiles.FirstOrDefault(x => x.Name == CurrentDbProfileName)
            ?? throw new InvalidOperationException($"{nameof(DbProfiles)}に'{CurrentDbProfileName}'という名前の設定がありません。");

        return profile;
    }
    #endregion データベース接続
}
