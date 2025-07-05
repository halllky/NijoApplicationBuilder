using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace MyApp.Core;

partial class OverridedApplicationService {

    protected override IQueryable<患者マスタSearchResult> CreateQuerySource(患者マスタSearchCondition searchCondition, IPresentationContext<患者マスタSearchConditionMessages> context) {
        return DbContext.患者マスタDbSet.Select(e => new 患者マスタSearchResult {
            患者ID = e.患者ID,
            氏名 = e.氏名,
            氏名カナ = e.氏名カナ,
            生年月日 = e.生年月日,
            性別 = e.性別,
            メールアドレス = e.メールアドレス,
            電話番号 = e.電話番号,
            住所_郵便番号 = e.患者マスタの住所!.郵便番号,
            住所_都道府県 = e.患者マスタの住所!.都道府県,
            住所_市区町村 = e.患者マスタの住所!.市区町村,
            住所_番地建物名 = e.患者マスタの住所!.番地建物名,
            患者情報_初診日 = e.患者情報!.初診日,
            患者情報_最終受診日 = e.患者情報!.最終受診日,
            患者情報_診療履歴 = e.患者情報!.患者情報の診療履歴.Select(hist => new 患者情報の診療履歴SearchResult {
                履歴ID = hist.履歴ID,
                日付 = hist.日付,
                診療点数 = hist.診療点数,
                診療内容 = hist.診療内容,
            }).ToList(),
            患者情報_患者分類 = e.患者情報!.患者分類,
            Version = (int)e.Version!,
        });
    }

    protected override IQueryable<医療機器マスタSearchResult> CreateQuerySource(医療機器マスタSearchCondition searchCondition, IPresentationContext<医療機器マスタSearchConditionMessages> context) {
        return DbContext.医療機器マスタDbSet.Select(e => new 医療機器マスタSearchResult {
            機器ID = e.機器ID,
            機器名 = e.機器名,
            単価 = e.単価,
            機器分類_機器分類ID = e.機器分類_機器分類ID,
            機器分類_機器分類名 = e.機器分類!.機器分類名,
            供給業者_供給業者ID = e.供給業者_供給業者ID,
            供給業者_供給業者名 = e.供給業者!.供給業者名,
            供給業者_担当者名 = e.供給業者!.担当者名,
            供給業者_電話番号 = e.供給業者!.電話番号,
            供給業者_メールアドレス = e.供給業者!.メールアドレス,
            機器詳細_機器説明 = e.機器詳細!.機器説明,
            機器詳細_機器仕様_重量 = e.機器詳細!.機器仕様!.重量,
            機器詳細_機器仕様_サイズ_幅 = e.機器詳細!.機器仕様!.サイズ!.幅,
            機器詳細_機器仕様_サイズ_高さ = e.機器詳細!.機器仕様!.サイズ!.高さ,
            機器詳細_機器仕様_サイズ_奥行 = e.機器詳細!.機器仕様!.サイズ!.奥行,
            在庫情報 = e.在庫情報.Select(inv => new 在庫情報SearchResult {
                保管庫_保管庫ID = inv.保管庫!.保管庫ID,
                保管庫_保管庫名 = inv.保管庫!.保管庫名,
                保管庫_住所_郵便番号 = inv.保管庫!.保管庫マスタの住所!.郵便番号,
                保管庫_住所_都道府県 = inv.保管庫!.保管庫マスタの住所!.都道府県,
                保管庫_住所_市区町村 = inv.保管庫!.保管庫マスタの住所!.市区町村,
                保管庫_住所_番地建物名 = inv.保管庫!.保管庫マスタの住所!.番地建物名,
                保管庫_管理責任者_医療従事者ID = inv.保管庫!.管理責任者!.医療従事者ID,
                保管庫_管理責任者_氏名 = inv.保管庫!.管理責任者!.氏名,
                保管庫_管理責任者_氏名カナ = inv.保管庫!.管理責任者!.氏名カナ,
                保管庫_管理責任者_退職日 = inv.保管庫!.管理責任者!.退職日,
                在庫数 = inv.在庫数,
                棚卸日時 = inv.棚卸日時,
                在庫状況履歴 = inv.在庫状況履歴.Select(hist => new 在庫状況履歴SearchResult {
                    履歴ID = hist.履歴ID,
                    変更日時 = hist.変更日時,
                    変更前在庫数 = hist.変更前在庫数,
                    変更後在庫数 = hist.変更後在庫数,
                    担当者_医療従事者ID = hist.担当者_医療従事者ID,
                    担当者_氏名 = hist.担当者!.氏名,
                    担当者_氏名カナ = hist.担当者!.氏名カナ,
                    担当者_退職日 = hist.担当者!.退職日,
                }).ToList(),
            }).ToList(),
            機器詳細_付属品 = e.機器詳細!.付属品.Select(acc => new 付属品SearchResult {
                付属品ID = acc.付属品ID,
                付属品名 = acc.付属品名,
                数量 = acc.数量,
            }).ToList(),
            Version = (int)e.Version!,
        });
    }

