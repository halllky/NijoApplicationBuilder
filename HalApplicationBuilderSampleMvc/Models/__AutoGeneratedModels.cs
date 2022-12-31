
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
    public class 営業所__SearchCondition {
        public string 営業所名 { get; set; }
        public HalApplicationBuilder.Runtime.ReferenceDTO 担当者 { get; set; } = new();
    }
    public class 支店__SearchCondition {
        public string 支店名 { get; set; }
    }
    public class 担当者__SearchCondition {
        public string ユーザーID { get; set; }
        public string 氏名 { get; set; }
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
    public class 担当者__SearchResult {
        public string ユーザーID { get; set; }
        public string 氏名 { get; set; }
    }

    public class 会社 {
        public string 会社ID { get; set; }
        public string 会社名 { get; set; }
        public HalApplicationBuilderSampleMvc.Models.連絡先 連絡先 { get; set; } = new();
        public int? 資本情報 { get; set; }
        public HalApplicationBuilderSampleMvc.Models.上場企業資本情報 資本情報__上場企業資本情報 { get; set; } = new();
        public HalApplicationBuilderSampleMvc.Models.非上場企業資本情報 資本情報__非上場企業資本情報 { get; set; } = new();
        public List<HalApplicationBuilderSampleMvc.Models.営業所> 営業所 { get; set; } = new();
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
    public class 営業所 {
        public string 営業所名 { get; set; }
        public List<HalApplicationBuilderSampleMvc.Models.支店> 支店 { get; set; } = new();
        public HalApplicationBuilder.Runtime.ReferenceDTO 担当者 { get; set; } = new();
    }
    public class 支店 {
        public string 支店名 { get; set; }
    }
    public class 担当者 {
        public string ユーザーID { get; set; }
        public string 氏名 { get; set; }
    }

}