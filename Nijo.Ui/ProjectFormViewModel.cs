using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Nijo.SchemaParsing;
using System.Text.Json;
using System.Data;
using System.Windows.Forms;
using Nijo.Models;
using Nijo.CodeGenerating;
using Nijo.Ui.Views;

namespace Nijo.Ui;

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

    #region サイドメニュー
    private const string MENU_TAG_ROOT = "ROOT";
    private const string MENU_TAG_ENUMS = "ENUMS";
    private const string MENU_TAG_VALUE_OJECTS = "VALUE-OBJECTS";

    /// <summary>
    /// メニュー項目の一覧を取得
    /// </summary>
    public IEnumerable<TreeNode> GetMenuItems() {
        if (_schemaParseContext.Document.Root == null) return [];

        // アプリケーション名をルートノードとして作成
        var rootNode = new TreeNode(ProjectName);
        rootNode.Tag = MENU_TAG_ROOT;

        // 属性種類定義
        var memberTypesNode = new TreeNode("属性種類定義");
        rootNode.Nodes.Add(memberTypesNode);

        // 列挙体一覧ノード
        var enumNode = new TreeNode("列挙体");
        enumNode.Tag = MENU_TAG_ENUMS;
        memberTypesNode.Nodes.Add(enumNode);

        // 値オブジェクト一覧ノード
        var valueObjectNode = new TreeNode("値オブジェクト");
        valueObjectNode.Tag = MENU_TAG_VALUE_OJECTS;
        memberTypesNode.Nodes.Add(valueObjectNode);

        // データモデル、コマンドモデル、クエリモデルをそれぞれ個別ノードとして追加
        foreach (var el in _schemaParseContext.Document.Root.Elements()) {
            if (!_schemaParseContext.TryGetModel(el, out var model)) continue;

            if (model is DataModel || model is QueryModel || model is CommandModel) {
                var node = new TreeNode($"{el.Name.LocalName} ({model.SchemaName})");
                node.Tag = el;
                rootNode.Nodes.Add(node);
            }
        }

        rootNode.ExpandAll();

        return [rootNode];
    }
    #endregion サイドメニュー

    /// <summary>
    /// アプリケーション設定を取得
    /// </summary>
    public CodeRenderingConfig GetAppConfig() {
        return new CodeRenderingConfig(_schemaParseContext.Document);
    }

    /// <summary>
    /// データモデルの詳細情報を取得
    /// </summary>
    /// <returns>新たに選択された要素の画面</returns>
    public Control ChangeSelectedElement(TreeNode node) {

        // アプリケーション全体設定
        if (node.Tag as string == MENU_TAG_ROOT) {
            // アプリケーション設定画面を返す
            return new AppConfigView(GetAppConfig());
        }

        // 列挙体一覧
        if (node.Tag as string == MENU_TAG_ENUMS) {
            // 列挙体一覧の編集画面を返す
            return new EnumListView(_schemaParseContext);
        }

        // 値オブジェクト一覧
        if (node.Tag as string == MENU_TAG_VALUE_OJECTS) {
            // 値オブジェクト一覧の編集画面を返す
            return new ValueObjectListView(_schemaParseContext);
        }

        // モデル1個単位の詳細画面
        if (node.Tag is XElement xElement && _schemaParseContext.TryGetModel(xElement, out var model)) {
            var component = new RootAggregateView();
            component.DisplayRootAggregateInfo(xElement, model, _schemaParseContext);
            return component;
        }

        // その他の場合（何も表示しない）
        var unknownLabel = new Label();
        unknownLabel.Text = "";
        return unknownLabel;
    }
}
