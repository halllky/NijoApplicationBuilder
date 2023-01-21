
namespace HalApplicationBuilderSampleMvc.EntityFramework {
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.EntityFrameworkCore;

    partial class SampleDbContext {
    
        /// <summary>
        /// オートコンプリートのデータソースを読み込む
        /// </summary>
        public IEnumerable<HalApplicationBuilderSampleMvc.EntityFramework.Entities.商品> LoadAutoCompleteSource_商品(string keyword = null) {
            var query = (IQueryable<HalApplicationBuilderSampleMvc.EntityFramework.Entities.商品>)this.商品;
            return query
                .Take(100 + 1)
                .ToArray();
        }
        /// <summary>
        /// オートコンプリートのデータソースを読み込む
        /// </summary>
        public IEnumerable<HalApplicationBuilderSampleMvc.EntityFramework.Entities.売上> LoadAutoCompleteSource_売上(string keyword = null) {
            var query = (IQueryable<HalApplicationBuilderSampleMvc.EntityFramework.Entities.売上>)this.売上;
            return query
                .Take(100 + 1)
                .ToArray();
        }

    }
}