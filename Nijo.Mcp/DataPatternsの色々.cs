using System.Text;

namespace Nijo.Mcp;

partial class NijoMcpTools {

    public static async Task<bool> アプリケーションテンプレートを自動テストで作成されたプロジェクトにコピーする(
        WorkDirectory workDirectory,
        string dataPatternsXmlFileFullPath,
        bool deleteAll) {

        workDirectory.WriteSectionTitle("アプリケーションテンプレートを自動テストで作成されたプロジェクトにコピー");

        var appsrvFileName = Path.Combine(DATA_PATTERN_REVEALED_DIR, "Core", "OverridedApplicationService.cs");
        var pagesDirectory = Path.Combine(DATA_PATTERN_REVEALED_DIR, "react", "src", "pages");
        var nijoXmlDestNme = Path.Combine(DATA_PATTERN_REVEALED_DIR, "nijo.xml");
        var pagesIndexFile = Path.Combine(pagesDirectory, "index.tsx");

        if (deleteAll) {
            // 「自動テストで作成されたプロジェクト」フォルダの内容を全部クリア
            workDirectory.WriteToMainLog("「自動テストで作成されたプロジェクト」フォルダの内容を全部クリアします。");
            DeleteDirectoryContentsRecursively(DATA_PATTERN_REVEALED_DIR);

            // アプリケーションテンプレートを「自動テストで作成されたプロジェクト」フォルダにコピー
            workDirectory.WriteToMainLog("アプリケーションテンプレートを「自動テストで作成されたプロジェクト」フォルダにコピーします。");
            CopyDirectoryRecursively(APPLICATION_TEMPLATE_DIR, DATA_PATTERN_REVEALED_DIR);

            // ユニットテストはコンパイラーチェックの邪魔なので削除
            Directory.Delete(Path.Combine(DATA_PATTERN_REVEALED_DIR, "Test", "Tests"), true);

            // 画面フォルダ
            foreach (var dir in Directory.GetDirectories(pagesDirectory)) {
                var dirInfo = new DirectoryInfo(dir);

                // テンプレートの「顧客」「従業員」フォルダは「画面実装例（顧客）」「画面実装例（従業員）」にリネーム。
                // AIエージェントに参考にさせるために敢えて残す。
                dirInfo.MoveTo(Path.Combine(dirInfo.Parent!.FullName, $"画面実装例（{dirInfo.Name}）"));

                // 中身のソースコードはTypeScriptコンパイラーに認識されないよう拡張子を変える。
                foreach (var file in dirInfo.GetFiles()) {
                    file.MoveTo($"{file.FullName}.SAMPLE");
                }
            }

        } else {
            // 2回目以降なので最低限のファイルだけ初期化する（Reactのpagesディレクトリだけ消せばよい）
            foreach (var dir in Directory.GetDirectories(pagesDirectory)) {
                var dirInfo = new DirectoryInfo(dir);
                if (!dirInfo.Name.StartsWith("画面実装例")) dirInfo.Delete(true);
            }
        }

        // 「自動テストで作成されたプロジェクト」のnijo.xmlを指定されたXMLファイルで上書き
        workDirectory.WriteToMainLog("「自動テストで作成されたプロジェクト」のnijo.xmlを指定されたXMLファイルで上書きします。");
        File.Copy(dataPatternsXmlFileFullPath, nijoXmlDestNme, true);

        // -------------------------------------

        // AIエージェントに実装させるファイルを指示つきで用意

        workDirectory.WriteToMainLog($"右記ファイルをサンプルコードで初期化します: {appsrvFileName}");

        await File.WriteAllTextAsync(appsrvFileName, $$"""
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Text;
            using System.Threading.Tasks;

            namespace MyApp.Core;

            partial class OverridedApplicationService {
                // このクラスに生成されるabstractメソッドを実装してください。

                // 例: 従業員DataModel から 従業員QueryModel のインスタンスを生成するメソッドを実装する
                // protected override IQueryable<従業員SearchResult> CreateQuerySource(従業員SearchCondition searchCondition, IPresentationContext<従業員SearchConditionMessages> context) {
                //     return DbContext.従業員DbSet.Select(e => new 従業員SearchResult {
                //         従業員ID = e.従業員ID,
                //         氏名 = e.氏名,
                //         氏名カナ = e.氏名カナ,
                //         退職日 = e.退職日,
                //         所属部署 = e.所属部署.Select(d => new 所属部署SearchResult {
                //             年度 = d.年度,
                //             部署_部署名 = d.部署!.部署名,
                //             部署_部署コード = d.部署!.部署コード,
                //         }).ToList(),
                //         Version = (int)e.Version!,
                //     });
                // }
            }
            """, new UTF8Encoding(false, false));

        workDirectory.WriteToMainLog($"右記ファイルをサンプルコードで初期化します: {pagesIndexFile}");

        await File.WriteAllTextAsync(pagesIndexFile, $$"""
            import { RouteObjectWithSideMenuSetting } from "../routes";

            // 例:
            // import { 従業員一覧検索 } from "./画面実装例（従業員）/従業員一覧検索";
            // import { 従業員詳細編集 } from "./画面実装例（従業員）/従業員詳細編集";
            // import { 顧客一覧検索 } from "./画面実装例（顧客）/顧客一覧検索";
            // import { 顧客詳細編集 } from "./画面実装例（顧客）/顧客詳細編集";

            /** Home以外の画面のルーティング設定 */
            export default function (): RouteObjectWithSideMenuSetting[] {
              return []

              // 例:
              // return [
              //   { path: '顧客', element: <顧客一覧検索 />, sideMenuLabel: "顧客一覧" },
              //   { path: '顧客/new', element: <顧客詳細編集 /> },
              //   { path: '顧客/:顧客ID', element: <顧客詳細編集 /> },
              //   { path: '従業員', element: <従業員一覧検索 />, sideMenuLabel: "従業員一覧" },
              //   { path: '従業員/new', element: <従業員詳細編集 /> },
              //   { path: '従業員/:従業員ID', element: <従業員詳細編集 /> },
              // ]
            }
            """, new UTF8Encoding(false, false));

        return true;
    }

