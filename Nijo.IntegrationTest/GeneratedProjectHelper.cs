using Microsoft.Extensions.Logging;
using Nijo;
using System;
using System.IO;
using System.Xml.Linq;

namespace Nijo.IntegrationTest;

public class GeneratedProjectHelper {
    private readonly string _projectRoot;
    private readonly ILogger _logger;

    public GeneratedProjectHelper(string projectRoot, ILogger logger) {
        _projectRoot = projectRoot;
        _logger = logger;
    }

    public void GenerateCode() {
        var project = new GeneratedProject(_projectRoot, _logger);
        project.GenerateCode();
    }

    public void ValidateSchema() {
        var project = new GeneratedProject(_projectRoot, _logger);
        if (!project.ValidateSchema()) {
            throw new Exception("スキーマの検証に失敗しました。");
        }
    }

    public string GetSchemaXml() {
        return File.ReadAllText(Path.Combine(_projectRoot, "nijo.xml"));
    }
}
