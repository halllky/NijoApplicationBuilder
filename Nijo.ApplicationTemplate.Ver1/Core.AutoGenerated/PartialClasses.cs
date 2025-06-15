using Microsoft.EntityFrameworkCore;

namespace MyApp.Core;

partial class MyDbContext {
    //public virtual DbSet<売上分析SearchResult> 売上分析 { get; set; }
    //public virtual DbSet<カテゴリ別売上SearchResult> カテゴリ別売上 { get; set; }
    //public virtual DbSet<商品別売上SearchResult> 商品別売上 { get; set; }
    //public virtual DbSet<時間帯別売上SearchResult> 時間帯別売上 { get; set; }
}

// 売上分析SearchResult の partial class 定義
// Class_SearchResult.csで主要なプロパティが自動生成されるため、
// ここでは主にリレーションシップのためのプロパティを定義します。
public partial class 売上分析SearchResult {
    // カテゴリ別売上 および 時間帯別売上 のナビゲーションプロパティは
    // Class_SearchResult.cs で定義されているため、ここでは定義しません。
}

public partial class カテゴリ別売上SearchResult {
    // Class_SearchResult.csで主要なプロパティが自動生成されるため、
    // ここでは主にリレーションシップのためのプロパティを定義します。

    // 親の売上分析への外部キーとナビゲーションプロパティ
    public int? 売上分析_年月 { get; set; }
    public string? 売上分析_店舗_店舗ID { get; set; }
    public virtual 売上分析SearchResult? 売上分析 { get; set; }

    // 商品別売上 のナビゲーションプロパティは
    // Class_SearchResult.cs で定義されているため、ここでは定義しません。
}

public partial class 商品別売上SearchResult {
    // Class_SearchResult.csで主要なプロパティが自動生成されますが、
    // 親エンティティへのリレーション定義に必要な外部キーとナビゲーションプロパティを定義します。

    // カテゴリ別売上SearchResultへの外部キー
    public int? カテゴリ別売上_売上分析_年月 { get; set; }
    public string? カテゴリ別売上_売上分析_店舗_店舗ID { get; set; }
    public string? カテゴリ別売上_カテゴリ_カテゴリID { get; set; }
    public virtual カテゴリ別売上SearchResult? カテゴリ別売上 { get; set; }

    // 商品_ID, 売上金額, 売上数量, 平均単価 などのプロパティは
    // Class_SearchResult.cs で定義されているため、ここでは定義しません。
    // 注意: Class_SearchResult.cs の 商品_ID と nijo.xml の 商品ID の名称が一致しているか確認してください。
}

public partial class 時間帯別売上SearchResult {
    // Class_SearchResult.csで主要なプロパティが自動生成されるため、
    // ここでは主にリレーションシップのためのプロパティを定義します。

    // 親の売上分析への外部キーとナビゲーションプロパティ
    public int? 売上分析_年月 { get; set; }
    public string? 売上分析_店舗_店舗ID { get; set; }
    public virtual 売上分析SearchResult? 売上分析 { get; set; }

    // 時間帯, 売上金額, 売上件数, 平均客単価 などのプロパティは
    // Class_SearchResult.cs で定義されているため、ここでは定義しません。
}
