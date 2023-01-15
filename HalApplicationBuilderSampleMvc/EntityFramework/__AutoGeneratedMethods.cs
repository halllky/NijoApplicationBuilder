
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
        public IEnumerable<HalApplicationBuilderSampleMvc.EntityFramework.Entities.会社> LoadAutoCompleteSource_会社(string keyword = null) {
            var query = (IQueryable<HalApplicationBuilderSampleMvc.EntityFramework.Entities.会社>)this.会社;
            return query
                .Take(100 + 1)
                .ToArray();
        }
        /// <summary>
        /// オートコンプリートのデータソースを読み込む
        /// </summary>
        public IEnumerable<HalApplicationBuilderSampleMvc.EntityFramework.Entities.担当者> LoadAutoCompleteSource_担当者(string keyword = null) {
            var query = (IQueryable<HalApplicationBuilderSampleMvc.EntityFramework.Entities.担当者>)this.担当者;
            return query
                .Take(100 + 1)
                .ToArray();
        }

    }
}