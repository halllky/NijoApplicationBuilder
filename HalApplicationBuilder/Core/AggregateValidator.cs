using System;
using System.Collections.Generic;
using System.Linq;
using HalApplicationBuilder.Core.DBModel;
using HalApplicationBuilder.Core.UIModel;
using Microsoft.Extensions.DependencyInjection;

namespace HalApplicationBuilder.Core {
    /// <summary>
    /// 集約定義ルール
    /// </summary>
    public class AggregateValidator {
        public AggregateValidator(IServiceProvider service) {
            ApplicationSchema = service.GetRequiredService<IApplicationSchema>();
            DbSchema = service.GetRequiredService<IDbSchema>();
            ViewModelProvider = service.GetRequiredService<IViewModelProvider>();
        }

        private IApplicationSchema ApplicationSchema { get; }
        private IDbSchema DbSchema { get; }
        private IViewModelProvider ViewModelProvider { get; }

        public bool HasError(Action<string> errorHandler) {
            var hasAnyError = false;
            foreach (var aggregate in ApplicationSchema.AllAggregates()) {
                foreach (var error in Validate(aggregate)) {
                    hasAnyError = true;
                    errorHandler?.Invoke($"{aggregate.Name}: {error}");
                }
            }
            return hasAnyError;
        }

        private IEnumerable<string> Validate(Aggregate aggregate) {

            var dbEntity = DbSchema.GetDbEntity(aggregate);

            // 主キー存在チェック
            if (dbEntity.PKColumns.Any() == false)
                yield return $"主キーがありません。";

            // 各member固有のエラー
            foreach (var error in aggregate.Members.SelectMany(m => m.GetInvalidErrors())) {
                yield return error;
            }

            // InstanceNameにできる型の制約
            // TODO
        }
    }
}
