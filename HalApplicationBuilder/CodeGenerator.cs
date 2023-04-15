using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;
using HalApplicationBuilder.Core;

namespace HalApplicationBuilder {

    /// <summary>
    /// C#ソースコードの自動生成機能を提供します。
    /// </summary>
    public sealed class CodeGenerator {
        /// <summary>
        /// アセンブリ内から集約定義クラスを収集し、コード生成機能をもったオブジェクトを返します。
        /// </summary>
        /// <param name="assembly">このアセンブリ内から集約定義のクラスを収集します。</param>
        /// <param name="namespace">この名前空間の中にあるクラスのみを対象とします。未指定の場合、アセンブリ内の全クラスが対象となります。</param>
        /// <returns>コード生成機能をもったオブジェクト</returns>
        public static CodeGenerator FromAssembly(Assembly assembly, string? @namespace = null) {
            return new CodeGenerator(config => {
                var types = assembly
                    .GetTypes()
                    .Where(type => type.GetCustomAttribute<AggregateAttribute>() != null);
                if (!string.IsNullOrWhiteSpace(@namespace)) {
                    types = types.Where(type => type.Namespace?.StartsWith(@namespace) == true);
                }
                return types.Select(t => new RootAggregate(config, new Core.Definition.ReflectionDefine(config, t, types)));
            });
        }
        /// <summary>
        /// 引数の型を集約定義として、コード生成機能をもったオブジェクトを返します。
        /// </summary>
        /// <param name="rootAggregateTypes">集約ルートの型</param>
        /// <returns>コード生成機能をもったオブジェクト</returns>
        public static CodeGenerator FromReflection(IEnumerable<Type> rootAggregateTypes) {
            return new CodeGenerator(config => {
                return rootAggregateTypes.Select(t => {
                    var def = new Core.Definition.ReflectionDefine(config, t, rootAggregateTypes);
                    return new RootAggregate(config, def);
                });
            });
        }
        /// <summary>
        /// 引数のXML文字列を集約定義として、コード生成機能をもったオブジェクトを返します。
        /// </summary>
        /// <param name="xml">XML（ファイル名ではなくXMLそのもの）</param>
        /// <returns>コード生成機能をもったオブジェクト</returns>
        public static CodeGenerator FromXml(string xml) {
            return new CodeGenerator(config => {
                return Core.Definition.XmlDefine
                    .Create(config, xml)
                    .Select(def => new RootAggregate(config, def));
            });
        }

        internal CodeGenerator(Func<Config, IEnumerable<RootAggregate>> func) {
            _rootAggregateBuilder = func;
        }
        private readonly Func<Config, IEnumerable<RootAggregate>> _rootAggregateBuilder;

