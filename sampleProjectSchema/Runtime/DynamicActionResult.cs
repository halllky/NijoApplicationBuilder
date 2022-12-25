using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace haldoc.Runtime {
    public class DynamicActionResult {
        public string View { get; set; }
        public object Model { get; set; }
        public ICollection<ValidationResult> Errors { get; set; } = new List<ValidationResult>();
    }
}
