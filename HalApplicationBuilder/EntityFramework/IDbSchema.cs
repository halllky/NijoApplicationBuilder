using System;
using System.Collections.Generic;
using System.Reflection;

namespace HalApplicationBuilder.EntityFramework {
    public interface IDbSchema {
        DbEntity GetDbEntity(Core.Aggregate aggregate);
    }
}
