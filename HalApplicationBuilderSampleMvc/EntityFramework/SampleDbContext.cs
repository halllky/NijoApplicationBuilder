using System;
using Microsoft.EntityFrameworkCore;

namespace HalApplicationBuilderSampleMvc.EntityFramework {
    public partial class SampleDbContext : DbContext {
        public SampleDbContext(DbContextOptions<SampleDbContext> options) : base(options) {
        }
    }
}
