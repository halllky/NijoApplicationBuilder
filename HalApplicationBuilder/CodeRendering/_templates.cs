using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using HalApplicationBuilder.Core;

namespace HalApplicationBuilder.CodeRendering {

    partial class DbSetTemplate {
        internal DbSetTemplate(Core.Config config, IEnumerable<Core.Aggregate> aggregates) {
            _config = config;
            _aggregates = aggregates;
        }
        private readonly Core.Config _config;
        private readonly IEnumerable<Core.Aggregate> _aggregates;
    }

    partial class EntityClassTemplate {
        internal EntityClassTemplate(Core.Config config, IEnumerable<Core.Aggregate> aggregates) {
            _config = config;
            _dbEntities = aggregates
                .Select(a => a.ToDbEntity());
            _referredDbEntities = aggregates
                .SelectMany(a => a.EnumerateRefTargetsRecursively())
                .Select(refTarget => refTarget.GetEFCoreEntiyHavingOnlyReferredNavigationProp());
        }
        private readonly Core.Config _config;
        private readonly IEnumerable<CodeRendering.RenderedEFCoreEntity> _dbEntities;
        private readonly IEnumerable<CodeRendering.RenderedClass> _referredDbEntities;
    }

    partial class OnModelCreatingTemplate {
        internal OnModelCreatingTemplate(Core.Config config, IEnumerable<Core.Aggregate> aggregates) {
            _config = config;
            _dbEntities = aggregates.Select(a => a.ToDbEntity());
        }
        private readonly Core.Config _config;
        private readonly IEnumerable<CodeRendering.RenderedEFCoreEntity> _dbEntities;
    }

    partial class SearchMethodTemplate {
        internal SearchMethodTemplate(Core.Config config, IEnumerable<Core.RootAggregate> rootAggregates) {
            _config = config;
            _rootAggregates = rootAggregates;
        }
        private readonly Core.Config _config;
        private readonly IEnumerable<Core.RootAggregate> _rootAggregates;
    }

    partial class AutoCompleteSourceTemplate {
        internal AutoCompleteSourceTemplate(Core.Config config, IEnumerable<Core.Aggregate> aggregates) {
            _config = config;
            _aggregates = aggregates;
        }
        private readonly Core.Config _config;
        private readonly IEnumerable<Core.Aggregate> _aggregates;
    }
}


namespace HalApplicationBuilder.CodeRendering.AspNetMvc {
    using ControllerBase = Runtime.AspNetMvc.ControllerBase<Runtime.SearchConditionBase, Runtime.SearchResultBase, Runtime.UIInstanceBase>;

    partial class MvcModelsTemplate {
        internal MvcModelsTemplate(Core.Config config, IEnumerable<Core.Aggregate> aggregates) {
            _config = config;
            _aggregates = aggregates;
        }
        private readonly Core.Config _config;
        private readonly IEnumerable<Core.Aggregate> _aggregates;
    }

    partial class MvcControllerTemplate {
        internal MvcControllerTemplate(Core.Config config, IEnumerable<Core.RootAggregate> rootAggregates) {
            _config = config;
            _rootAggregates = rootAggregates;
        }

        private readonly Core.Config _config;
        private readonly IEnumerable<Core.RootAggregate> _rootAggregates;

        private static string GetBaseClassFullName(Aggregate aggregate) {
            return $"{typeof(ControllerBase).Namespace}.{nameof(ControllerBase)}"
                + $"<{aggregate.ToSearchConditionClass().CSharpTypeName},"
                + $" {aggregate.ToSearchResultClass().CSharpTypeName},"
                + $" {aggregate.ToUiInstanceClass().CSharpTypeName}>";
        }
        private static string GetControllerName(Aggregate aggregate) {
            return $"{aggregate.GetDisplayName()}Controller";
        }
        private string GetMultiViewName(RootAggregate rootAggregate) {
            return $"~/{Path.Combine(_config.MvcViewDirectoryRelativePath, new MvcMultiViewTemplate(_config, rootAggregate).FileName)}";
        }
        private string GetSingleViewName(RootAggregate rootAggregate) {
            return $"~/{Path.Combine(_config.MvcViewDirectoryRelativePath, new MvcSingleViewTemplate(_config, rootAggregate).FileName)}";
        }
        private string GetCreateViewName(RootAggregate rootAggregate) {
            return $"~/{Path.Combine(_config.MvcViewDirectoryRelativePath, new MvcCreateViewTemplate(_config, rootAggregate).FileName)}";
        }