    private static bool AIエージェントの実装成果をスナップショットフォルダに保存する(string dataPatternsXmlFileFullPath, string snapshotDir) {

        // 前回のセッションの結果が残っているなら削除
        if (Directory.Exists(snapshotDir)) {
            Directory.Delete(snapshotDir, true);
        }

        // 「自動テストで作成されたプロジェクト」のフォルダの内容のうち、
        // 当該データパターンのデータ構造に由来するカスタマイズ実装をスナップショットフォルダにコピーする
        var destinationCoreDir = Path.Combine(snapshotDir, "Core");
        var destinationPagesDir = Path.Combine(snapshotDir, "react", "src", "pages");
        if (!Directory.Exists(destinationCoreDir)) Directory.CreateDirectory(destinationCoreDir);
        if (!Directory.Exists(destinationPagesDir)) Directory.CreateDirectory(destinationPagesDir);

        // ApplicationService
        File.Copy(
            Path.Combine(DATA_PATTERN_REVEALED_DIR, "Core", "OverridedApplicationService.cs"),
            Path.Combine(destinationCoreDir, "OverridedApplicationService.cs"),
            overwrite: true);

        // Reactルーティング
        File.Copy(
            Path.Combine(DATA_PATTERN_REVEALED_DIR, "react", "src", "pages", "index.tsx"),
            Path.Combine(destinationPagesDir, "index.tsx"),
            overwrite: true);

        // React画面
        foreach (var sourceDirectory in Directory.GetDirectories(Path.Combine(DATA_PATTERN_REVEALED_DIR, "react", "src", "pages"))) {
            CopyDirectoryRecursively(
                sourceDirectory,
                Path.Combine(destinationPagesDir, Path.GetFileName(sourceDirectory)));
        }

        return true;
    }

