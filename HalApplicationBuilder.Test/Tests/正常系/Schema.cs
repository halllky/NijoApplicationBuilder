using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HalApplicationBuilder.DotnetEx;

namespace HalApplicationBuilder.Test.Tests.正常系 {

    [Aggregate]
    public class ルート集約1 {
        [Key]
        public string 主キー1 { get; set; }
        [Key]
        public int 主キー2 { get; set; }

        public decimal 非主キー { get; set; }

        public Child<子_キーあり> Childキーあり { get; set; }
        public Child<子_キーなし> Childキーなし { get; set; }

        public Children<子_キーあり> Childrenキーあり { get; set; }
        public Children<子_キーなし> Childrenキーなし { get; set; }

        [Variation(0, typeof(子VariationImpl_Keyあり))]
        [Variation(1, typeof(子VariationImpl_Keyなし))]
        public Child<I子Variation> 子Variation { get; set; }
    }

    public class 子_キーあり {
        [Key]
        public int キー { get; set; }
        [Key]
        public Enumキー EnumKey { get; set; }

        public int? 非主キー { get; set; }

        public Child<孫_キーあり> 孫キーあり単数 { get; set; }
        public Child<孫_キーなし> 孫キーなし単数 { get; set; }

        public Children<孫_キーあり> 孫キーあり複数 { get; set; }
        public Children<孫_キーなし> 孫キーなし複数 { get; set; }

        [Variation(0, typeof(孫VariationImpl_Keyあり))]
        [Variation(1, typeof(孫VariationImpl_Keyなし))]
        public Child<I孫Variation> 孫Variation { get; set; }
    }
    public enum Enumキー {
        EnumValue0,
        EnumValue1,
    }
    public class 子_キーなし {
        public float? 非主キー { get; set; }

        [AggregateId("agg-1")]
        public Child<孫_キーあり> 孫キーあり単数 { get; set; }
        public Child<孫_キーなし> 孫キーなし単数 { get; set; }

        public Children<孫_キーあり> 孫キーあり複数 { get; set; }
        [AggregateId("agg-2")]
        public Children<孫_キーなし> 孫キーなし複数 { get; set; }

        [Variation(0, typeof(孫VariationImpl_Keyあり))]
        [Variation(1, typeof(孫VariationImpl_Keyなし))]
        public Child<I孫Variation> 孫Variation { get; set; }
    }
    public interface I子Variation {
        public decimal 共通項目 { get; set; }
    }
    public class 子VariationImpl_Keyあり : I子Variation {
        [Key]
        public decimal 共通項目 { get; set; }
    }
    public class 子VariationImpl_Keyなし : I子Variation {
        [NotMapped]
        public decimal 共通項目 { get; set; }
    }

    public class 孫_キーあり {
        [Key]
        public int キー { get; set; }
        [Key]
        public Enumキー EnumKey { get; set; }

        public int? 非主キー { get; set; }
    }
    public class 孫_キーなし {
        public float? 非主キー { get; set; }
    }
    public interface I孫Variation {
        public decimal 共通項目 { get; set; }
    }
    public class 孫VariationImpl_Keyあり : I孫Variation {
        [Key]
        public decimal 共通項目 { get; set; }
    }
    public class 孫VariationImpl_Keyなし : I孫Variation {
        [NotMapped]
        public decimal 共通項目 { get; set; }
    }

    [Aggregate]
    public class ルート集約2 {
        [Key]
        public RefTo<ルート集約1> 外部参照主キー1A { get; set; }
        [Key]
        public RefTo<ルート集約1> 外部参照主キー1B { get; set; }
    }
    //[Aggregate]
    //public class ルート集約3 {
    //    [Key]
    //    public RefTo<ルート集約1> 外部参照主キー1 { get; set; }
    //    [Key]
    //    public RefTo<ルート集約2> 外部参照主キー2 { get; set; }

    //    public Child<ルート集約3の子> Child { get; set; }
    //    public Children<ルート集約3の子> Children { get; set; }
    //}

    //public class ルート集約3の子 {
    //    [Key, RefTargetId("agg-1")]
    //    public RefTo<孫_キーあり> 集約子要素への参照の主キー1 { get; set; }
    //    [Key, RefTargetId("agg-2")]
    //    public RefTo<孫_キーなし> 集約子要素への参照の主キー2 { get; set; }
    //}
}
