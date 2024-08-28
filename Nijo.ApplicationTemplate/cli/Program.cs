using Microsoft.Extensions.DependencyInjection;

// このファイルはコマンドラインアプリケーションのエントリーポイントです。

namespace NIJO_APPLICATION_TEMPLATE_Cli;

internal class Program {
    private static async Task Main(string[] args) {

        // DI機構（IServiceProvider）初期化
        var services = new ServiceCollection();
        DefaultConfigurationInCli.InitAsBatchProcess(services);
        var serviceProvider = services.BuildServiceProvider();

        // コマンドライン引数を解釈し処理実行
        await BatchBase.StartExecuting(args, System.Reflection.Assembly.GetExecutingAssembly(), serviceProvider);
    }
}
