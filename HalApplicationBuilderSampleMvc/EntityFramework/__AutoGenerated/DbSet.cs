
namespace HalApplicationBuilderSampleMvc.EntityFramework {
    using Microsoft.EntityFrameworkCore;

    partial class SampleDbContext {
        public DbSet<HalApplicationBuilderSampleMvc.EntityFramework.Entities.商品> 商品 { get; set; }
        public DbSet<HalApplicationBuilderSampleMvc.EntityFramework.Entities.売上> 売上 { get; set; }
        public DbSet<HalApplicationBuilderSampleMvc.EntityFramework.Entities.売上明細> 売上明細 { get; set; }
    }
}
