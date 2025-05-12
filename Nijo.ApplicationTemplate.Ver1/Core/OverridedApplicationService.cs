using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApp.Core;

partial class OverridedApplicationService {

    protected override IQueryable<顧客マスタSearchResult> CreateQuerySource(顧客マスタSearchCondition searchCondition, IPresentationContext<顧客マスタSearchConditionMessages> context) {
        return DbContext.顧客マスタDbSet.Select(e => new 顧客マスタSearchResult {
            顧客ID = e.顧客ID,
            氏名 = e.氏名,
            氏名カナ = e.氏名カナ,
            生年月日 = e.生年月日,
            性別 = e.性別,
            メールアドレス = e.メールアドレス,
            電話番号 = e.電話番号,
            住所_郵便番号 = e.顧客マスタの住所!.郵便番号,
            住所_都道府県 = e.顧客マスタの住所!.都道府県,
            住所_市区町村 = e.顧客マスタの住所!.市区町村,
            住所_番地建物名 = e.顧客マスタの住所!.番地建物名,
            会員情報_会員ランク = e.会員情報!.会員ランク,
            会員情報_入会日 = e.会員情報!.入会日,
            会員情報_最終来店日 = e.会員情報!.最終来店日,
            会員情報_ポイント履歴 = e.会員情報!.ポイント履歴.Select(hist => new ポイント履歴SearchResult {
                履歴ID = hist.履歴ID,
                日付 = hist.日付,
                ポイント = hist.ポイント,
                理由 = hist.理由,
            }).ToList(),
            Version = (int)e.Version!,
        });
    }

    protected override IQueryable<商品マスタSearchResult> CreateQuerySource(商品マスタSearchCondition searchCondition, IPresentationContext<商品マスタSearchConditionMessages> context) {
        return DbContext.商品マスタDbSet.Select(e => new 商品マスタSearchResult {
            商品ID = e.商品ID,
            商品名 = e.商品名,
            価格 = e.価格,
            カテゴリ_カテゴリID = e.カテゴリ_カテゴリID,
            カテゴリ_カテゴリ名 = e.カテゴリ!.カテゴリ名,
            仕入先_仕入先ID = e.仕入先_仕入先ID,
            仕入先_仕入先名 = e.仕入先!.仕入先名,
            仕入先_担当者名 = e.仕入先!.担当者名,
            仕入先_電話番号 = e.仕入先!.電話番号,
            仕入先_メールアドレス = e.仕入先!.メールアドレス,
            商品詳細_説明文 = e.商品詳細!.説明文,
            商品詳細_商品仕様_重量 = e.商品詳細!.商品仕様!.重量,
            商品詳細_商品仕様_サイズ_幅 = e.商品詳細!.商品仕様!.サイズ!.幅,
            商品詳細_商品仕様_サイズ_高さ = e.商品詳細!.商品仕様!.サイズ!.高さ,
            商品詳細_商品仕様_サイズ_奥行 = e.商品詳細!.商品仕様!.サイズ!.奥行,
            商品詳細_付属品 = e.商品詳細!.付属品.Select(acc => new 付属品SearchResult {
                付属品ID = acc.付属品ID,
                付属品名 = acc.付属品名,
                数量 = acc.数量,
            }).ToList(),
            在庫情報 = e.在庫情報.Select(inv => new 在庫情報SearchResult {
                倉庫_倉庫ID = inv.倉庫_倉庫ID,
                倉庫_倉庫名 = inv.倉庫!.倉庫名,
                倉庫_住所_郵便番号 = inv.倉庫!.倉庫マスタの住所!.郵便番号,
                倉庫_住所_都道府県 = inv.倉庫!.倉庫マスタの住所!.都道府県,
                倉庫_住所_市区町村 = inv.倉庫!.倉庫マスタの住所!.市区町村,
                倉庫_住所_番地建物名 = inv.倉庫!.倉庫マスタの住所!.番地建物名,
                倉庫_管理責任者_従業員ID = inv.倉庫!.管理責任者!.従業員ID,
                倉庫_管理責任者_氏名 = inv.倉庫!.管理責任者!.氏名,
                倉庫_管理責任者_氏名カナ = inv.倉庫!.管理責任者!.氏名カナ,
                倉庫_管理責任者_所属部署 = inv.倉庫!.管理責任者!.所属部署.Select(dep => new 所属部署SearchResult {
                    年度 = dep.年度,
                    部署_部署コード = dep.部署_部署コード,
                    部署_部署名 = dep.部署!.部署名,
                }).ToList(),
                倉庫_管理責任者_権限 = inv.倉庫!.管理責任者!.権限.Select(role => new 権限SearchResult {
                    権限レベル = role.権限レベル,
                }).ToList(),
                倉庫_管理責任者_退職日 = inv.倉庫!.管理責任者!.退職日,
                在庫数 = inv.在庫数,
                棚卸日時 = inv.棚卸日時,
                在庫状況履歴 = inv.在庫状況履歴.Select(hist => new 在庫状況履歴SearchResult {
                    履歴ID = hist.履歴ID,
                    変更日時 = hist.変更日時,
                    変更前在庫数 = hist.変更前在庫数,
                    変更後在庫数 = hist.変更後在庫数,
                    担当者_従業員ID = hist.担当者_従業員ID,
                    担当者_氏名 = hist.担当者!.氏名,
                    担当者_氏名カナ = hist.担当者!.氏名カナ,
                    担当者_所属部署 = hist.担当者!.所属部署.Select(dep => new 所属部署SearchResult {
                        年度 = dep.年度,
                        部署_部署コード = dep.部署_部署コード,
                        部署_部署名 = dep.部署!.部署名,
                    }).ToList(),
                    担当者_権限 = hist.担当者!.権限.Select(role => new 権限SearchResult {
                        権限レベル = role.権限レベル,
                    }).ToList(),
                    担当者_退職日 = hist.担当者!.退職日,
                }).ToList(),
            }).ToList(),
            Version = (int)e.Version!,
        });
    }

