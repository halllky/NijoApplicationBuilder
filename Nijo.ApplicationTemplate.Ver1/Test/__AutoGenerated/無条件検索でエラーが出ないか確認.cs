using NUnit;
using NUnit.Framework;

namespace MyApp;

partial class DB等入出力あり {

    [Test(Description = "無条件検索でエラーが出ないか確認(顧客)")]
    public void NonConditionLoadTest0000() {
        using var util = TestUtilBuilder.Build();
        using var scope = util.CreateScope<顧客Messages>();
        var searchCondition = new 顧客SearchCondition {
            Take = 10,
        };
    
        Assert.DoesNotThrowAsync(() => scope.App.LoadAsync(searchCondition, scope.PresentationContext));
    }
}
