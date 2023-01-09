using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.EntityFramework;
using HalApplicationBuilder.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace HalApplicationBuilder.Impl {
    public abstract class AggregateMemberBase :
        IAggregateMember,
        AspNetMvc.IMvcModelPropertySource,
        AspNetMvc.IMemberRenderer,
        Runtime.IInstanceConverter {

        internal AggregateMemberBase(PropertyInfo propertyInfo, Aggregate owner, IServiceProvider serviceProvider) {
            UnderlyingPropertyInfo = propertyInfo;
            Owner = owner;
            _services = serviceProvider;
        }

        internal PropertyInfo UnderlyingPropertyInfo { get; }

        protected readonly IServiceProvider _services;
        private protected IApplicationSchema AppSchema => _services.GetRequiredService<IApplicationSchema>();
        protected EntityFramework.IDbSchema DbSchema => _services.GetRequiredService<EntityFramework.IDbSchema>();
        protected AspNetMvc.IViewModelProvider ViewModelProvider => _services.GetRequiredService<AspNetMvc.IViewModelProvider>();
        protected IAggregateMemberFactory MemberFactory => _services.GetRequiredService<IAggregateMemberFactory>();
        protected Config Config => _services.GetRequiredService<Config>();

        public string Name => UnderlyingPropertyInfo.Name;
        public bool IsPrimaryKey => UnderlyingPropertyInfo.GetCustomAttribute<KeyAttribute>() != null;
        /// <summary>
        /// Entity Framework エンティティ作成時に連番カラムを生成するか否かに影響
        /// </summary>
        public abstract bool IsCollection { get; }


        #region リレーション
        public Aggregate Owner { get; }
        public abstract IEnumerable<Aggregate> GetChildAggregates();
        #endregion リレーション


        #region CodeGenerating
        public abstract IEnumerable<DbColumn> ToDbColumnModel();

        public abstract IEnumerable<AspNetMvc.MvcModelProperty> CreateSearchConditionModels();
        public abstract IEnumerable<AspNetMvc.MvcModelProperty> CreateSearchResultModels();
        public abstract IEnumerable<AspNetMvc.MvcModelProperty> CreateInstanceModels();

        public abstract string RenderSearchConditionView(AspNetMvc.ViewRenderingContext context);
        public abstract string RenderSearchResultView(AspNetMvc.ViewRenderingContext context);
        public abstract string RenderInstanceView(AspNetMvc.ViewRenderingContext context);
        #endregion CodeGenerating


        #region Runtime
        public abstract void MapUIToDB(object uiInstance, object dbInstance, Runtime.RuntimeContext context);
        public abstract void MapDBToUI(object dbInstance, object uiInstance, Runtime.RuntimeContext context);

        public abstract void BuildSelectStatement(SelectStatement selectStatement, object searchCondition, RuntimeContext context, string selectClausePrefix);
        public abstract void MapSearchResultToUI(System.Data.Common.DbDataReader reader, object searchResult, Runtime.RuntimeContext context, string selectClausePrefix);
        #endregion Runtime


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
