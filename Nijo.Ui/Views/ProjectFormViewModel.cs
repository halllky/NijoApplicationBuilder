using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Nijo.SchemaParsing;
using System.Text.Json;
using System.Data;

namespace Nijo.Ui.Views;

/// <summary>
/// NijoApplicationBuilderで構築されるプロジェクト1件と対応する。
/// UIをレイアウトに集中させるためにロジックを隠蔽する責務をもつ。
/// </summary>
public class ProjectFormViewModel {

    internal ProjectFormViewModel(GeneratedProject project) {
        _project = project;
        _schemaParseContext = new SchemaParseContext(
            XDocument.Load(_project.SchemaXmlPath),
            SchemaParseRule.Default());
    }

    /// <summary>
    /// 現在開かれているプロジェクト
    /// </summary>
    private readonly GeneratedProject _project;
    /// <summary>
    /// スキーマ定義解釈コンテキスト
    /// </summary>
    private readonly SchemaParseContext _schemaParseContext;

    /// <summary>
    /// プロジェクトルートフォルダ
    /// </summary>
    public string ProjectRoot => _project.ProjectRoot;
    /// <summary>
    /// プロジェクト名
    /// </summary>
    public string ProjectName => Path.GetFileName(_project.ProjectRoot);

    /// <summary>
    /// ルート集約の一覧を取得
    /// </summary>
    public IEnumerable<XElement> GetRootAggregates() {
        if (_schemaParseContext.Document.Root == null) return Enumerable.Empty<XElement>();

        return _schemaParseContext.Document.Root.Elements()
            .Where(e => {
                var typeAttr = e.Attribute("Type")?.Value;
                return typeAttr == "data-model" || typeAttr == "command-model" ||
                       typeAttr == "query-model" || typeAttr == "enum";
            });
    }

    /// <summary>
    /// スキーマ定義解釈コンテキストを取得
    /// </summary>
    public SchemaParseContext GetSchemaParseContext() {
        return _schemaParseContext;
    }
}
