
namespace HalApplicationBuilderSampleMvc.EntityFramework {
    using Microsoft.EntityFrameworkCore;

    partial class SampleDbContext {
    
        public DbSet<HalApplicationBuilderSampleMvc.EntityFramework.Entities.営業所> 営業所 { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.Entity<HalApplicationBuilderSampleMvc.EntityFramework.Entities.営業所>()
                .HasKey(e => new {
                    e.営業所ID,
                });
        }
    }
}

namespace HalApplicationBuilderSampleMvc.EntityFramework.Entities {
    using System;
    using System.Collections.Generic;

    public partial class 営業所 {
        public string 営業所ID { get; set; }
        public string 営業所名 { get; set; }
        public DateTime? 運用開始日 { get; set; }
    }

}