using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.CodeRendering.ReactAndWebApi;

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
        internal static CodeGenerator FromAppSchema(AppSchema appSchema) {
            return new CodeGenerator(appSchema.GetRootAggregates);
        }

        internal CodeGenerator(Func<Config, IEnumerable<RootAggregate>> func) {
            _rootAggregateBuilder = func;
        }
        private readonly Func<Config, IEnumerable<RootAggregate>> _rootAggregateBuilder;

        public void GenerateAspNetCoreMvc(string rootDir, Config config, bool verbose, TextWriter? log, CancellationToken? cancellationToken) {
            var generator = new AspNetCoreMvcGenerator(config, log, cancellationToken, _rootAggregateBuilder, verbose);
            generator.GenerateCode(rootDir);
        }
        public void GenerateReactAndWebApi(string rootDir, Config config, bool verbose, TextWriter? log, CancellationToken? cancellationToken) {
            var generator = new ReactAndWebApiGenerator(config, log, cancellationToken, _rootAggregateBuilder, verbose);
            generator.GenerateCode(rootDir);
        }

        internal abstract class GeneratorBase {
            internal GeneratorBase(
                Config config,
                TextWriter? log,
                CancellationToken? cancellationToken,
                Func<Config, IEnumerable<RootAggregate>> rootAggregateBuilder,
                bool verbose) {
                _config = config;
                _log = log;
                _cancellationToken = cancellationToken;
                _rootAggregateBuilder = rootAggregateBuilder;
                _verbose = verbose;
            }

            protected readonly Config _config;
            protected readonly TextWriter? _log;
            protected readonly bool _verbose;
            private readonly CancellationToken? _cancellationToken;
            private readonly Func<Config, IEnumerable<RootAggregate>> _rootAggregateBuilder;

            protected abstract string DotNetNew { get; }
            protected abstract void ModifyCsproj(Microsoft.Build.Evaluation.Project csproj);
            protected abstract void GenerateInitCode(DotnetEx.Cmd cmd);
            protected abstract void GenerateNonInitCode(string rootDir, IEnumerable<Aggregate> allAggregates, IEnumerable<RootAggregate> rootAggregates);

            /// <summary>
            /// コードの自動生成を実行します。
            /// </summary>
            /// <param name="config">コード生成に関する設定</param>
            /// <param name="log">このオブジェクトを指定した場合、コード生成の詳細を記録します。</param>
            /// <param name="cancellationToken">このオブジェクトを指定した場合、処理を途中でキャンセルすることが可能になります。</param>
            public void GenerateCode(string rootDir) {

                _log?.WriteLine($"コード自動生成開始");

                string? tempDir = null;
                try {
                    // プロジェクト初回作成時
                    if (!Directory.Exists(rootDir)) {
                        var project = $"halapp.temp.{Path.GetRandomFileName()}";
                        tempDir = Path.Combine(Directory.GetCurrentDirectory(), project);

                        Directory.CreateDirectory(tempDir);
                        var cmd = new DotnetEx.Cmd {
                            WorkingDirectory = tempDir,
                            CancellationToken = _cancellationToken,
                            Verbose = _verbose,
                        };

                        // dotnet CLI でプロジェクトを新規作成
                        _log?.WriteLine($"プロジェクトを作成します。");
                        cmd.Exec("dotnet", "new", DotNetNew, "--output", ".", "--name", _config.ApplicationName);

                        _log?.WriteLine($"Microsoft.EntityFrameworkCore パッケージへの参照を追加します。");
                        cmd.Exec("dotnet", "add", "package", "Microsoft.EntityFrameworkCore");

                        _log?.WriteLine($"Microsoft.EntityFrameworkCore.Proxies パッケージへの参照を追加します。");
                        cmd.Exec("dotnet", "add", "package", "Microsoft.EntityFrameworkCore.Proxies");

                        _log?.WriteLine($"Microsoft.EntityFrameworkCore.Design パッケージへの参照を追加します。"); // migration add に必要
                        cmd.Exec("dotnet", "add", "package", "Microsoft.EntityFrameworkCore.Design");

                        _log?.WriteLine($"Microsoft.EntityFrameworkCore.Sqlite パッケージへの参照を追加します。");
                        cmd.Exec("dotnet", "add", "package", "Microsoft.EntityFrameworkCore.Sqlite");

                        // halapp.dll への参照を加える。実行時にRuntimeContextを参照しているため
                        _log?.WriteLine($"halapp.dll を参照に追加します。");

                        // dllをプロジェクトディレクトリにコピー
                        const string HALAPP_DLL_DIR = "halapp-resource";
                        var halappDirCopySource = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
                        var halappDirCopyDist = Path.Combine(tempDir, HALAPP_DLL_DIR);
                        DotnetEx.IO.CopyDirectory(halappDirCopySource, halappDirCopyDist);

                        // csprojファイルを編集: csprojファイルを開く
                        var csprojPath = Path.Combine(tempDir, $"{_config.ApplicationName}.csproj");
                        var projectOption = new Microsoft.Build.Definition.ProjectOptions {
                            // Referenceを追加するだけなので Microsoft.NET.Sdk.Web が無くてもエラーにならないようにしたい
                            LoadSettings = Microsoft.Build.Evaluation.ProjectLoadSettings.IgnoreMissingImports,
                        };
                        var csproj = Microsoft.Build.Evaluation.Project.FromFile(csprojPath, projectOption);
                        var itemGroup = csproj.Xml.AddItemGroup();

                        // csprojファイルを編集: halapp.dll への参照を追加する（dll参照は dotnet add でサポートされていないため）
                        var reference = itemGroup.AddItem("Reference", include: "halapp");
                        reference.AddMetadata("HintPath", Path.Combine(HALAPP_DLL_DIR, "halapp.dll"));

                        // csprojファイルを編集: ビルド時に halapp.dll が含まれるディレクトリがコピーされるようにする
                        var none = itemGroup.AddItem("None", Path.Combine(HALAPP_DLL_DIR, "**", "*.*"));
                        none.AddMetadata("CopyToOutputDirectory", "Always");

                        // その他csproj編集
                        ModifyCsproj(csproj);
                        csproj.Save();

                        // Program.cs書き換え
                        _log?.WriteLine($"HalappDefaultConfigure.cs ファイルを作成します。");
                        using (var sw = new StreamWriter(Path.Combine(tempDir, "HalappDefaultConfigure.cs"), append: false, encoding: Encoding.UTF8)) {
                            sw.Write(new CodeRendering.DefaultRuntimeConfigTemplate(_config).TransformText());
                        }
                        _log?.WriteLine($"Program.cs ファイルを書き換えます。");
                        var programCsPath = Path.Combine(tempDir, "Program.cs");
                        var lines = File.ReadAllLines(programCsPath).ToList();
                        var regex1 = new Regex(@"^.*[a-zA-Z]+ builder = .+;$");
                        var position1 = lines.FindIndex(regex1.IsMatch);
                        if (position1 == -1) throw new InvalidOperationException("Program.cs の中にIServiceCollectionを持つオブジェクトを初期化する行が見つかりません。");
                        lines.InsertRange(position1 + 1, new[] {
                            $"",
                            $"/* HalApplicationBuilder によって自動生成されたコード ここから */",
                            $"var runtimeRootDir = System.IO.Directory.GetCurrentDirectory();",
                            $"HalApplicationBuilder.Runtime.HalAppDefaultConfigurer.Configure(builder.Services, runtimeRootDir);",
                            $"// HTMLのエンコーディングをUTF-8にする(日本語のHTMLエンコード防止)",
                            $"builder.Services.Configure<Microsoft.Extensions.WebEncoders.WebEncoderOptions>(options => {{",
                            $"    options.TextEncoderSettings = new System.Text.Encodings.Web.TextEncoderSettings(System.Text.Unicode.UnicodeRanges.All);",
                            $"}});",
                            $"// npm start で実行されるポートがASP.NETのそれと別なので",
                            $"builder.Services.AddCors(options => {{",
                            $"    options.AddDefaultPolicy(builder => {{",
                            $"        builder.AllowAnyOrigin()",
                            $"            .AllowAnyMethod()",
                            $"            .AllowAnyHeader();",
                            $"    }});",
                            $"}});",
                            $"/* HalApplicationBuilder によって自動生成されたコード ここまで */",
                            $"",
                        });
                        var regex2 = new Regex(@"^.*[a-zA-Z]+ app = .+;$");
                        var position2 = lines.FindIndex(regex2.IsMatch);
                        if (position2 == -1) throw new InvalidOperationException("Program.cs の中にappオブジェクトを初期化する行が見つかりません。");
                        lines.InsertRange(position2 + 1, new[] {
                            $"",
                            $"/* HalApplicationBuilder によって自動生成されたコード ここから */",
                            $"// 前述AddCorsの設定をするならこちらも必要",
                            $"app.UseCors();",
                            $"/* HalApplicationBuilder によって自動生成されたコード ここまで */",
                            $"",
                        });
                        File.WriteAllLines(programCsPath, lines);

                        // DbContext生成
                        var dbContextFileName = $"{_config.DbContextName}.cs";
                        _log?.WriteLine($"{dbContextFileName} ファイルを作成します。");
                        var dbContextDir = Path.Combine(tempDir, "EntityFramework");
                        Directory.CreateDirectory(dbContextDir);
                        using (var sw = new StreamWriter(Path.Combine(dbContextDir, dbContextFileName), append: false, encoding: Encoding.UTF8)) {
                            sw.Write(new CodeRendering.DbContextTemplate(_config).TransformText());
                        }

                        // その他
                        GenerateInitCode(cmd);

                        // ここまでの処理がすべて成功したら一時ディレクトリを本来のディレクトリ名に変更
                        if (Directory.Exists(rootDir)) throw new InvalidOperationException($"プロジェクトディレクトリを {rootDir} に移動できません。");
                        Directory.Move(tempDir, rootDir);
                    }

                    var _rootAggregates = _rootAggregateBuilder.Invoke(_config);
                    var allAggregates = _rootAggregates
                        .SelectMany(a => a.GetDescendantsAndSelf())
                        .ToArray();

                    _log?.WriteLine("コード自動生成: スキーマ定義");
                    using (var sw = new StreamWriter(Path.Combine(rootDir, "halapp.json"), append: false, encoding: Encoding.UTF8)) {
                        var schema = new Serialized.AppSchemaJson {
                            Config = _config.ToJson(onlyRuntimeConfig: true),
                            Aggregates = _rootAggregates.Select(a => a.ToJson()).ToArray(),
                        };
                        sw.Write(System.Text.Json.JsonSerializer.Serialize(schema, new System.Text.Json.JsonSerializerOptions {
                            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All), // 日本語用
                            WriteIndented = true,
                            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull, // nullのフィールドをシリアライズしない
                        }));
                    }

                    var modelDir = Path.Combine(rootDir, _config.MvcModelDirectoryRelativePath);
                    if (Directory.Exists(modelDir)) Directory.Delete(modelDir, recursive: true);
                    Directory.CreateDirectory(modelDir);

                    _log?.WriteLine("コード自動生成: UI Model");
                    using (var sw = new StreamWriter(Path.Combine(modelDir, "Models.cs"), append: false, encoding: Encoding.UTF8)) {
                        sw.Write(new CodeRendering.UIModelsTemplate(_config, allAggregates).TransformText());
                    }

                    var efSourceDir = Path.Combine(rootDir, _config.EntityFrameworkDirectoryRelativePath);
                    if (Directory.Exists(efSourceDir)) Directory.Delete(efSourceDir, recursive: true);
                    Directory.CreateDirectory(efSourceDir);

                    _log?.WriteLine("コード自動生成: Entity定義");
                    using (var sw = new StreamWriter(Path.Combine(efSourceDir, "Entities.cs"), append: false, encoding: Encoding.UTF8)) {
                        sw.Write(new CodeRendering.EntityClassTemplate(_config, allAggregates).TransformText());
                    }
                    _log?.WriteLine("コード自動生成: DbSet");
                    using (var sw = new StreamWriter(Path.Combine(efSourceDir, "DbSet.cs"), append: false, encoding: Encoding.UTF8)) {
                        sw.Write(new CodeRendering.DbSetTemplate(_config, allAggregates).TransformText());
                    }
                    _log?.WriteLine("コード自動生成: OnModelCreating");
                    using (var sw = new StreamWriter(Path.Combine(efSourceDir, "OnModelCreating.cs"), append: false, encoding: Encoding.UTF8)) {
                        sw.Write(new CodeRendering.OnModelCreatingTemplate(_config, allAggregates).TransformText());
                    }
                    _log?.WriteLine("コード自動生成: Search");
                    using (var sw = new StreamWriter(Path.Combine(efSourceDir, "Search.cs"), append: false, encoding: Encoding.UTF8)) {
                        sw.Write(new CodeRendering.SearchMethodTemplate(_config, _rootAggregates).TransformText());
                    }
                    _log?.WriteLine("コード自動生成: AutoCompleteSource");
                    using (var sw = new StreamWriter(Path.Combine(efSourceDir, "AutoCompleteSource.cs"), append: false, encoding: Encoding.UTF8)) {
                        sw.Write(new CodeRendering.AutoCompleteSourceTemplate(_config, allAggregates).TransformText());
                    }

                    GenerateNonInitCode(rootDir, allAggregates, _rootAggregates);

                    _log?.WriteLine("コード自動生成終了");

                } catch (OperationCanceledException) {
                    Console.WriteLine("キャンセルされました。");

                } finally {
                    if (tempDir != null && Directory.Exists(tempDir)) {
                        Directory.Delete(tempDir, recursive: true);
                    }
                }
            }
        }

        private class AspNetCoreMvcGenerator : GeneratorBase {
            private const string PACKAGE_JSON_CSS_BUILD_SCRIPT_NAME = "buildcss";

            internal AspNetCoreMvcGenerator(
                Config config,
                TextWriter? log,
                CancellationToken? cancellationToken,
                Func<Config, IEnumerable<RootAggregate>> rootAggregateBuilder,
                bool verbose)
                : base(config, log, cancellationToken, rootAggregateBuilder, verbose) {
            }

            protected override string DotNetNew => "mvc";

            protected override void ModifyCsproj(Microsoft.Build.Evaluation.Project csproj) {
                // ビルド後にcssをビルドするコマンドが走るようにする
                var target = csproj.Xml.AddTarget("PostBuild");
                target.AfterTargets = "PostBuildEvent";
                var exec = target.AddTask("Exec");
                exec.SetParameter("Command", $"npm run {PACKAGE_JSON_CSS_BUILD_SCRIPT_NAME}");
            }

            protected override void GenerateInitCode(DotnetEx.Cmd cmd) {

                const string LAYOUT_CSHTML = "_Layout.cshtml";
                _log?.WriteLine($"{LAYOUT_CSHTML} ファイルを書き換えます。");
                var layoutCshtmlDir = Path.Combine(cmd.WorkingDirectory, "Views", "Shared");
                using (var sw = new StreamWriter(Path.Combine(layoutCshtmlDir, LAYOUT_CSHTML), append: false, encoding: Encoding.UTF8)) {
                    sw.Write(new CodeRendering.AspNetMvc.LayoutCshtmlTemplate().TransformText());
                }

                // tailwindcssを有効にする
                _log?.WriteLine($"package.jsonを作成します。");
                cmd.Exec("npm", "init", "-y");
                var packageJsonPath = Path.Combine(cmd.WorkingDirectory, "package.json");
                var packageJson = JObject.Parse(File.ReadAllText(packageJsonPath));
                var scripts = new JObject();
                scripts[PACKAGE_JSON_CSS_BUILD_SCRIPT_NAME] = "postcss wwwroot/css/app.css -o wwwroot/css/app.min.css";
                packageJson.SelectToken("name").Replace($"halapp-{Guid.NewGuid()}");
                packageJson.SelectToken("scripts").Replace(scripts);
                File.WriteAllText(packageJsonPath, packageJson.ToString());

                _log?.WriteLine($"必要な Node.js パッケージをインストールします。");
                cmd.Exec("npm", "install", "tailwindcss", "postcss", "postcss-cli", "autoprefixer");

                _log?.WriteLine($"app.cssファイルを生成します。");
                using (var sw = new StreamWriter(Path.Combine(cmd.WorkingDirectory, "wwwroot", "css", "app.css"), append: false, encoding: Encoding.UTF8)) {
                    sw.Write(new CodeRendering.AspNetMvc.app_css().TransformText());
                }
                _log?.WriteLine($"postcss.config.jsファイルを生成します。");
                using (var sw = new StreamWriter(Path.Combine(cmd.WorkingDirectory, "postcss.config.js"), append: false, encoding: Encoding.UTF8)) {
                    sw.Write(new CodeRendering.AspNetMvc.postcss_config_js().TransformText());
                }
                _log?.WriteLine($"tailwind.config.jsファイルを生成します。");
                using (var sw = new StreamWriter(Path.Combine(cmd.WorkingDirectory, "tailwind.config.js"), append: false, encoding: Encoding.UTF8)) {
                    sw.Write(new CodeRendering.AspNetMvc.tailwind_config_js().TransformText());
                }
            }

            protected override void GenerateNonInitCode(string rootDir, IEnumerable<Aggregate> allAggregates, IEnumerable<RootAggregate> rootAggregates) {

                var viewDir = Path.Combine(rootDir, _config.MvcViewDirectoryRelativePath);
                if (Directory.Exists(viewDir)) Directory.Delete(viewDir, recursive: true);
                Directory.CreateDirectory(viewDir);

                _log?.WriteLine("コード自動生成: MVC View - MultiView");
                foreach (var rootAggregate in rootAggregates) {
                    var view = new CodeRendering.AspNetMvc.MvcMultiViewTemplate(_config, rootAggregate);
                    var filename = Path.Combine(viewDir, view.FileName);
                    using var sw = new StreamWriter(filename, append: false, encoding: Encoding.UTF8);
                    sw.Write(view.TransformText());
                }

                _log?.WriteLine("コード自動生成: MVC View - SingleView");
                foreach (var rootAggregate in rootAggregates) {
                    var view = new CodeRendering.AspNetMvc.MvcSingleViewTemplate(_config, rootAggregate);
                    var filename = Path.Combine(viewDir, view.FileName);
                    using var sw = new StreamWriter(filename, append: false, encoding: Encoding.UTF8);
                    sw.Write(view.TransformText());
                }

                _log?.WriteLine("コード自動生成: MVC View - CreateView");
                foreach (var rootAggregate in rootAggregates) {
                    var view = new CodeRendering.AspNetMvc.MvcCreateViewTemplate(_config, rootAggregate);
                    var filename = Path.Combine(viewDir, view.FileName);
                    using var sw = new StreamWriter(filename, append: false, encoding: Encoding.UTF8);
                    sw.Write(view.TransformText());
                }

                _log?.WriteLine("コード自動生成: MVC View - 集約部分ビュー");
                foreach (var aggregate in allAggregates) {
                    var view = new CodeRendering.AspNetMvc.InstancePartialViewTemplate(_config, aggregate);
                    var filename = Path.Combine(viewDir, view.FileName);
                    using var sw = new StreamWriter(filename, append: false, encoding: Encoding.UTF8);
                    sw.Write(view.TransformText());
                }

                _log?.WriteLine("コード自動生成: MVC Controller");
                var controllerDir = Path.Combine(rootDir, _config.MvcControllerDirectoryRelativePath);
                if (Directory.Exists(controllerDir)) Directory.Delete(controllerDir, recursive: true);
                Directory.CreateDirectory(controllerDir);
                using (var sw = new StreamWriter(Path.Combine(controllerDir, "Controllers.cs"), append: false, encoding: Encoding.UTF8)) {
                    sw.Write(new CodeRendering.AspNetMvc.MvcControllerTemplate(_config, rootAggregates).TransformText());
                }

                _log?.WriteLine("コード自動生成: JS");
                {
                    var view = new CodeRendering.AspNetMvc.JsTemplate();
                    var filename = Path.Combine(viewDir, CodeRendering.AspNetMvc.JsTemplate.FILE_NAME);
                    using var sw = new StreamWriter(filename, append: false, encoding: Encoding.UTF8);
                    sw.Write(view.TransformText());
                }
            }
        }

        internal class ReactAndWebApiGenerator : GeneratorBase {

            internal ReactAndWebApiGenerator(
                Config config,
                TextWriter? log,
                CancellationToken? cancellationToken,
                Func<Config, IEnumerable<RootAggregate>> rootAggregateBuilder,
                bool verbose)
                : base(config, log, cancellationToken, rootAggregateBuilder, verbose) {
            }

            protected override string DotNetNew => "webapi";

            protected override void ModifyCsproj(Microsoft.Build.Evaluation.Project csproj) {
                // 特になにもしない
            }

            protected override void GenerateInitCode(DotnetEx.Cmd cmd) {
                _log?.WriteLine($"React.jsアプリケーションを作成します。");

                // プロジェクトテンプレートのコピー
                var halappDllDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
                var projectTemplateDir = Path.Combine(halappDllDir, "CodeRendering", "ReactAndWebApi", "project-template");
                var reactDir = Path.Combine(cmd.WorkingDirectory, REACT_DIR);
                DotnetEx.IO.CopyDirectory(projectTemplateDir, reactDir);

                var npmProcess = new DotnetEx.Cmd {
                    WorkingDirectory = reactDir,
                    CancellationToken = cmd.CancellationToken,
                    Verbose = _verbose,
                };
                npmProcess.Exec("npm", "ci");
            }

            protected override void GenerateNonInitCode(string rootDir, IEnumerable<Aggregate> allAggregates, IEnumerable<RootAggregate> rootAggregates) {

                // Web API
                using (var sw = new StreamWriter(Path.Combine(rootDir, "Controllers", "__AutoGenerated.cs"), append: false, encoding: Encoding.UTF8)) {
                    var template = new WebApiControllerTemplate(_config, rootAggregates);
                    sw.Write(template.TransformText());
                }

                // React.js
                var tsDir = Path.Combine(rootDir, REACT_DIR, "src", "__AutoGenerated");
                if (!Directory.Exists(tsDir)) Directory.CreateDirectory(tsDir);

                var generateStartTime = DateTime.Now;

                // 共通hooks等
                _log?.WriteLine("コード自動生成: 共通hooks等");
                var halappDllDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
                var sourceDir = Path.Combine(halappDllDir, "CodeRendering", "ReactAndWebApi", "project-template", "src", "__AutoGenerated");

                var componentsIn = Path.Combine(sourceDir, "components");
                var componentsOut = Path.Combine(tsDir, "components");
                foreach (var inFile in Directory.GetFiles(componentsIn)) {
                    var outFile = Path.Combine(componentsOut, Path.GetFileName(inFile));
                    File.Copy(inFile, outFile, overwrite: true);
                    File.SetLastWriteTime(outFile, DateTime.Now);
                }
                var hooksIn = Path.Combine(sourceDir, "hooks");
                var hooksOut = Path.Combine(tsDir, "hooks");
                foreach (var inFile in Directory.GetFiles(hooksIn)) {
                    var outFile = Path.Combine(hooksOut, Path.GetFileName(inFile));
                    File.Copy(inFile, outFile, overwrite: true);
                    File.SetLastWriteTime(outFile, DateTime.Now);
                }

                var utf8withoutBOM = new UTF8Encoding(false);

                // 集約定義
                _log?.WriteLine("コード自動生成: 集約のTypeScript型定義");
                using (var sw = new StreamWriter(Path.Combine(tsDir, ReactTypeDefTemplate.FILE_NAME), append: false, encoding: utf8withoutBOM)) {
                    var template = new ReactTypeDefTemplate(allAggregates);
                    sw.Write(template.TransformText());
                }
                // コンポーネント
                _log?.WriteLine("コード自動生成: 集約のReactコンポーネント");
                foreach (var rootAggregate in rootAggregates) {
                    var template = new ReactComponentTemplate(rootAggregate);
                    using var sw = new StreamWriter(Path.Combine(tsDir, template.FileName), append: false, encoding: utf8withoutBOM);
                    sw.Write(template.TransformText());
                }

                _log?.WriteLine("コード自動生成: index.ts等");
                // menu.tsx
                using (var sw = new StreamWriter(Path.Combine(tsDir, menuItems.FILE_NAME), append: false, encoding: utf8withoutBOM)) {
                    var template = new menuItems(rootAggregates);
                    sw.Write(template.TransformText());
                }
                // index.ts
                using (var sw = new StreamWriter(Path.Combine(tsDir, index.FILE_NAME), append: false, encoding: utf8withoutBOM)) {
                    var template = new index(rootAggregates);
                    sw.Write(template.TransformText());
                }

                // 元々自動生成先フォルダに存在したが今回の生成では無くなっているファイルを削除
                void DeleteNotGeneratedFiles(string dir) {
                    foreach (var file in Directory.GetFiles(dir)) {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.Exists && fileInfo.LastWriteTime < generateStartTime) {
                            fileInfo.Delete();
                        }
                    }
                    foreach (var subDirectory in Directory.GetDirectories(dir)) {
                        DeleteNotGeneratedFiles(subDirectory);
                    }
                }
                DeleteNotGeneratedFiles(tsDir);
            }

            internal const string REACT_DIR = "ClientApp";
        }
    }
}
