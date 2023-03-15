using System;
namespace HalApplicationBuilder.ReArchTo関数型.Runtime.AspNetMvc {
    public class CreateViewModel<T> where T : UIInstanceBase {
        public required T Item { get; set; }
    }
}

