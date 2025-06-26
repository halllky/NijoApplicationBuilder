using System;
using System.Collections.Generic;
using System.Linq;
using Nijo.CodeGenerating;
using Nijo.CodeGenerating.Helpers;
using Nijo.ImmutableSchema;
using Nijo.Models.DataModelModules;
using Nijo.Util.DotnetEx;
using Nijo.ValueMemberTypes;

namespace Nijo.Parts.Common;

/// <summary>
/// nijo ui のデータプレビューのためのDataModelのメタデータ
/// </summary>
internal class MetadataForDataPreview : IMultiAggregateSourceFile {

    private readonly List<RootAggregate> _rootAggregates = new();
    internal MetadataForDataPreview Register(RootAggregate rootAggregate) {
        _rootAggregates.Add(rootAggregate);
        return this;
    }

    void IMultiAggregateSourceFile.RegisterDependencies(IMultiAggregateSourceFileManager ctx) {
        // 特になし
    }

    void IMultiAggregateSourceFile.Render(CodeRenderingContext ctx) {
        ctx.CoreLibrary(dir => {
            dir.Directory("Util", utilDir => {
                utilDir.Generate(RenderCSharp(ctx));
            });
        });

        ctx.ReactProject(dir => {
            dir.Directory("util", utilDir => {
                utilDir.Generate(RenderTypeScriptType());
            });
        });
    }

