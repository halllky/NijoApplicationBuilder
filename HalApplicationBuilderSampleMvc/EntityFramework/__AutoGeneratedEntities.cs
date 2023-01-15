
namespace HalApplicationBuilderSampleMvc.EntityFramework {
    using Microsoft.EntityFrameworkCore;

    partial class SampleDbContext {
    
        public DbSet<HalApplicationBuilderSampleMvc.EntityFramework.Entities.会社> 会社 { get; set; }
        public DbSet<HalApplicationBuilderSampleMvc.EntityFramework.Entities.連絡先> 連絡先 { get; set; }
        public DbSet<HalApplicationBuilderSampleMvc.EntityFramework.Entities.上場企業資本情報> 上場企業資本情報 { get; set; }
        public DbSet<HalApplicationBuilderSampleMvc.EntityFramework.Entities.非上場企業資本情報> 非上場企業資本情報 { get; set; }
        public DbSet<HalApplicationBuilderSampleMvc.EntityFramework.Entities.営業所> 営業所 { get; set; }
        public DbSet<HalApplicationBuilderSampleMvc.EntityFramework.Entities.支店> 支店 { get; set; }
        public DbSet<HalApplicationBuilderSampleMvc.EntityFramework.Entities.担当者> 担当者 { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.Entity<HalApplicationBuilderSampleMvc.EntityFramework.Entities.会社>(entity => {
                entity.HasKey(e => new {
                    e.会社ID,
                });
            });
            modelBuilder.Entity<HalApplicationBuilderSampleMvc.EntityFramework.Entities.連絡先>(entity => {
                entity.HasKey(e => new {
                    e.会社ID,
                });
                entity.HasOne(e => e.会社).WithOne(e => e.連絡先);
            });
            modelBuilder.Entity<HalApplicationBuilderSampleMvc.EntityFramework.Entities.上場企業資本情報>(entity => {
                entity.HasKey(e => new {
                    e.会社ID,
                });
                entity.HasOne(e => e.会社).WithOne(e => e.資本情報__上場企業資本情報);
            });
            modelBuilder.Entity<HalApplicationBuilderSampleMvc.EntityFramework.Entities.非上場企業資本情報>(entity => {
                entity.HasKey(e => new {
                    e.会社ID,
                });
                entity.HasOne(e => e.会社).WithOne(e => e.資本情報__非上場企業資本情報);
            });
            modelBuilder.Entity<HalApplicationBuilderSampleMvc.EntityFramework.Entities.営業所>(entity => {
                entity.HasKey(e => new {
                    e.会社ID,
                    e.営業所_連番,
                });
                entity.HasOne(e => e.会社).WithMany(e => e.営業所);
            });
            modelBuilder.Entity<HalApplicationBuilderSampleMvc.EntityFramework.Entities.支店>(entity => {
                entity.HasKey(e => new {
                    e.会社ID,
                    e.営業所_連番,
                    e.支店_連番,
                });
                entity.HasOne(e => e.営業所).WithMany(e => e.支店);
            });
            modelBuilder.Entity<HalApplicationBuilderSampleMvc.EntityFramework.Entities.担当者>(entity => {
                entity.HasKey(e => new {
                    e.ユーザーID,
                });
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
        public string 主担当_ユーザーID { get; set; }
        public int? 資本情報 { get; set; }
        public virtual HalApplicationBuilderSampleMvc.EntityFramework.Entities.担当者 主担当 { get; set; }
        public virtual HalApplicationBuilderSampleMvc.EntityFramework.Entities.連絡先 連絡先 { get; set; }
        public virtual HalApplicationBuilderSampleMvc.EntityFramework.Entities.上場企業資本情報 資本情報__上場企業資本情報 { get; set; }
        public virtual HalApplicationBuilderSampleMvc.EntityFramework.Entities.非上場企業資本情報 資本情報__非上場企業資本情報 { get; set; }
        public virtual ICollection<HalApplicationBuilderSampleMvc.EntityFramework.Entities.営業所> 営業所 { get; set; } = new HashSet<HalApplicationBuilderSampleMvc.EntityFramework.Entities.営業所>();
    }
    public partial class 連絡先 {
        public string 会社ID { get; set; }
        public string 電話番号 { get; set; }
        public string 郵便番号 { get; set; }
        public string 住所1 { get; set; }
        public string 住所2 { get; set; }
        public string 住所3 { get; set; }
        public virtual HalApplicationBuilderSampleMvc.EntityFramework.Entities.会社 会社 { get; set; }
    }
    public partial class 上場企業資本情報 {
        public string 会社ID { get; set; }
        public decimal 自己資本比率 { get; set; }
        public decimal 利益率 { get; set; }
        public virtual HalApplicationBuilderSampleMvc.EntityFramework.Entities.会社 会社 { get; set; }
    }
    public partial class 非上場企業資本情報 {
        public string 会社ID { get; set; }
        public string 主要株主 { get; set; }
        public HalApplicationBuilderSampleSchema.E_安定性 安定性 { get; set; }
        public virtual HalApplicationBuilderSampleMvc.EntityFramework.Entities.会社 会社 { get; set; }
    }
    public partial class 営業所 {
        public string 会社ID { get; set; }
        public string 営業所_連番 { get; set; } = Guid.NewGuid().ToString();
        public string 営業所名 { get; set; }
        public string 担当者_ユーザーID { get; set; }
        public virtual ICollection<HalApplicationBuilderSampleMvc.EntityFramework.Entities.支店> 支店 { get; set; } = new HashSet<HalApplicationBuilderSampleMvc.EntityFramework.Entities.支店>();
        public virtual HalApplicationBuilderSampleMvc.EntityFramework.Entities.担当者 担当者 { get; set; }
        public virtual HalApplicationBuilderSampleMvc.EntityFramework.Entities.会社 会社 { get; set; }
    }
    public partial class 支店 {
        public string 会社ID { get; set; }
        public string 営業所_連番 { get; set; } = Guid.NewGuid().ToString();
        public string 支店_連番 { get; set; } = Guid.NewGuid().ToString();
        public string 支店名 { get; set; }
        public virtual HalApplicationBuilderSampleMvc.EntityFramework.Entities.営業所 営業所 { get; set; }
    }
    public partial class 担当者 {
        public string ユーザーID { get; set; }
        public string 氏名 { get; set; }
    }

}