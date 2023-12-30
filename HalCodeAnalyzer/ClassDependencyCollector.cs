using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;

namespace HalCodeAnalyzer {
    internal class ClassDependencyCollector : CSharpSyntaxWalker {

        public required Solution _solution;
        public required Compilation _compilation;
        public event EventHandler<string>? OnLogout;
        public event EventHandler<NodeId>? AddGraphNode;
        public event EventHandler<GraphEdgeInfo>? AddGraphEdge;

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node) {
            var semanticModel = _compilation.GetSemanticModel(node.SyntaxTree);
            var method = semanticModel.GetDeclaredSymbol(node);
            if (method == null) return;

            var task = SymbolFinder.FindCallersAsync(method, _solution);
            task.Wait();
            foreach (var @ref in task.Result) {
                // そのメソッド自身からの参照は割愛
                if (@ref.CallingSymbol.Equals(method, SymbolEqualityComparer.Default)) continue;

                var initial = new NodeId(@ref.CallingSymbol.Name, GetClass(@ref.CallingSymbol.ContainingType) ?? NodeGroup.Root);
                var terminal = new NodeId(method.Name, GetClass(method.ContainingType) ?? NodeGroup.Root);
                OnLogout?.Invoke(this, $"{terminal}\t<-\t{initial}");
                AddGraphNode?.Invoke(this, initial);
                AddGraphNode?.Invoke(this, terminal);
                AddGraphEdge?.Invoke(this, new GraphEdgeInfo {
                    Initial = initial,
                    Terminal = terminal,
                    RelationName = string.Empty,
                });
            }
        }
        /// <summary>
        /// クラスを表すシンボルをNodeGroup型で取得
        /// </summary>
        private static NodeGroup? GetClass(ISymbol? symbol) {
            if (symbol == null) return null;

            var classSymbol
                = (symbol as INamedTypeSymbol)
                ?? symbol.ContainingType;
            if (classSymbol == null) return null;

            var classFullname = new List<string>();
            classFullname.AddRange(GetFullNameSpace(classSymbol.ContainingNamespace));
            classFullname.Add(classSymbol.Name);

            return new NodeGroup(classFullname);
        }
        /// <summary>
        /// その式が記述されているクラスをNodeId型で取得
        /// </summary>
        private static NodeId? GetDescribingClass(ExpressionSyntax node, SemanticModel semanticModel) {
            var callerClassSyntax = node.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (callerClassSyntax == null) return null;

            var callerClass = semanticModel.GetDeclaredSymbol(callerClassSyntax);
            if (callerClass == null) return null;

            var classFullname = new List<string>();
            classFullname.AddRange(GetFullNameSpace(callerClass.ContainingNamespace));
            classFullname.Add(callerClass.Name);

            return new NodeId(classFullname);
        }

        /// <summary>
        /// 名前空間をフルネームで取得
        /// </summary>
        private static IEnumerable<string> GetFullNameSpace(INamespaceSymbol nsSymbol) {
            if (nsSymbol.ContainingNamespace != null) {
                foreach (var ancestor in GetFullNameSpace(nsSymbol.ContainingNamespace)) {
                    yield return ancestor;
                }
            }
            if (!string.IsNullOrWhiteSpace(nsSymbol.Name)) {
                yield return nsSymbol.Name;
            }
        }

        //public override void VisitIdentifierName(IdentifierNameSyntax node) {
        //    var semanticModel = _compilation.GetSemanticModel(node.SyntaxTree);
        //    var nodeSymbol = Resolve(semanticModel.GetSymbolInfo(node));
        //    var callerClass = GetDescribingClass(node, semanticModel);
        //    var targetClass = GetClass(nodeSymbol);

        //    if (nodeSymbol?.Kind == SymbolKind.NamedType
        //        && callerClass != null
        //        && targetClass != null) {

        //        AddGraphNode?.Invoke(this, callerClass);
        //        AddGraphNode?.Invoke(this, targetClass);
        //        AddGraphEdge?.Invoke(this, new GraphEdgeInfo {
        //            Initial = callerClass,
        //            Terminal = targetClass,
        //            RelationName = string.Empty,
        //        });
        //    }

        //    //// デバッグ用
        //    //var text = node.GetText().ToString().Replace(Environment.NewLine, "");
        //    //OnLogout?.Invoke(this,
        //    //    $"{nodeSymbol?.Kind}\t{callerClass}\t==>\t{targetClass}\t{text}");
        //}

        ///// <summary>
        ///// メソッド呼び出しによる依存
        ///// </summary>
        //public override void VisitInvocationExpression(InvocationExpressionSyntax node) {
        //    var semanticModel = _compilation.GetSemanticModel(node.SyntaxTree);

        //    var method = Resolve(semanticModel.GetSymbolInfo(node));
        //    var callerClass = GetDescribingClass(node, semanticModel);
        //    var targetClass = GetClass(method?.ContainingType);

        //    if (method != null
        //        && callerClass != null
        //        && targetClass != null) {

        //        AddGraphNode?.Invoke(this, callerClass);
        //        AddGraphNode?.Invoke(this, targetClass);
        //        AddGraphEdge?.Invoke(this, new GraphEdgeInfo {
        //            Initial = callerClass,
        //            Terminal = targetClass,
        //            RelationName = method.Name,
        //        });
        //    }

        //    //// デバッグ用

        //    //OnLogout?.Invoke(this,
        //    //    $"呼出: {callerTypeFullName.Join(".")},  " +
        //    //    $"宣言: {declaringTypeFullName.Join(".")}.{methodName}");

        //    //var text = node
        //    //    .GetText()
        //    //    .ToString()
        //    //    .Trim()
        //    //    .Replace(Environment.NewLine, string.Empty);
        //    //OnLogout?.Invoke(this,
        //    //    $"{caller ?? "呼出クラス不明"}" +
        //    //    $"\t== {methodName ?? "メソッド名不明"} ==>" +
        //    //    $"\t{declaringTypeName ?? "定義クラス不明"}" +
        //    //    $"\t{warning}" +
        //    //    $"\t{node.GetLocation()}" +
        //    //    $"\t{text}");

        //    //// 要素をまるごとエクスポートする処理のテンプレ
        //    //OnLogout?.Invoke(this, $$"""
        //    //    -------------------------- VisitInvocationExpression --------------------------
        //    //    {{node.GetText()}}
        //    //    """);
        //}

        //private ISymbol? Resolve(SymbolInfo symbolInfo) {
        //    if (symbolInfo.Symbol != null) {
        //        return symbolInfo.Symbol;

        //    } else if (symbolInfo.CandidateSymbols.Length == 1) {
        //        return symbolInfo.CandidateSymbols.Single();

        //    } else if (symbolInfo.CandidateSymbols.Length >= 2) {
        //        // インターフェース経由でのメソッド呼び出しやオーバーロードがある場合はシンボルが一意に決まらず候補シンボルの配列に入る
        //        //OnLogout?.Invoke(this, $"シンボル解決困難(候補{symbolInfo.CandidateSymbols.Length}個)({symbolInfo.CandidateReason})。");
        //        return symbolInfo.CandidateSymbols.First();

        //    } else {
        //        return null;
        //    }
        //}
    }
}
