using Microsoft.EntityFrameworkCore;

namespace HalApplicationBuilder.Test.DistMvc.EntityFramework {

    public partial class MyDbContext : DbContext {
        public MyDbContext(DbContextOptions<MyDbContext> options) : base(options) { }

        private static bool _migrated = false;
        public override int SaveChanges() {

            // テストのためこのタイミングでMigrateしている
            if (!_migrated) {
                Database.EnsureDeleted();
                Database.EnsureCreated();
                _migrated = true;
            }

            return base.SaveChanges();
        }
    }
}
