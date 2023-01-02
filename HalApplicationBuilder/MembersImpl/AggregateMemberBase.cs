using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.EntityFramework;

namespace HalApplicationBuilder.MembersImpl {
    public abstract class AggregateMemberBase :
        Core.IAggregateMember,
        AspNetMvc.IMvcModelPropertyFactory {

        internal ApplicationSchema Schema { get; init; }
        internal PropertyInfo UnderlyingPropertyInfo { get; init; }

        public string Name => UnderlyingPropertyInfo.Name;
        public bool IsPrimaryKey => UnderlyingPropertyInfo.GetCustomAttribute<KeyAttribute>() != null;
        /// <summary>
        /// Entity Framework エンティティ作成時に連番カラムを生成するか否かに影響
        /// </summary>
        public abstract bool IsCollection { get; }


        #region リレーション
        public Aggregate Owner { get; init; }
        public abstract IEnumerable<Aggregate> GetChildAggregates();
        #endregion リレーション


        #region CodeGenerating
        public abstract IEnumerable<DbColumn> ToDbColumnModel();

        private IReadOnlyList<AspNetMvc.MvcModelProperty> _searchConditionClass;
        private IReadOnlyList<AspNetMvc.MvcModelProperty> _searchResultClass;
        private IReadOnlyList<AspNetMvc.MvcModelProperty> _instanceClass;

        internal IReadOnlyList<AspNetMvc.MvcModelProperty> SearchConditionModels {
            get {
                if (_searchConditionClass == null) {
                    _searchConditionClass = CreateSearchConditionModels(this).ToArray();
                    foreach (var x in _searchConditionClass) x.Source = this;
                }
                return _searchConditionClass;
            }
        }
        internal IReadOnlyList<AspNetMvc.MvcModelProperty> SearchResultModels {
            get {
                if (_searchResultClass == null) {
                    _searchResultClass = CreateSearchResultModels(this).ToArray();
                    foreach (var x in _searchResultClass) x.Source = this;
                }
                return _searchResultClass;
            }
        }
        internal IReadOnlyList<AspNetMvc.MvcModelProperty> InstanceModels {
            get {
                if (_instanceClass == null) {
                    _instanceClass = CreateInstanceModels(this).ToArray();
                    foreach (var x in _instanceClass) x.Source = this;
                }
                return _instanceClass;
            }
        }

        public abstract IEnumerable<AspNetMvc.MvcModelProperty> CreateSearchConditionModels(IAggregateMember member);
        public abstract IEnumerable<AspNetMvc.MvcModelProperty> CreateSearchResultModels(IAggregateMember member);
        public abstract IEnumerable<AspNetMvc.MvcModelProperty> CreateInstanceModels(IAggregateMember member);

        internal abstract string RenderSearchConditionView(AspNetMvc.ViewRenderingContext context);
        internal abstract string RenderSearchResultView(AspNetMvc.ViewRenderingContext context);
        internal abstract string RenderInstanceView(AspNetMvc.ViewRenderingContext context);
        #endregion CodeGenerating


        #region Runtime

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
