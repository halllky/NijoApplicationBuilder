using System;
namespace HalApplicationBuilder.ReArchTo関数型.Runtime.AspNetMvc {
    public class SingleViewModel<T> where T : UIInstanceBase {
        public required string InstanceName { get; set; }
        public required T Item { get; set; }
    }
}

