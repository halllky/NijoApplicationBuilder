using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HalApplicationBuilder.Core.Runtime;
using HalApplicationBuilder.Core.UIModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace HalApplicationBuilder.AspNetMvc {

    public abstract class ControllerBase<TSearchCondition, TSearchResult, TInstanceModel>
        : Microsoft.AspNetCore.Mvc.Controller
        where TInstanceModel : UIInstanceBase
        where TSearchResult : SearchResultBase {

        public ControllerBase(IServiceProvider services) {
            ServiceProvider = services;
            RuntimeContext = services.GetRequiredService<RuntimeContext>();
        }
        protected IServiceProvider ServiceProvider { get; }
        private RuntimeContext RuntimeContext { get; }

        #region MultiView
        protected abstract string MultiViewName { get; }

        [HttpGet]
        public virtual IActionResult Index(TSearchCondition searchCondition) {
            var model = new MultiView.Model<TSearchCondition, TSearchResult> {
                SearchCondition = searchCondition,
                SearchResult = new(),
            };
            return View(MultiViewName, model);
        }
        [HttpGet]
        public virtual IActionResult Search(MultiView.Model<TSearchCondition, TSearchResult> model) {
            model.SearchResult = RuntimeContext
                .Search(model.SearchCondition)
                .Cast<TSearchResult>()
                .ToList();

            return View(MultiViewName, model);
        }
        [HttpGet]
        public virtual IActionResult Clear(MultiView.Model<TSearchCondition, TSearchResult> model) {
            ModelState.Clear();
            model.SearchCondition = Activator.CreateInstance<TSearchCondition>();
            return View(MultiViewName, model);
        }
        #endregion MultiView


        #region CreateView, SingleView
        protected abstract string CreateViewName { get; }
        protected abstract string SingleViewName { get; }

        [HttpGet]
        public virtual IActionResult New(CreateView.Model<TInstanceModel> model) {
            if (model.Item == null) {
                var instance = RuntimeContext.CreateInstance<TInstanceModel>();
                instance.halapp_fields.IsRoot = true;
                model.Item = instance;
            }
            return View(CreateViewName, model);
        }
        [HttpPost]
        public virtual IActionResult Create(CreateView.Model<TInstanceModel> model) {
            if (RuntimeContext.SaveNewInstance(model.Item, out var id, out var errors)) {
                return RedirectToAction(nameof(Detail), new { id = id.StringValue });
            } else {
                foreach (var err in errors) ModelState.AddModelError("", err);
                return View(CreateViewName, model);
            }
        }

        [HttpGet]
        public virtual IActionResult Detail(string id) {
            var aggregate = RuntimeContext.FindAggregateByRuntimeType(typeof(TInstanceModel));
            var dbEntity = RuntimeContext.DbSchema.GetDbEntity(aggregate);
            if (!InstanceKey.TryParse(id, dbEntity, out var instanceKey)) return NotFound();

            var instance = RuntimeContext.FindInstance<TInstanceModel>(instanceKey);
            if (instance == null) return NotFound();

            instance.halapp_fields.IsRoot = true;
            var model = new SingleView.Model<TInstanceModel> {
                InstanceName = InstanceName.Create(instance, dbEntity).Value,
                Item = instance,
            };

            return View(SingleViewName, model);
        }
        [HttpPost]
        public virtual IActionResult Update(SingleView.Model<TInstanceModel> model) {
            if (RuntimeContext.UpdateInstance(model.Item, out var id, out var errors)) {
                return RedirectToAction(nameof(Detail), new { id = id.StringValue });
            } else {
                foreach (var err in errors) ModelState.AddModelError("", err);
                return View(SingleViewName, model);
            }
        }
        [HttpPost]
        public virtual IActionResult Delete(SingleView.Model<TInstanceModel> model) {
            RuntimeContext.DeleteInstance(model);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public virtual IActionResult NewChild(
            [FromServices] Core.IApplicationSchema applicationSchema,
            [FromServices] IViewModelProvider viewModelProvider,
            [FromServices] Core.Config config,
            string aggregateTreePath,
            string modelPath,
            int currentArrayCount) {

            // 新インスタンスを作成
            var aggregate = applicationSchema.FindByPath(aggregateTreePath);
            if (aggregate == null) return BadRequest($"{aggregateTreePath} と対応するメンバーが見つからない");
            var partialView = new InstancePartialView(aggregate, config);
            var instanceModel = viewModelProvider.GetInstanceModel(aggregate);
            var newChildType = RuntimeContext.RuntimeAssembly.GetType(instanceModel.RuntimeFullName);
            var instance = RuntimeContext.CreateInstance(newChildType);

            // name属性用
            var index = $"[{currentArrayCount}]";
            ViewData.TemplateInfo.HtmlFieldPrefix = modelPath + index;

            return PartialView(partialView.AspViewPath, instance);
        }
        #endregion CreateView, SingleView


        [HttpGet]
        public virtual IActionResult Autocomplete(Guid aggregateGuid, string term) {
            var aggregate = RuntimeContext.FindAggregateByGuid(aggregateGuid);
            var items = RuntimeContext.LoadAutoCompleteDataSource(aggregate, term);
            return Json(items.ToArray());
        }
    }
}
