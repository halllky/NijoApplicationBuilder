using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using HalApplicationBuilder.Core.DBModel;
using HalApplicationBuilder.Core.Runtime;
using HalApplicationBuilder.Core.UIModel;
using Microsoft.Extensions.DependencyInjection;

namespace HalApplicationBuilder.Core.Members {
    public abstract class AggregateMemberBase :
        IAggregateMember,
        IMvcModelPropertySource,
        IInstanceConverter {

        internal AggregateMemberBase(PropertyInfo propertyInfo, Aggregate owner, IServiceProvider serviceProvider) {
            UnderlyingPropertyInfo = propertyInfo;
            Owner = owner;
            _services = serviceProvider;
        }

        internal PropertyInfo UnderlyingPropertyInfo { get; }

        protected readonly IServiceProvider _services;
        private protected IApplicationSchema AppSchema => _services.GetRequiredService<IApplicationSchema>();
        protected IDbSchema DbSchema => _services.GetRequiredService<IDbSchema>();
        protected IViewModelProvider ViewModelProvider => _services.GetRequiredService<IViewModelProvider>();
        protected IAggregateMemberFactory MemberFactory => _services.GetRequiredService<IAggregateMemberFactory>();
        protected Config Config => _services.GetRequiredService<Config>();

        public string Name => UnderlyingPropertyInfo.Name;

        public bool IsPrimaryKey => UnderlyingPropertyInfo.GetCustomAttribute<KeyAttribute>() != null;
        public bool IsInstanceName => UnderlyingPropertyInfo.GetCustomAttribute<InstanceNameAttribute>() != null;
        public int? InstanceNameOrder => UnderlyingPropertyInfo.GetCustomAttribute<InstanceNameAttribute>()?.Order;

        /// <summary>
        /// Entity Framework エンティティ作成時に連番カラムを生成するか否かに影響
        /// </summary>
        public abstract bool IsCollection { get; }


        #region リレーション
        public Aggregate Owner { get; }
        public abstract IEnumerable<Aggregate> GetChildAggregates();
        #endregion リレーション


        #region CodeGenerating
        public abstract IEnumerable<Core.DBModel.DbColumn> ToDbColumnModel();

        public abstract IEnumerable<MvcModelProperty> CreateSearchConditionModels();
        public abstract IEnumerable<MvcModelProperty> CreateSearchResultModels();
        public abstract IEnumerable<MvcModelProperty> CreateInstanceModels();
        #endregion CodeGenerating


        #region Runtime
        public abstract void MapDBToUI(object dbInstance, object uiInstance, Runtime.RuntimeContext context);

        public abstract void BuildSelectStatement(SelectStatement selectStatement, object searchCondition, RuntimeContext context, string selectClausePrefix);
        public abstract void MapSearchResultToUI(System.Data.Common.DbDataReader reader, object searchResult, Runtime.RuntimeContext context, string selectClausePrefix);

        public abstract void BuildAutoCompleteSelectStatement(SelectStatement selectStatement, string inputText, RuntimeContext context, string selectClausePrefix);
        #endregion Runtime

        public abstract IEnumerable<string> GetInvalidErrors();

        private protected abstract void Accept(IMemberVisitor visitor);
        void IAggregateMember.Accept(IMemberVisitor visitor) => Accept(visitor);

        public override string ToString() {
            var path = new List<string>();
            path.Insert(0, Name);

            var parent = Owner.Parent;
            while (parent != null) {
                path.Insert(0, parent.Name);
                parent = parent.Owner.Parent;
            }
            path.Insert(0, Owner.GetRoot().Name);
            return $"{nameof(AggregateMemberBase)}[{string.Join(".", path)}]";
        }
    }
}
