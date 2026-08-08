using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Nijo.Util.DotnetEx;

public static class ProcessExtension {

    /// <summary>
    /// 実行ファイル名をOSの実行可能ファイル探索規則に従って解決する。
    /// <see cref="ProcessStartInfo.UseShellExecute"/> = false のとき、
    /// .NET は Windows の PATHEXT による拡張子解決を行わない
    /// （"npm" を指定しても実体の "npm.cmd" を見つけられない）ため、これを補う。
    /// Windows以外では入力をそのまま返す（シェルを介さずとも実行ファイルとして解決できるため）。
    /// </summary>
    public static string ResolveExecutablePath(string fileName) {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return fileName;
        if (string.IsNullOrEmpty(fileName) || Path.IsPathRooted(fileName)) return fileName;

        var pathExtensions = (Environment.GetEnvironmentVariable("PATHEXT") ?? ".COM;.EXE;.BAT;.CMD")
            .Split(';', StringSplitOptions.RemoveEmptyEntries);
        var searchDirectories = (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

        // 拡張子が既に指定されている場合はそのファイル名のみを探す。無指定ならPATHEXTの全候補を試す
        var candidateNames = Path.HasExtension(fileName)
            ? [fileName]
            : pathExtensions.Select(ext => fileName + ext).ToArray();

        foreach (var directory in searchDirectories) {
            foreach (var candidateName in candidateNames) {
                var candidatePath = Path.Combine(directory, candidateName);
                if (File.Exists(candidatePath)) return candidatePath;
            }
        }

        // 見つからなければ元の指定のまま返し、以降の解決は Process.Start に委ねる
        return fileName;
    }

    /// <summary>
    /// 既定のブラウザでURLを開く。
    /// プロセス起動という副作用を持つため厳密には「純粋な処理」ではないが、
    /// OS間差異の吸収という点で Util.DotnetEx に置く。
    /// </summary>
    public static void OpenBrowser(string url) {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Process.Start(new ProcessStartInfo {
                FileName = "cmd",
                Arguments = $"/c \"start {url}\"",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            });
        } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
            Process.Start(new ProcessStartInfo {
                FileName = "open",
                Arguments = url,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            });
        } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BROWSER"))) {
            Process.Start(new ProcessStartInfo {
                FileName = Environment.GetEnvironmentVariable("BROWSER")!,
                Arguments = url,
                UseShellExecute = false,
            });
        } else {
            Console.Error.WriteLine(
                $"このOSではブラウザを自動起動できません。" +
                $"手動で次のURLを開いてください: {url}");
        }
    }
}
