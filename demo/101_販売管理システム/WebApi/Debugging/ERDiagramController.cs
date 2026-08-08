using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace MyApp.WebApi.Debugging;

[ApiController]
[Route("api/debug/er-diagram")]
public class ERDiagramController : ControllerBase {

    private const string SAVE_FILE_NAME = "ERDiagram.layout.json";

    [HttpGet]
    public IActionResult GetERDiagram([FromServices] OverridedApplicationService app, [FromServices] IWebHostEnvironment env) {
        var model = app.DbContext.GetService<IDesignTimeModel>().Model;
        var entityTypes = model.GetEntityTypes().ToList();

        return Ok(new ERDiagramResponse {
            LogicalNameDataSet = BuildDataSet(entityTypes, useLogicalName: true),
            PhysicalNameDataSet = BuildDataSet(entityTypes, useLogicalName: false),
            SavedState = LoadSavedState(env),
        });
    }

    [HttpPost("layout")]
    public async Task<IActionResult> SaveLayout([FromBody] ERDiagramSavedState request, [FromServices] IWebHostEnvironment env) {
        if (!env.IsDevelopment()) {
            return Forbid();
        }

        var saveFilePath = GetSaveFilePath(env);
        var directoryPath = Path.GetDirectoryName(saveFilePath);
        if (directoryPath == null) {
            return Problem("保存先ディレクトリを解決できませんでした。", statusCode: StatusCodes.Status500InternalServerError);
        }

        Directory.CreateDirectory(directoryPath);

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) {
            WriteIndented = true,
        };

        await using var stream = System.IO.File.Create(saveFilePath);
        await JsonSerializer.SerializeAsync(stream, request, options);