    protected override IQueryable<診療収益分析SearchResult> CreateQuerySource(診療収益分析SearchCondition searchCondition, IPresentationContext<診療収益分析SearchConditionMessages> context) {
        return DbContext.Set<診療収益分析SearchResult>()
            .Include(e => e.機器分類別収益)
            .ThenInclude(e => e.機器別収益)
            .Include(e => e.時間帯別収益);
    }

    protected override IQueryable<医療従事者マスタSearchResult> CreateQuerySource(医療従事者マスタSearchCondition searchCondition, IPresentationContext<医療従事者マスタSearchConditionMessages> context) {
        return DbContext.医療従事者マスタDbSet.Select(e => new 医療従事者マスタSearchResult {
            医療従事者ID = e.医療従事者ID,
            氏名 = e.氏名,
            氏名カナ = e.氏名カナ,
            所属診療科 = e.所属診療科.Select(dep => new 所属診療科SearchResult {
                年度 = dep.年度,
                診療科_診療科コード = dep.診療科_診療科コード,
                診療科_診療科名 = dep.診療科!.診療科名,
            }).ToList(),
            権限 = e.権限.Select(role => new 権限SearchResult {
                権限レベル = role.権限レベル,
            }).ToList(),
            退職日 = e.退職日,
            Version = (int)e.Version!,
        });
    }

    protected override IQueryable<診療科マスタSearchResult> CreateQuerySource(診療科マスタSearchCondition searchCondition, IPresentationContext<診療科マスタSearchConditionMessages> context) {
        return DbContext.診療科マスタDbSet.Select(e => new 診療科マスタSearchResult {
            診療科ID = e.診療科ID,
            診療科名 = e.診療科名,
            住所_郵便番号 = e.診療科マスタの住所!.郵便番号,
            住所_都道府県 = e.診療科マスタの住所!.都道府県,
            住所_市区町村 = e.診療科マスタの住所!.市区町村,
            住所_番地建物名 = e.診療科マスタの住所!.番地建物名,
            電話番号 = e.電話番号,
            診療時間_開始時間 = e.診療時間!.開始時間,
            診療時間_終了時間 = e.診療時間!.終了時間,
            科長_医療従事者ID = e.科長!.医療従事者ID,
            科長_氏名 = e.科長!.氏名,
            科長_氏名カナ = e.科長!.氏名カナ,
            科長_退職日 = e.科長!.退職日,
            Version = (int)e.Version!,
        });
    }

    protected override IQueryable<供給業者マスタSearchResult> CreateQuerySource(供給業者マスタSearchCondition searchCondition, IPresentationContext<供給業者マスタSearchConditionMessages> context) {
        return DbContext.供給業者マスタDbSet.Select(e => new 供給業者マスタSearchResult {
            供給業者ID = e.供給業者ID,
            供給業者名 = e.供給業者名,
            担当者名 = e.担当者名,
            電話番号 = e.電話番号,
            メールアドレス = e.メールアドレス,
            Version = (int)e.Version!,
        });
    }

