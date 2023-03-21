using System;
namespace HalApplicationBuilder.Runtime.AspNetMvc {
    public class CreateViewModel<T> where T : UIInstanceBase {
        public required T Item { get; set; }
    }
}