        return Ok();
    }

    private static GraphView2DataSet BuildDataSet(IReadOnlyList<IReadOnlyEntityType> entityTypes, bool useLogicalName) {
        var nodes = new List<GraphView2Node>();
        var edges = new List<GraphView2Edge>();

        foreach (var entityType in entityTypes) {
            var isView = entityType.GetTableName() == null && entityType.GetViewName() != null;
            var physicalName = entityType.GetTableName() ?? entityType.GetViewName() ?? entityType.DisplayName();
            var shortName = entityType.ClrType?.Name ?? entityType.DisplayName();
            // "入荷DbEntity" → "入荷"
            var logicalName = shortName.EndsWith("DbEntity")
                ? shortName[..^"DbEntity".Length]
                : shortName;

            var nodeLabel = useLogicalName ? logicalName : physicalName;
            if (isView) nodeLabel += " (ビュー)";

            var pk = entityType.FindPrimaryKey();
            var pkProps = pk?.Properties.ToHashSet() ?? [];
            var uniqueIndexProps = entityType.GetIndexes()
                .Where(i => i.IsUnique)
                .SelectMany(i => i.Properties)
                .ToHashSet();

            var headers = new[] {
                useLogicalName ? "カラム名(論理)" : "カラム名(物理)",
                "型", "PK", "NOT NULL", "ユニーク",
            };
            var rows = entityType.GetProperties()
                .OrderBy(p => p.GetColumnOrder() ?? int.MaxValue)
                .Select(p => new[] {
                    useLogicalName ? p.Name : (p.GetColumnName() ?? p.Name),
                    GetStoreTypeDisplay(p),
                    pkProps.Contains(p) ? "○" : "",
                    p.IsNullable ? "" : "○",
                    uniqueIndexProps.Contains(p) ? "○" : "",
                })
                .ToArray();

            nodes.Add(new GraphView2Node {
                Id = entityType.Name,
                Label = nodeLabel,
                Table = new TableData { Headers = headers, Rows = rows },
            });

            // 外部キーをエッジとして追加（このエンティティが依存側）
            foreach (var fk in entityType.GetForeignKeys()) {
                edges.Add(new GraphView2Edge {
                    Source = entityType.Name,
                    Target = fk.PrincipalEntityType.Name,
                    TargetEndShape = "triangle",
                });
            }
        }

        return new GraphView2DataSet { Nodes = nodes, Edges = edges };
    }

    private static string GetStoreTypeDisplay(IReadOnlyProperty property) {
        var storeObject = StoreObjectIdentifier.Create(property.DeclaringType, StoreObjectType.Table)
            ?? StoreObjectIdentifier.Create(property.DeclaringType, StoreObjectType.View);

        var storeType = storeObject.HasValue
            ? property.GetColumnType(storeObject.Value)
            : property.GetColumnType();

        return storeType
            ?? property.GetRelationalTypeMapping().StoreType;
    }

    private static ERDiagramSavedState? LoadSavedState(IWebHostEnvironment env) {
        var saveFilePath = GetSaveFilePath(env);
        if (!System.IO.File.Exists(saveFilePath)) return null;

        try {
            var json = System.IO.File.ReadAllText(saveFilePath);
            return JsonSerializer.Deserialize<ERDiagramSavedState>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        } catch {
            return null;
        }
    }

    private static string GetSaveFilePath(IWebHostEnvironment env) {
        return Path.Combine(env.ContentRootPath, SAVE_FILE_NAME);
    }

    private class ERDiagramResponse {
        public GraphView2DataSet LogicalNameDataSet { get; set; } = new();
        public GraphView2DataSet PhysicalNameDataSet { get; set; } = new();
        public ERDiagramSavedState? SavedState { get; set; }
    }

    public class ERDiagramSavedState {
        [JsonPropertyName("logicalName")]
        public GraphView2SavedDataSet? LogicalName { get; set; }
        [JsonPropertyName("physicalName")]
        public GraphView2SavedDataSet? PhysicalName { get; set; }
        [JsonPropertyName("selectedDataSetKey")]
        public string? SelectedDataSetKey { get; set; }
    }

    public class GraphView2DataSet {
        [JsonPropertyName("nodes")]
        public List<GraphView2Node> Nodes { get; set; } = [];
        [JsonPropertyName("edges")]
        public List<GraphView2Edge> Edges { get; set; } = [];
    }

    public class GraphView2SavedDataSet : GraphView2DataSet {
        [JsonPropertyName("nodePositions")]
        public Dictionary<string, NodePosition> NodePositions { get; set; } = [];
        [JsonPropertyName("defaultPan")]
        public NodePosition? DefaultPan { get; set; }
        [JsonPropertyName("defaultZoom")]
        public double? DefaultZoom { get; set; }
    }

    public class NodePosition {
        [JsonPropertyName("x")]
        public double X { get; set; }
        [JsonPropertyName("y")]
        public double Y { get; set; }
    }

    /// <summary>
    /// GraphView2 コンポーネントで使用するノードのデータ構造。
    /// ER図で言うとテーブルまたはビュー1個分。
    /// </summary>
    public class GraphView2Node {
        [JsonPropertyName("id")]
        public string Id { get; set; } = default!;
        [JsonPropertyName("label")]
        public string? Label { get; set; }
        [JsonPropertyName("table")]
        public TableData? Table { get; set; }
    }
    /// <summary>
    /// GraphView2 コンポーネントで使用するテーブルデータの構造。
    /// ER図で言うとテーブルのカラム名、NOT NULL などの情報。
    /// </summary>
    public class TableData {
        [JsonPropertyName("headers")]
        public string[] Headers { get; set; } = default!;
        [JsonPropertyName("rows")]
        public string[][] Rows { get; set; } = default!;
    }
    /// <summary>
    /// GraphView2 コンポーネントで使用するエッジのデータ構造。
    /// ER図で言うとテーブル同士のリレーションシップ（外部キー制約など）を表す。
    /// </summary>
    public class GraphView2Edge {
        [JsonPropertyName("label")]
        public string? Label { get; set; }
        [JsonPropertyName("source")]
        public string Source { get; set; } = default!;
        [JsonPropertyName("target")]
        public string Target { get; set; } = default!;

        /// <summary>cytoscape.Css.ArrowShape で利用できる値のみ指定可能</summary>
        [JsonPropertyName("sourceEndShape")]
        public string? SourceEndShape { get; set; }
        /// <summary>cytoscape.Css.ArrowShape で利用できる値のみ指定可能</summary>
        [JsonPropertyName("targetEndShape")]
        public string? TargetEndShape { get; set; }
    }
}
