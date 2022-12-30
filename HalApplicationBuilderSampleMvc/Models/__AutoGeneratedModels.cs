
namespace HalApplicationBuilderSampleMvc.Models {
    using System;
    using System.Collections.Generic;

    public class 営業所__SearchCondition {
        public string 営業所ID { get; set; }
        public string 営業所名 { get; set; }
        public HalApplicationBuilder.Runtime.FromTo<System.DateTime> 運用開始日 { get; set; }
    }

    public class 営業所__SearchResult {
        public string 営業所ID { get; set; }
        public string 営業所名 { get; set; }
        public DateTime? 運用開始日 { get; set; }
    }

    public class 営業所 {
        public string 営業所ID { get; set; }
        public string 営業所名 { get; set; }
        public DateTime? 運用開始日 { get; set; }
    }

}