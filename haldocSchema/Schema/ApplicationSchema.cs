using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using haldoc.Models;

namespace haldoc.Schema {
    public abstract class ApplicationSchema {

        public abstract string ApplicationName { get; }
        protected abstract IEnumerable<Type> RegisterAggregates();

        private HashSet<Type> _cache;
        private HashSet<Type> CachedTypes {
            get {
                if (_cache == null) _cache = RegisterAggregates().ToHashSet();
                return _cache;
            }
        }

        /// <summary>プロトタイプのためDBは適当な配列で代用している</summary>
        private HashSet<object> DB { get; }
        public ApplicationSchema(HashSet<object> db = null) {
            DB = db ?? new();
        }

        public IEnumerable<KeyValuePair<Guid, string>> GetMenu() {
            foreach (var type in CachedTypes) {
                if (type.GetCustomAttribute<AggregateRootAttribute>() == null) continue;
                yield return KeyValuePair.Create(type.GUID, type.Name);
            }
        }

        #region 一覧画面
        public ListViewModel InitListViewModel(Guid aggregateId) {
            var aggregate = CachedTypes.Single(a => a.GUID == aggregateId);
            var props = aggregate
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(prop => prop.GetCustomAttribute<ChildrenAttribute>() == null);

            var searchConditionItems = props
                .Select(prop => new SearchConditionItem {
                    Name = prop.Name,
                    Value = "",
                })
                .ToArray();
            var tableHeader = props.Select(prop => prop.Name).ToArray();

            var model = new ListViewModel {
                PageTitle = aggregate.Name,
                AggregateID = aggregate.GUID,
                SearchConditionItems = searchConditionItems,
                TableHeader = tableHeader,
                SearchResults = new List<IList<string>>(),
            };

            ExecuteSearch(model);

            return model;
        }

        public void ClearSearchCondition(ListViewModel model) {
            foreach (var item in model.SearchConditionItems) {
                item.Value = "";
            }
        }
        public void ExecuteSearch(ListViewModel model) {
            var aggregate = CachedTypes.Single(a => a.GUID == model.AggregateID);
            var query = DB.Where(item => item.GetType() == aggregate);

            foreach (var searchCondition in model.SearchConditionItems) {
                if (string.IsNullOrWhiteSpace(searchCondition.Value)) continue;
                var prop = aggregate.GetProperty(searchCondition.Name);
                query = query.Where(item => prop.GetValue(item)?.ToString() == searchCondition.Value);
            }
            var searchResult = query.ToArray();

            var props = aggregate
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(prop => prop.GetCustomAttribute<ChildrenAttribute>() == null)
                .ToArray();
            var values = new List<IList<string>>();
            for (int i = 0; i < searchResult.Length; i++) {
                var row = new string[props.Length];
                for (int j = 0; j < props.Length; j++) {
                    row[j] = props[j].GetValue(searchResult[i])?.ToString();
                }
                values.Add(row);
            }
            model.SearchResults = values;
        }

        #endregion

        #region 新規作成画面
        public CreateViewModel InitCreateViewModel(Guid aggregateId) {
            var aggregate = CachedTypes.Single(a => a.GUID == aggregateId);
            var instance = Activator.CreateInstance(aggregate);

            return new CreateViewModel {
                PageTitle = $"{aggregate.Name} - 新規作成",
                AggregateID = aggregate.GUID,
                Instance = instance,
            };
        }
        public PropertyInfo[] EnumerateProps(Guid typeGuid) {
            var type = CachedTypes.Single(t => t.GUID == typeGuid);
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        }
        public void AddChild(CreateViewModel model, string propName) {
            var aggregate = CachedTypes.Single(a => a.Name == model.PageTitle);
            var prop = aggregate.GetProperty(propName);
        }
        public void SaveNewInstance(CreateViewModel model) {
            DB.Add(model.Instance);
        }
        #endregion

        #region シングルビュー
        public Models.SingleViewModel InitSingleViewModel(Guid aggregateId) {
            var random = new Random();
            var item = DB
                .Where(x => x.GetType().GUID == aggregateId)
                .OrderBy(_ => random.Next())
                .FirstOrDefault();

            return new SingleViewModel {
                AggregateID = aggregateId,
                Instance = item,
                PageTitle = "",
                UpdateTime = DateTime.Now,
            };
        }
        #endregion

        public string GetInstanceName(object instance) {
            if (instance == null) return "";

            var instanceNameProps = instance
                .GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(prop => prop.GetCustomAttribute<InstanceNameAttribute>() != null);

            var name = "";
            foreach (var prop in instanceNameProps) {
                name += prop.GetValue(instance)?.ToString();
            }

            return name;
        }
    }
}
