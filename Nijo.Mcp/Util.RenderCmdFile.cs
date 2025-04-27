using System.Text;

namespace Nijo.Mcp;

partial class NijoMcpTools {
    /// <summary>
    /// .cmd ファイルを、文字コードや行末処理を加えたうえで出力する。
    /// </summary>
    /// <param name="cmdFilePath">ファイルパス</param>
    /// <param name="cmdFileContent">ファイルの内容</param>
    private static void RenderCmdFile(string cmdFilePath, string cmdFileContent) {
        File.WriteAllText(
            cmdFilePath,
            // cmd処理中にchcpしたときは各行の改行コードの前にスペースが無いと上手く動かないので
            cmdFileContent.ReplaceLineEndings(" \r\n"),
            new UTF8Encoding(false, false));
    }
}

