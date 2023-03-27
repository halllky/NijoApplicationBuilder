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

            var xmlFilename = new Argument<string?>();

            var gen = new Command(name: "gen", description: "ソースコードの自動生成を実行します。") {
                xmlFilename,
            };
            gen.SetHandler(xmlFilename => {
                if (xmlFilename == null) throw new InvalidOperationException($"対象XMLを指定してください。");
                var xmlFullpath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), xmlFilename));
                using var sr = new StreamReader(xmlFullpath);
                var xmlContent = sr.ReadToEnd();
                var config = Core.Config.FromXml(xmlContent);
                CodeGenerator
                    .FromXml(xmlContent)
                    .GenerateCode(config);
            }, xmlFilename);

            var rootCommand = new RootCommand("HalApplicationBuilder");
            rootCommand.AddCommand(gen);
            return await rootCommand.InvokeAsync(args);
        }
    }
}
