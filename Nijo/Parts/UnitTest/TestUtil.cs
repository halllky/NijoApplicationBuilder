using Nijo.CodeGenerating;
using Nijo.Parts.Common;
using Nijo.Parts.CSharp;
using Nijo.Models.DataModelModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts.UnitTest;

/// <summary>
/// ユニットテスト用のユーティリティクラス。
/// 実装はアプリケーションテンプレート側で行なう。
/// </summary>
internal class TestUtil {

    internal const string INTERFACE_UTIL = "ITestUtil";
    internal const string INTERFACE_SCOPE = "ITestScope";

    internal static SourceFile Render(CodeRenderingContext ctx) {
        return new SourceFile {
            FileName = "TestUtil.cs",
            Contents = $$"""
                namespace {{ctx.Config.RootNamespace}};

                /// <summary>
                /// ユニットテスト用のユーティリティクラス。
                /// このインスタンスの生存期間はユニットテストのテストケース1個分と対応する。
                /// </summary>
                public interface {{INTERFACE_UTIL}} : IDisposable {
                    /// <summary>
                    /// <see cref="{{INTERFACE_SCOPE}}"/> のインスタンスを作成する。
                    /// </summary>
                    {{INTERFACE_SCOPE}}<TMessageRoot> CreateScope<TMessageRoot>({{PresentationContext.OPTIONS}}? options = null) where TMessageRoot : {{MessageContainer.INTERFACE}};
                }

                /// <summary>
                /// ユニットテスト用のユーティリティクラス。
                /// このインスタンスの生存期間は <see cref="{{ApplicationService.ABSTRACT_CLASS}}"/> のライフサイクル（webapiの場合はHTTPリクエスト1回分）に相当する。
                /// </summary>
                public interface {{INTERFACE_SCOPE}}<TMessageRoot> : IDisposable where TMessageRoot : {{MessageContainer.INTERFACE}} {
                    /// <summary>
                    /// アプリケーションサービスのインスタンス
                    /// </summary>
                    {{ApplicationService.ABSTRACT_CLASS}} App { get; }
                    /// <summary>
                    /// <see cref="{{PresentationContext.INTERFACE}}"/> のインスタンス
                    /// </summary>
                    {{PresentationContext.INTERFACE}}<TMessageRoot> PresentationContext { get; }
                }
                """,
        };
    }
}
