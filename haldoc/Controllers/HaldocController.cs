using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace haldoc.Controllers {
    public class HaldocController : Controller {
        public HaldocController(Schema.ApplicationSchema schema) {
            _modelBuidler = new Models.ModelBuidler(schema);
        }
        private readonly Models.ModelBuidler _modelBuidler;

        #region 一覧画面
        public IActionResult List(Guid aggregateId) {
            var model = _modelBuidler.InitListViewModel(aggregateId);
            return View(model);
        }
        public IActionResult ClearSearchCondition(Models.ListViewModel model) {
            _modelBuidler.ClearSearchCondition(model);
            return View(nameof(List), model);
        }
        public IActionResult ExecuteSearch(Models.ListViewModel model) {
            _modelBuidler.ExecuteSearch(model);
            return View(nameof(List), model);
        }
        #endregion

        #region 新規作成画面
        public IActionResult Create(Guid aggregateId) {
            var model = _modelBuidler.InitCreateViewModel(aggregateId);
            return View(model);
        }
        public IActionResult SaveNewInstance(Models.CreateViewModel model) {
            _modelBuidler.SaveNewInstance(model);
            return View(nameof(Create), model);
        }
        #endregion

        #region シングルビュー
        public IActionResult Single(Guid aggregateId) {
            var model = _modelBuidler.InitSingleViewModel(aggregateId);
            return View(model);
        }
        #endregion
    }
}
