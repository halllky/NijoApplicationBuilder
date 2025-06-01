using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Parts.CSharp;
using Nijo.Parts.UnitTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nijo.Models.QueryModelModules;

/// <summary>
/// 自動的に作成されるユニットテスト。
/// テスト観点は、検索処理を実行しただけでエラーになるといったことが無いかどうかを確かめる程度。
/// </summary>
internal class QueryModelUnitTest : IMultiAggregateSourceFile {

    private readonly List<RootAggregate> _rootAggregates = new();
    private readonly List<AggregateBase> _refEntries = new();
    private readonly Lock _lock = new();

    internal QueryModelUnitTest Add(RootAggregate rootAggregate) {
        lock (_lock) {
            _rootAggregates.Add(rootAggregate);
            return this;
        }
    }
    internal QueryModelUnitTest AddRefEntry(AggregateBase refEntry) {
        lock (_lock) {
            _refEntries.Add(refEntry);
            return this;
        }
    }

    void IMultiAggregateSourceFile.RegisterDependencies(IMultiAggregateSourceFileManager ctx) {
        // 特になし
    }

    void IMultiAggregateSourceFile.Render(CodeRenderingContext ctx) {
        ctx.UnitTestProject(dir => {
            dir.Generate(Render(ctx));
        });
    }

    private SourceFile Render(CodeRenderingContext ctx) {

        var orderedRefEntries = _refEntries
            .OrderBy(r => r.GetRoot().GetIndexOfDataFlow())
            .ThenBy(r => r.GetRoot().GetOrderInTree())
            .ToArray();

        return new SourceFile {
            FileName = "QueryModelTestCases.cs",
            Contents = $$"""
                using NUnit;
                using NUnit.Framework;

                namespace {{ctx.Config.RootNamespace}};

                /// <summary>
                /// ユニットテスト用のテストケース。
                /// 実際のテストは自動生成されない。このテストケースを呼び出して各自で実装する。
                /// </summary>
                public partial class QueryModelTestCases {

                    /// <summary>
                    /// 無条件検索（通常の一覧検索）
                    /// </summary>
                    public static IEnumerable<TestCaseData> 無条件検索テストケース() {
                {{If(_rootAggregates.Count == 0, () => $$"""
                        yield break;
                """).Else(() => $$"""
                {{_rootAggregates.OrderByDataFlow().SelectTextTemplate((root, index) => $$"""
                        // {{root.DisplayName}}
                        {{WithIndent(RenderSimpleLoadTestCase(root, index), "        ")}}

                """)}}
                """)}}
                    }

                    /// <summary>
                    /// 無条件検索（参照検索）
                    /// </summary>
                    public static IEnumerable<TestCaseData> 無条件外部参照検索テストケース() {
                {{If(orderedRefEntries.Length == 0, () => $$"""
                        yield break;
                """).Else(() => $$"""
                {{orderedRefEntries.SelectTextTemplate((refEntry, index) => $$"""
                        // {{refEntry.DisplayName}}
                        {{WithIndent(RenderSimpleRefLoadTestCase(refEntry, index), "        ")}}

                """)}}
                """)}}
                    }
                }
                """,
        };
    }

    private static string RenderSimpleLoadTestCase(RootAggregate rootAggregate, int index) {
        var searchCondition = new SearchCondition.Entry(rootAggregate);
        var searchConditionMessage = new SearchConditionMessageContainer(rootAggregate);

        return $$"""
            yield return new TestCaseData(
                $"{{rootAggregate.DisplayName.Replace("\"", "\\\"")}}",
                new Func<{{TestUtil.INTERFACE_UTIL}}, Task<IEnumerable<object>>>(async util => {
                    using var scope = util.CreateScope<{{searchConditionMessage.CsClassName}}>();
                    var searchCondition = new {{searchCondition.CsClassName}} {
                        {{SearchCondition.Entry.TAKE_CS}} = 10,
                    };
                    var result = await scope.App.{{SearchProcessing.LOAD_METHOD}}(searchCondition, scope.PresentationContext);
                    return result.{{SearchProcessingReturn.CURRENT_PAGE_ITEMS_CS}}.Cast<object>();
                }))
                .SetName($"無条件検索_{{rootAggregate.DisplayName.Replace("\"", "\\\"")}}");
            """;
    }

    private static string RenderSimpleRefLoadTestCase(AggregateBase refEntry, int index) {
        var searchCondition = new SearchCondition.Entry(refEntry.GetRoot());
        var searchConditionMessage = new SearchConditionMessageContainer(refEntry.GetRoot());
        var searchProcessingRefs = new SearchProcessingRefs(refEntry);

        return $$"""
            yield return new TestCaseData(
                $"{{refEntry.DisplayName.Replace("\"", "\\\"")}}",
                new Func<{{TestUtil.INTERFACE_UTIL}}, Task<IEnumerable<object>>>(async util => {
                    using var scope = util.CreateScope<{{searchConditionMessage.CsClassName}}>();
                    var searchCondition = new {{searchCondition.CsClassName}} {
                        {{SearchCondition.Entry.TAKE_CS}} = 10,
                    };
                    var result = await scope.App.{{searchProcessingRefs.LoadMethod}}(searchCondition, scope.PresentationContext);
                    return result.{{SearchProcessingReturn.CURRENT_PAGE_ITEMS_CS}}.Cast<object>();
                }))
                .SetName($"無条件Ref検索_{{refEntry.DisplayName.Replace("\"", "\\\"")}}");
            """;
    }
}
