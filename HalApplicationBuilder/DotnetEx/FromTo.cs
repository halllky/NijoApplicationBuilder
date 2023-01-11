using System;
namespace HalApplicationBuilder.DotnetEx {
    public class FromTo {
        public object From { get; set; }
        public object To { get; set; }
    }
    public class FromTo<T> : FromTo {
        public new T From {
            get => (T)base.From;
            set => base.From = value;
        }
        public new T To {
            get => (T)base.To;
            set => base.To = value;
        }
    }
}
