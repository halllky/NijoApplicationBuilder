using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Nijo.CodeGenerating;
using Nijo.SchemaParsing;

namespace Nijo.WebService.DemoMode;

/// <summary>
/// nijo.xml からコードを再生成する(nijo自身のプロセス内呼び出しなので子プロセス不要)。
/// デモモードでは保存後の自動生成・リセット後の生成など複数箇所で使うため共通化している。
/// </summary>
public static class SchemaGenerateHelper {

    public static bool TryGenerate(string workspaceRoot, ILogger logger) {
        if (!GeneratedProject.TryOpen(workspaceRoot, out var project, out var error)) {
            logger.LogError("プロジェクトオープンに失敗しました: {error}", error);
            return false;
        }

        var xDocument = XDocument.Load(project.SchemaXmlPath);
        var rule = SchemaParseRule.Default();
        var parseContext = new SchemaParseContext(xDocument, rule, GeneratedProjectOptions.Parse(xDocument, true));
        var renderingOptions = new CodeRenderingOptions { AllowNotImplemented = false };

        if (!project.GenerateCode(parseContext, renderingOptions, logger)) {
            logger.LogError("コード生成に失敗しました。");
            return false;
        }
        return true;
    }
}
