namespace Nijo.Mcp;

partial class NijoMcpTools {

    /// <summary>
    /// 指定されたディレクトリを再帰的にコピーします。
    /// </summary>
    /// <param name="sourceDir">コピー元のディレクトリのパス。</param>
    /// <param name="destinationDir">コピー先のディレクトリのパス。</param>
    /// <param name="shouldCopy">コピーを行わない場合はfalseを返す関数。</param>
    /// <exception cref="DirectoryNotFoundException">コピー元のディレクトリが見つからない場合にスローされます。</exception>
    private static void CopyDirectoryRecursively(string sourceDir, string destinationDir, Func<string, bool> shouldCopy) {
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
            if (!shouldCopy(targetFilePath)) continue;
            file.CopyTo(targetFilePath);
        }

        // If recursive and copying subdirectories, recursively call this method
        foreach (DirectoryInfo subDir in dirs) {
            string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
            if (!shouldCopy(newDestinationDir)) continue;
            CopyDirectoryRecursively(subDir.FullName, newDestinationDir, shouldCopy);
        }
    }
}
