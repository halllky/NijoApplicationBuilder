using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace haldoc.Controllers {
    public class HaldocController : Controller {
        public HaldocController(haldoc.Core.ProjectContext context) {
            _projectContext = context;
        }
        private readonly haldoc.Core.ProjectContext _projectContext;

        #region 一覧画面
        public IActionResult List(Guid aggregateId) {
            throw new NotImplementedException();
        }
        public IActionResult ClearSearchCondition(haldoc.Runtime.ListViewModel model) {
            throw new NotImplementedException();
        }
        public IActionResult ExecuteSearch(haldoc.Runtime.ListViewModel model) {
            throw new NotImplementedException();
        }
        #endregion

        #region 新規作成画面
        public IActionResult Create(Guid aggregateId) {
            throw new NotImplementedException();
        }
        [HttpPost]
        public IActionResult SaveNewInstance(haldoc.Runtime.SingleViewModel model) {
            throw new NotImplementedException();
        }
        #endregion

        #region シングルビュー
        public IActionResult Single(Guid aggregateId) {
            throw new NotImplementedException();
        }
        #endregion
    }
}