        /// <summary>
        /// コードの自動生成を実行します。
        /// </summary>
        /// <param name="config">コード生成に関する設定</param>
        /// <param name="log">このオブジェクトを指定した場合、コード生成の詳細を記録します。</param>
        public void GenerateCode(Config config, TextWriter? log = null) {

            log?.WriteLine($"コード自動生成開始");

            var rootDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), config.OutProjectDir));
            log?.WriteLine($"ルートディレクトリ: {rootDir}");

            // プロジェクト初回作成時
            if (!Directory.Exists(rootDir)) {
                var project = $"halapp.temp.{Path.GetRandomFileName()}";
                var tempDir = Path.Combine(Directory.GetCurrentDirectory(), project);

                void DeleteTempDirectory() {
                    Console.WriteLine("一時ディレクトリを削除します。");
                    if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
                }
                Program.OnCancelKeyPress.Push(DeleteTempDirectory);

                try {
                    Directory.CreateDirectory(tempDir);

                    // dotnet CLI でプロジェクトを新規作成
                    log?.WriteLine($"ASP.NET MVC Core プロジェクトを作成します。");
                    Program.Cmd(tempDir, "dotnet", "new", "mvc", "--output", ".", "--name", config.ApplicationName);

                    log?.WriteLine($"Microsoft.EntityFrameworkCore パッケージへの参照を追加します。");
                    Program.Cmd(tempDir, "dotnet", "add", "package", "Microsoft.EntityFrameworkCore");

                    log?.WriteLine($"Microsoft.EntityFrameworkCore.Proxies パッケージへの参照を追加します。");
                    Program.Cmd(tempDir, "dotnet", "add", "package", "Microsoft.EntityFrameworkCore.Proxies");

                    log?.WriteLine($"Microsoft.EntityFrameworkCore.Sqlite パッケージへの参照を追加します。");
                    Program.Cmd(tempDir, "dotnet", "add", "package", "Microsoft.EntityFrameworkCore.Sqlite");

                    // halapp.dll への参照を加える。実行時にRuntimeContextを参照しているため
                    log?.WriteLine($"halapp.dll を参照に追加します。");

                    // dllをプロジェクトディレクトリにコピー
                    const string HALAPP_DLL = "halapp.dll";
                    var halappDllDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
                    var halappDllPath = Path.Combine(halappDllDir, HALAPP_DLL);
                    var halappDllDist = Path.Combine(tempDir, HALAPP_DLL);

                    File.Copy(halappDllPath, halappDllDist, true);

                    // csprojファイルを編集して halapp.dll への参照を追加する（dll参照は dotnet add でサポートされていないため）
                    var csprojPath = Path.Combine(tempDir, $"{config.ApplicationName}.csproj");
                    var projectOption = new Microsoft.Build.Definition.ProjectOptions {
                        // Referenceを追加するだけなので Microsoft.NET.Sdk.Web が無くてもエラーにならないようにしたい
                        LoadSettings = Microsoft.Build.Evaluation.ProjectLoadSettings.IgnoreMissingImports,
                    };
                    var csproj = Microsoft.Build.Evaluation.Project.FromFile(csprojPath, projectOption);
                    var itemGroup = csproj.Xml.AddItemGroup();
                    var reference = itemGroup.AddItem("Reference", "MyAssembly");
                    reference.AddMetadata("HintPath", $".\\{HALAPP_DLL}");
                    csproj.Save();

                    // ソースコード生成
                    var dbContextFileName = $"{config.DbContextName}.cs";
                    log?.WriteLine($"{dbContextFileName} ファイルを作成します。");
                    var dbContextDir = Path.Combine(tempDir, "EntityFramework");
                    Directory.CreateDirectory(dbContextDir);
                    using (var sw = new StreamWriter(Path.Combine(dbContextDir, dbContextFileName), append: false, encoding: Encoding.UTF8)) {
                        sw.Write(new CodeRendering.DbContextTemplate(config).TransformText());
                    }

                    const string LAYOUT_CSHTML = "_Layout.cshtml";
                    log?.WriteLine($"{LAYOUT_CSHTML} ファイルを書き換えます。");
                    var layoutCshtmlDir = Path.Combine(tempDir, "Views", "Shared");
                    using (var sw = new StreamWriter(Path.Combine(layoutCshtmlDir, LAYOUT_CSHTML), append: false, encoding: Encoding.UTF8)) {
                        sw.Write(new CodeRendering.AspNetMvc.LayoutCshtmlTemplate().TransformText());
                    }

                    // Program.cs書き換え
                    log?.WriteLine($"HalappDefaultConfigure.cs ファイルを作成します。");
                    using (var sw = new StreamWriter(Path.Combine(tempDir, "HalappDefaultConfigure.cs"), append: false, encoding: Encoding.UTF8)) {
                        sw.Write(new CodeRendering.DefaultRuntimeConfigTemplate().TransformText());
                    }
                    log?.WriteLine($"Program.cs ファイルを書き換えます。");
                    var insertLines = new[] {
                        $"",
                        $"/* HalApplicationBuilder によって自動生成されたコード ここから */",
                        $"var runtimeRootDir = System.IO.Directory.GetCurrentDirectory();",
                        $"HalApplicationBuilder.Runtime.HalAppDefaultConfigurer.Configure(builder.Services, runtimeRootDir);",

                        $"// HTMLのエンコーディングをUTF-8にする(日本語のHTMLエンコード防止)",
                        $"builder.Services.Configure<Microsoft.Extensions.WebEncoders.WebEncoderOptions>(options => {{",
                        $"    options.TextEncoderSettings = new System.Text.Encodings.Web.TextEncoderSettings(System.Text.Unicode.UnicodeRanges.All);",
                        $"}});",
                        $"/* HalApplicationBuilder によって自動生成されたコード ここまで */",
                        $"",
                    };
                    var regex = new Regex(@"^.*[a-zA-Z]+ builder = .+;$");
                    var programCsPath = Path.Combine(tempDir, "Program.cs");
                    var lines = File.ReadAllLines(programCsPath).ToList();
                    var position = lines.FindIndex(regex.IsMatch);
                    if (position == -1) throw new InvalidOperationException("Program.cs の中にIServiceCollectionを持つオブジェクトを初期化する行が見つかりません。");
                    lines.InsertRange(position + 1, insertLines);
                    File.WriteAllLines(programCsPath, lines);

                    // tailwindcssを有効にする
                    log?.WriteLine($"package.jsonを作成します。");
                    Program.Cmd(tempDir, "npm", "init", "-y");
                    var packageJsonPath = Path.Combine(tempDir, "package.json");
                    var packageJson = JObject.Parse(File.ReadAllText(packageJsonPath));
                    var scripts = new JObject();
                    scripts["buildcss"] = "postcss wwwroot/css/app.css -o wwwroot/css/app.min.css";
                    packageJson.SelectToken("name").Replace($"halapp-{Guid.NewGuid()}");
                    packageJson.SelectToken("scripts").Replace(scripts);
                    File.WriteAllText(packageJsonPath, packageJson.ToString());

                    log?.WriteLine($"必要な Node.js パッケージをインストールします。");
                    Program.Cmd(tempDir, "npm", "install", "tailwindcss", "postcss", "postcss-cli", "autoprefixer");

                    log?.WriteLine($"app.cssファイルを生成します。");
                    using (var sw = new StreamWriter(Path.Combine(tempDir, "wwwroot", "css", "app.css"), append: false, encoding: Encoding.UTF8)) {
                        sw.Write(new CodeRendering.AspNetMvc.app_css().TransformText());
                    }
                    log?.WriteLine($"postcss.config.jsファイルを生成します。");
                    using (var sw = new StreamWriter(Path.Combine(tempDir, "postcss.config.js"), append: false, encoding: Encoding.UTF8)) {
                        sw.Write(new CodeRendering.AspNetMvc.postcss_config_js().TransformText());
                    }
                    log?.WriteLine($"tailwind.config.jsファイルを生成します。");
                    using (var sw = new StreamWriter(Path.Combine(tempDir, "tailwind.config.js"), append: false, encoding: Encoding.UTF8)) {
                        sw.Write(new CodeRendering.AspNetMvc.tailwind_config_js().TransformText());
                    }

                    // ここまでの処理がすべて成功したら一時ディレクトリを本来のディレクトリ名に変更
                    if (Directory.Exists(rootDir)) throw new InvalidOperationException($"プロジェクトディレクトリを {rootDir} に移動できません。");
                    Directory.Move(tempDir, rootDir);

                } finally {
                    DeleteTempDirectory();
                }
            }

            var _rootAggregates = _rootAggregateBuilder.Invoke(config);
            var allAggregates = _rootAggregates
                .SelectMany(a => a.GetDescendantsAndSelf())
                .ToArray();

            log?.WriteLine("コード自動生成: スキーマ定義");
            using (var sw = new StreamWriter(Path.Combine(rootDir, "halapp.json"), append: false, encoding: Encoding.UTF8)) {
                var schema = new Serialized.AppSchemaJson {
                    Config = config.ToJson(onlyRuntimeConfig: true),
                    Aggregates = _rootAggregates.Select(a => a.ToJson()).ToArray(),
                };
                sw.Write(System.Text.Json.JsonSerializer.Serialize(schema, new System.Text.Json.JsonSerializerOptions {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All), // 日本語用
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull, // nullのフィールドをシリアライズしない
                }));
            }

            var efSourceDir = Path.Combine(rootDir, config.EntityFrameworkDirectoryRelativePath);
            if (Directory.Exists(efSourceDir)) Directory.Delete(efSourceDir, recursive: true);
            Directory.CreateDirectory(efSourceDir);

            log?.WriteLine("コード自動生成: Entity定義");
            using (var sw = new StreamWriter(Path.Combine(efSourceDir, "Entities.cs"), append: false, encoding: Encoding.UTF8)) {
                sw.Write(new CodeRendering.EntityClassTemplate(config, allAggregates).TransformText());
            }
            log?.WriteLine("コード自動生成: DbSet");
            using (var sw = new StreamWriter(Path.Combine(efSourceDir, "DbSet.cs"), append: false, encoding: Encoding.UTF8)) {
                sw.Write(new CodeRendering.DbSetTemplate(config, allAggregates).TransformText());
            }
            log?.WriteLine("コード自動生成: OnModelCreating");
            using (var sw = new StreamWriter(Path.Combine(efSourceDir, "OnModelCreating.cs"), append: false, encoding: Encoding.UTF8)) {
                sw.Write(new CodeRendering.OnModelCreatingTemplate(config, allAggregates).TransformText());
            }
            log?.WriteLine("コード自動生成: Search");
            using (var sw = new StreamWriter(Path.Combine(efSourceDir, "Search.cs"), append: false, encoding: Encoding.UTF8)) {
                sw.Write(new CodeRendering.SearchMethodTemplate(config, _rootAggregates).TransformText());
            }
            log?.WriteLine("コード自動生成: AutoCompleteSource");
            using (var sw = new StreamWriter(Path.Combine(efSourceDir, "AutoCompleteSource.cs"), append: false, encoding: Encoding.UTF8)) {
                sw.Write(new CodeRendering.AutoCompleteSourceTemplate(config, allAggregates).TransformText());
            }

            var modelDir = Path.Combine(rootDir, config.MvcModelDirectoryRelativePath);
            if (Directory.Exists(modelDir)) Directory.Delete(modelDir, recursive: true);
            Directory.CreateDirectory(modelDir);

            log?.WriteLine("コード自動生成: MVC Model");
            using (var sw = new StreamWriter(Path.Combine(modelDir, "Models.cs"), append: false, encoding: Encoding.UTF8)) {
                sw.Write(new CodeRendering.AspNetMvc.MvcModelsTemplate(config, allAggregates).TransformText());
            }

            var viewDir = Path.Combine(rootDir, config.MvcViewDirectoryRelativePath);
            if (Directory.Exists(viewDir)) Directory.Delete(viewDir, recursive: true);
            Directory.CreateDirectory(viewDir);

            log?.WriteLine("コード自動生成: MVC View - MultiView");
            foreach (var rootAggregate in _rootAggregates) {
                var view = new CodeRendering.AspNetMvc.MvcMultiViewTemplate(config, rootAggregate);
                var filename = Path.Combine(viewDir, view.FileName);
                using var sw = new StreamWriter(filename, append: false, encoding: Encoding.UTF8);
                sw.Write(view.TransformText());
            }

            log?.WriteLine("コード自動生成: MVC View - SingleView");
            foreach (var rootAggregate in _rootAggregates) {
                var view = new CodeRendering.AspNetMvc.MvcSingleViewTemplate(config, rootAggregate);
                var filename = Path.Combine(viewDir, view.FileName);
                using var sw = new StreamWriter(filename, append: false, encoding: Encoding.UTF8);
                sw.Write(view.TransformText());
            }

            log?.WriteLine("コード自動生成: MVC View - CreateView");
            foreach (var rootAggregate in _rootAggregates) {
                var view = new CodeRendering.AspNetMvc.MvcCreateViewTemplate(config, rootAggregate);
                var filename = Path.Combine(viewDir, view.FileName);
                using var sw = new StreamWriter(filename, append: false, encoding: Encoding.UTF8);
                sw.Write(view.TransformText());
            }

            log?.WriteLine("コード自動生成: MVC View - 集約部分ビュー");
            foreach (var aggregate in allAggregates) {
                var view = new CodeRendering.AspNetMvc.InstancePartialViewTemplate(config, aggregate);
                var filename = Path.Combine(viewDir, view.FileName);
                using var sw = new StreamWriter(filename, append: false, encoding: Encoding.UTF8);
                sw.Write(view.TransformText());
            }

            log?.WriteLine("コード自動生成: MVC Controller");
            var controllerDir = Path.Combine(rootDir, config.MvcControllerDirectoryRelativePath);
            if (Directory.Exists(controllerDir)) Directory.Delete(controllerDir, recursive: true);
            Directory.CreateDirectory(controllerDir);
            using (var sw = new StreamWriter(Path.Combine(controllerDir, "Controllers.cs"), append: false, encoding: Encoding.UTF8)) {
                sw.Write(new CodeRendering.AspNetMvc.MvcControllerTemplate(config, _rootAggregates).TransformText());
            }

            log?.WriteLine("コード自動生成: JS");
            {
                var view = new CodeRendering.AspNetMvc.JsTemplate();
                var filename = Path.Combine(viewDir, CodeRendering.AspNetMvc.JsTemplate.FILE_NAME);
                using var sw = new StreamWriter(filename, append: false, encoding: Encoding.UTF8);
                sw.Write(view.TransformText());
            }

            log?.WriteLine("コード自動生成終了");
        }
    }
}
