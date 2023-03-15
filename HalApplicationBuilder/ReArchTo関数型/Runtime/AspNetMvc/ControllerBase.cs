using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace HalApplicationBuilder.ReArchTo関数型.Runtime.AspNetMvc {
    public abstract class ControllerBase<TSearchCondition, TSearchResult, TInstanceModel>
        : Microsoft.AspNetCore.Mvc.Controller
        where TInstanceModel : UIInstanceBase
        where TSearchResult : SearchResultBase {

        public ControllerBase(IServiceProvider services) {
            ServiceProvider = services;
        }
        protected IServiceProvider ServiceProvider { get; }


        #region MultiView
        protected abstract string MultiViewName { get; }

        [HttpGet]
        public virtual IActionResult Index(TSearchCondition searchCondition) {
            throw new NotImplementedException();
        }
        [HttpGet]
        public virtual IActionResult Search(MultiViewModel<TSearchCondition, TSearchResult> model) {
            throw new NotImplementedException();
        }
        [HttpGet]
        public virtual IActionResult Clear(MultiViewModel<TSearchCondition, TSearchResult> model) {
            throw new NotImplementedException();
        }
        #endregion MultiView


        #region CreateView, SingleView
        protected abstract string CreateViewName { get; }
        protected abstract string SingleViewName { get; }

        [HttpGet]
        public virtual IActionResult New(CreateViewModel<TInstanceModel> model) {
            throw new NotImplementedException();
        }
        [HttpPost]
        public virtual IActionResult Create(CreateViewModel<TInstanceModel> model) {
            throw new NotImplementedException();
        }

        [HttpGet]
        public virtual IActionResult Detail(string id) {
            throw new NotImplementedException();
        }
        [HttpPost]
        public virtual IActionResult Update(SingleViewModel<TInstanceModel> model) {
            throw new NotImplementedException();
        }
        [HttpPost]
        public virtual IActionResult Delete(SingleViewModel<TInstanceModel> model) {
            throw new NotImplementedException();
        }

        [HttpGet]
        public virtual IActionResult NewChild([FromServices] Core.Config config, string aggregateTreePath, string modelPath, int currentArrayCount) {
            throw new NotImplementedException();
        }
        #endregion CreateView, SingleView


        [HttpGet]
        public virtual IActionResult Autocomplete(Guid aggregateGuid, string term) {
            throw new NotImplementedException();
        }
    }
}

