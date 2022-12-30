using System;
using System.Collections.Generic;
using System.Reflection;

namespace haldoc.Runtime {
    public class RuntimeService {
        public Assembly RuntimeAssembly { get; internal init; }
        public haldoc.Core.ProjectContext ProjectContext { get; internal init; }
        public haldoc.EntityFramework.DbSchema DbSchema { get; internal init; }

        /// <summary>ナビゲーションプロパティ未実装のためIEnumerableで返している</summary>
        public IEnumerable<object> TransformMvcModelToDbEntities(haldoc.Core.Aggregate aggregate, object mvcModel) {
            var dbEntity = DbSchema.FindTable(aggregate).CreateRuntimeInstance(RuntimeAssembly);
            foreach (var prop in GetProperties()) {
                foreach (var descendantDbEntity in prop.AssignMvcToDb(mvcModel, dbEntity)) {
                    yield return descendantDbEntity;
                }
            }
            yield return dbEntity;
        }
    }
}
