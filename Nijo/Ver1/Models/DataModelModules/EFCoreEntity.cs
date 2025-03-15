using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.Models.DataModelModules {
    /// <summary>
    /// Entity Framework Core のエンティティ
    /// </summary>
    internal class EFCoreEntity {

        internal EFCoreEntity(AggregateBase aggregate) {
            _aggregate = aggregate;
        }
        private readonly AggregateBase _aggregate;

        internal string CsClassName => $"{_aggregate.PhysicalName}DbEntity";
        internal string DbSetName => $"{_aggregate.PhysicalName}DbSet";

        private const string ON_MODEL_CREATING = "OnModelCreating";

        internal string RenderClassDeclaring(CodeRenderingContext ctx) {
            return $$"""
                public partial class {{CsClassName}} {

                    internal static void {{ON_MODEL_CREATING}}(ModelBuilder modelBuilder) {
                    }
                }
                """;
        }

        internal string RenderDbSetProperty(CodeRenderingContext ctx) {
            return $$"""
                public virtual DbSet<{{CsClassName}}> {{DbSetName}} { get; set; }
                """;
        }

        internal string RenderOnModelCreatingCalling(CodeRenderingContext ctx) {
            return $$"""
                {{CsClassName}}.{{ON_MODEL_CREATING}}(modelBuilder);
                """;
        }
    }
}
