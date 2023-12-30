using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalCodeAnalyzer {
    internal class ClassDependencyCollector : CSharpSyntaxWalker {

        public required Compilation _compilation;
        public event EventHandler<string>? OnLogout;
        public event EventHandler<NodeId>? AddGraphNode;
        public event EventHandler<GraphEdgeInfo>? AddGraphEdge;

        /// <summary>
        /// メソッド呼び出しによる依存
        /// </summary>
        public override void VisitInvocationExpression(InvocationExpressionSyntax node) {
            var semanticModel = _compilation.GetSemanticModel(node.SyntaxTree);
            var symbolInfo = semanticModel.GetSymbolInfo(node);

            // インターフェース経由でのメソッド呼び出しやオーバーロードがある場合はシンボルが一意に決まらず候補シンボルの配列に入る
            ISymbol? methodSymbol;
            var warning = string.Empty;
            if (symbolInfo.Symbol != null) {
                methodSymbol = symbolInfo.Symbol;
            } else if (symbolInfo.CandidateSymbols.Length == 1) {
                methodSymbol = symbolInfo.CandidateSymbols.Single();
            } else if (symbolInfo.CandidateSymbols.Length >= 2) {
                methodSymbol = symbolInfo.CandidateSymbols.First();
                warning += $"シンボル解決困難(候補{symbolInfo.CandidateSymbols.Length}個)({symbolInfo.CandidateReason})。";
            } else {
                methodSymbol = null;
            }

            static IEnumerable<string> GetNamespace(INamespaceSymbol nsSymbol) {
                if (nsSymbol.ContainingNamespace != null) {
                    foreach (var ancestor in GetNamespace(nsSymbol.ContainingNamespace)) {
                        yield return ancestor;
                    }
                }
                if (!string.IsNullOrWhiteSpace(nsSymbol.Name)) {
                    yield return nsSymbol.Name;
                }
            }

            var callerClassSyntax = node.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            var callerClass = callerClassSyntax != null
                ? semanticModel.GetDeclaredSymbol(callerClassSyntax)
                : null;
            var methodName = methodSymbol?.Name;

            var callerTypeFullName = new List<string>();
            var declaringTypeFullName = new List<string>();
            if (callerClass != null) {
                callerTypeFullName.AddRange(GetNamespace(callerClass.ContainingNamespace));
                callerTypeFullName.Add(callerClass.Name);
            }
            if (methodSymbol != null) {
                declaringTypeFullName.AddRange(GetNamespace(methodSymbol.ContainingType.ContainingNamespace));
                declaringTypeFullName.Add(methodSymbol.ContainingType.Name);
            }

            //OnLogout?.Invoke(this,
            //    $"呼出: {callerTypeFullName.Join(".")},  " +
            //    $"宣言: {declaringTypeFullName.Join(".")}.{methodName}");

            if (methodName != null
                && callerTypeFullName.Any()
                && declaringTypeFullName.Any()) {

                var initial = new NodeId(callerTypeFullName);
                var terminal = new NodeId(declaringTypeFullName);
                AddGraphNode?.Invoke(this, initial);
                AddGraphNode?.Invoke(this, terminal);
                AddGraphEdge?.Invoke(this, new GraphEdgeInfo {
                    Initial = initial,
                    Terminal = terminal,
                    RelationName = methodName,
                });
            }

            //// デバッグ用
            //var text = node
            //    .GetText()
            //    .ToString()
            //    .Trim()
            //    .Replace(Environment.NewLine, string.Empty);
            //OnLogout?.Invoke(this,
            //    $"{caller ?? "呼出クラス不明"}" +
            //    $"\t== {methodName ?? "メソッド名不明"} ==>" +
            //    $"\t{declaringTypeName ?? "定義クラス不明"}" +
            //    $"\t{warning}" +
            //    $"\t{node.GetLocation()}" +
            //    $"\t{text}");

            //// 要素をまるごとエクスポートする処理のテンプレ
            //OnLogout?.Invoke(this, $$"""
            //    -------------------------- VisitInvocationExpression --------------------------
            //    {{node.GetText()}}
            //    """);
        }
    }
}
