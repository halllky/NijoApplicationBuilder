using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace HalApplicationBuilder.ReArchTo関数型.Runtime.AspNetMvc {
    public abstract class ControllerBase<TSearchCondition, TSearchResult, TInstanceModel>
        : Microsoft.AspNetCore.Mvc.Controller
        where TInstanceModel : UIInstanceBase
        where TSearchCondition : SearchConditionBase
        where TSearchResult : SearchResultBase {

        public ControllerBase(IServiceProvider services) {
            ServiceProvider = services;
            HalApp = services.GetRequiredService<HalApp.RuntimeService>();
        }
        protected IServiceProvider ServiceProvider { get; }
        protected HalApp.RuntimeService HalApp { get; }


        #region MultiView
        protected abstract string MultiViewName { get; }

        [HttpGet]
        public virtual IActionResult Index(TSearchCondition searchCondition) {
            var model = new MultiViewModel<TSearchCondition, TSearchResult> {
                SearchCondition = searchCondition,
                SearchResult = new(),
            };
            return View(MultiViewName, model);
        }
        [HttpGet]
        public virtual IActionResult Search(MultiViewModel<TSearchCondition, TSearchResult> model) {
            model.SearchResult = HalApp
                .Search(model.SearchCondition)
                .Cast<TSearchResult>()
                .ToList();

            return View(MultiViewName, model);
        }
        [HttpGet]
        public virtual IActionResult Clear(MultiViewModel<TSearchCondition, TSearchResult> model) {
            ModelState.Clear();
            model.SearchCondition = (TSearchCondition)HalApp.CreateSearchCondition(typeof(TSearchCondition));
            return View(MultiViewName, model);
        }
        #endregion MultiView


        #region CreateView, SingleView
        protected abstract string CreateViewName { get; }
        protected abstract string SingleViewName { get; }

        [HttpGet]
        public virtual IActionResult New(CreateViewModel<TInstanceModel> model) {
            if (model.Item == null) {
                var instance = (TInstanceModel)HalApp.CreateUIInstance(typeof(TInstanceModel));
                instance.halapp_fields.IsRoot = true; // TODO: HalAppの中でやるべきでは
                model.Item = instance;
            }
            return View(CreateViewName, model);
        }
        [HttpPost]
        public virtual IActionResult Create(CreateViewModel<TInstanceModel> model) {
            if (HalApp.TrySaveNewInstance(model.Item, out var id, out var errors)) {
                return RedirectToAction(nameof(Detail), new { id });
            } else {
                foreach (var err in errors) ModelState.AddModelError("", err);
                return View(CreateViewName, model);
            }
        }

        [HttpGet]
        public virtual IActionResult Detail(string id) {
            var instance = HalApp.FindInstance<TInstanceModel>(id, out var name);

            if (instance == null) {
                return NotFound();
            }

            instance.halapp_fields.IsRoot = true; // TODO: HalAppの中でやるべきでは
            var model = new SingleViewModel<TInstanceModel> {
                InstanceName = name,
                Item = instance,
            };
            return View(SingleViewName, model);
        }
        [HttpPost]
        public virtual IActionResult Update(SingleViewModel<TInstanceModel> model) {
            if (HalApp.TryUpdate(model.Item, out var id, out var errors)) {
                return RedirectToAction(nameof(Detail), new { id });
            } else {
                foreach (var err in errors) ModelState.AddModelError("", err);
                return View(SingleViewName, model);
            }
        }
        [HttpPost]
        public virtual IActionResult Delete(SingleViewModel<TInstanceModel> model) {
            HalApp.DeleteInstance(model.Item);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public virtual IActionResult NewChild([FromServices] Core.Config config, string aggregateTreePath, string modelPath, int currentArrayCount) {

            var instance = HalApp.CreateUIInstance(aggregateTreePath);

            var aggregate = HalApp.FindAggregate(aggregateTreePath);
            if (aggregate == null) return BadRequest($"{aggregateTreePath} と対応するメンバーが見つからない");
            var partialView = new CodeRendering.AspNetMvc.InstancePartialViewTemplate(config, aggregate);
            var partialViewPath = Path.Combine("~", config.MvcViewDirectoryRelativePath, partialView.FileName);

            // name属性用
            var index = $"[{currentArrayCount}]";
            ViewData.TemplateInfo.HtmlFieldPrefix = $"{nameof(SingleViewModel<UIInstanceBase>.Item)}.{modelPath + index}";

            return PartialView(partialViewPath, instance);
        }
        #endregion CreateView, SingleView


        [HttpGet]
        public virtual IActionResult Autocomplete(Guid aggregateGuid, string term) {
            var items = HalApp.LoadAutoCompleteDataSource(aggregateGuid, term).ToArray();
            return Json(items);
        }
    }
}

