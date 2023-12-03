using HalApplicationBuilder.Features.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Features.Searching {
    partial class SearchFeature {
        internal string RenderControllerAction() {
            var controller = new WebClient.Controller(PhysicalName);

            return $$"""
                {{controller.Render(Context)}}

                namespace {{Context.Config.RootNamespace}} {
                    using Microsoft.AspNetCore.Mvc;
                    using {{Context.Config.EntityNamespace}};

                    partial class {{controller.ClassName}} {
                        [HttpGet("{{WebClient.Controller.SEARCH_ACTION_NAME}}")]
                        public virtual IActionResult Search([FromQuery] string param) {
                            var json = System.Web.HttpUtility.UrlDecode(param);
                            var condition = string.IsNullOrWhiteSpace(json)
                                ? new {{SearchConditionClassName}}()
                                : {{Utility.CLASSNAME}}.{{Utility.PARSE_JSON}}<{{SearchConditionClassName}}>(json);
                            var searchResult = _dbContext
                                .{{DbContextSearchMethodName}}(condition)
                                .AsEnumerable();
                            return this.JsonContent(searchResult);
                        }
                    }
                }
                """;
        }
    }
}
