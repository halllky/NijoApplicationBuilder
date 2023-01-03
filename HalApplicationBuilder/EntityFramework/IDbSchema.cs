using System;
namespace HalApplicationBuilder.EntityFramework {
    public interface IDbSchema {
        DbEntity GetDbEntity(Core.Aggregate aggregate);
    }
}
