using HalApplicationBuilder.Core20230514;
using HalApplicationBuilder.DotnetEx;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering20230514.EFCore {
    partial class DbContext : ITemplate {
        internal DbContext(CodeRenderingContext ctx) {
            _ctx = ctx;
        }
        private readonly CodeRenderingContext _ctx;

        private const string ENTITY = "entt";

        public string FileName => $"{_ctx.Config.DbContextName.ToFileNameSafe()}.cs";

        private IEnumerable<string> RenderNavigationPropertyOnModelCreating(GraphNode<EFCoreEntity> dbEntity) {

            foreach (var nav in dbEntity.GetNavigationProperties(_ctx.Config)) {

                if (nav.Principal.Owner != dbEntity) continue;

                // Has
                if (nav.Principal.OppositeIsMany) {
                    yield return $"{ENTITY}.HasMany(e => e.{nav.Principal.PropertyName})";
                } else {
                    yield return $"{ENTITY}.HasOne(e => e.{nav.Principal.PropertyName})";
                }

                // With
                if (nav.Relevant.OppositeIsMany) {
                    yield return $"    .WithMany(e => e.{nav.Relevant.PropertyName})";
                } else {
                    yield return $"    .WithOne(e => e.{nav.Relevant.PropertyName})";
                }

                // FK
                if (nav.Principal.OppositeIsMany && nav.Relevant.OppositeIsMany) {
                    // HasOneWithOneのときは型引数が要るらしい
                    yield return $"    .HasForeignKey<{nav.Principal.Owner.Item.ClassName}>(e => new {{";
                } else {
                    yield return $"    .HasForeignKey(e => new {{";
                }
                foreach (var fk in nav.Relevant.ForeignKeys) {
                    yield return $"        e.{fk.PropertyName},";
                }
                yield return $"    }})";

                // OnDelete
                yield return $"    .OnDelete({nameof(DeleteBehavior)}.{nav.OnPrincipalDeleted});";
            }
        }
    }
}