    protected override IQueryable<部署SearchResult> CreateQuerySource(部署SearchCondition searchCondition, IPresentationContext<部署SearchConditionMessages> context) {
        return DbContext.部署DbSet.Select(e => new 部署SearchResult {
            部署コード = e.部署コード,
            部署名 = e.部署名,
            Version = (int)e.Version!,
        });
    }

    protected override IQueryable<売上分析SearchResult> CreateQuerySource(売上分析SearchCondition searchCondition, IPresentationContext<売上分析SearchConditionMessages> context) {
        return Enumerable.Empty<売上分析SearchResult>().AsQueryable();
    }

    protected override IQueryable<従業員マスタSearchResult> CreateQuerySource(従業員マスタSearchCondition searchCondition, IPresentationContext<従業員マスタSearchConditionMessages> context) {
        return DbContext.従業員マスタDbSet.Select(e => new 従業員マスタSearchResult {
            従業員ID = e.従業員ID,
            氏名 = e.氏名,
            氏名カナ = e.氏名カナ,
            所属部署 = e.所属部署.Select(dep => new 所属部署SearchResult {
                年度 = dep.年度,
                部署_部署コード = dep.部署_部署コード,
                部署_部署名 = dep.部署!.部署名,
            }).ToList(),
            権限 = e.権限.Select(role => new 権限SearchResult {
                権限レベル = role.権限レベル,
            }).ToList(),
            退職日 = e.退職日,
            Version = (int)e.Version!,
        });
    }

    protected override IQueryable<店舗マスタSearchResult> CreateQuerySource(店舗マスタSearchCondition searchCondition, IPresentationContext<店舗マスタSearchConditionMessages> context) {
        return DbContext.店舗マスタDbSet.Select(e => new 店舗マスタSearchResult {
            店舗ID = e.店舗ID,
            店舗名 = e.店舗名,
            住所_郵便番号 = e.店舗マスタの住所!.郵便番号,
            住所_都道府県 = e.店舗マスタの住所!.都道府県,
            住所_市区町村 = e.店舗マスタの住所!.市区町村,
            住所_番地建物名 = e.店舗マスタの住所!.番地建物名,
            電話番号 = e.電話番号,
            営業時間_開店時間 = e.営業時間!.開店時間,
            営業時間_閉店時間 = e.営業時間!.閉店時間,
            店長_従業員ID = e.店長!.従業員ID,
            店長_氏名 = e.店長!.氏名,
            店長_氏名カナ = e.店長!.氏名カナ,
            店長_所属部署 = e.店長!.所属部署.Select(dep => new 所属部署SearchResult {
                年度 = dep.年度,
                部署_部署コード = dep.部署_部署コード,
                部署_部署名 = dep.部署!.部署名,
            }).ToList(),
            店長_権限 = e.店長!.権限.Select(role => new 権限SearchResult {
                権限レベル = role.権限レベル,
            }).ToList(),
            店長_退職日 = e.店長!.退職日,
            Version = (int)e.Version!,
        });
    }

    protected override IQueryable<仕入先マスタSearchResult> CreateQuerySource(仕入先マスタSearchCondition searchCondition, IPresentationContext<仕入先マスタSearchConditionMessages> context) {
        return DbContext.仕入先マスタDbSet.Select(e => new 仕入先マスタSearchResult {
            仕入先ID = e.仕入先ID,
            仕入先名 = e.仕入先名,
            担当者名 = e.担当者名,
            電話番号 = e.電話番号,
            メールアドレス = e.メールアドレス,
            Version = (int)e.Version!,
        });
    }

    protected override IQueryable<カテゴリマスタSearchResult> CreateQuerySource(カテゴリマスタSearchCondition searchCondition, IPresentationContext<カテゴリマスタSearchConditionMessages> context) {
        return DbContext.カテゴリマスタDbSet.Select(e => new カテゴリマスタSearchResult {
            カテゴリID = e.カテゴリID,
            カテゴリ名 = e.カテゴリ名,
            Version = (int)e.Version!,
        });
    }

