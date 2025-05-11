using System;
using System.IO;

namespace Nijo.Util.DotnetEx;

/// <summary>
/// ディレクトリ操作に関するヘルパーメソッドを提供します。
/// </summary>
public static class DirectoryHelper {
    /// <summary>
    /// 指定されたディレクトリを再帰的にコピーします。
    /// </summary>
    /// <param name="sourceDir">コピー元のディレクトリのパス。</param>
    /// <param name="destinationDir">コピー先のディレクトリのパス。</param>
    /// <exception cref="DirectoryNotFoundException">コピー元のディレクトリが見つからない場合にスローされます。</exception>
    public static void CopyDirectoryRecursively(string sourceDir, string destinationDir) {
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
