using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(consoleLogOptions => {
    // Configure all logs to go to stderr
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();
var host = builder.Build();
await host.RunAsync();

namespace DotnetMcp {

    /// <summary>
    /// この MCP (Model Context Protocol) ツールは、
    /// AIエージェントが.NETのソリューションに関するタスクの遂行の精度を上げるためのものです。
    /// 予めソリューションのパスを設定ファイル（json）に記述し、このツールを起動することで、
    /// AIエージェントはそのソリューションに関する情報を柔軟に参照することができます。
    /// </summary>
    [McpServerToolType]
    public static partial class DotnetMcpTools {

        [McpServerTool(Name = "find_definition"), Description(
            "とあるC#のファイルの特定の位置に記載されている変数、関数、クラス、プロパティといった" +
            "シンボルの定義情報がどのファイルに記載されているかを探して返します。")]
        public static async Task<string> FindDefinition(
            [Description("ソースコードのファイルパス。絶対パスで指定すること。")] string sourceFilePath,
            [Description("ソースコードの何行目か")] string line,
            [Description("ソースコードの当該行の何文字目か")] string character) {

            // 引数のチェック
            if (string.IsNullOrEmpty(sourceFilePath)) {
                return $"{nameof(sourceFilePath)}は空文字列ではいけません。";
            }
            if (!Path.IsPathRooted(sourceFilePath)) {
                return $"{nameof(sourceFilePath)}は絶対パスで指定してください。";
            }
            if (!int.TryParse(line, out var lineNumber)) {
                return $"{nameof(line)}は整数で指定してください。指定された値: {line}";
            }
            if (!int.TryParse(character, out var columnNumber)) {
                return $"{nameof(character)}は整数で指定してください。指定された値: {character}";
            }

            if (!TrySetup(out var ctx, out var error)) return error;

            try {
                using var workSpace = MSBuildWorkspace.Create();
                var solution = await workSpace.OpenSolutionAsync(ctx.SolutionFileFullPath);
                var documents = solution.Projects.SelectMany(p => p.Documents).ToArray();

                // ドキュメントを探す
                var document = documents.FirstOrDefault(d => d.FilePath == sourceFilePath)
                    ?? throw new InvalidOperationException($"指定されたファイルが見つかりません: {sourceFilePath}");

                // ソースコード上の該当位置のシンタックスノードを探す
                var linePosition = new LinePosition(lineNumber - 1, columnNumber - 1);
                var syntaxTree = await document.GetSyntaxTreeAsync()
                    ?? throw new InvalidOperationException($"ドキュメントのシンタックスツリーを取得できません: {sourceFilePath}");
                var position = syntaxTree.GetText().Lines.GetPosition(linePosition);
                var syntaxNode = syntaxTree.GetRoot().FindToken(position).Parent
                    ?? throw new InvalidOperationException($"シンボルを取得できません: {sourceFilePath}, {lineNumber}, {columnNumber}");

                // シンタックスノードからシンボルを取得
                var semanticModel = await document.GetSemanticModelAsync()
                    ?? throw new InvalidOperationException($"セマンティックモデルを取得できません: {sourceFilePath}");
                var symbol = semanticModel.GetDeclaredSymbol(syntaxNode)
                    ?? semanticModel.GetSymbolInfo(syntaxNode).Symbol
                    ?? throw new InvalidOperationException($"シンボルを取得できません: {sourceFilePath}, {lineNumber}, {columnNumber}");

                // シンボルの定義情報を取得する。クラスはpartialで定義されている場合があるため、複数ある場合はすべて取得する。
                var definition = symbol.DeclaringSyntaxReferences.Select(r => r.GetSyntax()).ToArray();

                // 定義情報のうち、以下を返す
                // * どのファイルで定義されているか
                // * そのファイルのどの位置で定義されているか
                var result = definition.Select(d => new {
                    d.SyntaxTree.FilePath,
                    Line = d.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    Column = d.GetLocation().GetLineSpan().StartLinePosition.Character + 1
                }).ToArray();

                // 見つからなかった場合
                if (result.Length == 0) {
                    return $$"""
                        シンボル {{symbol.Name}} の定義情報は見つかりませんでした。
                        ---
                        シンボル {{symbol.Name}} の詳細:
                        {{symbol.ToDisplayString()}}
                        """;
                }

                return $$"""
                    シンボル {{symbol.Name}} は以下のソースコードで定義されています。
                    {{string.Join("\r\n", result.Select(r => $"* {r.FilePath}: {r.Line}行目 {r.Column}文字目 付近"))}}
                    """;

            } catch (Exception ex) {
                ctx.WriteLog(ex.ToString());
                return ex.ToString();
            }
        }

        [McpServerTool(Name = "find_references"), Description(
            "とあるC#のファイルの特定の位置に記載されている変数、関数、クラス、プロパティ等のシンボルについて、" +
            "そのシンボルを参照している箇所を一覧して返します。")]
        public static async Task<string> FindReferences(
            [Description("ソースコードのファイルパス。絶対パスで指定すること。")] string sourceFilePath,
            [Description("ソースコードの何行目か")] string line,
            [Description("ソースコードの当該行の何文字目か")] string character) {

            // 引数のチェック
            if (string.IsNullOrEmpty(sourceFilePath)) {
                return $"{nameof(sourceFilePath)}は空文字列ではいけません。";
            }
            if (!Path.IsPathRooted(sourceFilePath)) {
                return $"{nameof(sourceFilePath)}は絶対パスで指定してください。";
            }
            if (!int.TryParse(line, out var lineNumber)) {
                return $"{nameof(line)}は整数で指定してください。指定された値: {line}";
            }
            if (!int.TryParse(character, out var columnNumber)) {
                return $"{nameof(character)}は整数で指定してください。指定された値: {character}";
            }

            if (!TrySetup(out var ctx, out var error)) return error;

            try {
                using var workSpace = MSBuildWorkspace.Create();
                var solution = await workSpace.OpenSolutionAsync(ctx.SolutionFileFullPath);
                var documents = solution.Projects.SelectMany(p => p.Documents).ToArray();

                // ドキュメントを探す
                var document = documents.FirstOrDefault(d => d.FilePath == sourceFilePath)
                    ?? throw new InvalidOperationException($"指定されたファイルが見つかりません: {sourceFilePath}");

                // ソースコード上の該当位置のシンタックスノードを探す
                var linePosition = new LinePosition(lineNumber - 1, columnNumber - 1);
                var syntaxTree = await document.GetSyntaxTreeAsync()
                    ?? throw new InvalidOperationException($"ドキュメントのシンタックスツリーを取得できません: {sourceFilePath}");
                var position = syntaxTree.GetText().Lines.GetPosition(linePosition);
                var syntaxNode = syntaxTree.GetRoot().FindToken(position).Parent
                    ?? throw new InvalidOperationException($"シンボルを取得できません: {sourceFilePath}, {lineNumber}, {columnNumber}");

                // シンタックスノードからシンボルを取得
                var semanticModel = await document.GetSemanticModelAsync()
                    ?? throw new InvalidOperationException($"セマンティックモデルを取得できません: {sourceFilePath}");
                var symbol = semanticModel.GetDeclaredSymbol(syntaxNode)
                    ?? semanticModel.GetSymbolInfo(syntaxNode).Symbol
                    ?? throw new InvalidOperationException($"シンボルを取得できません: {sourceFilePath}, {lineNumber}, {columnNumber}");

                // シンボルを参照している箇所を一覧する
                var references = await SymbolFinder.FindReferencesAsync(symbol, solution);

                // 参照箇所のうち、以下を返す。複数個所から参照されている場合はその全ての情報を返す
                // * どのファイルで参照されているか
                // * そのファイルのどの位置で参照されているか
                var result = references.SelectMany(r => r.Locations).Select(r => new {
                    r.Location.SourceTree?.FilePath,
                    Line = r.Location.SourceTree?.GetText().Lines.GetLineFromPosition(r.Location.SourceSpan.Start).LineNumber + 1,
                    Column = r.Location.SourceTree?.GetText().Lines.GetLineFromPosition(r.Location.SourceSpan.Start).LineNumber + 1
                }).ToArray();

                // 見つからなかった場合
                if (result.Length == 0) {
                    return $$"""
                        シンボル {{symbol.Name}} はどこからも参照されていません。
                        ---
                        シンボル {{symbol.Name}} の詳細:
                        {{symbol.ToDisplayString()}}
                        """;
                }

                return $$"""
                    シンボル {{symbol.Name}} は以下のソースコードで参照されています。
                    {{string.Join("\r\n", result.Select(r => $"* {r.FilePath}: {r.Line}行目 {r.Column}文字目 付近"))}}
                    """;

            } catch (Exception ex) {
                ctx.WriteLog(ex.ToString());
                return ex.ToString();
            }
        }
    }
}
