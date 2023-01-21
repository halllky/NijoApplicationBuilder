
namespace HalApplicationBuilderSampleMvc.Models {
    using System;
    using System.Collections.Generic;

    public class 商品__SearchCondition {
        public string 商品コード { get; set; }
        public string 商品名 { get; set; }
        public HalApplicationBuilder.DotnetEx.FromTo<int?> 単価 { get; set; } = new();
    }
    public class 売上__SearchCondition {
        public string ID { get; set; }
        public HalApplicationBuilder.DotnetEx.FromTo<DateTime?> 売上日時 { get; set; } = new();
    }
    public class 売上明細__SearchCondition {
        public HalApplicationBuilder.Core.UIModel.ReferenceDTO 商品 { get; set; } = new();
        public HalApplicationBuilder.DotnetEx.FromTo<int?> 数量 { get; set; } = new();
    }

    public class 商品__SearchResult : HalApplicationBuilder.Core.UIModel.SearchResultBase {
        public string 商品コード { get; set; }
        public string 商品名 { get; set; }
        public int 単価 { get; set; }
    }
    public class 売上__SearchResult : HalApplicationBuilder.Core.UIModel.SearchResultBase {
        public string ID { get; set; }
        public DateTime 売上日時 { get; set; }
    }

    public class 商品 : HalApplicationBuilder.Core.UIModel.UIInstanceBase {
        public string 商品コード { get; set; }
        public string 商品名 { get; set; }
        public int 単価 { get; set; }
    }
    public class 売上 : HalApplicationBuilder.Core.UIModel.UIInstanceBase {
        public string ID { get; set; }
        public DateTime 売上日時 { get; set; }
        public List<HalApplicationBuilderSampleMvc.Models.売上明細> 明細 { get; set; } = new();
    }
    public class 売上明細 : HalApplicationBuilder.Core.UIModel.UIInstanceBase {
        public HalApplicationBuilder.Core.UIModel.ReferenceDTO 商品 { get; set; } = new() { AggreageteGuid = new Guid("455cfaa3-8c77-3040-9d7e-eee168fe54f5") };
        public int 数量 { get; set; }
    }

}