using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.Search {
    partial class SearchFeature {
        internal string RenderControllerAction() {
            var controller = new WebClient.Controller(PhysicalName, Context);

            return $$"""
                {{controller.Render()}}

                namespace {{Context.Config.RootNamespace}} {
                    using Microsoft.AspNetCore.Mvc;
                    using {{Context.Config.EntityNamespace}};

                    partial class {{controller.ClassName}} {
                        [HttpGet("{{WebClient.Controller.SEARCH_ACTION_NAME}}")]
                        public virtual IActionResult Search([FromQuery] string param) {
                            var json = System.Web.HttpUtility.UrlDecode(param);
                            var condition = string.IsNullOrWhiteSpace(json)
                                ? new {{SearchConditionClassName}}()
                                : System.Text.Json.JsonSerializer.Deserialize<{{SearchConditionClassName}}>(json)!;
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