    private SourceFile RenderCSharp(CodeRenderingContext ctx) {
        var efCoreEntities = _rootAggregates
            .OrderByDataFlow()
            .Select(agg => new EFCoreEntity(agg));

        return new SourceFile {
            FileName = "DataModelMetadata.cs",
            Contents = $$"""
                using System.Collections.Generic;
                using System.Text.Json.Serialization;

                namespace {{ctx.Config.RootNamespace}};

                /// <summary>
                /// DataModelのメタデータ
                /// </summary>
                public class MetadataForDataPreview {
                    /// <summary>
                    /// データフローの上流から順番にデータモデルの集約を列挙する。
                    /// </summary>
                    public static IEnumerable<Aggregate> EnumerateDataModelsOrderByDataFlow() {
                {{efCoreEntities.SelectTextTemplate(agg => $$"""
                        yield return {{WithIndent(RenderAggregate(agg), "        ")}};
                """)}}
                    }

                    #region 型
                    /// <summary>
                    /// 集約。ルート集約、Child, Children のいずれか。
                    /// </summary>
                    public class Aggregate : IAggregateMember {
                        /// <summary>
                        /// "root", "child", "children" のいずれか。
                        /// </summary>
                        [JsonPropertyName("type")]
                        public required string Type { get; set; }
                        /// <summary>
                        /// この集約の物理名のパス。この集約がChild, Children の場合はルート集約からのスラッシュ区切り。
                        /// </summary>
                        [JsonPropertyName("path")]
                        public required string Path { get; set; }
                        /// <summary>
                        /// 直近の親集約のパス。直近の親がルート集約でない場合はスラッシュ区切り。
                        /// この集約がルート集約の場合はnull。
                        /// </summary>
                        [JsonPropertyName("parentAggregatePath")]
                        public required string? ParentAggregatePath { get; set; }
                        [JsonPropertyName("physicalName")]
                        public required string PhysicalName { get; set; }
                        [JsonPropertyName("displayName")]
                        public required string DisplayName { get; set; }
                        [JsonPropertyName("tableName")]
                        public required string TableName { get; set; }
                        [JsonPropertyName("description")]
                        public required string Description { get; set; }
                        [JsonPropertyName("members")]
                        public required List<IAggregateMember> Members { get; set; }
                    }
                    /// <summary>
                    /// 集約のメンバー
                    /// </summary>
                    public interface IAggregateMember {
                        string Type { get; }
                    }
                    /// <summary>
                    /// 値メンバー。DBのカラムに対応する。文字列、数値、日付など。
                    /// このテーブル自身に定義されたカラム、親テーブルの主キー、外部参照先テーブルの主キーのいずれか。
                    /// </summary>
                    public class ValueMember : IAggregateMember {
                        /// <summary>
                        /// "own-column", "parent-key", "ref-key" のいずれか。
                        /// </summary>
                        [JsonPropertyName("type")]
                        public required string Type { get; set; }
                        [JsonPropertyName("physicalName")]
                        public required string PhysicalName { get; set; }
                        [JsonPropertyName("displayName")]
                        public required string DisplayName { get; set; }
                        [JsonPropertyName("columnName")]
                        public required string ColumnName { get; set; }
                        [JsonPropertyName("description")]
                        public required string Description { get; set; }
                        /// <summary>
                        /// 値メンバーの型名。XMLスキーマ定義上の型名。
                        /// </summary>
                        [JsonPropertyName("typeName")]
                        public required string TypeName { get; set; }
                        /// <summary>
                        /// 列挙体種類名。
                        /// このメンバーが列挙体でない場合はnull。
                        /// </summary>
                        [JsonPropertyName("enumType")]
                        public required string? EnumType { get; set; }

                        [JsonPropertyName("isPrimaryKey")]
                        public required bool IsPrimaryKey { get; set; }
                        [JsonPropertyName("isNullable")]
                        public required bool IsNullable { get; set; }

                        /// <summary>
                        /// 外部参照先とこの集約の関係性の名前。
                        /// テーブルAからBへ複数の参照経路がある場合にそれらの識別に用いる。
                        /// このメンバーがref-keyでない場合はnull。
                        /// </summary>
                        [JsonPropertyName("refToRelationName")]
                        public required string? RefToRelationName { get; set; }
                        /// <summary>
                        /// 外部参照先テーブルのルート集約からのパス（スラッシュ区切り）。
                        /// このメンバーがref-keyでない場合はnull。
                        /// </summary>
                        [JsonPropertyName("refToAggregatePath")]
                        public required string? RefToAggregatePath { get; set; }
                        /// <summary>
                        /// このメンバーと対応する、外部参照先テーブルのメンバーのDB上のカラム名。
                        /// このメンバーがref-keyでない場合はnull。
                        /// </summary>
                        [JsonPropertyName("refToColumnName")]
                        public required string? RefToColumnName { get; set; }
                    }
                    #endregion 型
                }
                """,
        };

        static string RenderAggregate(EFCoreEntity entity) {
            var type = entity.Aggregate switch {
                RootAggregate => "root",
                ChildAggregate => "child",
                ChildrenAggregate => "children",
                _ => throw new InvalidOperationException(),
            };
            var path = entity.Aggregate.EnumerateThisAndAncestors().Select(a => a.PhysicalName).Join("/");
            var parentPath = entity.Aggregate is RootAggregate
                ? "null"
                : entity.Aggregate.EnumerateAncestors().Select(a => a.PhysicalName).Join("/");

            return $$"""
                new Aggregate {
                    Type = "{{type}}",
                    Path = "{{path}}",
                    ParentAggregatePath = "{{parentPath}}",
                    PhysicalName = "{{entity.Aggregate.PhysicalName}}",
                    DisplayName = "{{entity.Aggregate.DisplayName.Replace("\"", "\\\"")}}",
                    TableName = "{{entity.Aggregate.DbName}}",
                    Description = "{{entity.Aggregate.GetComment(E_CsTs.CSharp).Replace("\"", "\\\"")}}",
                    Members = new List<IAggregateMember> {
                {{RenderMembers(entity).OrderBy(x => x.Order).SelectTextTemplate(x => $$"""
                        {{WithIndent(x.SourceCode, "        ")}},
                """)}}
                    },
                }
                """;
        }
        static IEnumerable<(string SourceCode, decimal Order)> RenderMembers(EFCoreEntity entity) {

            // カラムを列挙
            foreach (var column in entity.GetColumns()) {
                var type = column switch {
                    EFCoreEntity.OwnColumnMember => "own-column",
                    EFCoreEntity.ParentKeyMember => "parent-key",
                    EFCoreEntity.RefKeyMember refTo => refTo.IsParentKey
                        ? "parent-key" // 親かつ外部参照のキーのとき、ソースコード自動生成ではそれを
                                       // 外部参照のキーとした方が都合がよいのでRefKeyMemberになっているが、
                                       // メタデータとしては親キーとして扱う。
                        : "ref-key",
                    _ => throw new InvalidOperationException(),
                };
                // 列挙体種類名はJavaScriptで引き当てる際に使用するためTS型名を使用
                var enumType = column.Member.Type is StaticEnumMember staticEnumMember
                    ? $"\"{staticEnumMember.Definition.TsTypeName.Replace("\"", "\\\"")}\""
                    : "null";

                string? refToRelationName = null;
                string? refToAggregatePath = null;
                string? refToColumnName = null;
                if (column is EFCoreEntity.RefKeyMember refKeyMember && !refKeyMember.IsParentKey) {
                    refToRelationName = $"\"{refKeyMember.RefEntry.DisplayName.Replace("\"", "\\\"")}\"";

