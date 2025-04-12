using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.IntegrationTest.Implementors;

public class ScalarMemberImplementor : IApplicationServiceImplementor {
    public string TargetXmlFileName => "012_スカラメンバー網羅.xml";

    public string GetImplementation(XDocument schemaXml) {
        return @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApp.Core;

public partial class OverridedApplicationService {
    public override Task<CommandModelテストReturnType> Execute(CommandModelテストParameter param, IPresentationContext<CommandModelテストParameterMessages> context) {
        throw new NotImplementedException();
    }

    protected override IQueryable<スカラー網羅SearchResult> CreateQuerySource(スカラー網羅SearchCondition searchCondition, IPresentationContext<スカラー網羅Messages> context) {
        return DbContext.スカラー網羅DbSet.Select(e => new スカラー網羅SearchResult {
            スカラー網羅ID = e.スカラー網羅ID,
            文字列 = e.文字列,
            数値 = e.数値,
            日付 = e.日付,
            真偽値 = e.真偽値,
            列挙値 = e.列挙値,
            Version = (int)e.Version!,
        });
    }
}";
    }
}
