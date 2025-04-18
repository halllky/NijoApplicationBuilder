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
    /// 画面左側で選択されているルート集約
    /// </summary>
    private XElement? _selectedElement;

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
    /// データモデルの詳細情報を取得
    /// </summary>
    /// <returns>新たに選択された要素の画面</returns>
    public Control ChangeSelectedElement(XElement xElement) {
        _selectedElement = xElement;

        string typeName = xElement.Attribute("Type")?.Value ?? string.Empty;

        if (typeName == "data-model") {
            // データモデルの場合の処理
            var component = new RootAggregateDataModelComponent();
            component.DisplayRootAggregateInfo(xElement, _schemaParseContext);
            return component;

        } else {
            // 非データモデルの場合
            var label = new Label();
            label.Text = $" (未対応のモデルタイプ: {typeName})";
            return label;
        }
    }
}
