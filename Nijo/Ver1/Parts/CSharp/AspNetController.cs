using Nijo.Util.DotnetEx;
using Nijo.Ver1.ImmutableSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.Parts.CSharp {
    public class AspNetController {
        public AspNetController(RootAggregate rootAggregate) {
            _rootAggregate = rootAggregate;
        }

        private readonly RootAggregate _rootAggregate;

        private const string API_SUBDOMAIN = "api";

        /// <summary>
        /// RootAttributeの引数に指定するルーティング定義
        /// </summary>
        public string Route => $"{API_SUBDOMAIN}/{_rootAggregate.LatinName.ToKebabCase()}";
        /// <summary>
        /// ASP.NET Core コントローラークラス名
        /// </summary>
        public string CsClassName => $"{_rootAggregate.PhysicalName}Controller";

        /// <summary>
        /// Webクライアント側からアクセスするときのAction名を返す。
        /// </summary>
        /// <param name="action">Action名単体</param>
        /// <returns>URLのドメイン部分を除いたサブドメイン部分</returns>
        public string GetActionNameForClient(string action) {
            return $"{Route}/{action}";
        }
    }
}
