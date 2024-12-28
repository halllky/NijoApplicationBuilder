using Microsoft.EntityFrameworkCore;
using Nijo.Core;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.WriteModel2Features {
    /// <summary>
    /// Entity Framework Core のエンティティ
    /// </summary>
    internal class EFCoreEntity {
        internal EFCoreEntity(GraphNode<Aggregate> agg) {
            _aggregate = agg;
        }
        internal GraphNode<Aggregate> Aggregate => _aggregate;
        private readonly GraphNode<Aggregate> _aggregate;

        internal string ClassName => _aggregate.Item.EFCoreEntityClassName;
        internal string DbSetName => $"{_aggregate.Item.PhysicalName}DbSet";

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

        /// <summary>
        /// このエンティティに関するテーブルやカラムの詳細を定義する処理（"Fluent API  Entity FrameWork Core" で調べて）を
        /// エンティティクラス内にstaticメソッドで記述することにしているが、そのstaticメソッドの名前
        /// </summary>
        private string OnModelCreating => $"OnModelCreating{_aggregate.Item.PhysicalName}";

        /// <summary>
        /// このエンティティのテーブルに属するカラムと対応するメンバーを列挙します。
        /// </summary>
        internal IEnumerable<AggregateMember.ValueMember> GetTableColumnMembers() {
            foreach (var member in _aggregate.GetMembers()) {
                if (member is not AggregateMember.ValueMember vm) continue;
                if (vm.Inherits?.GetRefForeignKeyProxy() != null) continue;

                yield return vm;
            }
        }

        /// <summary>
        /// このエンティティがもつナビゲーションプロパティを、
        /// このエンティティがPrincipal側かRelevant側かを考慮しつつ列挙します。
        /// </summary>
        internal IEnumerable<NavigationProperty.PrincipalOrRelevant> GetNavigationPropertiesThisSide() {
            foreach (var nav in GetNavigationProperties()) {
                if (nav.Principal.Owner == _aggregate) yield return nav.Principal;
                if (nav.Relevant.Owner == _aggregate) yield return nav.Relevant;
            }
        }
        /// <summary>
        /// このエンティティがもつナビゲーションプロパティを列挙します。
        /// このエンティティがPrincipal側かRelevant側かは考慮しません。
        /// </summary>
        internal IEnumerable<NavigationProperty> GetNavigationProperties() {
            foreach (var member in _aggregate.GetMembers()) {
                if (member is not AggregateMember.RelationMember relationMember) continue;
                yield return relationMember.GetNavigationProperty();
            }

            foreach (var refered in _aggregate.GetReferedEdges()) {
                if (!refered.Initial.IsStored()) continue;
                yield return new NavigationProperty(refered);
            }
        }

        /// <summary>
        /// エンティティクラス定義をレンダリングします。
        /// </summary>
        internal string Render(CodeRenderingContext context) {
            return $$"""
                /// <summary>
                /// Entity Framework Core のルールに則った{{_aggregate.Item.DisplayName}}のデータ型
                /// </summary>
                public partial class {{_aggregate.Item.EFCoreEntityClassName}} {
                {{GetTableColumnMembers().SelectTextTemplate(col => $$"""
                    public {{col.Options.MemberType.GetCSharpTypeName()}}? {{col.MemberName}} { get; set; }
                """)}}
                {{If(_aggregate.IsRoot(), () => $$"""
                    /// <summary>楽観排他制御用のバージョニング用カラム</summary>
                    public int? {{VERSION}} { get; set; }
                    /// <summary>データが新規作成された日時</summary>
                    public DateTime? {{CREATED_AT}} { get; set; }
                    /// <summary>データが更新された日時</summary>
                    public DateTime? {{UPDATED_AT}} { get; set; }
                    /// <summary>データを新規作成したユーザー</summary>
                    public string? {{CREATE_USER}} { get; set; }
                    /// <summary>データを更新したユーザー</summary>
                    public string? {{UPDATE_USER}} { get; set; }
                """)}}

                {{GetNavigationPropertiesThisSide().SelectTextTemplate(nav => $$"""
                    public virtual {{nav.CSharpTypeName}} {{nav.PropertyName}} { get; set; }{{nav.Initializer}}
                """)}}

                    /// <summary>このオブジェクトと比較対象のオブジェクトの主キーが一致するかを返します。</summary>
                    public bool {{KEYEQUALS}}({{ClassName}} entity) {
                {{_aggregate.GetKeys().OfType<AggregateMember.ValueMember>().SelectTextTemplate(col => $$"""
                        if (entity.{{col.MemberName}} != this.{{col.MemberName}}) return false;
                """)}}
                        return true;
                    }
                }

                partial class {{Parts.Configure.ABSTRACT_CLASS_NAME}} {
                    /// <summary>
                    /// テーブルやカラムの詳細を定義します。
                    /// 参考: "Fluent API" （Entity FrameWork Core の仕組み）
                    /// </summary>
                    public virtual void {{OnModelCreating}}({{context.Config.DbContextName}} dbContext, ModelBuilder modelBuilder) {
                        modelBuilder.Entity<{{context.Config.EntityNamespace}}.{{_aggregate.Item.EFCoreEntityClassName}}>(entity => {

                            entity.ToTable("{{_aggregate.Item.Options.DbName ?? _aggregate.Item.PhysicalName}}");

                            entity.HasKey(e => new {
                {{_aggregate.GetKeys().OfType<AggregateMember.ValueMember>().SelectTextTemplate(pk => $$"""
                                e.{{pk.MemberName}},
                """)}}
                            });

                {{_aggregate.GetMembers().OfType<AggregateMember.ValueMember>().SelectTextTemplate(col => $$"""
                            entity.Property(e => e.{{col.MemberName}})
                                .HasColumnName("{{col.DbColumnName}}")
                {{If(col.Options.MaxLength != null, () => $$"""
                                .HasMaxLength({{col.Options.MaxLength}})
                """)}}
                                .IsRequired({{(col.IsRequired ? "true" : "false")}});
                """)}}
                {{If(_aggregate.IsRoot() && context.Config.CreateUserDbColumnName != null, () => $$"""
                            entity.Property(e => e.{{CREATE_USER}})
                                .HasColumnName("{{context.Config.CreateUserDbColumnName?.Replace("\"", "\\\"")}}");
                """)}}
                {{If(_aggregate.IsRoot() && context.Config.UpdateUserDbColumnName != null, () => $$"""
                            entity.Property(e => e.{{UPDATE_USER}})
                                .HasColumnName("{{context.Config.UpdateUserDbColumnName?.Replace("\"", "\\\"")}}");
                """)}}
                {{If(_aggregate.IsRoot() && context.Config.CreatedAtDbColumnName != null, () => $$"""
                            entity.Property(e => e.{{CREATED_AT}})
                                .HasColumnName("{{context.Config.CreatedAtDbColumnName?.Replace("\"", "\\\"")}}");
                """)}}
                {{If(_aggregate.IsRoot() && context.Config.UpdatedAtDbColumnName != null, () => $$"""
                            entity.Property(e => e.{{UPDATED_AT}})
                                .HasColumnName("{{context.Config.UpdatedAtDbColumnName?.Replace("\"", "\\\"")}}");
                """)}}
                {{If(_aggregate.IsRoot(), () => $$"""
                            entity.Property(e => e.{{VERSION}})
                {{If(context.Config.VersionDbColumnName != null, () => $$"""
                                .HasColumnName("{{context.Config.VersionDbColumnName?.Replace("\"", "\\\"")}}")
                """)}}
                                .IsRequired(true)
                                .IsConcurrencyToken(true);
                """)}}

                            {{WithIndent(RenderNavigationPropertyOnModelCreating(), "            ")}}
                        });
                    }
                }
                """;
        }

        /// <summary>
        /// ナビゲーションプロパティの Fluent API 定義
        /// </summary>
        private IEnumerable<string> RenderNavigationPropertyOnModelCreating() {
            foreach (var nav in GetNavigationProperties()) {

                if (nav.Principal.Owner != _aggregate) continue;

                // Has
                if (nav.Principal.OppositeIsMany) {
                    yield return $"entity.HasMany(e => e.{nav.Principal.PropertyName})";
                } else {
                    yield return $"entity.HasOne(e => e.{nav.Principal.PropertyName})";
                }

                // With
                if (nav.Relevant.OppositeIsMany) {
                    yield return $"    .WithMany(e => e.{nav.Relevant.PropertyName})";
                } else {
                    yield return $"    .WithOne(e => e.{nav.Relevant.PropertyName})";
                }

                // FK
                if (!nav.Principal.OppositeIsMany && !nav.Relevant.OppositeIsMany) {
                    // HasOneWithOneのときは型引数が要るらしい
                    yield return $"    .HasForeignKey<{nav.Relevant.Owner.Item.EFCoreEntityClassName}>(e => new {{";
                } else {
                    yield return $"    .HasForeignKey(e => new {{";
                }
                foreach (var fk in nav.Relevant.GetForeignKeys()) {
                    yield return $"        e.{fk.MemberName},";
                }
                yield return $"    }})";

                // OnDelete
                yield return $"    .OnDelete({nameof(DeleteBehavior)}.{nav.OnPrincipalDeleted});";
            }
        }

        /// <summary>
        /// <see cref="ON_MODEL_CREATING"/> メソッドを呼び出す
        /// </summary>
        internal Func<string, string> RenderCallingOnModelCreating(CodeRenderingContext context) {
            return modelBuilder => $$"""
                customizedConfigure.{{OnModelCreating}}(this, {{modelBuilder}});
                """;
        }

        /// <summary>
        /// 子孫要素をIncludeする処理をレンダリングします。
        /// </summary>
        internal string RenderInclude(bool includeRefs) {
            var includeEntities = _aggregate
                .EnumerateThisAndDescendants()
                .ToList();
            if (includeRefs) {
                var refEntities = _aggregate
                    .EnumerateThisAndDescendants()
                    .SelectMany(agg => agg.GetMembers())
                    .Select(m => m.DeclaringAggregate);
                foreach (var entity in refEntities) {
                    includeEntities.Add(entity);
                }
            }
            var paths = includeEntities
                .Select(entity => entity.PathFromEntry())
                .Distinct()
                .SelectMany(edge => edge)
                .Select(edge => edge.As<Aggregate>())
                .Select(edge => {
                    var source = edge.Source.As<Aggregate>();
                    var nav = new NavigationProperty(edge);
                    var prop = edge.Source.As<Aggregate>() == nav.Principal.Owner
                        ? nav.Principal.PropertyName
                        : nav.Relevant.PropertyName;
                    return new { source, prop };
                });

            return paths.SelectTextTemplate(path => path.source == _aggregate ? $$"""
                .Include(x => x.{{path.prop}})
                """ : $$"""
                .ThenInclude(x => x.{{path.prop}})
                """);
        }
    }

    internal static partial class GetFullPathExtensions {

        /// <summary>
        /// エントリーからのパスを <see cref="EFCoreEntity"/> のインスタンスの型のルールにあわせて返す。
        /// </summary>
        internal static IEnumerable<string> GetFullPathAsDbEntity(this GraphNode<Aggregate> aggregate, GraphNode<Aggregate>? since = null, GraphNode<Aggregate>? until = null) {
            var path = aggregate.PathFromEntry();
            if (since != null) path = path.Since(since);
            if (until != null) path = path.Until(until);

            foreach (var edge in path) {
                var navigation = new NavigationProperty(edge.As<Aggregate>());
                if (navigation.Principal.Owner == edge.Source.As<Aggregate>()) {
                    yield return navigation.Principal.PropertyName;
                } else {
                    yield return navigation.Relevant.PropertyName;
                }
            }
        }

        /// <summary>
        /// エントリーからのパスを <see cref="EFCoreEntity"/> のインスタンスの型のルールにあわせて返す。
        /// </summary>
        internal static IEnumerable<string> GetFullPathAsDbEntity(this AggregateMember.AggregateMemberBase member, GraphNode<Aggregate>? since = null, GraphNode<Aggregate>? until = null) {
            foreach (var path in member.Owner.GetFullPathAsDbEntity(since, until)) {
                yield return path;
            }
            yield return member.MemberName;
        }

        /// <summary>
        /// フルパスの途中で配列が出てきた場合はSelectやmapをかける
        /// </summary>
        internal static IEnumerable<string> GetFullPathAsDbEntity(this AggregateMember.AggregateMemberBase member, E_CsTs csts, out bool isArray, GraphNode<Aggregate>? since = null, GraphNode<Aggregate>? until = null) {
            var path = member.Owner.PathFromEntry();
            if (since != null) path = path.Since(since);
            if (until != null) path = path.Until(until);

            isArray = false;
            var edges = path.ToArray();
            var result = new List<string>();
            for (int i = 0; i < edges.Length; i++) {
                var edge = edges[i];

                var navigation = new NavigationProperty(edge.As<Aggregate>());
                var relationName = navigation.Principal.Owner == edge.Source.As<Aggregate>()
                    ? navigation.Principal.PropertyName
                    : navigation.Relevant.PropertyName;

                var isMany = false;
                if (edge.IsParentChild()
                    && edge.Source == edge.Initial
                    && edge.Terminal.As<Aggregate>().IsChildrenMember()) {
                    isMany = true;
                }

                if (isMany) {
                    result.Add(isArray
                        ? (csts == E_CsTs.CSharp
                            ? $"SelectMany(x => x.{relationName})"
                            : $"flatMap(x => x.{relationName})")
                        : relationName);
                    isArray = true;

                } else {
                    result.Add(isArray
                        ? (csts == E_CsTs.CSharp
                            ? $"Select(x => x.{relationName})"
                            : $"map(x => x.{relationName})")
                        : relationName);
                }
            }

            result.Add(member.MemberName);
            return result;
        }
    }
}
