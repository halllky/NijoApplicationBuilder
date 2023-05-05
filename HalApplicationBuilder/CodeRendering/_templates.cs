using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using HalApplicationBuilder.Core;

namespace HalApplicationBuilder.CodeRendering {

    partial class DefaultRuntimeConfigTemplate {
        internal DefaultRuntimeConfigTemplate(Core.Config config) {
            _dbContextFullName = $"{config.DbContextNamespace}.{config.DbContextName}";
        }
        private readonly string _dbContextFullName;
    }

    partial class UIModelsTemplate {
        internal UIModelsTemplate(Core.Config config, IEnumerable<Core.Aggregate> aggregates) {
            _config = config;
            _aggregates = aggregates;
        }
        private readonly Core.Config _config;
        private readonly IEnumerable<Core.Aggregate> _aggregates;
    }
}

namespace HalApplicationBuilder.CodeRendering.EFCore {

        partial class DbContextTemplate {
        internal DbContextTemplate(Core.Config config) {
            _nameSpaceName = config.DbContextNamespace;
            _dbContextName = config.DbContextName;
        }
        private readonly string _nameSpaceName;
        private readonly string _dbContextName;
    }

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

namespace HalApplicationBuilder.CodeRendering.ReactAndWebApi {

    partial class WebApiDebuggerTemplate {
        internal WebApiDebuggerTemplate(Config config) {
            _config = config;
        }
        private readonly Config _config;
    }

    partial class WebApiControllerTemplate {
        internal WebApiControllerTemplate(Config config, IEnumerable<RootAggregate> rootAggregates) {
            _config = config;
            _rootAggregates = rootAggregates;
        }
        private readonly Config _config;
        private readonly IEnumerable<RootAggregate> _rootAggregates;

        private static string GetControllerName(RootAggregate rootAggregate) {
            return $"{rootAggregate.GetCSharpSafeName()}Controller";
        }
    }

    partial class ReactTypeDefTemplate {
        internal ReactTypeDefTemplate(IEnumerable<Aggregate> aggregates) {
            _aggregates = aggregates;
        }
        private readonly IEnumerable<Aggregate> _aggregates;

        internal const string FILE_NAME = "halapp.types.ts";
    }

    partial class ReactComponentTemplate : ITemplate {
        internal ReactComponentTemplate(RootAggregate rootAggregate) {
            _rootAggregate = rootAggregate;
            _searchCondition = rootAggregate.ToSearchConditionClass();
            _searchResult = rootAggregate.ToSearchResultClass();
            _uiInstance = rootAggregate.ToUiInstanceClass();

            FileName = $"{rootAggregate.GetFileSafeName()}.tsx";
        }

        internal string FileName { get; }
        internal string MultiViewComponentName => $"HalappMultiViewOF{_rootAggregate.GetGuid().ToString().Replace("-", "")}";
        internal string CreateViewComponentName => $"HalappCreateViewOF{_rootAggregate.GetGuid().ToString().Replace("-", "")}";
        internal string SingleViewComponentName => $"HalappSingleViewOF{_rootAggregate.GetGuid().ToString().Replace("-", "")}";
        private string MultiViewUrl => menuItems.GetMultiViewUrl(_rootAggregate);
        private string CreateViewUrl => menuItems.GetCreateViewUrl(_rootAggregate);

        private readonly RootAggregate _rootAggregate;
        private readonly RenderedClass _searchCondition;
        private readonly RenderedClass _searchResult;
        private readonly RenderedClass _uiInstance;

        private static string GetImportFromTypes() => $"../{Path.GetFileNameWithoutExtension(ReactTypeDefTemplate.FILE_NAME)}";
    }

#pragma warning disable IDE1006 // 命名スタイル
    partial class menuItems {
#pragma warning restore IDE1006 // 命名スタイル
        internal menuItems(IEnumerable<RootAggregate> rootAggregates) {
            _rootAggregates = rootAggregates;
        }

        private readonly IEnumerable<RootAggregate> _rootAggregates;

        private static string GetImport(RootAggregate rootAggregate) {
            return $"./{GeneratedProject.REACT_PAGE_DIR}/{Path.GetFileNameWithoutExtension(new ReactComponentTemplate(rootAggregate).FileName)}";
        }
        internal static string GetMultiViewUrl(RootAggregate rootAggregate) => $"/{rootAggregate.GetGuid()}";
        internal static string GetCreateViewUrl(RootAggregate rootAggregate) => $"/{rootAggregate.GetGuid()}/new";
        internal static string GetSingleViewUrl(RootAggregate rootAggregate) => $"/{rootAggregate.GetGuid()}/detail/{{id}}";
        private static string GetMultiViewComponentName(RootAggregate rootAggregate) => new ReactComponentTemplate(rootAggregate).MultiViewComponentName;
        private static string GetCreateViewComponentName(RootAggregate rootAggregate) => new ReactComponentTemplate(rootAggregate).CreateViewComponentName;
        private static string GetSingleViewComponentName(RootAggregate rootAggregate) => new ReactComponentTemplate(rootAggregate).SingleViewComponentName;
        internal const string FILE_NAME = "menuItems.tsx";
    }

#pragma warning disable IDE1006 // 命名スタイル
    partial class index {
#pragma warning restore IDE1006 // 命名スタイル
        internal index(IEnumerable<RootAggregate> rootAggregates) {
            _rootAggregates = rootAggregates;
        }

        private readonly IEnumerable<RootAggregate> _rootAggregates;

        private static string GetImportFromMenuItems() {
            return $"./{Path.GetFileNameWithoutExtension(menuItems.FILE_NAME)}";
        }
        private static string GetImport(RootAggregate rootAggregate) {
            return $"./{GeneratedProject.REACT_PAGE_DIR}/{Path.GetFileNameWithoutExtension(new ReactComponentTemplate(rootAggregate).FileName)}";
        }
        internal const string FILE_NAME = "index.ts";
    }
}
