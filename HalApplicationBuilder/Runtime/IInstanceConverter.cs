using System;
using System.Collections.Generic;

namespace HalApplicationBuilder.Runtime {
    public interface IInstanceConverter {
        void MapUIToDB(object uiInstance, object dbInstance, RuntimeContext context, HashSet<object> dbInstances);
        void MapDBToUI(object dbInstance, object uiInstance, RuntimeContext context);
    }
}
