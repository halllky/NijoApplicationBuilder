using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Data;
using Nijo.SchemaParsing;

namespace Nijo.Ui.Views;

/// <summary>
/// ルート集約1件と対応する。
/// UIをレイアウトに集中させるためにロジックを隠蔽する責務をもつ。
/// </summary>
public class RootAggregateViewModel {

    public RootAggregateViewModel(XElement rootAggregateElement, ProjectFormViewModel projectViewModel) {
        _rootAggregateElement = rootAggregateElement;
        _projectViewModel = projectViewModel;
    }

    private readonly XElement _rootAggregateElement;
    private readonly ProjectFormViewModel _projectViewModel;

    /// <summary>
    /// データモデルの詳細情報を取得
    /// </summary>
    public (DataTable DataTable, string Label) GetDataModelDetail() {
        var element = _rootAggregateElement;

        var dataTable = new DataTable(element.Name.LocalName);
        var label = $"{element.Name.LocalName}";
        string typeName = element.Attribute("Type")?.Value ?? string.Empty;

        if (typeName == "data-model") {
            // データモデルの場合の処理
            CreateDataModelDetailTable(dataTable, element);
            label += " (データモデル)";
        } else {
            // 非データモデルの場合
            label += $" (未対応のモデルタイプ: {typeName})";
        }

        return (dataTable, label);
    }

    /// <summary>
    /// データモデル詳細用のデータテーブルを作成
    /// </summary>
    private void CreateDataModelDetailTable(DataTable dataTable, XElement element) {
        var schemaContext = _projectViewModel.GetSchemaParseContext();

        // 基本列の定義
        dataTable.Columns.Add("項目定義", typeof(string));
        dataTable.Columns.Add("種類", typeof(string));
        dataTable.Columns.Add("物理名", typeof(string));

        // SchemaParseRuleからNodeOptionsを取得し、データモデルに適用可能な属性のみをフィルタリング
        var dataModelType = schemaContext.Models
            .FirstOrDefault(m => m.Value.SchemaName == "data-model");

        var availableOptions = schemaContext
            .GetOptions(element)
            .Where(opt => opt.IsAvailableModel == null ||
                   dataModelType.Value != null && opt.IsAvailableModel(dataModelType.Value))
            .ToArray();

        // 動的にNodeOptionsの属性に対応する列を追加
        var optionColumns = new Dictionary<string, string>();
        foreach (var option in availableOptions) {
            dataTable.Columns.Add(option.DisplayName, typeof(string));
            optionColumns.Add(option.AttributeName, option.DisplayName);
        }

        // 追加の列
        if (!dataTable.Columns.Contains("添付可能な拡張子")) {
            dataTable.Columns.Add("添付可能な拡張子", typeof(string));
        }

        // モデル自身の行を追加
        var modelRow = dataTable.NewRow();
        modelRow["項目定義"] = element.Name.LocalName;
        modelRow["種類"] = "DataModel";
        modelRow["物理名"] = "-";

        // 動的に属性値を設定
        foreach (var attr in element.Attributes()) {
            if (optionColumns.TryGetValue(attr.Name.LocalName, out var columnName)) {
                if (attr.Value.ToLower() == "true" || attr.Value.ToLower() == "false") {
                    modelRow[columnName] = GetBoolAttributeValue(element, attr.Name.LocalName);
                } else {
                    modelRow[columnName] = attr.Value;
                }
            }
        }

        dataTable.Rows.Add(modelRow);

        // 各メンバーの行を追加
        foreach (var member in element.Elements()) {
            AddMemberRow(dataTable, member, "", optionColumns);

            // Childrenタイプの場合は子要素も追加
            if (member.Attribute("Type")?.Value == "children" || member.Attribute("Type")?.Value == "child") {
                foreach (var childMember in member.Elements()) {
                    AddMemberRow(dataTable, childMember, "    ", optionColumns);
                }
            }
        }
    }

    /// <summary>
    /// データテーブルにメンバー行を追加
    /// </summary>
    private void AddMemberRow(DataTable dataTable, XElement member, string indent, Dictionary<string, string> optionColumns) {
        var row = dataTable.NewRow();
        row["項目定義"] = indent + member.Name.LocalName;
        row["種類"] = member.Attribute("Type")?.Value ?? "-";
        row["物理名"] = member.Attribute("PhysicalName")?.Value ?? "-";

        // 動的に属性値を設定
        foreach (var attr in member.Attributes()) {
            if (optionColumns.TryGetValue(attr.Name.LocalName, out var columnName)) {
                if (attr.Value.ToLower() == "true" || attr.Value.ToLower() == "false") {
                    row[columnName] = GetBoolAttributeValue(member, attr.Name.LocalName);
                } else {
                    row[columnName] = attr.Value;
                }
            }
        }

        dataTable.Rows.Add(row);
    }

    /// <summary>
    /// Boolean型の属性値を取得
    /// </summary>
    private string GetBoolAttributeValue(XElement element, string attributeName) {
        var attr = element.Attribute(attributeName);
        if (attr == null) return "-";

        return attr.Value.ToLower() == "true" ? "○" : "×";
    }
}
