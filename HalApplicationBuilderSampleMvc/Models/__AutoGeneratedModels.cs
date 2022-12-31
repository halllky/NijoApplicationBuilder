
namespace HalApplicationBuilderSampleMvc.Models {
    using System;
    using System.Collections.Generic;

    public class 会社__SearchCondition {
        public string 会社ID { get; set; }
        public string 会社名 { get; set; }
        public HalApplicationBuilderSampleMvc.Models.連絡先__SearchCondition 連絡先 { get; set; } = new();
        public bool 資本情報_上場企業資本情報 { get; set; } = true;
        public bool 資本情報_非上場企業資本情報 { get; set; } = true;
    }
    public class 連絡先__SearchCondition {
        public string 電話番号 { get; set; }
        public string 郵便番号 { get; set; }
        public string 住所1 { get; set; }
        public string 住所2 { get; set; }
        public string 住所3 { get; set; }
    }
    public class 上場企業資本情報__SearchCondition {
        public HalApplicationBuilder.Runtime.FromTo<decimal?> 自己資本比率 { get; set; }
        public HalApplicationBuilder.Runtime.FromTo<decimal?> 利益率 { get; set; }
    }
    public class 非上場企業資本情報__SearchCondition {
        public string 主要株主 { get; set; }
        public HalApplicationBuilderSampleSchema.E_安定性 安定性 { get; set; }
    }

    public class 会社__SearchResult {
        public string 会社ID { get; set; }
        public string 会社名 { get; set; }
        public string 電話番号 { get; set; }
        public string 郵便番号 { get; set; }
        public string 住所1 { get; set; }
        public string 住所2 { get; set; }
        public string 住所3 { get; set; }
        public string 資本情報 { get; set; }
    }

    public class 会社 {
        public string 会社ID { get; set; }
        public string 会社名 { get; set; }
        public HalApplicationBuilderSampleMvc.Models.連絡先 連絡先 { get; set; } = new();
        public int? 資本情報 { get; set; }
        public HalApplicationBuilderSampleMvc.Models.上場企業資本情報 資本情報__上場企業資本情報 { get; set; } = new();
        public HalApplicationBuilderSampleMvc.Models.非上場企業資本情報 資本情報__非上場企業資本情報 { get; set; } = new();
    }
    public class 連絡先 {
        public string 電話番号 { get; set; }
        public string 郵便番号 { get; set; }
        public string 住所1 { get; set; }
        public string 住所2 { get; set; }
        public string 住所3 { get; set; }
    }
    public class 上場企業資本情報 {
        public decimal 自己資本比率 { get; set; }
        public decimal 利益率 { get; set; }
    }
    public class 非上場企業資本情報 {
        public string 主要株主 { get; set; }
        public HalApplicationBuilderSampleSchema.E_安定性 安定性 { get; set; }
    }

}