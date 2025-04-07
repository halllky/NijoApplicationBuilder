using System.Xml.Linq;

namespace Nijo.IntegrationTest.Implementors;

public class SimpleAggregateImplementor : IApplicationServiceImplementor
{
    public string TargetXmlFileName => "000_単純な集約.xml";

    public string GetImplementation(XDocument schemaXml)
    {
        return @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace MyApp.Core;

public partial class OverridedApplicationService {
    public override async Task<QueryModel.集約A一覧> 集約A一覧(QueryModel.集約A一覧.QueryParameter param) {
        var query = DbContext.集約A.AsNoTracking();
        return new QueryModel.集約A一覧 {
            Items = await query
                .Select(x => new QueryModel.集約A一覧.Item {
                    ID = x.ID,
                    名前 = x.名前,
                    従属項目 = x.従属項目,
                })
                .ToListAsync(),
        };
    }
}";
    }
}