    /// <summary>
    /// "DataPatterns" フォルダ内のファイルのうち、ファイル名の昇順で次のパターンを返す。
    /// 既に最後のファイルならnullを返す
    /// </summary>
    /// <param name="current">現在処理中のパターンのXMLのファイル名絶対パス</param>
    private static string? GetNextDataPatternXmlFullPath(string? current) {
        var skip = true;

        foreach (var fullpath in Directory.GetFiles(DATA_PATTERN_DIR)) {

            // 途中から始めたいときはコメントアウトを外す
            // if (fullpath == @"C:\Users\krpzx\OneDrive\ドキュメント\local\20230409_haldoc\haldoc\Nijo.IntegrationTest\DataPatterns\000_単純な集約.xml") continue;
            // if (fullpath == @"C:\Users\krpzx\OneDrive\ドキュメント\local\20230409_haldoc\haldoc\Nijo.IntegrationTest\DataPatterns\001_Refのみ.xml") continue;
            // if (fullpath == @"C:\Users\krpzx\OneDrive\ドキュメント\local\20230409_haldoc\haldoc\Nijo.IntegrationTest\DataPatterns\002_Childrenのみ.xml") continue;
            // if (fullpath == @"C:\Users\krpzx\OneDrive\ドキュメント\local\20230409_haldoc\haldoc\Nijo.IntegrationTest\DataPatterns\003_Childのみ.xml") continue;
            // if (fullpath == @"C:\Users\krpzx\OneDrive\ドキュメント\local\20230409_haldoc\haldoc\Nijo.IntegrationTest\DataPatterns\010_ChildrenからChildrenへの参照.xml") continue;
            // if (fullpath == @"C:\Users\krpzx\OneDrive\ドキュメント\local\20230409_haldoc\haldoc\Nijo.IntegrationTest\DataPatterns\011_ダブル.xml") continue;
            // if (fullpath == @"C:\Users\krpzx\OneDrive\ドキュメント\local\20230409_haldoc\haldoc\Nijo.IntegrationTest\DataPatterns\012_スカラメンバー網羅.xml") continue;
            // if (fullpath == @"C:\Users\krpzx\OneDrive\ドキュメント\local\20230409_haldoc\haldoc\Nijo.IntegrationTest\DataPatterns\013_主キーにref.xml") continue;
            // if (fullpath == @"C:\Users\krpzx\OneDrive\ドキュメント\local\20230409_haldoc\haldoc\Nijo.IntegrationTest\DataPatterns\014_複数経路の参照.xml") continue;
            // if (fullpath == @"C:\Users\krpzx\OneDrive\ドキュメント\local\20230409_haldoc\haldoc\Nijo.IntegrationTest\DataPatterns\017_CommandModel.xml") continue;
            //if (fullpath == @"C:\Users\krpzx\OneDrive\ドキュメント\local\20230409_haldoc\haldoc\Nijo.IntegrationTest\DataPatterns\100_RDRA.xml") continue;
            //if (fullpath == @"C:\Users\krpzx\OneDrive\ドキュメント\local\20230409_haldoc\haldoc\Nijo.IntegrationTest\DataPatterns\101_売上管理.xml") continue;
            //if (fullpath == @"C:\Users\krpzx\OneDrive\ドキュメント\local\20230409_haldoc\haldoc\Nijo.IntegrationTest\DataPatterns\102_社内備品管理.xml") continue;
            //if (fullpath == @"C:\Users\krpzx\OneDrive\ドキュメント\local\20230409_haldoc\haldoc\Nijo.IntegrationTest\DataPatterns\103_011ダブルのReadWrite混在版.xml") continue;
            //if (fullpath == @"C:\Users\krpzx\OneDrive\ドキュメント\local\20230409_haldoc\haldoc\Nijo.IntegrationTest\DataPatterns\104_required属性のテスト.xml") continue;
            //if (fullpath == @"C:\Users\krpzx\OneDrive\ドキュメント\local\20230409_haldoc\haldoc\Nijo.IntegrationTest\DataPatterns\105_外部キー代理.xml") continue;

            // 現在処理中のxmlが無い場合は最初に見つかったxmlのファイル名を返す
            if (current == null) return fullpath;

            if (skip) {
                if (fullpath == current) skip = false;
            } else {
                // 現在処理中のxmlの1個次
                return fullpath;
            }
        }

        // 現在処理中のxmlが最後のxmlの場合
        return null;
    }

    /// <summary>
    /// 指定されたディレクトリの内容を削除
    /// </summary>
    private static void DeleteDirectoryContentsRecursively(string directory) {
        foreach (var dir in Directory.GetDirectories(directory, "*", SearchOption.TopDirectoryOnly)) {
            try {
                Directory.Delete(dir, true);
            } catch {
                // 無視
            }
        }
        foreach (var file in Directory.GetFiles(directory, "*", SearchOption.TopDirectoryOnly)) {
            try {
                File.Delete(file);
            } catch {
                // 無視
            }
        }
    }

    /// <summary>
    /// 指定されたディレクトリを再帰的にコピーします。
    /// </summary>
    /// <param name="sourceDir">コピー元のディレクトリのパス。</param>
    /// <param name="destinationDir">コピー先のディレクトリのパス。</param>
    /// <exception cref="DirectoryNotFoundException">コピー元のディレクトリが見つからない場合にスローされます。</exception>
    private static void CopyDirectoryRecursively(string sourceDir, string destinationDir) {
        // Get information about the source directory
        var dir = new DirectoryInfo(sourceDir);

        // Check if the source directory exists
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        // Cache directories before we start copying
        DirectoryInfo[] dirs = dir.GetDirectories();

        // Create the destination directory
        Directory.CreateDirectory(destinationDir);

        // Get the files in the source directory and copy to the destination directory
        foreach (FileInfo file in dir.GetFiles()) {
            string targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath);
        }

        // If recursive and copying subdirectories, recursively call this method
        foreach (DirectoryInfo subDir in dirs) {
            string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
            CopyDirectoryRecursively(subDir.FullName, newDestinationDir);
        }
    }
}
