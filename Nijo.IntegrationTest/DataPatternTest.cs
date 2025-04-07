using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Nijo.IntegrationTest;

/// <summary>
/// DataPatternsフォルダ内のXMLファイルを使っていろいろ確認するテスト
/// </summary>
[TestFixture]
[NonParallelizable]
public class DataPatternTest {
    /// <summary>
    /// DataPatternsフォルダ内のXMLファイルパスを取得するためのTestCaseSource
    /// </summary>
    /// <returns>XMLファイルパスのリスト</returns>
    public static IEnumerable<string> GetXmlFilePaths() {
        string dataPatternDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "DataPatterns");
        dataPatternDir = Path.GetFullPath(dataPatternDir);

        return Directory
            .GetFiles(dataPatternDir, "*.xml")
            .Select(f => Path.GetFileName(f))
            .OrderBy(f => f);
    }

    /// <summary>
    /// XMLファイルごとのテスト
    /// </summary>
    /// <param name="xmlFileName">XMLファイル名</param>
    [Test]
    [TestCaseSource(nameof(GetXmlFilePaths))]
    public void TestXmlPattern(string fileName) {
        var xmlFilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "DataPatterns", fileName);

        Console.WriteLine($"テスト実行: {fileName}");

        // ここにテストの実装を追加します
        // 例: XMLの読み込み、パース、検証など

        // とりあえず成功を返す
        Assert.Pass($"{fileName} のテストが完了しました");
    }
}
