
namespace HalApplicationBuilderSampleMvc.EntityFramework {
    using Microsoft.EntityFrameworkCore;

    partial class SampleDbContext {

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.Entity<HalApplicationBuilderSampleMvc.EntityFramework.Entities.商品>(entity => {
                entity.HasKey(e => new {
                    e.商品コード,
                });
            });
            modelBuilder.Entity<HalApplicationBuilderSampleMvc.EntityFramework.Entities.売上>(entity => {
                entity.HasKey(e => new {
                    e.ID,
                });
            });
            modelBuilder.Entity<HalApplicationBuilderSampleMvc.EntityFramework.Entities.売上明細>(entity => {
                entity.HasKey(e => new {
                    e.ID,
                    e.商品_商品コード,
                });
                entity.HasOne(e => e.売上).WithMany(e => e.明細);
            });
        }
    }
}
