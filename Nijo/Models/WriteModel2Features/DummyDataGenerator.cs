using Nijo.Core;
using Nijo.Core.AggregateMemberTypes;
using Nijo.Models.RefTo;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Nijo.Core.AggregateMember;

namespace Nijo.Models.WriteModel2Features {
    /// <summary>
    /// デバッグ用ダミーデータ作成関数
    /// </summary>
    internal class DummyDataGenerator : ISummarizedFile {
        private readonly List<GraphNode<Aggregate>> _aggregates = new();
        internal void Add(GraphNode<Aggregate> aggregate) {
            _aggregates.Add(aggregate);
        }

        internal const string APPSRV_METHOD_NAME = "GenerateDummyData";

        void ISummarizedFile.OnEndGenerating(CodeRenderingContext context) {
            context.CoreLibrary.UtilDir(dir => {
                dir.Generate(RenderAppSrv());
            });
        }

        private SourceFile RenderAppSrv() {
            return new SourceFile {
                FileName = "GenerateDummyData.cs",
                RenderContent = ctx => {
                    var appsrv = new Parts.WebServer.ApplicationService();

                    return $$"""
                        #if DEBUG

                        namespace {{ctx.Config.RootNamespace}};

                        partial class {{appsrv.AbstractClassName}} {

                            /// <summary>
                            /// デバッグ用のダミーデータを作成します。
                            /// データベースはいずれのテーブルも空の前提です。
                            /// </summary>
                            /// <param name="count">作成するデータの数（集約単位）</param>
                            public virtual void {{APPSRV_METHOD_NAME}}(int count) {
                                if (count <= 0) throw new ArgumentOutOfRangeException();

                                BatchUpdateState saveResult;
                                var ctx = new DummyDataGeneratorContext(DbContext) {
                                    Random = new Random(0),
                                    GenerateCount = count,
                                    SaveOptions = new SaveOptions {
                                        IgnoreConfirm = true,
                                    },
                                };

                                // --------------------------------------------------
                                // データの流れの上流（参照される方）から順番にダミーデータを作成する
                        {{_aggregates.OrderByDataFlow().SelectTextTemplate(agg => $$"""

                                // {{agg.Item.DisplayName}}
                                saveResult = GenerateDummyDataOf{{agg.Item.PhysicalName}}(ctx);
                                if (saveResult.HasError()) {
                                    throw new InvalidOperationException($"{{agg.Item.DisplayName.Replace("\"", "\\\"")}}のダミーデータ作成でエラーが発生しました: {saveResult.GetErrorDataJson().ToJson()}");
                                }
                        """)}}
                            }

                        {{_aggregates.SelectTextTemplate(agg => $$"""
                            {{WithIndent(RenderAggregate(agg), "    ")}}
                        """)}}
                        }

                        {{WithIndent(RenderDummyDataGenerateContext(ctx), "")}}

                        #endif
                        """;
                },
            };
        }

        private static string RenderAggregate(GraphNode<Aggregate> rootAggregate) {
            var createData = new DataClassForSave(rootAggregate, DataClassForSave.E_Type.Create);
            var refTo = rootAggregate
                .EnumerateThisAndDescendants()
                .SelectMany(agg => agg.GetMembers().OfType<AggregateMember.Ref>())
                .Distinct();

            return $$"""
                /// <summary>
                /// {{rootAggregate.Item.DisplayName}} のデバッグ用ダミーデータを作成します。
                /// </summary>
                protected virtual BatchUpdateState GenerateDummyDataOf{{rootAggregate.Item.PhysicalName}}(DummyDataGeneratorContext ctx) {
                    var data = Enumerable
                        .Range(0, ctx.GenerateCount)
                        .Select(i => new CreateCommand<{{createData.CsClassName}}> {
                            Values = new {{createData.CsClassName}} {
                                {{WithIndent(RenderMembers(createData), "                ")}}
                            },
                        })
                        .ToArray();
                    return BatchUpdateWriteModels(data, ctx.SaveOptions);
                }
                """;

            IEnumerable<string> RenderMembers(DataClassForSave createData) {

                // 文字列系の項目のダミーデータに使われる文字列
                const string RANDOM_CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()_+-=[]{}\\\"|;:,.<>?";
                var RANDOM_CHARS_LENGTH = RANDOM_CHARS.Length;

                foreach (var member in createData.GetOwnMembers()) {
                    if (member is AggregateMember.ValueMember vm) {

                        var dummyValue = vm.Options.MemberType switch {
                            Core.AggregateMemberTypes.Boolean => "ctx.Random.Next(0, 1) == 0",
                            EnumList enumList => $$"""
                                new[] { {{enumList.Definition.Items.Select(x => $"{enumList.GetCSharpTypeName()}.{x.PhysicalName}").Join(", ")}} }[ctx.Random.Next(0, {{enumList.Definition.Items.Count - 1}})]
                                """,
                            Integer => $"ctx.Random.Next(0, 999999)",
                            Numeric => $"ctx.Random.Next(0, 999999) / 7m",
                            Sentence => $$"""
                                string.Concat(Enumerable
                                    .Range(0, ctx.Random.Next(0, {{vm.Options.MaxLength ?? 40}}))
                                    .Select(_ => ctx.Random.Next(10) == 0 ? Environment.NewLine : new string("{{RANDOM_CHARS}}"[ctx.Random.Next(0, {{RANDOM_CHARS_LENGTH - 1}})], 1)))
                                """,
                            Year => $"ctx.Random.Next(1990, 2040)",
                            YearMonth => $$"""
                                new {{Parts.WebServer.RuntimeYearMonthClass.CLASS_NAME}}(ctx.Random.Next(1900, 9999), ctx.Random.Next(1, 12))
                                """,
                            YearMonthDay => $$"""
                                new {{Parts.WebServer.RuntimeDateClass.CLASS_NAME}}(ctx.Random.Next(1900, 9999), ctx.Random.Next(1, 12), ctx.Random.Next(1, 28))
                                """,
                            YearMonthDayTime => $$"""
                                new DateTime((long)ctx.Random.Next(999999))
                                """,
                            Uuid => $"Guid.NewGuid().ToString()",
                            VariationSwitch => null, // Variationの分岐で処理済み
                            Word => $$"""
                                string.Concat(Enumerable.Range(0, {{vm.Options.MaxLength ?? 40}}).Select(_ => "{{RANDOM_CHARS}}"[ctx.Random.Next(0, {{RANDOM_CHARS_LENGTH - 1}})]))
                                """,
                            ValueObjectMember vo => $$"""
                                ({{vo.GetCSharpTypeName()}}?)string.Concat(Enumerable.Range(0, {{vm.Options.MaxLength ?? 40}}).Select(_ => "{{RANDOM_CHARS}}"[ctx.Random.Next(0, {{RANDOM_CHARS_LENGTH - 1}})]))
                                """,
                            _ => null, // 未定義
                        };
                        if (dummyValue != null) {
                            yield return $$"""
                                {{member.MemberName}} = {{dummyValue}},
                                """;
                        }

                    } else if (member is AggregateMember.Children children) {

                        // 主キーにref-toが含まれる場合、
                        // 参照先のデータの数よりもこの集約のデータの数の方が多いとき
                        // どう足掻いても登録エラーになるので明細の数は1件しか作成できない
                        var containsRefInPk = children.ChildrenAggregate
                            .EnumerateThisAndDescendants()
                            .Any(agg => agg.GetKeys().Any(m => m is AggregateMember.Ref));

                        var childrenClass = new DataClassForSave(children.ChildrenAggregate, DataClassForSave.E_Type.Create);
                        var childrenCount = containsRefInPk
                            ? "1"
                            : "ctx.GenerateCount";

                        yield return $$"""
                            {{member.MemberName}} = Enumerable.Range(0, {{childrenCount}}).Select(i => new {{childrenClass.CsClassName}} {
                                {{WithIndent(RenderMembers(childrenClass), "    ")}}
                            }).ToList(),
                            """;

                    } else if (member is AggregateMember.Ref @ref) {
                        var refTargetKey = new DataClassForRefTargetKeys(@ref.RefTo, @ref.RefTo);

                        yield return $$"""
                            {{member.MemberName}} = ctx.GetRefTargetKeyOf<{{refTargetKey.CsClassName}}>(i),
                            """;

                    } else if (member is AggregateMember.RelationMember rm) {
                        var childrenClass = new DataClassForSave(rm.MemberAggregate, DataClassForSave.E_Type.Create);
                        yield return $$"""
                            {{member.MemberName}} = new() {
                                {{WithIndent(RenderMembers(childrenClass), "    ")}}
                            },
                            """;

                    } else {
                        throw new NotImplementedException();
                    }
                }
            }
        }

        private string RenderDummyDataGenerateContext(CodeRenderingContext ctx) {
            // ほかの集約から参照される集約
            var referableAggregates = _aggregates
                .SelectMany(agg => agg.EnumerateThisAndDescendants())
                .Where(agg => agg.GetReferedEdges().Any());

            return $$"""
                /// <summary>
                /// デバッグ用のダミーデータ作成処理のみで使われる情報
                /// </summary>
                public class DummyDataGeneratorContext {
                    public DummyDataGeneratorContext({{ctx.Config.DbContextName}} dbContext) {
                        _dbContext = dbContext;
                    }

                    private readonly {{ctx.Config.DbContextName}} _dbContext;

                    public required int GenerateCount { get; init; }
                    public required Random Random { get; init; }
                    public required SaveOptions SaveOptions { get; init; }

                    /// <summary>
                    /// 外部参照のキーを取得する。
                    /// これの都合上、ダミーデータの作成はデータの流れの順番に実行される必要がある。
                    /// </summary>
                    public TRefTargetKey GetRefTargetKeyOf<TRefTargetKey>(int index) {
                        if (!_refTargetKeysCache.TryGetValue(typeof(TRefTargetKey), out var keys)) {
                #pragma warning disable CS8602
                {{referableAggregates.SelectTextTemplate(agg => $$"""
                            {{WithIndent(RenderRefToLoading(agg), "            ")}}
                """)}}
                #pragma warning restore CS8602
                            if (keys == null) {
                                throw new InvalidOperationException($"{typeof(TRefTargetKey).Name}型には外部参照先のキーの型を指定してください。");
                            }
                        }
                        return (TRefTargetKey)keys[index];
                    }
                    private readonly Dictionary<Type, object[]> _refTargetKeysCache = [];
                }
                """;

            string RenderRefToLoading(GraphNode<Aggregate> aggregate) {
                var asEntry = aggregate.AsEntry();
                var entity = new EFCoreEntity(asEntry);
                var refTargetKey = new DataClassForRefTargetKeys(asEntry, asEntry);
                var orderBy = asEntry
                    .GetKeys()
                    .OfType<AggregateMember.ValueMember>();

                return $$"""
                    if (typeof(TRefTargetKey) == typeof({{refTargetKey.CsClassName}})) {
                        keys = _dbContext.{{entity.DbSetName}}
                    {{orderBy.SelectTextTemplate((vm, i) => i == 0 ? $$"""
                            .OrderBy(e => e.{{vm.Declared.GetFullPathAsDbEntity().Join(".")}})    
                    """ : $$"""
                            .ThenBy(e => e.{{vm.Declared.GetFullPathAsDbEntity().Join(".")}})
                    """)}}
                            .Select(e => new {{refTargetKey.CsClassName}} {
                                {{WithIndent(RenderMembers(refTargetKey), "            ")}}
                            })
                            .Cast<object>()
                            .ToArray();
                        _refTargetKeysCache[typeof({{refTargetKey.CsClassName}})] = keys;
                    }
                    """;

                IEnumerable<string> RenderMembers(DataClassForRefTargetKeys refTargetKey) {
                    foreach (var key in refTargetKey.GetValueMembers()) {
                        yield return $$"""
                            {{key.MemberName}} = e.{{key.Member.Declared.GetFullPathAsDbEntity(since: aggregate).Join(".")}},
                            """;
                    }
                    foreach (var rm in refTargetKey.GetRelationMembers()) {
                        yield return $$"""
                            {{rm.MemberName}} = new() {
                                {{WithIndent(RenderMembers(rm), "    ")}}
                            },
                            """;
                    }
                }
            }
        }
    }
}
