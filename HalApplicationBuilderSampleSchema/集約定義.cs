using System;
using System.ComponentModel.DataAnnotations;
using HalApplicationBuilder;

namespace HalApplicationBuilderSampleSchema {

    [Aggregate]
    public class 営業所 {
        [Key]
        public string 営業所ID { get; set; }
        [Required, InstanceName]
        public string 営業所名 { get; set; }

        public DateTime? 運用開始日 { get; set; }
    }
}
