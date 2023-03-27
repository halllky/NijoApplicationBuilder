using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace HalApplicationBuilder {
    public class Program {

        static async Task<int> Main(string[] args) {

            var configOption = new Option<Serialized.ConfigJson?>(
                name: "--config",
                description: "出力設定のjsonファイルのパスを指定します。",
                parseArgument: result => {
                    if (result.Tokens.Count != 1) {
                        Console.WriteLine("--configが指定されていないか複数の値が指定されています。");
                        return null;
                    }
                    var filename = result.Tokens.Single().Value;
                    try {
                        using var stream = new FileStream(filename, FileMode.Open, FileAccess.Read);
                        return System.Text.Json.JsonSerializer.Deserialize<Serialized.ConfigJson>(stream);
                    } catch (System.IO.FileNotFoundException) {
                        Console.WriteLine($"'{filename}' が見つかりません。");
                        return null;
                    } catch (System.Text.Json.JsonException) {
                        Console.WriteLine($"'{filename}' の内容が不正です。");
                        return null;
                    }
                });

            var dllOption = new Argument<string?>(
                name: "--dll",
                description: "集約定義dll");

            var namespaceOption = new Option<string?>(
                name: "--namespace",
                description: "dll中で特定の名前空間のみ対象にしたい場合はこのオプションを指定します。");

            var gen = new Command(name: "gen", description: "ソースコードの自動生成を実行します。") {
                dllOption,
                configOption,
                namespaceOption,
            };
            gen.SetHandler((dll, configJson, @namespace) => {
                if (dll == null) throw new InvalidOperationException($"対象dllを指定してください。");
                var dllFullpath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), dll));
                Assembly assembly;
                try {
                    assembly = Assembly.LoadFile(dllFullpath);
                } catch (System.IO.FileNotFoundException) {
                    Console.WriteLine($"'{dllFullpath}' が見つかりません。");
                    return;
                }
                var config = Core.Config.FromJson(configJson ?? Serialized.ConfigJson.GetDefault("MyRootNamespace"));
                CodeGenerator
                    .FromAssembly(assembly, @namespace)
                    .GenerateCode(config);
            }, dllOption, configOption, namespaceOption);


            var rootCommand = new RootCommand("HalApplicationBuilder");
            rootCommand.AddCommand(gen);
            return await rootCommand.InvokeAsync(args);
        }
    }
}
