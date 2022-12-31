using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HalApplicationBuilder.AspNetMvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace HalApplicationBuilder.AspNetMvc {

    public abstract class ControllerBase<TSearchCondition, TSearchResult, TInstanceModel>
        : Microsoft.AspNetCore.Mvc.Controller {

        public ControllerBase(IServiceProvider services) {
            ServiceProvider = services;
            RuntimeContext = services.GetRequiredService<Runtime.RuntimeContext>();
        }
        protected IServiceProvider ServiceProvider { get; }
        private Runtime.RuntimeContext RuntimeContext { get; }

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
            model.SearchResult = RuntimeContext
                .Search(model.SearchCondition)
                .Cast<TSearchResult>()
                .ToList();
            return View(MultiViewName, model);
        }
        public virtual IActionResult Clear(MultiView.Model<TSearchCondition, TSearchResult> model) {
            ModelState.Clear();
            model.SearchCondition = Activator.CreateInstance<TSearchCondition>();
            return View(MultiViewName, model);
        }
        #endregion MultiView


        #region CreateView
        protected abstract string CreateViewName { get; }

        public virtual IActionResult New() {
            var model = new CreateView.Model<TInstanceModel> {
                Item = RuntimeContext.CreateInstance<TInstanceModel>(),
            };
            return View(CreateViewName, model);
        }
        [HttpPost]
        public virtual IActionResult Create(CreateView.Model<TInstanceModel> model) {
            var id = RuntimeContext.SaveNewInstance(model.Item);
            return RedirectToAction(nameof(Detail), new { id = id.StringValue });
        }
        #endregion CreateView


        #region SingleView
        protected abstract string SingleViewName { get; }

        public virtual IActionResult Detail(string id) {
            var aggregate = RuntimeContext.FindAggregateByRuntimeType(typeof(TInstanceModel));
            var key = new Runtime.InstanceKey(id, aggregate);
            var instance = RuntimeContext.FindInstance(key);
            if (instance == null) return NotFound();

            var model = new SingleView.Model<TInstanceModel> {
                InstanceName = new Runtime.InstanceName(instance, aggregate).Value,
                Item = (TInstanceModel)instance,
            };
            return View(SingleViewName, model);
        }
        [HttpPost]
        public virtual IActionResult Update(SingleView.Model<TInstanceModel> model) {
            RuntimeContext.UpdateInstance(model.Item);
            return View(SingleViewName, model);
        }
        [HttpPost]
        public virtual IActionResult Delete(SingleView.Model<TInstanceModel> model) {
            RuntimeContext.DeleteInstance(model);
            return RedirectToAction(nameof(Index));
        }
        #endregion SingleView
    }
}
