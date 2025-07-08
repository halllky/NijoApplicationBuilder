using Nijo.CodeGenerating;
using NUnit.Framework;
using System.Xml.Linq;

namespace Nijo.IntegrationTest;

public abstract class ApplicationServiceImplementorBase {
    /// <summary>
    /// この実装が対応するXMLファイルの名前を返します。
    /// </summary>
    public abstract string TargetXmlFileName { get; }

    /// <summary>
    /// OverridedApplicationServiceの実装を返します。
    /// </summary>
    public abstract string GetImplementation(XDocument schemaXml);

    /// <summary>
    /// <see cref="Nijo.Models.QueryModelModules.SearchResult"/> のパスが正しく生成されているかどうかを確認します。
    /// </summary>
    /// <param name="properties">SearchResultのメンバー。配列はSelect, SelectManyで記述する必要あり。大元の変数名は集約ルートの物理名。</param>
    public virtual void AssertSearchResultMemberPath(IInstanceProperty[] properties) {
        // テストが必要な場合はこのメソッドをオーバーライドして実装すること。
    }

    #region ユーティリティ
    /// <summary>
    /// メンバーパスのアサーションを簡単に書けるようにするためのユーティリティ。
    /// 指定の条件に見合うプロパティが1個存在するかどうかを確認します。
    /// </summary>
    /// <param name="properties">全プロパティ</param>
    /// <param name="expectedPath">大元の変数からのパスの期待結果</param>
    protected static void AssertExists<TPropertyType>(IEnumerable<IInstanceProperty> properties, string expectedPath) where TPropertyType : IInstanceProperty {
        Assert.That(properties.Count(p => p is TPropertyType && p.GetJoinedPathFromInstance(E_CsTs.CSharp) == expectedPath), Is.EqualTo(1), $"プロパティが無い: {expectedPath}");
    }
    #endregion ユーティリティ
}
