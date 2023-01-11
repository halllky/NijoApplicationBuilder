using System;
using System.Collections.Generic;
using System.Linq;
using HalApplicationBuilder.Core.Members;
using HalApplicationBuilder.Impl;
using Microsoft.Extensions.DependencyInjection;

namespace HalApplicationBuilder.Core {
    /// <summary>
    /// 集約定義ルール
    /// </summary>
    public class AggregateValidator {
        public AggregateValidator(IServiceProvider service) {
            ApplicationSchema = service.GetRequiredService<IApplicationSchema>();
            DbSchema = service.GetRequiredService<EntityFramework.IDbSchema>();
            ViewModelProvider = service.GetRequiredService<AspNetMvc.IViewModelProvider>();
        }

        private IApplicationSchema ApplicationSchema { get; }
        private EntityFramework.IDbSchema DbSchema { get; }
        private AspNetMvc.IViewModelProvider ViewModelProvider { get; }

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

            // 主キーにできる型の制約
            foreach (var member in aggregate.Members.Where(member => member.IsPrimaryKey)) {
                switch (member) {
                    case Child:
                    case Children:
                    case Variation:
                        yield return $"{member.Name} は子要素のため主キーに設定できません。";
                        break;
                    default:
                        break;
                }
            }

            // InstanceNameにできる型の制約
            
        }
    }
}