    protected override IQueryable<機器分類マスタSearchResult> CreateQuerySource(機器分類マスタSearchCondition searchCondition, IPresentationContext<機器分類マスタSearchConditionMessages> context) {
        return DbContext.機器分類マスタDbSet.Select(e => new 機器分類マスタSearchResult {
            機器分類ID = e.機器分類ID,
            機器分類名 = e.機器分類名,
            Version = (int)e.Version!,
        });
    }

    protected override IQueryable<予約SearchResult> CreateQuerySource(予約SearchCondition searchCondition, IPresentationContext<予約SearchConditionMessages> context) {
        return DbContext.予約DbSet.Select(e => new 予約SearchResult {
            予約ID = e.予約ID,
            患者_患者ID = e.患者_患者ID,
            患者_氏名 = e.患者!.氏名,
            患者_氏名カナ = e.患者!.氏名カナ,
            患者_生年月日 = e.患者!.生年月日,
            患者_性別 = e.患者!.性別,
            患者_メールアドレス = e.患者!.メールアドレス,
            患者_電話番号 = e.患者!.電話番号,
            患者_住所_郵便番号 = e.患者!.患者マスタの住所!.郵便番号,
            患者_住所_都道府県 = e.患者!.患者マスタの住所!.都道府県,
            患者_住所_市区町村 = e.患者!.患者マスタの住所!.市区町村,
            患者_住所_番地建物名 = e.患者!.患者マスタの住所!.番地建物名,
            患者_患者情報_患者分類 = e.患者!.患者情報!.患者分類,
            患者_患者情報_初診日 = e.患者!.患者情報!.初診日,
            患者_患者情報_最終受診日 = e.患者!.患者情報!.最終受診日,
            予約日時 = e.予約日時,
            予約区分 = e.予約区分,
            予約メモ = e.予約メモ,
            担当医_医療従事者ID = e.担当医!.医療従事者ID,
            担当医_氏名 = e.担当医!.氏名,
            担当医_氏名カナ = e.担当医!.氏名カナ,
            担当医_退職日 = e.担当医!.退職日,
            Version = (int)e.Version!,
        });
    }

    protected override IQueryable<保管庫マスタSearchResult> CreateQuerySource(保管庫マスタSearchCondition searchCondition, IPresentationContext<保管庫マスタSearchConditionMessages> context) {
        return DbContext.保管庫マスタDbSet.Select(e => new 保管庫マスタSearchResult {
            保管庫ID = e.保管庫ID,
            保管庫名 = e.保管庫名,
            住所_郵便番号 = e.保管庫マスタの住所!.郵便番号,
            住所_都道府県 = e.保管庫マスタの住所!.都道府県,
            住所_市区町村 = e.保管庫マスタの住所!.市区町村,
            住所_番地建物名 = e.保管庫マスタの住所!.番地建物名,
            管理責任者_医療従事者ID = e.管理責任者!.医療従事者ID,
            管理責任者_氏名 = e.管理責任者!.氏名,
            管理責任者_氏名カナ = e.管理責任者!.氏名カナ,
            管理責任者_退職日 = e.管理責任者!.退職日,
            Version = (int)e.Version!,
        });
    }

