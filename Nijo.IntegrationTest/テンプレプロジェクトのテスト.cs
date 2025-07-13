using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Nijo.SchemaParsing;
using NUnit.Framework;

namespace Nijo.IntegrationTest;

[Category("テンプレプロジェクトのテスト")]
public class テンプレプロジェクトのテスト {

    [Test]
    public void テンプレプロジェクトの自動生成でエラーが出ないか() {
        var workspaceRoot = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory, // exeがあるフォルダ (net9.0)
            "..", // Debug
            "..", // bin
            "..", // Nijo.IntegrationTest
            "..", // プロジェクトルート
            "Nijo.ApplicationTemplate.Ver1"));

        if (!GeneratedProject.TryOpen(workspaceRoot, out var project, out var error)) {
            Assert.Fail($"プロジェクトフォルダを開くのに失敗しました: {error}");
            return;
        }

        var logger = new ConsoleLogger();
        var schemaXml = XDocument.Load(project.SchemaXmlPath);
        var parseContext = new SchemaParseContext(schemaXml, SchemaParseRule.Default());
        var result = project.GenerateCode(parseContext, new() {
            AllowNotImplemented = true,
        }, logger);

        if (!result) {
            Assert.Fail($"ソースコード自動生成に失敗しました。");
            return;
        }

        Assert.Pass("ソースコード自動生成に成功しました。");
    }

}
