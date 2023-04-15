using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace HalApplicationBuilder {
    public class Program {

        static async Task<int> Main(string[] args) {

            Console.CancelKeyPress += (sender, e) => {
                while (OnCancelKeyPress.TryPop(out var action)) {
                    try {
                        action();
                    } catch {
                        // 全てのリソースの破棄を優先するため例外を握りつぶす
                    }
                }
                e.Cancel = true;
            };

            var xmlFilename = new Argument<string?>();

            var gen = new Command(name: "gen", description: "ソースコードの自動生成を実行します。") {
                xmlFilename,
            };
            gen.SetHandler(Gen, xmlFilename);

            var debug = new Command(name: "debug", description: "プロジェクトのデバッグを開始します。") {
                xmlFilename,
            };
            debug.SetHandler(Debug, xmlFilename);

            var template = new Command(name: "template", description: "アプリケーション定義ファイルのテンプレートを表示します。");
            template.SetHandler(Template);

            // -------------------
            var rootCommand = new RootCommand("HalApplicationBuilder");
            rootCommand.AddCommand(gen);
            rootCommand.AddCommand(debug);
            rootCommand.AddCommand(template);
            return await rootCommand.InvokeAsync(args);
        }


        private static void Gen(string? xmlFilename) {
            if (xmlFilename == null) throw new InvalidOperationException($"対象XMLを指定してください。");
            var xmlFullpath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), xmlFilename));
            using var sr = new StreamReader(xmlFullpath);
            var xmlContent = sr.ReadToEnd();
            var config = Core.Config.FromXml(xmlContent);
            CodeGenerator
                .FromXml(xmlContent)
                .GenerateCode(config, Console.Out);
        }

        private static void Debug(string? xmlFilename) {

        }

        private static void Template() {
            var thisAssembly = Assembly.GetExecutingAssembly();
            var source = thisAssembly.GetManifestResourceStream("HalApplicationBuilder.Template.xml")!;
            using var sourceReader = new StreamReader(source);

            while (!sourceReader.EndOfStream) {
                Console.WriteLine(sourceReader.ReadLine());
            }
        }

        #region HELPER METHOD
        internal static Stack<Action> OnCancelKeyPress { get; } = new();
        internal static void Cmd(string workingDirectory, string filename, params string[] args) {
            using var cmd = new System.Diagnostics.Process();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                // windowsでnvmを使うとき直にnpmを実行できないためcmd経由で実行する
                cmd.StartInfo.FileName = "cmd";
                cmd.StartInfo.ArgumentList.Add("/c");
                cmd.StartInfo.ArgumentList.Add($"{filename} {string.Join(" ", args)}");
            } else {
                cmd.StartInfo.FileName = filename;
                foreach (var arg in args) cmd.StartInfo.ArgumentList.Add(arg);
            }
            cmd.StartInfo.WorkingDirectory = workingDirectory;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.RedirectStandardError = true;
            cmd.StartInfo.StandardOutputEncoding = Console.OutputEncoding;
            cmd.StartInfo.StandardErrorEncoding = Console.OutputEncoding;
            cmd.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
            cmd.ErrorDataReceived += (sender, e) => Console.WriteLine(e.Data);

            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            OnCancelKeyPress.Push(cancellationTokenSource.Cancel);

            try {
                cmd.Start();
                cmd.BeginOutputReadLine();
                cmd.BeginErrorReadLine();
                while (!cmd.HasExited) {
                    cancellationToken.ThrowIfCancellationRequested();
                    Thread.Sleep(100);
                }
                if (cmd.ExitCode != 0) {
                    throw new InvalidOperationException($"外部コマンド( {filename} {string.Join(" ", args)})実行時にエラーが発生しました。");
                }
            } catch (OperationCanceledException) {
                cmd.Kill(entireProcessTree: true);
            } catch (Exception ex) {
                throw new Exception($"EXCEPTION: '{filename} {string.Join(" ", args)}'", ex);
            }
        }
        #endregion HELPER METHOD
    }
}
