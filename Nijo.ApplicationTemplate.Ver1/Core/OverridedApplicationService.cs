using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApp.Core;

partial class OverridedApplicationService {

    public override Task<CommandModelテストReturnValue> Execute(CommandModelテストParameter param, IPresentationContext<CommandModelテストParameterMessages> context) {

        context.Messages.AddInfo($"サーバー側処理が呼び出されました。現在の時刻は {CurrentTime:G} です。");

        return Task.FromResult(new CommandModelテストReturnValue {
            // 戻り値を記載する場合はここに書く
        });
    }

    protected override IQueryable<部署SearchResult> CreateQuerySource(部署SearchCondition searchCondition, IPresentationContext<部署SearchConditionMessages> context) {
        return DbContext.部署DbSet.Select(e => new 部署SearchResult {
            部署コード = e.部署コード,
            部署名 = e.部署名,
            Version = (int)e.Version!,
        });
    }

    protected override IQueryable<従業員SearchResult> CreateQuerySource(従業員SearchCondition searchCondition, IPresentationContext<従業員SearchConditionMessages> context) {
        return DbContext.従業員DbSet.Select(e => new 従業員SearchResult {
            従業員ID = e.従業員ID,
            氏名 = e.氏名,
            氏名カナ = e.氏名カナ,
            退職日 = e.退職日,
            所属部署 = e.所属部署.Select(d => new 所属部署SearchResult {
                年度 = d.年度,
                部署_部署名 = d.部署!.部署名,
                部署_部署コード = d.部署!.部署コード,
            }).ToList(),
            Version = (int)e.Version!,
        });
    }

    protected override IQueryable<顧客SearchResult> CreateQuerySource(顧客SearchCondition searchCondition, IPresentationContext<顧客SearchConditionMessages> context) {
        return DbContext.顧客DbSet.Select(e => new 顧客SearchResult {
            顧客ID = e.顧客ID,
            顧客名 = e.顧客名,
            住所_市町村 = e.住所!.市町村,
            住所_都道府県 = e.住所!.都道府県,
            住所_番地以降 = e.住所!.番地以降,
            備考 = e.備考,
            年齢 = e.年齢,
            生年月日 = e.生年月日,
            Version = (int)e.Version!,
        });
    }
}