                    refToAggregatePath = $"\"{refKeyMember.RefEntry.RefTo.EnumerateThisAndAncestors().Select(a => a.PhysicalName).Join("/")}\"";

                    var mappingKey = column.Member.ToMappingKey();
                    var refToColumns = new EFCoreEntity(refKeyMember.RefEntry.RefTo).GetColumns();
                    refToColumnName = $"\"{refToColumns.First(c => c.Member.ToMappingKey() == mappingKey).DbName}\"";
                } else {
                    refToRelationName = "null";
                    refToAggregatePath = "null";
                    refToColumnName = "null";
                }

                yield return ($$"""
                    new ValueMember {
                        Type = "{{type}}",
                        PhysicalName = "{{column.PhysicalName}}",
                        DisplayName = "{{column.DisplayName.Replace("\"", "\\\"")}}",
                        ColumnName = "{{column.DbName}}",
                        Description = "{{column.Member.GetComment(E_CsTs.CSharp).Replace("\"", "\\\"")}}",
                        TypeName = "{{column.Member.Type.SchemaTypeName}}",
                        EnumType = {{enumType}},
                        IsPrimaryKey = {{(column.IsKey ? "true" : "false")}},
                        IsNullable = {{(!column.IsKey && !column.Member.IsRequired ? "true" : "false")}},
                        RefToRelationName = {{refToRelationName}},
                        RefToAggregatePath = {{refToAggregatePath}},
                        RefToColumnName = {{refToColumnName}},
                    }
                    """, column.Member.Order);
            }

            // 子テーブルを列挙
            foreach (var member in entity.Aggregate.GetMembers()) {
                if (member is ChildAggregate child) {
                    yield return (RenderAggregate(new EFCoreEntity(child)), child.Order);

                } else if (member is ChildrenAggregate children) {
                    yield return (RenderAggregate(new EFCoreEntity(children)), children.Order);

                } else {
                    // 無視
                }
            }
        }
    }

    /// <summary>
    /// C#側でレンダリングされた型と対応するTypeScriptの型定義。
    /// アプリケーションテンプレートにレンダリングされたものを nijo ui で使用するというトリッキーな参照のされ方をする。
    /// </summary>
    private static SourceFile RenderTypeScriptType() {
        return new SourceFile {
            FileName = "data-model-metadata.ts",
            Contents = $$"""
                /** DataModelのメタデータ */
                export namespace DataModelMetadata {

                  /** 集約 */
                  export type Aggregate = {
                    type: "root" | "child" | "children"
                    /**
                     * この集約の物理名のパス。この集約がChild, Children の場合はルート集約からのスラッシュ区切り。
                     */
                    path: string
                    /**
                     * 直近の親集約のパス。直近の親がルート集約でない場合はスラッシュ区切り。
                     * この集約がルート集約の場合はnull。
                     */
                    parentAggregatePath: string | null
                    physicalName: string
                    displayName: string
                    tableName: string
                    description: string
                    members: (AggregateMember | Aggregate)[]
                  }

                  /** 集約のメンバー */
                  export type AggregateMember = {
                    type: "own-column" | "parent-key" | "ref-key"
                    physicalName: string
                    displayName: string
                    columnName: string
                    description: string
                    /**
                     * 値メンバーの型名。XMLスキーマ定義上の型名。
                     */
                    typeName: string
                    /**
                     * 列挙体種類名。
                     * このメンバーが列挙体でない場合はnull。
                     */
                    enumType: string | null
                    isPrimaryKey: boolean
                    isNullable: boolean
                    /**
                     * 外部参照先とこの集約の関係性の名前。
                     * テーブルAからBへ複数の参照経路がある場合にそれらの識別に用いる。
                     * このメンバーがref-keyでない場合はnull。
                     */
                    refToRelationName: string | null
                    /**
                     * 外部参照先テーブルのルート集約からのパス（スラッシュ区切り）。
                     * このメンバーがref-keyでない場合はnull。
                     */
                    refToAggregatePath: string | null
                    /**
                     * このメンバーと対応する、外部参照先テーブルのメンバーのDB上のカラム名。
                     * このメンバーがref-keyでない場合はnull。
                     */
                    refToColumnName: string | null
                  }
                }
                """,
        };
    }
}