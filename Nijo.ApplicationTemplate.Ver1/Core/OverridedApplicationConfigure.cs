using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApp.Core;

public class OverridedApplicationConfigure : DefaultConfiguration {

    protected override MyDbContext ConfigureDbContext(IServiceProvider services) {
        var options = new Microsoft.EntityFrameworkCore.DbContextOptions<MyDbContext>();
        var logger = services.GetRequiredService<NLog.Logger>();
        var dbContext = new MyDbContext(options, this, logger);
        return dbContext;
    }
}
