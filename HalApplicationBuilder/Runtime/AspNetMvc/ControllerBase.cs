using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HalApplicationBuilder.Runtime.AspNetMvc;
using Microsoft.AspNetCore.Mvc;

namespace HalApplicationBuilder.Runtime {

    public abstract class ControllerBase<TSearchCondition, TSearchResult, TInstanceModel>
        : Microsoft.AspNetCore.Mvc.Controller {

        #region MultiView
        protected abstract string MultiViewName { get; }

        public virtual IActionResult Index(TSearchCondition searchCondition) {
            var model = new MultiView.Model<TSearchCondition, TSearchResult> {
                SearchCondition = searchCondition,
                SearchResult = new(),
            };
            return View(MultiViewName, model);
        }
        public virtual IActionResult Search(MultiView.Model<TSearchCondition, TSearchResult> model) {
            model.SearchResult = new(); // TODO
            return View(MultiViewName, model);
        }
        #endregion MultiView


        #region CreateView
        protected abstract string CreateViewName { get; }

        public virtual IActionResult New() {
            var model = new CreateView.Model<TInstanceModel> {
                Item = Activator.CreateInstance<TInstanceModel>(), // TODO
            };
            return View(CreateViewName, model);
        }
        [HttpPost]
        public virtual IActionResult Create(CreateView.Model<TInstanceModel> model) {
            // TODO: CREATE
            var id = "12345678"; // TODO
            return RedirectToAction(nameof(Detail), new { id });
        }
        #endregion CreateView


        #region SingleView
        protected abstract string SingleViewName { get; }

        public virtual IActionResult Detail(string id) {
            var model = new SingleView.Model<TInstanceModel> {
                InstanceName = "TODO",
                Item = Activator.CreateInstance<TInstanceModel>(), // TODO
            };
            return View(SingleViewName, model);
        }
        [HttpPost]
        public virtual IActionResult Update(SingleView.Model<TInstanceModel> model) {
            // TODO: UPDATE
            return View(SingleViewName, model);
        }
        [HttpPost]
        public virtual IActionResult Delete() {
            // TODO: DELETE
            return RedirectToAction(nameof(Index));
        }
        #endregion SingleView
    }
}
