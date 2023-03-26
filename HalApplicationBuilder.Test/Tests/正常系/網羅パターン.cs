using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HalApplicationBuilder.DotnetEx;

#pragma warning disable CS8618 // null 非許容の変数には、コンストラクターの終了時に null 以外の値が入っていなければなりません

namespace HalApplicationBuilder.Test.Tests.正常系.網羅パターン {

    [Aggregate]
    public class ルート集約1 {
        [Key]
        public string 主キー1 { get; set; }
        [Key]
        public int 主キー2 { get; set; }

        public decimal 非主キー { get; set; }

        public Child<子_キーあり> Childキーあり1 { get; set; }
        public Child<子_キーあり> Childキーあり2 { get; set; }
        public Child<子_キーなし> Childキーなし1 { get; set; }
        public Child<子_キーなし> Childキーなし2 { get; set; }

        public Children<子_キーあり> Childrenキーあり1 { get; set; }
        public Children<子_キーあり> Childrenキーあり2 { get; set; }
        public Children<子_キーなし> Childrenキーなし1 { get; set; }
        public Children<子_キーなし> Childrenキーなし2 { get; set; }

        [Variation(0, typeof(子VariationImpl_Keyあり))]
        [Variation(1, typeof(子VariationImpl_Keyなし))]
        public Child<I子Variation> 子Variation1 { get; set; }
        [Variation(0, typeof(子VariationImpl_Keyあり))]
        [Variation(1, typeof(子VariationImpl_Keyなし))]
        public Child<I子Variation> 子Variation2 { get; set; }
    }

    public class 子_キーあり {
        [Key]
        public int キー { get; set; }
        [Key]
        public Enumキー EnumKey { get; set; }

        public int? 非主キー { get; set; }

        public Child<孫_キーあり> 孫キーあり単数1 { get; set; }
        public Child<孫_キーあり> 孫キーあり単数2 { get; set; }
        public Child<孫_キーなし> 孫キーなし単数1 { get; set; }
        public Child<孫_キーなし> 孫キーなし単数2 { get; set; }

        public Children<孫_キーあり> 孫キーあり複数1 { get; set; }
        public Children<孫_キーあり> 孫キーあり複数2 { get; set; }
        public Children<孫_キーなし> 孫キーなし複数1 { get; set; }
        public Children<孫_キーなし> 孫キーなし複数2 { get; set; }

        [Variation(0, typeof(孫VariationImpl_Keyあり))]
        [Variation(1, typeof(孫VariationImpl_Keyなし))]
        public Child<I孫Variation> 孫Variation1 { get; set; }
        [Variation(0, typeof(孫VariationImpl_Keyあり))]
        [Variation(1, typeof(孫VariationImpl_Keyなし))]
        public Child<I孫Variation> 孫Variation2 { get; set; }
    }
    public enum Enumキー {
        EnumValue0,
        EnumValue1,
    }
    public class 子_キーなし {
        public float? 非主キー { get; set; }

        public Child<孫_キーあり> 孫キーあり単数1 { get; set; }
        public Child<孫_キーあり> 孫キーあり単数2 { get; set; }
        public Child<孫_キーなし> 孫キーなし単数1 { get; set; }
        public Child<孫_キーなし> 孫キーなし単数2 { get; set; }

        public Children<孫_キーあり> 孫キーあり複数1 { get; set; }
        public Children<孫_キーあり> 孫キーあり複数2 { get; set; }
        public Children<孫_キーなし> 孫キーなし複数1 { get; set; }
        public Children<孫_キーなし> 孫キーなし複数2 { get; set; }

        [Variation(0, typeof(孫VariationImpl_Keyあり))]
        [Variation(1, typeof(孫VariationImpl_Keyなし))]
        public Child<I孫Variation> 孫Variation1 { get; set; }
        [Variation(0, typeof(孫VariationImpl_Keyあり))]
        [Variation(1, typeof(孫VariationImpl_Keyなし))]
        public Child<I孫Variation> 孫Variation2 { get; set; }
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
    [Aggregate]
    public class ルート集約3 {
        [Key]
        public RefTo<ルート集約1> 外部参照主キー1A { get; set; }
        [Key]
        public RefTo<ルート集約1> 外部参照主キー1B { get; set; }

        public Children<集約3の子> 集約3の子 { get; set; }
    }

    public class 集約3の子 {
    }

    [Aggregate]
    public class ルート集約4a {
        [Key]
        public RefTo<集約3の子> 外部子要素参照主キー1A { get; set; }
        [Key]
        public RefTo<集約3の子> 外部子要素参照主キー1B { get; set; }
    }
    [Aggregate]
    public class ルート集約4b {
        [Key]
        public RefTo<集約3の子> 外部子要素参照主キー1A { get; set; }
        [Key]
        public RefTo<集約3の子> 外部子要素参照主キー1B { get; set; }
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
