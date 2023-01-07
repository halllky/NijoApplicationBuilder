using System;
using System.Collections.Generic;

namespace HalApplicationBuilder.Runtime {
    public interface IInstanceConverter {
        void MapUIToDB(object instanceModel, object dbEntity, RuntimeContext context, HashSet<object> dbEntities);
    }
}
