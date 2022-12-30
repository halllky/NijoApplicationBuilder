using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using haldoc.Schema;

namespace haldoc {

    [AggregateRoot]
    public class 取引先 {
        [Key]
        public string 企業ID { get; set; }
        [Required, InstanceName]
        public string 企業名 { get; set; }

        public E_重要度? 重要度 { get; set; }

        [Variation(0, typeof(上場企業資本情報))]
        [Variation(1, typeof(非上場企業資本情報))]
        public Child<I資本情報> 資本情報 { get; set; }

        public Children<コメント> 備考 { get; set; }

        public Children<担当者> 担当者 { get; set; }
    }

    // 1対0-1、同じ集約、多態
    public interface I資本情報 {
        E_安定性 安定性 { get; }
    }
    public class 上場企業資本情報 : I資本情報 {
        public decimal 自己資本比率 { get; set; }
        public decimal 利益率 { get; set; }
        [NotMapped]
        public E_安定性 安定性 => 自己資本比率 >= 0.2m ? E_安定性.安定 : E_安定性.不安;
    }
    public class 非上場企業資本情報 : I資本情報 {
        public string 主要株主 { get; set; }
        public E_安定性 安定性 { get; set; }
    }

    // 1対0-1、別集約、キー全部が相手集約
    [AggregateRoot]
    public class 請求情報 {
        [Key]
        public 取引先 取引先 { get; set; }

        public string 宛名 { get; set; }

        public Child<連絡先> 住所 { get; set; }
    }

    // 1対多、同じ集約
    public class コメント {
        public string Text { get; set; }

        public string At { get; set; }

        public 担当者 By { get; set; }
    }
    // 1対1、同じ集約
    public class 連絡先 {
        public string 郵便番号 { get; set; }
        public string 都道府県 { get; set; }
        public string 市町村 { get; set; }
        public string 丁番地 { get; set; }
    }

    // 1対多、別集約、キーの一部に相手集約が含まれる
    [AggregateRoot]
    public class 取引先支店 {
        [Key]
        public 取引先 会社 { get; set; }
        [Key]
        public int 支店連番 { get; set; }

        public string 支店名 { get; set; }
    }


    [AggregateRoot]
    public class 営業所 {
        [Key]
        public string 営業所ID { get; set; }
        [Required, InstanceName]
        public string 営業所名 { get; set; }
    }

    [AggregateRoot]
    public class 担当者 {
        [Key]
        public string ユーザーID { get; set; }
        [Required, InstanceName]
        public string 氏名 { get; set; }

        public 営業所 所属 { get; set; }
    }

    public enum E_重要度 {
        A,
        B,
        C,
    }
    public enum E_安定性 {
        安定,
        中程度,
        不安,
    }

    // ---------- 以下はプレゼン用 ----------

    //class SlidePage2 : ViewBuilderBase {

    //    [AggregateRoot]
    //    public class 商品 {
    //        [Key]
    //        public string 商品ID { get; set; }
    //        public string 商品名 { get; set; }
    //    }

    //    protected override void OnViewBuilding(Option option) {
    //        option.SingleView<商品>()
    //            .AddButton("発注する", e => {
    //                e.LinkTo<発注>(new {
    //                    e.商品ID,                // パラメータ
    //                    発注日時 = DateTime.Now, // パラメータ
    //                });
    //            });
    //    }

    //    [AggregateRoot]
    //    public class 発注 {
    //        [Key]
    //        public string 発注番号 { get; set; }
    //        public DateTime 発注日時 { get; set; }
    //        public 商品 商品 { get; set; }
    //        public int 個数 { get; set; }
    //    }
    //}

    //abstract class ViewBuilderBase {
    //    protected abstract void OnViewBuilding(Option option);
    //}
    //class Option {
    //    public SingleViewOption<T> SingleView<T>() => new();
    //}
    //class SingleViewOption<T> {
    //    public void AddButton(string buttonName, Action<dynamic> predicate) { }
    //}
}
