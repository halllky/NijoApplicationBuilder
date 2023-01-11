using System;
using System.Collections.Generic;

namespace HalApplicationBuilder.Core.DBModel {
    public class JoinKeyMap {

        public IEnumerable<(DbColumn principal, DbColumn relevant)> Get(DbEntity principalTable, DbEntity relevantTable) {
            throw new NotImplementedException();
        }
    }
}
