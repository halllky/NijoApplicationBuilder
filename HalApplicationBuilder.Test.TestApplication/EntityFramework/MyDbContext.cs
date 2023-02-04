using System;
using Microsoft.EntityFrameworkCore;

namespace HalApplicationBuilder.Test.TestApplication.EntityFramework {
    public partial class MyDbContext : DbContext {
        public MyDbContext(DbContextOptions<MyDbContext> options) : base(options) { }
    }
}
