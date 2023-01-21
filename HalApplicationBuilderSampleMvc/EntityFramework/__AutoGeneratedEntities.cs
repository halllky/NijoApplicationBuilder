
namespace HalApplicationBuilderSampleMvc.EntityFramework {
    using Microsoft.EntityFrameworkCore;

    partial class SampleDbContext {
    
        public DbSet<HalApplicationBuilderSampleMvc.EntityFramework.Entities.商品> 商品 { get; set; }
        public DbSet<HalApplicationBuilderSampleMvc.EntityFramework.Entities.売上> 売上 { get; set; }
        public DbSet<HalApplicationBuilderSampleMvc.EntityFramework.Entities.売上明細> 売上明細 { get; set; }

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

namespace HalApplicationBuilderSampleMvc.EntityFramework.Entities {
    using System;
    using System.Collections.Generic;

    public partial class 商品 {
        public string 商品コード { get; set; }
        public string 商品名 { get; set; }
        public int 単価 { get; set; }
    }
    public partial class 売上 {
        public string ID { get; set; }
        public DateTime 売上日時 { get; set; }
        public virtual ICollection<HalApplicationBuilderSampleMvc.EntityFramework.Entities.売上明細> 明細 { get; set; } = new HashSet<HalApplicationBuilderSampleMvc.EntityFramework.Entities.売上明細>();
    }
    public partial class 売上明細 {
        public string ID { get; set; }
        public string 商品_商品コード { get; set; }
        public int 数量 { get; set; }
        public virtual HalApplicationBuilderSampleMvc.EntityFramework.Entities.商品 商品 { get; set; }
        public virtual HalApplicationBuilderSampleMvc.EntityFramework.Entities.売上 売上 { get; set; }
    }

}