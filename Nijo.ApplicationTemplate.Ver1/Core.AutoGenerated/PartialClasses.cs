using Microsoft.EntityFrameworkCore;

namespace MyApp.Core;

partial class MyDbContext {
    //public virtual DbSet<診療収益分析SearchResult> 診療収益分析 { get; set; }
    //public virtual DbSet<機器分類別収益SearchResult> 機器分類別収益 { get; set; }
    //public virtual DbSet<機器別収益SearchResult> 機器別収益 { get; set; }
    //public virtual DbSet<時間帯別収益SearchResult> 時間帯別収益 { get; set; }
}

// 診療収益分析SearchResult の partial class 定義
// Class_SearchResult.csで主要なプロパティが自動生成されるため、
// ここでは主にリレーションシップのためのプロパティを定義します。
public partial class 診療収益分析SearchResult {
    // 機器分類別収益 および 時間帯別収益 のナビゲーションプロパティは
    // Class_SearchResult.cs で定義されているため、ここでは定義しません。
}

public partial class 機器分類別収益SearchResult {
    // Class_SearchResult.csで主要なプロパティが自動生成されるため、
    // ここでは主にリレーションシップのためのプロパティを定義します。

    // 親の診療収益分析への外部キーとナビゲーションプロパティ
    public int? 診療収益分析_年月 { get; set; }
    public string? 診療収益分析_診療科_診療科ID { get; set; }
    public virtual 診療収益分析SearchResult? 診療収益分析 { get; set; }

    // 機器別収益 のナビゲーションプロパティは
    // Class_SearchResult.cs で定義されているため、ここでは定義しません。
}

public partial class 機器別収益SearchResult {
    // Class_SearchResult.csで主要なプロパティが自動生成されますが、
    // 親エンティティへのリレーション定義に必要な外部キーとナビゲーションプロパティを定義します。

    // 機器分類別収益SearchResultへの外部キー
    public int? 機器分類別収益_診療収益分析_年月 { get; set; }
    public string? 機器分類別収益_診療収益分析_診療科_診療科ID { get; set; }
    public string? 機器分類別収益_機器分類_機器分類ID { get; set; }
    public virtual 機器分類別収益SearchResult? 機器分類別収益 { get; set; }

    // 医療機器_ID, 診療収益金額, 診療収益数量, 平均単価 などのプロパティは
    // Class_SearchResult.cs で定義されているため、ここでは定義しません。
    // 注意: Class_SearchResult.cs の 医療機器_ID と nijo.xml の 医療機器ID の名称が一致しているか確認してください。
}

public partial class 時間帯別収益SearchResult {
    // Class_SearchResult.csで主要なプロパティが自動生成されるため、
    // ここでは主にリレーションシップのためのプロパティを定義します。

    // 親の診療収益分析への外部キーとナビゲーションプロパティ
    public int? 診療収益分析_年月 { get; set; }
    public string? 診療収益分析_診療科_診療科ID { get; set; }
    public virtual 診療収益分析SearchResult? 診療収益分析 { get; set; }

    // 時間帯, 診療収益金額, 診療収益件数, 平均患者単価 などのプロパティは
    // Class_SearchResult.cs で定義されているため、ここでは定義しません。
}
