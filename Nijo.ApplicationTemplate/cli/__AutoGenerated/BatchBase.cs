using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace NIJO_APPLICATION_TEMPLATE_Cli;

/// <summary>
/// バッチ処理基盤。
/// このクラスを継承したクラスは <see cref="CommandLine.VerbAttribute"/> を設定する必要がある。
/// またコマンドライン引数で受け取る値はプロパティとして宣言したうえで
/// <see cref="CommandLine.OptionAttribute"/> や <see cref="CommandLine.ValueAttribute"/> をつける。
/// いずれも詳細なルールはライブラリ "CommandLineParser" のドキュメントを参照のこと。
/// </summary>
public abstract partial class BatchBase {

    /// <summary>
    /// 指定のアセンブリの中から実行可能なバッチの一覧を検索し、その型情報を返します。
    /// </summary>
    /// <param name="assembly">検索対象のアセンブリ</param>
    protected static IEnumerable<Type> CollectExecutableBatchTypes(Assembly assembly) {
        return assembly
            .GetTypes()
            .Where(type => type.IsSubclassOf(typeof(BatchBase))
                        && !type.IsAbstract);
    }

    /// <summary>
    /// バッチ処理を開始します。
    /// </summary>
    /// <param name="commandLineArgs">コマンドライン引数</param>
    /// <param name="batchSearchAssembly">このアセンブリ内にあるバッチから実行対象を検索します。</param>
    /// <param name="serviceProvider">DI機構</param>
    internal static async Task StartExecuting(string[] commandLineArgs, Assembly batchSearchAssembly, IServiceProvider serviceProvider) {

        // このプロジェクトの中から、実行可能なバッチ処理（バッチ基盤クラスを継承している具象クラス）の一覧を列挙する
        var batchBaseTypes = CollectExecutableBatchTypes(batchSearchAssembly).ToArray();

        // コマンドライン引数を解釈してバッチ基盤クラス型に変換
        var parseResult = Parser.Default.ParseArguments(commandLineArgs, batchBaseTypes);

        // コマンドライン引数が正しく指定されていなかった場合、使えるコマンドの一覧をコンソール出力して処理終了
        parseResult.WithNotParsed(errors => {
            Console.WriteLine("コマンドライン引数が不正です。");
            foreach (var error in errors) {
                if (error is MissingRequiredOptionError missingRequiredOptionError) {
                    Console.WriteLine($"- 必須オプション '{missingRequiredOptionError.NameInfo.NameText}' が指定されていません。");
                } else if (error is UnknownOptionError unknownOptionError) {
                    Console.WriteLine($"- 不明なオプション '{unknownOptionError.Token}' が指定されました。");
                } else if (error is BadFormatConversionError badFormatConversionError) {
                    Console.WriteLine($"- オプション '{badFormatConversionError.NameInfo.NameText}' の値が無効です。");
                } else {
                    Console.WriteLine("- コマンドライン引数の解析中にエラーが発生しました。");
                }
            }
            if (batchBaseTypes.Length > 0) {
                Console.WriteLine();
                Console.WriteLine("使用できるコマンドは以下です。");
                foreach (var batchType in batchBaseTypes) {
                    var verb = batchType.GetCustomAttribute<VerbAttribute>();
                    Console.WriteLine(verb == null
                        ? $"- エラー！{batchType.Name}クラスに {nameof(VerbAttribute)} が指定されていません。"
                        : $"- {verb.Name} ({batchType.Name}クラス): {verb.HelpText}");
                }
            }

        });

        // コマンドライン引数が正しく指定されていた場合、バッチ処理開始
        await parseResult.WithParsedAsync<BatchBase>(async batchBase => {
            // ジョブIDはここで発行したUUIDの先頭8桁とする（8桁もあれば同時期に重複が発生することはまずない）
            var jobId = Guid.NewGuid().ToString().Substring(0, 8);

            // コマンドライン引数をログ出力
            var logger = serviceProvider.GetRequiredService<Logger>();
            using var logScope = logger.PushScopeNested($"ID::{jobId}");

            // 本処理
            logger.Info(message: $"処理開始 コマンドライン引数:{string.Join(" ", commandLineArgs)}");
            try {
                var ctx = new BatchExecutingContext(jobId, serviceProvider);
                await batchBase.Execute(ctx);

            } catch (Exception ex) {
                logger.Error(ex, "予期しないエラーが発生しました。");
            }
            logger.Info(message: "処理終了");
        });
    }

    /// <summary>
    /// バッチ処理本体を記述してください。
    /// ロギングはこのメソッドを呼ぶ側で実施しているので不要です。
    /// エラーハンドリングはこのメソッドを呼ぶ側で実施しているので不要です。
    /// </summary>
    /// <param name="ctx">バッチ実行時コンテキスト情報が入っています。各種コア機能、現在時刻、DB接続など</param>
    protected abstract Task Execute(BatchExecutingContext ctx);
}

