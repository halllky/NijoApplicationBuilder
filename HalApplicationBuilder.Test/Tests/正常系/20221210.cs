using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HalApplicationBuilder;

#pragma warning disable CS8618 // null 非許容の変数には、コンストラクターの終了時に null 以外の値が入っていなければなりません

namespace HalApplicationBuilder.Test.Tests.正常系 {
    [Aggregate]
    public class 商品 {
        [Key]
        public string 商品コード { get; set; }
        [InstanceName]
        public string 商品名 { get; set; }
        public int 単価 { get; set; }
    }
    [Aggregate]
    public class 売上 {
        [Key]
        public string ID { get; set; }
        public DateTime 売上日時 { get; set; }
        public Children<売上明細> 明細 { get; set; }
    }
    public class 売上明細 {
        [Key]
        public RefTo<商品> 商品 { get; set; }
        public int 数量 { get; set; }
    }
}

// ↑ README撮影用

// ↓ デバッグ用

//namespace HalApplicationBuilderSampleSchema {

//    [Aggregate]
//    public class 会社 {
//        [Key]
//        public string 会社ID { get; set; }
//        [Required, InstanceName]
//        public string 会社名 { get; set; }

//        public RefTo<担当者> 主担当 { get; set; }

//        public Child<連絡先> 連絡先 { get; set; }

//        [Variation(0, typeof(上場企業資本情報))]
//        [Variation(1, typeof(非上場企業資本情報))]
//        public Child<I資本情報> 資本情報 { get; set; }

//        public Children<営業所> 営業所 { get; set; }
//    }

//    public class 連絡先 {
//        public string 電話番号 { get; set; }
//        public string 郵便番号 { get; set; }
//        public string 住所1 { get; set; }
//        public string 住所2 { get; set; }
//        public string 住所3 { get; set; }
//    }

//    public interface I資本情報 {
//        E_安定性 安定性 { get; }
//    }
//    public class 上場企業資本情報 : I資本情報 {
//        public decimal 自己資本比率 { get; set; }
//        public decimal 利益率 { get; set; }
//        [NotMapped]
//        public E_安定性 安定性 => 自己資本比率 >= 0.2m ? E_安定性.安定 : E_安定性.不安;
//    }
//    public class 非上場企業資本情報 : I資本情報 {
//        public string 主要株主 { get; set; }
//        public E_安定性 安定性 { get; set; }
//    }

//    public class 営業所 {
//        public string 営業所名 { get; set; }
//        public Children<支店> 支店 { get; set; }
//        public RefTo<担当者> 担当者 { get; set; }
//    }
//    public class 支店 {
//        public string 支店名 { get; set; }
//    }

//    [Aggregate]
//    public class 担当者 {
//        [Key]
//        public string ユーザーID { get; set; }
//        [InstanceName]
//        public string 氏名 { get; set; }
//    }

//    public enum E_重要度 {
//        A,
//        B,
//        C,
//    }
//    public enum E_安定性 {
//        安定,
//        中程度,
//        不安,
//    }
//}
