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
        private readonly GraphNode<Aggregate> _aggregate;

        internal string ClassName => _aggregate.Item.EFCoreEntityClassName;

        /// <summary>楽観排他制御用のバージョニング用カラムの名前</summary>
        internal const string VERSION = "Version";

        /// <summary>データが新規作成された日時</summary>
        internal const string CREATED_AT = "CteatedAt";
        /// <summary>データが更新された日時</summary>
        internal const string UPDATED_AT = "UpdatedAt";

        /// <summary>データを新規作成したユーザー</summary>
        internal const string CREATE_USER = "CreateUser";
        /// <summary>データを更新したユーザー</summary>
        internal const string UPDATE_USER = "UpdateUser";

        /// <summary>
        /// このエンティティに関するテーブルやカラムの詳細を定義する処理（"Fluent API  Entity FrameWork Core" で調べて）を
        /// エンティティクラス内にstaticメソッドで記述することにしているが、そのstaticメソッドの名前
        /// </summary>
        private const string ON_MODEL_CREATING = "OnModelCreating";

        /// <summary>
        /// このエンティティのテーブルに属するカラムと対応するメンバーを列挙します。
        /// </summary>
        internal IEnumerable<AggregateMember.ValueMember> GetTableColumnMembers() {
            return _aggregate.GetMembers().OfType<AggregateMember.ValueMember>();
        }

        /// <summary>
        /// このエンティティがもつナビゲーションプロパティを列挙します。
        /// </summary>
        internal IEnumerable<NavigationProperty.PrincipalOrRelevant> GetNavigationProperties() {
            foreach (var nav in _aggregate.GetNavigationProperties()) {
                if (nav.Principal.Owner == _aggregate) yield return nav.Principal;
                if (nav.Relevant.Owner == _aggregate) yield return nav.Relevant;
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

                {{GetNavigationProperties().SelectTextTemplate(nav => $$"""
                    public virtual {{nav.CSharpTypeName}} {{nav.PropertyName}} { get; set; }
                """)}}

                    /// <summary>
                    /// テーブルやカラムの詳細を定義します。
                    /// 参考: "Fluent API" （Entity FrameWork Core の仕組み）
                    /// </summary>
                    public static void {{ON_MODEL_CREATING}}(ModelBuilder modelBuilder) {
                        modelBuilder.Entity<{{context.Config.EntityNamespace}}.{{_aggregate.Item.EFCoreEntityClassName}}>(entity => {

                            entity.HasKey(e => new {
                {{_aggregate.GetKeys().OfType<AggregateMember.ValueMember>().SelectTextTemplate(pk => $$"""
                                e.{{pk.MemberName}},
                """)}}
                            });

                {{_aggregate.GetMembers().OfType<AggregateMember.ValueMember>().SelectTextTemplate(col => $$"""
                            entity.Property(e => e.{{col.MemberName}})
                                .IsRequired({{(col.IsRequired ? "true" : "false")}});
                """)}}

                                {{WithIndent(RenderNavigationPropertyOnModelCreating(), "                ")}}
                        });
                    }
                }
                """;
        }

        /// <summary>
        /// ナビゲーションプロパティの Fluent API 定義
        /// </summary>
        private IEnumerable<string> RenderNavigationPropertyOnModelCreating() {
            foreach (var nav in _aggregate.GetNavigationProperties()) {

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
                {{ClassName}}.{{ON_MODEL_CREATING}}({{modelBuilder}});
                """;
        }
    }
}
