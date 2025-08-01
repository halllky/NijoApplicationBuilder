using Microsoft.EntityFrameworkCore;
using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Parts.CSharp;
using Nijo.Util.DotnetEx;
using Nijo.ValueMemberTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.DataModelModules {
    /// <summary>
    /// Entity Framework Core のエンティティ
    /// </summary>
    internal class EFCoreEntity : IInstancePropertyOwnerMetadata {

        internal EFCoreEntity(AggregateBase aggregate) {
            Aggregate = aggregate;
        }
        internal AggregateBase Aggregate { get; }

        internal string CsClassName => $"{Aggregate.PhysicalName}DbEntity";
        internal string DbSetName => $"{Aggregate.PhysicalName}DbSet";

        /// <summary>
        /// 楽観排他用のバージョンを持つかどうか
        /// </summary>
        private bool HasVersionColumn => Aggregate is RootAggregate;

        /// <summary>楽観排他制御用のバージョニング用カラムの名前</summary>
        internal const string VERSION = "Version";
        /// <summary>データが新規作成された日時</summary>
        internal const string CREATED_AT = "CreatedAt";
        /// <summary>データが更新された日時</summary>
        internal const string UPDATED_AT = "UpdatedAt";
        /// <summary>データを新規作成したユーザー</summary>
        internal const string CREATE_USER = "CreateUser";
        /// <summary>データを更新したユーザー</summary>
        internal const string UPDATE_USER = "UpdateUser";

        /// <summary>主キーが一致するかどうかを調べるメソッドの名前</summary>
        internal const string KEYEQUALS = "KeyEquals";

        internal string OnModelCreatingAutoGenerated => $"OnModelCreating{Aggregate.PhysicalName}";
        private const string ON_MODEL_CREATING_CUSTOMIZE = "OnModelCreating";

        /// <summary>
        /// <list type="bullet">
        /// <item>自身の<see cref="ValueMember"/>: 列挙する</item>
        /// <item>親のキー: 列挙する</item>
        /// <item>参照先のキー: 列挙する</item>
        /// <item>Child, Children: 列挙しない</item>
        /// <item>ナビゲーションプロパティ: 列挙しない</item>
        /// </list>
        /// </summary>
        internal IEnumerable<EFCoreEntityColumn> GetColumns() {

            var parent = Aggregate.GetParent();
            if (parent != null) {
                foreach (var parentKey in EnumerateKeysRecursively(true, parent, null, ["Parent"])) {
                    yield return parentKey;
                }
            }

            foreach (var member in Aggregate.GetMembers()) {
                if (member is ValueMember vm) {
                    // 自身のキー
                    yield return new OwnColumnMember(vm);

                } else if (member is RefToMember refTo) {
                    foreach (var refToKey in EnumerateKeysRecursively(false, refTo.RefTo, refTo, [refTo.PhysicalName])) {
                        yield return refToKey;
                    }
                }
            }

            // 親や参照先の集約のキーを辿る。
            // 子孫テーブルは祖先のキーを継承する。
            // 参照元は参照先のキーを継承する（FOREIGN KEY）。
            static IEnumerable<EFCoreEntityColumn> EnumerateKeysRecursively(bool isParent, AggregateBase parentOrRef, RefToMember? refEntry, IEnumerable<string> path) {
                // 親のさらに親
                var ancestor = parentOrRef.GetParent();
                if (ancestor != null) {
                    foreach (var ancestorKey in EnumerateKeysRecursively(true, ancestor, refEntry, [.. path, "Parent"])) {
                        yield return ancestorKey;
                    }
                }

                foreach (var member in parentOrRef.GetMembers()) {
                    if (member is ValueMember vm && vm.IsKey) {
                        yield return refEntry != null
                            ? new RefKeyMember(refEntry, vm, path, isParent) // 祖先かつ参照先の場合は参照先が優先
                            : new ParentKeyMember(vm, path);

                    } else if (member is RefToMember refTo && refTo.IsKey) {
                        foreach (var refToKey in EnumerateKeysRecursively(isParent, refTo.RefTo, refEntry ?? refTo, [.. path, refTo.PhysicalName])) {
                            yield return refToKey;
                        }
                    }
                }

            }
        }

        /// <summary>
        /// ナビゲーションプロパティを列挙する
        /// </summary>
        internal IEnumerable<NavigationProperty> GetNavigationProperties() {
            var parent = Aggregate.GetParent();
            if (parent != null) {
                yield return new NavigationOfParentChild(parent, Aggregate);
            }

            foreach (var member in Aggregate.GetMembers()) {
                if (member is ChildAggregate child) {
                    yield return new NavigationOfParentChild(Aggregate, child);

                } else if (member is ChildrenAggregate children) {
                    yield return new NavigationOfParentChild(Aggregate, children);

                } else if (member is RefToMember refTo) {
                    yield return new NavigationOfRef(refTo);
                }
            }

            foreach (var refFrom in Aggregate.GetRefFroms()) {
                // クエリモデルやコマンドモデルから参照されることがあるが、それらはDBの実体ではないのでナビゲーションプロパティを張らない
                if (refFrom.Owner.GetRoot().Model is not DataModel) continue;

                yield return new NavigationOfRef(refFrom);
            }
        }

        /// <summary>
        /// 同ツリー内のEFCoreEntityを列挙する
        /// </summary>
        internal IEnumerable<EFCoreEntity> EnumerateThisAndDescendants() {
            return new[] { this }.Concat(Aggregate.EnumerateDescendants().Select(e => new EFCoreEntity(e)));
        }

        IEnumerable<IInstancePropertyMetadata> IInstancePropertyOwnerMetadata.GetMembers() {
            foreach (var col in GetColumns()) {
                yield return col;
            }
            foreach (var nav in GetNavigationProperties()) {
                if (nav.Principal.ThisSide == Aggregate) yield return nav.Principal;
                if (nav.Relevant.ThisSide == Aggregate) yield return nav.Relevant;
            }
        }


        #region レンダリング
        internal static string RenderClassDeclaring(EFCoreEntity rootEfCoreEntity, CodeRenderingContext ctx) {
            if (rootEfCoreEntity.Aggregate is not RootAggregate) throw new InvalidOperationException();

            var tree = rootEfCoreEntity
                .EnumerateThisAndDescendants()
                .ToArray();

            return $$"""
                #region Entity Framework Core エンティティ定義
                {{tree.SelectTextTemplate(efCoreEntity => $$"""
                {{efCoreEntity.RenderClassDeclaring()}}
                """)}}

                partial class {{ApplicationConfigure.ABSTRACT_CLASS_CORE}} {
                {{tree.SelectTextTemplate(efCoreEntity => $$"""
                    {{WithIndent(efCoreEntity.RenderOnModelCreating(ctx), "    ")}}
                """)}}
                {{tree.SelectTextTemplate(efCoreEntity => $$"""
                    {{WithIndent(efCoreEntity.RenderConfigureAbstractMethod(), "    ")}}
                """)}}
                }
                #endregion Entity Framework Core エンティティ定義
                """;
        }
        private string RenderClassDeclaring() {
            var columns = GetColumns().ToArray();
            var keys = columns
                .Where(col => col.IsKey)
                .ToArray();
            var sequences = columns
                .Where(col => col.Member.Type is SequenceMember)
                .ToArray();

            var navigations = GetNavigationProperties().ToArray();
            var navigationsForDeclaring = navigations
                .Select(nav => {
                    if (nav.Principal.ThisSide == Aggregate) return nav.Principal;
                    if (nav.Relevant.ThisSide == Aggregate) return nav.Relevant;
                    throw new InvalidOperationException(); // ありえない
                });
            // 親子のナビゲーションは親側のOnModelCreatingで定義
            var navigationsForConfiguringChildren = navigations
                .Where(nav => nav is NavigationOfParentChild && nav.Principal.ThisSide == Aggregate);
            // 外部参照のナビゲーションは参照元側のOnModelCreatingで定義
            var navigationsForConfigureRefTo = navigations
                .Where(nav => nav is NavigationOfRef && nav.Relevant.ThisSide == Aggregate);

            return $$"""
                /// <summary>
                /// {{Aggregate.DisplayName}}の Entity Framework Core エンティティ型。RDBMSのテーブル定義と対応。
                /// </summary>
                public partial class {{CsClassName}} {
                {{columns.SelectTextTemplate(col => $$"""
                    /// <summary>{{col.DisplayName}}</summary>
                    public {{col.CsType}}? {{col.PhysicalName}} { get; set; }
                """)}}
                    /// <summary>データが新規作成された日時</summary>
                    public DateTime? {{CREATED_AT}} { get; set; }
                    /// <summary>データが更新された日時</summary>
                    public DateTime? {{UPDATED_AT}} { get; set; }
                    /// <summary>データを新規作成したユーザー</summary>
                    public string? {{CREATE_USER}} { get; set; }
                    /// <summary>データを更新したユーザー</summary>
                    public string? {{UPDATE_USER}} { get; set; }
                {{If(HasVersionColumn, () => $$"""
                    /// <summary>楽観排他制御用のバージョニング用カラム</summary>
                    public int? {{VERSION}} { get; set; }
                """)}}

                {{navigationsForDeclaring.SelectTextTemplate(nav => $$"""
                    public virtual {{nav.GetOtherSideCsTypeName(true)}} {{nav.OtherSidePhysicalName}} { get; set; }{{nav.GetInitializerStatement()}}
                """)}}

                    /// <summary>このオブジェクトと比較対象のオブジェクトの主キーが一致するかを返します。</summary>
                    public bool {{KEYEQUALS}}({{CsClassName}} entity) {
                {{keys.SelectTextTemplate(col => $$"""
                        if (entity.{{col.PhysicalName}} != this.{{col.PhysicalName}}) return false;
                """)}}
                        return true;
                    }
                }
                """;
        }
        private string RenderOnModelCreating(CodeRenderingContext ctx) {
            var columns = GetColumns().ToArray();
            var keys = columns
                .Where(col => col.IsKey)
                .ToArray();
            var sequences = columns
                .Where(col => col.Member.Type is SequenceMember)
                .ToArray();

            var navigations = GetNavigationProperties().ToArray();
            var navigationsForDeclaring = navigations
                .Select(nav => {
                    if (nav.Principal.ThisSide == Aggregate) return nav.Principal;
                    if (nav.Relevant.ThisSide == Aggregate) return nav.Relevant;
                    throw new InvalidOperationException(); // ありえない
                });
            // 親子のナビゲーションは親側のOnModelCreatingで定義
            var navigationsForConfiguringChildren = navigations
                .Where(nav => nav is NavigationOfParentChild && nav.Principal.ThisSide == Aggregate);
            // 外部参照のナビゲーションは参照元側のOnModelCreatingで定義
            var navigationsForConfigureRefTo = navigations
                .Where(nav => nav is NavigationOfRef && nav.Relevant.ThisSide == Aggregate);

            return $$"""
                /// <summary>
                /// テーブルやカラムの詳細を定義します。
                /// 参考: "Fluent API" （Entity FrameWork Core の仕組み）
                /// </summary>
                public virtual void {{OnModelCreatingAutoGenerated}}({{ctx.Config.DbContextName}} dbContext, ModelBuilder modelBuilder) {
                {{If(sequences.Length > 0, () => $$"""
                    // シーケンスを定義
                {{sequences.SelectTextTemplate(col => $$"""
                    modelBuilder.HasSequence<int>("{{col.Member.SequenceName}}")
                        .StartsAt(1)
                        .IncrementsBy(1);
                """)}}

                """)}}
                    modelBuilder.Entity<{{CsClassName}}>(entity => {
                        entity.ToTable("{{Aggregate.DbName}}");

                        entity.HasKey(e => new {
                {{keys.SelectTextTemplate(col => $$"""
                            e.{{col.PhysicalName}},
                """)}}
                        })
                        .HasName("PK_{{Aggregate.DbName}}");

                {{columns.SelectTextTemplate((col, ix) => $$"""
                        entity.Property(e => e.{{col.PhysicalName}})
                            .HasColumnName("{{col.DbName}}")
                {{If(col.Member.TotalDigit != null, () => $$"""
                            .HasPrecision({{col.Member.TotalDigit}}, {{col.Member.DecimalPlace ?? 0}})
                """)}}
                {{If(col.Member.MaxLength != null, () => $$"""
                            .HasMaxLength({{col.Member.MaxLength}})
                """)}}
                            .IsRequired({{(col.IsKey || col.Member.IsRequired ? "true" : "false")}})
                            .HasColumnOrder({{ix}});
                """)}}
                        entity.Property(e => e.{{CREATED_AT}})
                            .HasColumnName("{{ctx.Config.CreatedAtDbColumnName.Replace("\"", "\\\"")}}")
                            .IsRequired(false)
                            .HasColumnOrder({{columns.Length + 0}});
                        entity.Property(e => e.{{CREATE_USER}})
                            .HasColumnName("{{ctx.Config.CreateUserDbColumnName.Replace("\"", "\\\"")}}")
                            .IsRequired(false)
                            .HasColumnOrder({{columns.Length + 1}});
                        entity.Property(e => e.{{UPDATED_AT}})
                            .HasColumnName("{{ctx.Config.UpdatedAtDbColumnName.Replace("\"", "\\\"")}}")
                            .IsRequired(false)
                            .HasColumnOrder({{columns.Length + 2}});
                        entity.Property(e => e.{{UPDATE_USER}})
                            .HasColumnName("{{ctx.Config.UpdateUserDbColumnName.Replace("\"", "\\\"")}}")
                            .IsRequired(false)
                            .HasColumnOrder({{columns.Length + 3}});
                {{If(HasVersionColumn, () => $$"""
                        entity.Property(e => e.{{VERSION}})
                            .HasColumnName("{{ctx.Config.VersionDbColumnName.Replace("\"", "\\\"")}}")
                            .IsRequired(true)
                            .IsConcurrencyToken(true)
                            .HasColumnOrder({{columns.Length + 4}});
                """)}}
                {{navigationsForConfiguringChildren.SelectTextTemplate(nav => $$"""

                        entity.{{(nav.Principal.OtherSideIsMany ? "HasMany" : "HasOne")}}(e => e.{{nav.Principal.OtherSidePhysicalName}})
                            .{{(nav.Relevant.OtherSideIsMany ? "WithMany" : "WithOne")}}(e => e.{{nav.Relevant.OtherSidePhysicalName}})
                            .HasForeignKey{{(nav.IsOneToOne ? $"<{nav.Principal.GetOtherSideCsTypeName()}>" : "")}}(e => new {
                {{nav.GetRelevantForeignKeys().SelectTextTemplate(fk => $$"""
                                e.{{fk.PhysicalName}},
                """)}}
                            })
                            .IsRequired(false)
                            .OnDelete({{nameof(DeleteBehavior)}}.{{nav.PrincipalDeletedBehavior}})
                            .HasConstraintName("{{nav.GetConstraintName().Replace("\"", "\\\"")}}");
                """)}}
                {{navigationsForConfigureRefTo.SelectTextTemplate(nav => $$"""

                        entity.HasOne(e => e.{{nav.Relevant.OtherSidePhysicalName}})
                            .{{(nav.Principal.OtherSideIsMany ? "WithMany" : "WithOne")}}(e => e.{{nav.Principal.OtherSidePhysicalName}})
                            .HasForeignKey{{(nav.IsOneToOne ? $"<{nav.Principal.GetOtherSideCsTypeName()}>" : "")}}(e => new {
                {{nav.GetRelevantForeignKeys().SelectTextTemplate(fk => $$"""
                                e.{{fk.PhysicalName}},
                """)}}
                            })
                            .OnDelete({{nameof(DeleteBehavior)}}.{{nav.PrincipalDeletedBehavior}})
                            .HasConstraintName("{{nav.GetConstraintName().Replace("\"", "\\\"")}}");
                """)}}

                        // 自動生成されない設定の手動定義（インデックス、ユニーク制約、デフォルト値など）
                        {{ON_MODEL_CREATING_CUSTOMIZE}}(entity);
                    });
                }
                """;
        }
        private string RenderConfigureAbstractMethod() {
            return $$"""
                /// <summary>
                /// 自動生成されない初期設定がある場合はこのメソッドをオーバーライドして設定してください。
                /// （インデックス、ユニーク制約、デフォルト値など）
                /// </summary>
                public virtual void {{ON_MODEL_CREATING_CUSTOMIZE}}(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<{{CsClassName}}> entity) {
                }
                """;
        }
        #endregion レンダリング


        #region メンバー
        internal abstract class EFCoreEntityColumn : IInstanceValuePropertyMetadata {
            internal abstract ValueMember Member { get; }
            internal abstract string CsType { get; }
            internal abstract string PhysicalName { get; }
            internal abstract string DisplayName { get; }
            internal abstract string DbName { get; }
            internal abstract bool IsKey { get; }

            ISchemaPathNode IInstancePropertyMetadata.SchemaPathNode => Member;
            string IInstancePropertyMetadata.GetPropertyName(E_CsTs csts) => PhysicalName;
            IValueMemberType IInstanceValuePropertyMetadata.Type => Member.Type;

            public override string ToString() {
                // デバッグ用
                return $"{DisplayName}({CsType})";
            }
        }
        /// <summary>
        /// 集約自身に定義されている属性で、テーブルに直接属するカラム
        /// </summary>
        internal class OwnColumnMember : EFCoreEntityColumn {
            internal OwnColumnMember(ValueMember member) {
                Member = member;
            }
            internal override ValueMember Member { get; }
            internal override string CsType => Member.Type.CsPrimitiveTypeName;
            internal override string PhysicalName => Member.PhysicalName;
            internal override string DisplayName => Member.DisplayName;
            internal override string DbName => Member.DbName;
            internal override bool IsKey => Member.IsKey;
        }
        /// <summary>
        /// 子テーブルに定義される、親テーブルの主キーを継承したカラム
        /// </summary>
        internal class ParentKeyMember : EFCoreEntityColumn {
            internal ParentKeyMember(ValueMember member, IEnumerable<string> path) {
                Member = member;
                PhysicalName = $"{path.Join("_")}_{member.PhysicalName}";
                DbName = $"{path.Join("_")}_{member.DbName}";
            }
            internal override ValueMember Member { get; }
            internal override string CsType => Member.Type.CsPrimitiveTypeName;
            internal override string PhysicalName { get; }
            internal override string DisplayName => Member.DisplayName;
            internal override string DbName { get; }
            internal override bool IsKey => true;
        }
        /// <summary>
        /// 外部参照がある場合の、参照先のキーを継承したカラム
        /// </summary>
        internal class RefKeyMember : EFCoreEntityColumn {
            internal RefKeyMember(RefToMember refEntry, ValueMember member, IEnumerable<string> path, bool isParentKey) {
                RefEntry = refEntry;
                Member = member;
                PhysicalName = $"{path.Join("_")}_{member.PhysicalName}";
                DbName = $"{path.Join("_")}_{member.DbName}";
                IsParentKey = isParentKey;
            }
            internal RefToMember RefEntry { get; }
            internal override ValueMember Member { get; }
            internal override string CsType => Member.Type.CsPrimitiveTypeName;
            internal override string PhysicalName { get; }
            internal override string DisplayName => Member.DisplayName;
            internal override string DbName { get; }
            internal override bool IsKey => RefEntry.IsKey;
            /// <summary>
            /// この参照先キーが同時に <see cref="ParentKeyMember"/> であるかどうか。
            /// </summary>
            internal bool IsParentKey { get; }
        }
        #endregion メンバー


        #region ナビゲーションプロパティ
        /// <summary>
        /// EFCoreのナビゲーションプロパティ
        /// </summary>
        internal abstract class NavigationProperty {
            internal abstract PrincipalOrRelevant Principal { get; }
            internal abstract PrincipalOrRelevant Relevant { get; }

            /// <summary>HasOneWithOneのときだけ設定時に型引数が必要らしいので</summary>
            internal bool IsOneToOne => !Principal.OtherSideIsMany && !Relevant.OtherSideIsMany;
            /// <summary>RDBMS上で主たるエンティティが削除されたときの挙動</summary>
            internal abstract DeleteBehavior PrincipalDeletedBehavior { get; }
            /// <summary>RDBMS上の制約の物理名</summary>
            internal abstract string GetConstraintName();
            /// <summary>外部キー項目を列挙</summary>
            internal abstract IEnumerable<EFCoreEntityColumn> GetRelevantForeignKeys();

            public override string ToString() {
                // デバッグ用
                return $"Principal = {Principal.ThisSide}, Relevant = {Relevant.ThisSide}";
            }
        }
        internal class PrincipalOrRelevant : IInstanceStructurePropertyMetadata {
            internal required NavigationProperty NavigationProperty { get; init; }
            internal required AggregateBase ThisSide { get; init; }
            internal required AggregateBase OtherSide { get; init; }
            internal required string OtherSidePhysicalName { get; init; }
            internal required bool OtherSideIsMany { get; init; }

            bool IInstanceStructurePropertyMetadata.IsArray => OtherSideIsMany;
            ISchemaPathNode IInstancePropertyMetadata.SchemaPathNode => OtherSide;
            string IInstancePropertyMetadata.GetPropertyName(E_CsTs csts) => OtherSidePhysicalName;
            string IInstanceStructurePropertyMetadata.GetTypeName(E_CsTs csts) => new EFCoreEntity(OtherSide).CsClassName;
            IEnumerable<IInstancePropertyMetadata> IInstancePropertyOwnerMetadata.GetMembers() {
                IInstancePropertyOwnerMetadata otherSideEfCoreEntity = new EFCoreEntity(OtherSide);
                foreach (var member in otherSideEfCoreEntity.GetMembers()) {
                    // 無限ループに陥るのでこのインスタンス自身は列挙しない
                    if (member is PrincipalOrRelevant por && por.OtherSide == this.ThisSide) continue;

                    // 無限ループに陥るので被参照ナビゲーションプロパティは列挙しない。
                    // IInstancePropertyOwnerMetadata.GetMembers で被参照ナビゲーションプロパティを列挙したい状況もおそらく無いはず……多分
                    if (NavigationProperty is NavigationOfRef refNav && refNav.Relation.RefTo == ThisSide) continue;

                    yield return member;
                }
            }

            /// <summary>C#型名</summary>
            /// <param name="withNullable">末尾にNull許容演算子をつけるかどうか</param>
            internal string GetOtherSideCsTypeName(bool withNullable = false) {
                if (OtherSideIsMany) {
                    return $"ICollection<{new EFCoreEntity(OtherSide).CsClassName}>";
                } else {
                    return withNullable
                        ? $"{new EFCoreEntity(OtherSide).CsClassName}?"
                        : $"{new EFCoreEntity(OtherSide).CsClassName}";
                }
            }
            /// <summary>プロパティ初期化式</summary>
            internal string GetInitializerStatement() {
                if (OtherSideIsMany) {
                    return $" = [];";
                } else {
                    return string.Empty;
                }
            }

            public override string ToString() {
                // デバッグ用
                return $"({ThisSide.DisplayName}).{OtherSidePhysicalName}";
            }
        }

        /// <summary>
        /// 親子間のナビゲーションプロパティ
        /// </summary>
        internal class NavigationOfParentChild : NavigationProperty {
            internal NavigationOfParentChild(AggregateBase parent, AggregateBase child) {
                Principal = new() {
                    NavigationProperty = this,
                    ThisSide = parent,
                    OtherSide = child,
                    OtherSideIsMany = child is ChildrenAggregate,
                    OtherSidePhysicalName = child.PhysicalName,
                };
                Relevant = new() {
                    NavigationProperty = this,
                    ThisSide = child,
                    OtherSide = parent,
                    OtherSideIsMany = false,
                    OtherSidePhysicalName = "Parent",
                };
            }
            internal override PrincipalOrRelevant Principal { get; }
            internal override PrincipalOrRelevant Relevant { get; }

            internal override DeleteBehavior PrincipalDeletedBehavior => DeleteBehavior.Cascade;

            internal override string GetConstraintName() {
                return $"FK_{Principal.ThisSide.DbName}_{Relevant.ThisSide.DbName}";
            }
            internal override IEnumerable<EFCoreEntityColumn> GetRelevantForeignKeys() {
                var child = new EFCoreEntity(Relevant.ThisSide);
                var childColumns = child.GetColumns();

                // 子の主キーのうち、親の主キーのいずれかとマッピングキーが合致するものが親子間の外部キー
                var parentKeys = Principal.ThisSide
                    .GetKeyVMs()
                    .Select(vm => vm.ToMappingKey())
                    .ToHashSet();
                return childColumns.Where(c => parentKeys.Contains(c.Member.ToMappingKey()));
            }
        }

        /// <summary>
        /// 外部参照のナビゲーションプロパティ
        /// </summary>
        internal class NavigationOfRef : NavigationProperty {
            public NavigationOfRef(RefToMember relation) {
                Relation = relation;

                Principal = new() {
                    NavigationProperty = this,
                    ThisSide = relation.RefTo,
                    OtherSide = relation.Owner,
                    OtherSideIsMany = !relation.RefTo.IsSingleKeyOf(relation.Owner),
                    OtherSidePhysicalName = $"RefFrom{relation.Owner.PhysicalName}_{relation.PhysicalName}",
                };
                Relevant = new() {
                    NavigationProperty = this,
                    ThisSide = relation.Owner,
                    OtherSide = relation.RefTo,
                    OtherSideIsMany = false,
                    OtherSidePhysicalName = relation.PhysicalName,
                };
            }
            internal RefToMember Relation { get; }

            internal override PrincipalOrRelevant Principal { get; }
            internal override PrincipalOrRelevant Relevant { get; }

            internal override DeleteBehavior PrincipalDeletedBehavior => DeleteBehavior.NoAction;

            internal override string GetConstraintName() {
                // 同じテーブルから同じテーブルへ複数の参照経路があるときのための物理名衝突回避用ハッシュ
                var hash = Relation.PhysicalName.ToHashedString().ToUpper().Substring(0, 8);

                return $"FK_{Principal.ThisSide.DbName}_{Relevant.ThisSide.DbName}_{hash}";
            }
            internal override IEnumerable<EFCoreEntityColumn> GetRelevantForeignKeys() {
                var refFrom = new EFCoreEntity(Relevant.ThisSide);
                return refFrom
                    .GetColumns()
                    .Where(col => col is RefKeyMember rm
                               && rm.RefEntry == Relation);
            }
        }
        #endregion ナビゲーションプロパティ


        /// <summary>
        /// 子孫要素をIncludeする式をレンダリングします。
        /// </summary>
        internal IEnumerable<string> RenderInclude() {
            foreach (var agg in Aggregate.EnumerateDescendants()) {
                var fullPath = agg.GetPathFromEntry().Skip(1).ToArray();

                for (int i = 0; i < fullPath.Length; i++) {
                    var parent = (AggregateBase?)fullPath[i].PreviousNode ?? throw new InvalidOperationException("ありえない");
                    var child = (AggregateBase?)fullPath[i] ?? throw new InvalidOperationException("ありえない");
                    var nav = new NavigationOfParentChild(parent, child);

                    yield return i == 0
                        ? $".Include(e => e!.{nav.Principal.OtherSidePhysicalName})"      // クエリのエンティティ直下の場合はInclude
                        : $".ThenInclude(e => e!.{nav.Principal.OtherSidePhysicalName})"; // クエリのエンティティ直下でない場合はThenInclude
                }
            }
        }
    }
}
