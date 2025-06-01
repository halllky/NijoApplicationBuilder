using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Core;
using MyApp.Core.Util;
using MyApp.Test;

namespace MyApp;

/// <summary>
/// <see cref="ITestUtil"/> の実装
/// </summary>
[SetUpFixture]
public class TestUtilImpl : ITestUtil {

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public static TestUtilImpl Instance { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    [OneTimeSetUp]
    public void Setup() {
        Instance = new();
    }

    [OneTimeTearDown]
    public void Dispose() {
        // ログの場所を標準出力に表示する
        TestContext.Out.WriteLine($$"""
            テストが実行されました。ログは以下の場所に出力されました。
            {{BaseWorkDirectory}}
            """);
    }

    /// <summary>
    /// このコンストラクタはNUnitのランナーまたはこのクラス内部でのみ呼ばれる想定です。
    /// 各テストケースからは <see cref="Instance"/> を参照してください。
    /// </summary>
    public TestUtilImpl() {
        // "TestContext.CurrentContext.WorkDirectory" は bin/Debug/net9.0
        var baseWorkDirectory = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.WorkDirectory,
            "..", // net9.0
            "..", // Debug
            "..", // bin
            "..", // Test
            $"Test.Log",
            $"テスト結果_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString().Substring(0, 8)}"));

        BaseWorkDirectory = baseWorkDirectory;
    }

    /// <summary>
    /// テスト実行全体で共有されるログのベースディレクトリ。
    /// テスト実行1回ごとに異なるフォルダが出力される。
    /// </summary>
    private string BaseWorkDirectory { get; }

    public TestScopeImpl<MessageContainer> CreateScope(string testCaseName, Action<IServiceCollection>? configureServices = null, IPresentationContextOptions? options = null) {
        return CreateScope<MessageContainer>(testCaseName, configureServices, options);
    }

    ITestScope<TMessageRoot> ITestUtil.CreateScope<TMessageRoot>(string testCaseName, Action<IServiceCollection>? configureServices, IPresentationContextOptions? options) {
        return CreateScope<TMessageRoot>(testCaseName, configureServices, options);
    }
    public TestScopeImpl<TMessageRoot> CreateScope<TMessageRoot>(string testCaseName, Action<IServiceCollection>? configureServices = null, IPresentationContextOptions? options = null) where TMessageRoot : IMessageContainer {

        var currentTestWorkDirectory = Path.Combine(BaseWorkDirectory, testCaseName);
        Directory.CreateDirectory(currentTestWorkDirectory);

        // DI機構
        var configure = new OverridedApplicationConfigureForTest();
        var services = new ServiceCollection();
        configure.ConfigureServices(services);

        // DI機構: テストケースごとのカスタマイズ
        configureServices?.Invoke(services);

        // DI機構: 実行時設定
        services.AddScoped(provider => {
            var settings = new RuntimeSetting();
            settings.LogDirectory = currentTestWorkDirectory; // テストケースごとのディレクトリ
            settings.CurrentDbProfileName = "SQLITE001";
            settings.MigrationsScriptFolder = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                "..", // net9.0
                "..", // Debug
                "..", // bin
                "..", // Test
                "..", // プロジェクトルート
                "MigrationsScript");

            var dbFileName = $"./{Path.GetRelativePath(TestContext.CurrentContext.WorkDirectory, currentTestWorkDirectory).Replace("\\", "/")}/UNITTEST.sqlite3";
            settings.DbProfiles.Add(new() {
                Name = "SQLITE001",
                ConnStr = $"Data Source={dbFileName};Pooling=False",
            });
            return settings;
        });

        // PresentationContext
        var messageRoot = MessageContainer.GetDefaultClass<TMessageRoot>([]);
        var contextOptions = options ?? new PresentationContextOptionsImpl();
        var presentationContext = new PresentationContextInUnitTest<TMessageRoot>(messageRoot, contextOptions);

        // DB作成
        var provider = services.BuildServiceProvider();
        var dbContext = provider.GetRequiredService<MyDbContext>();
        dbContext.EnsureCreatedAsyncEx(provider.GetRequiredService<RuntimeSetting>()).GetAwaiter().GetResult();

        return new TestScopeImpl<TMessageRoot>(provider, presentationContext, currentTestWorkDirectory);
    }

    #region
    /// <summary>
    /// <see cref="OverridedApplicationConfigure"/> のうちユニットテストの時だけ変更したい初期設定処理を変更したもの
    /// </summary>
    public class OverridedApplicationConfigureForTest : OverridedApplicationConfigure {
        // ログファイル名、Webアプリケーションの方では日付毎などだが、テストの場合は毎回別のフォルダに出力されるので、決め打ち
        protected override string LogFileNameRule => "テスト中に出力されたログ.log";
    }
    /// <summary>
    /// <see cref="IPresentationContextOptions"/> のユニットテスト用の実装。
    /// </summary>
    public class PresentationContextOptionsImpl : IPresentationContextOptions {
        public bool IgnoreConfirm { get; init; }
    }
    #endregion
}

/// <summary>
/// <see cref="ITestScope{TMessageRoot}"/> の実装
/// </summary>
public class TestScopeImpl<TMessage> : ITestScope<TMessage> where TMessage : IMessageContainer {
    internal TestScopeImpl(IServiceProvider serviceProvider, IPresentationContext<TMessage> presentationContext, string workDirectory) {
        ServiceProvider = serviceProvider;
        App = serviceProvider.GetRequiredService<OverridedApplicationService>();
        PresentationContext = presentationContext;
        WorkDirectory = workDirectory;
    }
    public IServiceProvider ServiceProvider { get; }
    public OverridedApplicationService App { get; }
    public IPresentationContext<TMessage> PresentationContext { get; }
    public string WorkDirectory { get; }

    AutoGeneratedApplicationService ITestScope<TMessage>.App => App;

    void IDisposable.Dispose() {
        // 特に後処理なし
    }
}
