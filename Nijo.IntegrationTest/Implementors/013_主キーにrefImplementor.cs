using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.IntegrationTest.Implementors;

public class PrimaryKeyRefImplementor : ApplicationServiceImplementorBase {
    public override string TargetXmlFileName => "013_主キーにref.xml";

    public override string GetImplementation(XDocument schemaXml) {
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

    protected override IQueryable<主SearchResult> CreateQuerySource(主SearchCondition searchCondition, IPresentationContext<主Messages> context) {
        return DbContext.主DbSet.Select(e => new 主SearchResult {
            主ID = e.主ID,
            主名 = e.主名,
            Version = (int)e.Version!,
        });
    }

    protected override IQueryable<従SearchResult> CreateQuerySource(従SearchCondition searchCondition, IPresentationContext<従Messages> context) {
        return DbContext.従DbSet.Select(e => new 従SearchResult {
            従ID = e.従ID,
            従名 = e.従名,
            主_主ID = e.主!.主ID,
            主_主名 = e.主!.主名,
            Version = (int)e.Version!,
        });
    }
}";
    }
}