        internal const string CLEAR_ACTION_NAME = nameof(ControllerBase.Clear);
        internal const string SEARCH_ACTION_NAME = nameof(ControllerBase.Search);
        internal const string LINK_TO_SINGLE_VIEW_ACTION_NAME = nameof(ControllerBase.Detail);
        internal const string CREATE_ACTION_NAME = nameof(ControllerBase.Create);
        internal const string UPDATE_ACTION_NAME = nameof(ControllerBase.Update);
        internal const string DELETE_ACTION_NAME = nameof(ControllerBase.Delete);
    }

    partial class MvcMultiViewTemplate : ITemplate {
        internal MvcMultiViewTemplate(Core.Config config, Core.RootAggregate rootAggregate) {
            _config = config;
            _rootAggregate = rootAggregate;

            var searchCondition = rootAggregate.ToSearchConditionClass();
            var searchResult = rootAggregate.ToSearchResultClass();
            var modelType = typeof(Runtime.AspNetMvc.MultiViewModel<,>).Namespace + "." + nameof(Runtime.AspNetMvc.MultiViewModel<object, object>);
            _modelTypeFullname = $"{modelType}<{searchCondition.CSharpTypeName}, {searchResult.CSharpTypeName}>";

            FileName = $"{rootAggregate.GetFileSafeName()}__MultiView.cshtml";
        }
        internal string FileName { get; }

        private readonly Core.Config _config;
        private readonly Core.RootAggregate _rootAggregate;
        private readonly string _modelTypeFullname;

        private static string BoundIdPropertyPathName => $"@Model.SearchResult[i].{nameof(Runtime.SearchResultBase.__halapp__InstanceKey)}";
    }

    partial class MvcSingleViewTemplate {
        internal MvcSingleViewTemplate(Core.Config config, Core.RootAggregate rootAggregate) {
            _config = config;

            var instanceClass = rootAggregate.ToUiInstanceClass();
            var modelType = typeof(Runtime.AspNetMvc.SingleViewModel<>).Namespace + "." + nameof(Runtime.AspNetMvc.SingleViewModel<Runtime.UIInstanceBase>);
            _modelTypeFullname = $"{modelType}<{instanceClass.CSharpTypeName}>";

            _partialViewName = new InstancePartialViewTemplate(config, rootAggregate).FileName;

            _boundObjectName = nameof(Runtime.AspNetMvc.SingleViewModel<Runtime.UIInstanceBase>.Item);

            FileName = $"{rootAggregate.GetFileSafeName()}__SingleView.cshtml";
        }
        internal string FileName { get; }

        private readonly Core.Config _config;
        private readonly string _boundObjectName;
        private readonly string _modelTypeFullname;
        private readonly string _partialViewName;
    }

    partial class MvcCreateViewTemplate {
        internal MvcCreateViewTemplate(Core.Config config, Core.RootAggregate rootAggregate) {
            _config = config;

            var instanceClass = rootAggregate.ToUiInstanceClass();
            var modelType = typeof(Runtime.AspNetMvc.CreateViewModel<>).Namespace + "." + nameof(Runtime.AspNetMvc.CreateViewModel<Runtime.UIInstanceBase>);
            _modelTypeFullname = $"{modelType}<{instanceClass.CSharpTypeName}>";

            _partialViewName = new InstancePartialViewTemplate(config, rootAggregate).FileName;

            _boundObjectName = nameof(Runtime.AspNetMvc.CreateViewModel<Runtime.UIInstanceBase>.Item);

            FileName = $"{rootAggregate.GetFileSafeName()}__CreateView.cshtml";
        }
        internal string FileName { get; }

        private readonly Core.Config _config;
        private readonly string _boundObjectName;
        private readonly string _modelTypeFullname;
        private readonly string _partialViewName;
    }

    partial class InstancePartialViewTemplate : ITemplate {
        internal InstancePartialViewTemplate(Core.Config config, Core.Aggregate aggregate) {
            _config = config;
            _aggregate = aggregate;

            _hiddenFields = new[] {
                ($"{nameof(Runtime.UIInstanceBase.halapp_fields)}.{nameof(Runtime.HalappViewState.IsRoot)}", ""),
                ($"{nameof(Runtime.UIInstanceBase.halapp_fields)}.{nameof(Runtime.HalappViewState.Removed)}", JsTemplate.REMOVE_HIDDEN_FIELD),
            };

            _modelTypeFullname = aggregate.ToUiInstanceClass().CSharpTypeName;

            _showRemoveButton = $"Model.{nameof(Runtime.UIInstanceBase.halapp_fields)}.{nameof(Runtime.HalappViewState.IsRoot)} == false";

            FileName = $"_{aggregate.GetFileSafeName()}__Partial.cshtml"; ;
        }

        internal string FileName { get; }

        private readonly Core.Config _config;
        private readonly Core.Aggregate _aggregate;
        private readonly IEnumerable<(string AspFor, string Class)> _hiddenFields;
        private readonly string _showRemoveButton;
        private readonly string _modelTypeFullname;
    }
}

