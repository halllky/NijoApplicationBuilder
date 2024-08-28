// このファイルは、アプリケーションテンプレート内でコンパイルエラーが起きないようにするため、クラスやメソッドの枠のみを定義するためのファイルです。
// このファイルはソース自動生成処理によって上書きされます。

namespace NIJO_APPLICATION_TEMPLATE_Cli;

public class BatchExecutingContext {
    public BatchExecutingContext(string jobId, IServiceProvider serviceProvider) {
        JobId = jobId;
        _serviceProvider = serviceProvider;
    }
    /// <summary>
    /// 処理1回ごとに発行される一意の不規則な番号
    /// </summary>
    public string JobId { get; }

    private readonly IServiceProvider _serviceProvider;
}