    protected override IQueryable<予約SearchResult> CreateQuerySource(予約SearchCondition searchCondition, IPresentationContext<予約SearchConditionMessages> context) {
        return DbContext.予約DbSet.Select(e => new 予約SearchResult {
            予約ID = e.予約ID,
            患者_顧客ID = e.患者_顧客ID,
            患者_氏名 = e.患者!.氏名,
            患者_氏名カナ = e.患者!.氏名カナ,
            患者_生年月日 = e.患者!.生年月日,
            患者_性別 = e.患者!.性別,
            患者_メールアドレス = e.患者!.メールアドレス,
            患者_電話番号 = e.患者!.電話番号,
            患者_住所_郵便番号 = e.患者!.顧客マスタの住所!.郵便番号,
            患者_住所_都道府県 = e.患者!.顧客マスタの住所!.都道府県,
            患者_住所_市区町村 = e.患者!.顧客マスタの住所!.市区町村,
            患者_住所_番地建物名 = e.患者!.顧客マスタの住所!.番地建物名,
            患者_会員情報_会員ランク = e.患者!.会員情報!.会員ランク,
            患者_会員情報_入会日 = e.患者!.会員情報!.入会日,
            患者_会員情報_最終来店日 = e.患者!.会員情報!.最終来店日,
            患者_会員情報_ポイント履歴 = e.患者!.会員情報!.ポイント履歴.Select(hist => new ポイント履歴SearchResult {
                履歴ID = hist.履歴ID,
                日付 = hist.日付,
                ポイント = hist.ポイント,
                理由 = hist.理由,
            }).ToList(),
            予約日時 = e.予約日時,
            予約区分 = e.予約区分,
            予約メモ = e.予約メモ,
            担当従業員_従業員ID = e.担当従業員!.従業員ID,
            担当従業員_氏名 = e.担当従業員!.氏名,
            担当従業員_氏名カナ = e.担当従業員!.氏名カナ,
            担当従業員_所属部署 = e.担当従業員!.所属部署.Select(dep => new 所属部署SearchResult {
                年度 = dep.年度,
                部署_部署コード = dep.部署_部署コード,
                部署_部署名 = dep.部署!.部署名,
            }).ToList(),
            担当従業員_権限 = e.担当従業員!.権限.Select(role => new 権限SearchResult {
                権限レベル = role.権限レベル,
            }).ToList(),
            担当従業員_退職日 = e.担当従業員!.退職日,
            Version = (int)e.Version!,
        });
    }

    protected override IQueryable<倉庫マスタSearchResult> CreateQuerySource(倉庫マスタSearchCondition searchCondition, IPresentationContext<倉庫マスタSearchConditionMessages> context) {
        return DbContext.倉庫マスタDbSet.Select(e => new 倉庫マスタSearchResult {
            倉庫ID = e.倉庫ID,
            倉庫名 = e.倉庫名,
            住所_郵便番号 = e.倉庫マスタの住所!.郵便番号,
            住所_都道府県 = e.倉庫マスタの住所!.都道府県,
            住所_市区町村 = e.倉庫マスタの住所!.市区町村,
            住所_番地建物名 = e.倉庫マスタの住所!.番地建物名,
            管理責任者_従業員ID = e.管理責任者!.従業員ID,
            管理責任者_氏名 = e.管理責任者!.氏名,
            管理責任者_氏名カナ = e.管理責任者!.氏名カナ,
            管理責任者_所属部署 = e.管理責任者!.所属部署.Select(dep => new 所属部署SearchResult {
                年度 = dep.年度,
                部署_部署コード = dep.部署_部署コード,
                部署_部署名 = dep.部署!.部署名,
            }).ToList(),
            管理責任者_権限 = e.管理責任者!.権限.Select(role => new 権限SearchResult {
                権限レベル = role.権限レベル,
            }).ToList(),
            管理責任者_退職日 = e.管理責任者!.退職日,
            Version = (int)e.Version!,
        });
    }

    protected override IQueryable<在庫調査報告SearchResult> CreateQuerySource(在庫調査報告SearchCondition searchCondition, IPresentationContext<在庫調査報告SearchConditionMessages> context) {
        return DbContext.在庫調査報告DbSet.Select(e => new 在庫調査報告SearchResult {
            // TODO: 在庫調査報告SearchResult クラスの定義を参照し、検索結果を実装してください。
            Version = (int)e.Version!,
        });
    }

    protected override IQueryable<アクション結果SearchResult> CreateQuerySource(アクション結果SearchCondition searchCondition, IPresentationContext<アクション結果SearchConditionMessages> context) {
        return DbContext.アクション結果DbSet.Select(e => new アクション結果SearchResult {
            // TODO: アクション結果SearchResult クラスの定義を参照し、検索結果を実装してください。
            Version = (int)e.Version!,
        });
    }

    public override Task<在庫調整ReturnValue> Execute(在庫調整Parameter parameter, IPresentationContext<在庫調整ParameterMessages> context) {
        // TODO: 在庫調整コマンドの処理を実装してください。
        return Task.FromResult(new 在庫調整ReturnValue());
    }

    public override Task<簡易注文ReturnValue> Execute(簡易注文Parameter parameter, IPresentationContext<簡易注文ParameterMessages> context) {
        // TODO: 簡易注文コマンドの処理を実装してください。
        return Task.FromResult(new 簡易注文ReturnValue());
    }
}