    protected override IQueryable<機器点検報告SearchResult> CreateQuerySource(機器点検報告SearchCondition searchCondition, IPresentationContext<機器点検報告SearchConditionMessages> context) {
        return DbContext.機器点検報告DbSet.Select(e => new 機器点検報告SearchResult {
            対象機器_Parent_機器ID = e.対象機器_Parent_機器ID,
            対象機器_Parent_機器名 = e.対象機器!.Parent!.機器名,
            対象機器_Parent_単価 = e.対象機器!.Parent!.単価,
            対象機器_Parent_機器分類_機器分類ID = e.対象機器!.Parent!.機器分類_機器分類ID,
            対象機器_Parent_機器分類_機器分類名 = e.対象機器!.Parent!.機器分類!.機器分類名,
            対象機器_Parent_供給業者_供給業者ID = e.対象機器!.Parent!.供給業者_供給業者ID,
            対象機器_Parent_供給業者_供給業者名 = e.対象機器!.Parent!.供給業者!.供給業者名,
            対象機器_Parent_供給業者_担当者名 = e.対象機器!.Parent!.供給業者!.担当者名,
            対象機器_Parent_供給業者_電話番号 = e.対象機器!.Parent!.供給業者!.電話番号,
            対象機器_Parent_供給業者_メールアドレス = e.対象機器!.Parent!.供給業者!.メールアドレス,
            対象機器_Parent_機器詳細_機器説明 = e.対象機器!.Parent!.機器詳細!.機器説明,
            対象機器_Parent_機器詳細_機器仕様_重量 = e.対象機器!.Parent!.機器詳細!.機器仕様!.重量,
            対象機器_Parent_機器詳細_機器仕様_サイズ_幅 = e.対象機器!.Parent!.機器詳細!.機器仕様!.サイズ!.幅,
            対象機器_Parent_機器詳細_機器仕様_サイズ_高さ = e.対象機器!.Parent!.機器詳細!.機器仕様!.サイズ!.高さ,
            対象機器_Parent_機器詳細_機器仕様_サイズ_奥行 = e.対象機器!.Parent!.機器詳細!.機器仕様!.サイズ!.奥行,
            対象機器_保管庫_保管庫ID = e.対象機器!.保管庫!.保管庫ID,
            対象機器_保管庫_保管庫名 = e.対象機器!.保管庫!.保管庫名,
            対象機器_保管庫_住所_郵便番号 = e.対象機器!.保管庫!.保管庫マスタの住所!.郵便番号,
            対象機器_保管庫_住所_都道府県 = e.対象機器!.保管庫!.保管庫マスタの住所!.都道府県,
            対象機器_保管庫_住所_市区町村 = e.対象機器!.保管庫!.保管庫マスタの住所!.市区町村,
            対象機器_保管庫_住所_番地建物名 = e.対象機器!.保管庫!.保管庫マスタの住所!.番地建物名,
            対象機器_保管庫_管理責任者_医療従事者ID = e.対象機器!.保管庫!.管理責任者!.医療従事者ID,
            対象機器_保管庫_管理責任者_氏名 = e.対象機器!.保管庫!.管理責任者!.氏名,
            対象機器_保管庫_管理責任者_氏名カナ = e.対象機器!.保管庫!.管理責任者!.氏名カナ,
            対象機器_保管庫_管理責任者_退職日 = e.対象機器!.保管庫!.管理責任者!.退職日,
            対象機器_在庫数 = e.対象機器!.在庫数,
            対象機器_棚卸日時 = e.対象機器!.棚卸日時,
            点検日 = e.点検日,
            点検担当者_医療従事者ID = e.点検担当者!.医療従事者ID,
            点検担当者_氏名 = e.点検担当者!.氏名,
            点検担当者_氏名カナ = e.点検担当者!.氏名カナ,
            点検担当者_退職日 = e.点検担当者!.退職日,
            実地確認数 = e.実地確認数,
            在庫差異 = e.在庫差異,
            点検メモ = e.点検メモ,
            写真URL = e.写真URL,
            対応措置 = e.対応措置.Select(action => new 対応措置SearchResult {
                措置ID = action.措置ID,
                措置種別 = action.措置種別,
                実施状況 = action.実施状況,
                実施日 = action.実施日,
                実施担当者_医療従事者ID = action.実施担当者!.医療従事者ID,
                実施担当者_氏名 = action.実施担当者!.氏名,
                実施担当者_氏名カナ = action.実施担当者!.氏名カナ,
                実施担当者_退職日 = action.実施担当者!.退職日,
                措置詳細 = action.措置詳細,
            }).ToList(),
            Version = (int)e.Version!,
        });
    }

