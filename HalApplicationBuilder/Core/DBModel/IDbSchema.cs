using System;
using System.Collections.Generic;
using System.Reflection;

namespace HalApplicationBuilder.Core.DBModel {
    public interface IDbSchema {
        DbEntity GetDbEntity(Core.Aggregate aggregate);
    }
}
