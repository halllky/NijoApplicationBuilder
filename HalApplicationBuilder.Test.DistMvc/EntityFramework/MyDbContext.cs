using Microsoft.EntityFrameworkCore;

namespace HalApplicationBuilder.Test.DistMvc.EntityFramework {

    public partial class MyDbContext : DbContext {
        public MyDbContext(DbContextOptions<MyDbContext> options) : base(options) { }
    }
}