    protected override IQueryable<措置結果SearchResult> CreateQuerySource(措置結果SearchCondition searchCondition, IPresentationContext<措置結果SearchConditionMessages> context) {
        return DbContext.措置結果DbSet.Select(e => new 措置結果SearchResult {
            対象措置_措置ID = e.対象措置!.措置ID,
            対象措置_措置種別 = e.対象措置!.措置種別,
            対象措置_実施状況 = e.対象措置!.実施状況,
            対象措置_実施日 = e.対象措置!.実施日,
            対象措置_実施担当者_医療従事者ID = e.対象措置!.実施担当者!.医療従事者ID,
            対象措置_実施担当者_氏名 = e.対象措置!.実施担当者!.氏名,
            対象措置_実施担当者_氏名カナ = e.対象措置!.実施担当者!.氏名カナ,
            対象措置_実施担当者_退職日 = e.対象措置!.実施担当者!.退職日,
            対象措置_Parent_対象機器_Parent_機器ID = e.対象措置!.Parent!.対象機器!.Parent!.機器ID,
            対象措置_Parent_対象機器_Parent_機器名 = e.対象措置!.Parent!.対象機器!.Parent!.機器名,
            対象措置_Parent_対象機器_Parent_単価 = e.対象措置!.Parent!.対象機器!.Parent!.単価,
            対象措置_Parent_対象機器_Parent_機器分類_機器分類ID = e.対象措置!.Parent!.対象機器!.Parent!.機器分類_機器分類ID,
            対象措置_Parent_対象機器_Parent_機器分類_機器分類名 = e.対象措置!.Parent!.対象機器!.Parent!.機器分類!.機器分類名,
            対象措置_Parent_対象機器_Parent_供給業者_供給業者ID = e.対象措置!.Parent!.対象機器!.Parent!.供給業者_供給業者ID,
            対象措置_Parent_対象機器_Parent_供給業者_供給業者名 = e.対象措置!.Parent!.対象機器!.Parent!.供給業者!.供給業者名,
            対象措置_Parent_対象機器_Parent_供給業者_担当者名 = e.対象措置!.Parent!.対象機器!.Parent!.供給業者!.担当者名,
            対象措置_Parent_対象機器_Parent_供給業者_電話番号 = e.対象措置!.Parent!.対象機器!.Parent!.供給業者!.電話番号,
            対象措置_Parent_対象機器_Parent_供給業者_メールアドレス = e.対象措置!.Parent!.対象機器!.Parent!.供給業者!.メールアドレス,
            対象措置_Parent_対象機器_Parent_機器詳細_機器説明 = e.対象措置!.Parent!.対象機器!.Parent!.機器詳細!.機器説明,
            対象措置_Parent_対象機器_Parent_機器詳細_機器仕様_重量 = e.対象措置!.Parent!.対象機器!.Parent!.機器詳細!.機器仕様!.重量,
            対象措置_Parent_対象機器_Parent_機器詳細_機器仕様_サイズ_幅 = e.対象措置!.Parent!.対象機器!.Parent!.機器詳細!.機器仕様!.サイズ!.幅,
            対象措置_Parent_対象機器_Parent_機器詳細_機器仕様_サイズ_高さ = e.対象措置!.Parent!.対象機器!.Parent!.機器詳細!.機器仕様!.サイズ!.高さ,
            対象措置_Parent_対象機器_Parent_機器詳細_機器仕様_サイズ_奥行 = e.対象措置!.Parent!.対象機器!.Parent!.機器詳細!.機器仕様!.サイズ!.奥行,
            対象措置_Parent_対象機器_保管庫_保管庫ID = e.対象措置!.Parent!.対象機器!.保管庫!.保管庫ID,
            対象措置_Parent_対象機器_保管庫_保管庫名 = e.対象措置!.Parent!.対象機器!.保管庫!.保管庫名,
            対象措置_Parent_対象機器_保管庫_住所_郵便番号 = e.対象措置!.Parent!.対象機器!.保管庫!.保管庫マスタの住所!.郵便番号,
            対象措置_Parent_対象機器_保管庫_住所_都道府県 = e.対象措置!.Parent!.対象機器!.保管庫!.保管庫マスタの住所!.都道府県,
            対象措置_Parent_対象機器_保管庫_住所_市区町村 = e.対象措置!.Parent!.対象機器!.保管庫!.保管庫マスタの住所!.市区町村,
            対象措置_Parent_対象機器_保管庫_住所_番地建物名 = e.対象措置!.Parent!.対象機器!.保管庫!.保管庫マスタの住所!.番地建物名,
            対象措置_Parent_対象機器_保管庫_管理責任者_医療従事者ID = e.対象措置!.Parent!.対象機器!.保管庫!.管理責任者!.医療従事者ID,
            対象措置_Parent_対象機器_保管庫_管理責任者_氏名 = e.対象措置!.Parent!.対象機器!.保管庫!.管理責任者!.氏名,
            対象措置_Parent_対象機器_保管庫_管理責任者_氏名カナ = e.対象措置!.Parent!.対象機器!.保管庫!.管理責任者!.氏名カナ,
            対象措置_Parent_対象機器_保管庫_管理責任者_退職日 = e.対象措置!.Parent!.対象機器!.保管庫!.管理責任者!.退職日,
            対象措置_Parent_対象機器_在庫数 = e.対象措置!.Parent!.対象機器!.在庫数,
            対象措置_Parent_対象機器_棚卸日時 = e.対象措置!.Parent!.対象機器!.棚卸日時,
            対象措置_Parent_写真URL = e.対象措置!.Parent!.写真URL,
            対象措置_Parent_在庫差異 = e.対象措置!.Parent!.在庫差異,
            対象措置_Parent_点検日 = e.対象措置!.Parent!.点検日,
            対象措置_Parent_点検担当者_医療従事者ID = e.対象措置!.Parent!.点検担当者!.医療従事者ID,
            対象措置_Parent_点検担当者_氏名 = e.対象措置!.Parent!.点検担当者!.氏名,
            対象措置_Parent_点検担当者_氏名カナ = e.対象措置!.Parent!.点検担当者!.氏名カナ,
            対象措置_Parent_点検担当者_退職日 = e.対象措置!.Parent!.点検担当者!.退職日,
            対象措置_Parent_実地確認数 = e.対象措置!.Parent!.実地確認数,
            対象措置_Parent_点検メモ = e.対象措置!.Parent!.点検メモ,
            対象措置_措置詳細 = e.対象措置!.措置詳細,
            次回措置_措置種別 = e.次回措置!.措置種別,
            次回措置_予定日 = e.次回措置!.予定日,
            次回措置_担当者_医療従事者ID = e.次回措置!.担当者!.医療従事者ID,
            次回措置_担当者_氏名 = e.次回措置!.担当者!.氏名,
            次回措置_担当者_氏名カナ = e.次回措置!.担当者!.氏名カナ,
            次回措置_担当者_退職日 = e.次回措置!.担当者!.退職日,
            次回措置_内容 = e.次回措置!.内容,
            フィードバック = e.フィードバック,
            結果日時 = e.結果日時,
            結果担当者_医療従事者ID = e.結果担当者!.医療従事者ID,
            結果担当者_氏名 = e.結果担当者!.氏名,
            結果担当者_氏名カナ = e.結果担当者!.氏名カナ,
            結果担当者_退職日 = e.結果担当者!.退職日,
            改善効果 = e.改善効果,
            結果状態 = e.結果状態,
            添付資料 = e.添付資料.Select(document => new 添付資料SearchResult {
                資料ID = document.資料ID,
                資料名 = document.資料名,
                資料種別 = document.資料種別,
                ファイルパス = document.ファイルパス,
                登録日時 = document.登録日時,
            }).ToList(),
            Version = (int)e.Version!,
        });
    }

    public override Task<医療機器管理ReturnValue> Execute(医療機器管理Parameter parameter, IPresentationContext<医療機器管理ParameterMessages> context) {
        // TODO: 医療機器管理コマンドの処理を実装してください。
        return Task.FromResult(new 医療機器管理ReturnValue());
    }

    public override Task<簡易診療登録ReturnValue> Execute(簡易診療登録Parameter parameter, IPresentationContext<簡易診療登録ParameterMessages> context) {
        // TODO: 簡易診療登録コマンドの処理を実装してください。
        return Task.FromResult(new 簡易診療登録ReturnValue());
    }
}
