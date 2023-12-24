using Nijo.Core;
using Nijo.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Architecture.WebClient {
    public class Controller {
        internal Controller(string physicalName) {
            _physicalName = physicalName;
        }
        internal Controller(Aggregate aggregate) : this(aggregate.DisplayName.ToCSharpSafe()) {
        }

        private readonly string _physicalName;

        public string ClassName => $"{_physicalName}Controller";

        internal const string SEARCH_ACTION_NAME = "list";
        internal const string CREATE_ACTION_NAME = "create";
        internal const string UPDATE_ACTION_NAME = "update";
        internal const string DELETE_ACTION_NAME = "delete";
        internal const string FIND_ACTION_NAME = "detail";

        internal const string SUBDOMAIN = "api";

        internal string SubDomain => $"{SUBDOMAIN}/{_physicalName}";
        internal string SearchCommandApi => $"/{SubDomain}/{SEARCH_ACTION_NAME}";
        internal string CreateCommandApi => $"/{SubDomain}/{CREATE_ACTION_NAME}";
        internal string UpdateCommandApi => $"/{SubDomain}/{UPDATE_ACTION_NAME}";
        internal string DeleteCommandApi => $"/{SubDomain}/{DELETE_ACTION_NAME}";
    }
}
