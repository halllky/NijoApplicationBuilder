
namespace HalApplicationBuilderSampleMvc.EntityFramework {
    using Microsoft.EntityFrameworkCore;

    partial class SampleDbContext {
    
        public DbSet<HalApplicationBuilderSampleMvc.EntityFramework.Entities.会社> 会社 { get; set; }
        public DbSet<HalApplicationBuilderSampleMvc.EntityFramework.Entities.連絡先> 連絡先 { get; set; }
        public DbSet<HalApplicationBuilderSampleMvc.EntityFramework.Entities.上場企業資本情報> 上場企業資本情報 { get; set; }
        public DbSet<HalApplicationBuilderSampleMvc.EntityFramework.Entities.非上場企業資本情報> 非上場企業資本情報 { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.Entity<HalApplicationBuilderSampleMvc.EntityFramework.Entities.会社>()
                .HasKey(e => new {
                    e.会社ID,
                });
            modelBuilder.Entity<HalApplicationBuilderSampleMvc.EntityFramework.Entities.連絡先>()
                .HasKey(e => new {
                    e.会社ID,
                });
            modelBuilder.Entity<HalApplicationBuilderSampleMvc.EntityFramework.Entities.上場企業資本情報>()
                .HasKey(e => new {
                    e.会社ID,
                });
            modelBuilder.Entity<HalApplicationBuilderSampleMvc.EntityFramework.Entities.非上場企業資本情報>()
                .HasKey(e => new {
                    e.会社ID,
                });
        }
    }
}

namespace HalApplicationBuilderSampleMvc.EntityFramework.Entities {
    using System;
    using System.Collections.Generic;

    public partial class 会社 {
        public string 会社ID { get; set; }
        public string 会社名 { get; set; }
        public int? 資本情報 { get; set; }
    }
    public partial class 連絡先 {
        public string 会社ID { get; set; }
        public string 電話番号 { get; set; }
        public string 郵便番号 { get; set; }
        public string 住所1 { get; set; }
        public string 住所2 { get; set; }
        public string 住所3 { get; set; }
    }
    public partial class 上場企業資本情報 {
        public string 会社ID { get; set; }
        public decimal 自己資本比率 { get; set; }
        public decimal 利益率 { get; set; }
    }
    public partial class 非上場企業資本情報 {
        public string 会社ID { get; set; }
        public string 主要株主 { get; set; }
        public HalApplicationBuilderSampleSchema.E_安定性 安定性 { get; set; }
    }

}