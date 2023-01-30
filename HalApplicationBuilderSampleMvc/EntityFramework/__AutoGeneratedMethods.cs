
namespace HalApplicationBuilderSampleMvc.EntityFramework {
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.EntityFrameworkCore;

    partial class SampleDbContext {
    
        public IEnumerable<HalApplicationBuilderSampleMvc.Models.商品__SearchResult> Search_商品(HalApplicationBuilderSampleMvc.Models.商品__SearchCondition searchCondition) {
            var query = this.商品.Select(e => new HalApplicationBuilderSampleMvc.Models.商品__SearchResult {
                商品コード = e.商品コード,
                商品名 = e.商品名,
                単価 = e.単価,
            });

            if (!string.IsNullOrWhiteSpace(searchCondition.商品コード)) {
                query = query.Where(e => e.商品コード.Contains(searchCondition.商品コード));
            }
            if (!string.IsNullOrWhiteSpace(searchCondition.商品名)) {
                query = query.Where(e => e.商品名.Contains(searchCondition.商品名));
            }
            if (searchCondition.単価.From != null) {
                query = query.Where(e => e.単価 >= searchCondition.単価.From);
            }
            if (searchCondition.単価.To != null) {
                query = query.Where(e => e.単価 <= searchCondition.単価.To);
            }

            var page = searchCondition.GetPageObject();
            query = query.Skip(page.SqlOffset).Take(page.SqlLimit);

            return query.AsEnumerable();
        }
        public IEnumerable<HalApplicationBuilderSampleMvc.Models.売上__SearchResult> Search_売上(HalApplicationBuilderSampleMvc.Models.売上__SearchCondition searchCondition) {
            var query = this.売上.Select(e => new HalApplicationBuilderSampleMvc.Models.売上__SearchResult {
                ID = e.ID,
                売上日時 = e.売上日時,
            });

            if (!string.IsNullOrWhiteSpace(searchCondition.ID)) {
                query = query.Where(e => e.ID.Contains(searchCondition.ID));
            }
            if (searchCondition.売上日時.From != null) {
                query = query.Where(e => e.売上日時 >= searchCondition.売上日時.From);
            }
            if (searchCondition.売上日時.To != null) {
                query = query.Where(e => e.売上日時 <= searchCondition.売上日時.To);
            }

            var page = searchCondition.GetPageObject();
            query = query.Skip(page.SqlOffset).Take(page.SqlLimit);

            return query.AsEnumerable();
        }
    
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