using MyApp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace MyApp.Test.Tests;


[Category("MessageContainerのテスト")]
internal class MessageContainerのテスト {

    [Test]
    public void ToJsonObjectのテスト() {
        // パスを意味のある値で初期化
        var container = new 診察記録SaveCommandMessages(new List<string> { "診察記録" });

        // ---------------------
        // Arrange
        container.処方[3].用量.AddError("用量が記載されていません。");
        container.AddError("ルートのエラー");

        // ---------------------
        // Act
        var json = container.ToJsonObject();

        // ---------------------
        // Assert
        Console.WriteLine($"JSON: {json}");

        // ルートレベルのエラーを確認
        Assert.That(json.ContainsKey("error"), Is.True, "ルートのエラーメッセージが存在しません");
        Assert.That((string)json["error"]![0]!, Is.EqualTo("ルートのエラー"));

        // 配列要素があることを確認
        Assert.That(json.ContainsKey("処方"), Is.True, "処方キーが存在しません");

        var 処方Json = json["処方"]!.AsObject();
        Assert.That(処方Json.ContainsKey("3"), Is.True, "処方[3]が存在しません");

        var 処方3 = 処方Json["3"]!.AsObject();
        Assert.That(処方3.ContainsKey("用量"), Is.True, "処方[3].用量が存在しません");

        var 用量 = 処方3["用量"]!.AsObject();
        Assert.That(用量.ContainsKey("error"), Is.True, "処方[3].用量.errorが存在しません");

        var エラー配列 = 用量["error"]!.AsArray();
        Assert.That(エラー配列.Count, Is.EqualTo(1), "エラーメッセージ数が一致しません");
        Assert.That((string)エラー配列[0]!, Is.EqualTo("用量が記載されていません。"));
    }

    [Test]
    public void メッセージがないときのJSONは空になること() {
        // ---------------------
        // Arrange
        var container = new 診察記録SaveCommandMessages(new List<string> { "診察記録" });
        // メッセージを追加しない

        // ---------------------
        // Act
        var json = container.ToJsonObject();

        // ---------------------
        // Assert
        Assert.That(json.Count, Is.EqualTo(0));
    }

    [Test]
    public void HasErrorで子孫のエラーが検出できること() {
        // ---------------------
        // Arrange
        var container = new 診察記録SaveCommandMessages(new List<string> { "診察記録" });

        // 直接コンテナにエラーを追加
        container.AddError("直接のエラー");

        // 深いネストの子要素にエラーを追加
        container.処方[3].用量.AddError("用量が記載されていません。");

        // ---------------------
        // Act & Assert
        Assert.That(container.HasError(), Is.True);
    }

    [Test]
    public void 異なる添字の配列要素にメッセージがある場合() {
        // ---------------------
        // Arrange
        var container = new 診察記録SaveCommandMessages(new List<string> { "診察記録" });

        // 不連続なインデックスにメッセージを追加
        container.処方[1].用量.AddError("用量1のエラー");
        container.処方[5].用量.AddError("用量5のエラー");

        // ---------------------
        // Act
        var json = container.ToJsonObject();
        Console.WriteLine($"JSON: {json}");

        // ---------------------
        // Assert
        // 処方が存在することを確認
        Assert.That(json.ContainsKey("処方"), Is.True, "処方キーが存在しません");
        var 処方Json = json["処方"]!.AsObject();

        // インデックス1が存在することを確認
        Assert.That(処方Json.ContainsKey("1"), Is.True, "処方[1]が存在しません");
        var 処方1 = 処方Json["1"]!.AsObject();
        Assert.That(処方1.ContainsKey("用量"), Is.True, "処方[1].用量が存在しません");
        var 用量1 = 処方1["用量"]!.AsObject();
        Assert.That(用量1.ContainsKey("error"), Is.True, "処方[1].用量.errorが存在しません");
        Assert.That((string)用量1["error"]![0]!, Is.EqualTo("用量1のエラー"));

        // インデックス5が存在することを確認
        Assert.That(処方Json.ContainsKey("5"), Is.True, "処方[5]が存在しません");
        var 処方5 = 処方Json["5"]!.AsObject();
        Assert.That(処方5.ContainsKey("用量"), Is.True, "処方[5].用量が存在しません");
        var 用量5 = 処方5["用量"]!.AsObject();
        Assert.That(用量5.ContainsKey("error"), Is.True, "処方[5].用量.errorが存在しません");
        Assert.That((string)用量5["error"]![0]!, Is.EqualTo("用量5のエラー"));

        // インデックス2は含まれていないことを確認
        Assert.That(処方Json.ContainsKey("2"), Is.False, "処方[2]が存在しています");
    }

    [Test]
    public void 複数種類のメッセージをひとつのプロパティに追加できること() {
        // ---------------------
        // Arrange
        var container = new 診察記録SaveCommandMessages(new List<string> { "診察記録" });

        // エラー・警告・情報の3種類すべてをひとつのプロパティに追加
        container.診察開始時刻.AddError("診察開始時刻がNULLです。");
        container.診察開始時刻.AddWarn("診察開始時刻が予約時刻より30分以上遅れています。");
        container.診察開始時刻.AddInfo("前回の診察開始時刻は9:30でした。");

        // ---------------------
        // Act
        var json = container.ToJsonObject();
        Console.WriteLine($"3種類のメッセージJSON: {json}");

        // ---------------------
        // Assert
        Assert.That(json.ContainsKey("診察開始時刻"), Is.True, "診察開始時刻キーが存在しません");
        var 診察開始時刻Json = json["診察開始時刻"]!.AsObject();

        // errorメッセージの確認
        Assert.That(診察開始時刻Json.ContainsKey("error"), Is.True, "errorメッセージが存在しません");
        Assert.That((string)診察開始時刻Json["error"]![0]!, Is.EqualTo("診察開始時刻がNULLです。"));

        // warnメッセージの確認
        Assert.That(診察開始時刻Json.ContainsKey("warn"), Is.True, "warnメッセージが存在しません");
        Assert.That((string)診察開始時刻Json["warn"]![0]!, Is.EqualTo("診察開始時刻が予約時刻より30分以上遅れています。"));

        // infoメッセージの確認
        Assert.That(診察開始時刻Json.ContainsKey("info"), Is.True, "infoメッセージが存在しません");
        Assert.That((string)診察開始時刻Json["info"]![0]!, Is.EqualTo("前回の診察開始時刻は9:30でした。"));
    }

    [Test]
    public void 複数のエラーメッセージを同じプロパティに追加できること() {
        // ---------------------
        // Arrange
        var container = new 診察記録SaveCommandMessages(new List<string> { "診察記録" });

        // 複数のエラーメッセージを同じプロパティに追加
        container.予約.AddError("予約IDが未入力です。");
        container.予約.AddError("指定された予約は存在しません。");
        container.予約.AddError("この予約は既にキャンセルされています。");

        // ---------------------
        // Act
        var json = container.ToJsonObject();
        Console.WriteLine($"複数エラーJSON: {json}");

        // ---------------------
        // Assert
        Assert.That(json.ContainsKey("予約"), Is.True, "予約キーが存在しません");
        var 予約Json = json["予約"]!.AsObject();

        // errorメッセージの確認
        Assert.That(予約Json.ContainsKey("error"), Is.True, "errorメッセージが存在しません");
        var エラー配列 = 予約Json["error"]!.AsArray();

        // 3つのエラーメッセージがあることを確認
        Assert.That(エラー配列.Count, Is.EqualTo(3), "エラーメッセージ数が一致しません");
        Assert.That((string)エラー配列[0]!, Is.EqualTo("予約IDが未入力です。"));
        Assert.That((string)エラー配列[1]!, Is.EqualTo("指定された予約は存在しません。"));
        Assert.That((string)エラー配列[2]!, Is.EqualTo("この予約は既にキャンセルされています。"));
    }

    [Test]
    public void 様々な階層のオブジェクトにメッセージを追加できること() {
        // ---------------------
        // Arrange
        var container = new 診察記録SaveCommandMessages(new List<string> { "診察記録" });

        // ルートオブジェクトにメッセージ追加
        container.AddError("全体的なエラーです。");

        // 直接の子要素にメッセージ追加
        container.体温.AddError("体温の値が異常です。");

        // 子の配列要素にメッセージ追加
        container.処方[2].薬剤名.AddError("薬剤名が未入力です。");

        // さらに別の添字の配列要素にメッセージ追加
        container.処方[7].用法.AddWarn("用法が標準的ではありません。");

        // ---------------------
        // Act
        var json = container.ToJsonObject();
        Console.WriteLine($"階層メッセージJSON: {json}");

        // ---------------------
        // Assert
        // ルートレベルのエラー確認
        Assert.That(json.ContainsKey("error"), Is.True, "ルートのエラーメッセージが存在しません");
        Assert.That((string)json["error"]![0]!, Is.EqualTo("全体的なエラーです。"));

        // 直接の子要素のエラー確認
        Assert.That(json.ContainsKey("体温"), Is.True, "体温キーが存在しません");
        Assert.That((string)json["体温"]!["error"]![0]!, Is.EqualTo("体温の値が異常です。"));

        // 配列要素のエラー確認
        Assert.That(json.ContainsKey("処方"), Is.True, "処方キーが存在しません");
        var 処方Json = json["処方"]!.AsObject();

        // インデックス2の要素確認
        Assert.That(処方Json.ContainsKey("2"), Is.True, "処方[2]が存在しません");
        Assert.That((string)処方Json["2"]!["薬剤名"]!["error"]![0]!, Is.EqualTo("薬剤名が未入力です。"));

        // インデックス7の要素確認
        Assert.That(処方Json.ContainsKey("7"), Is.True, "処方[7]が存在しません");
        Assert.That((string)処方Json["7"]!["用法"]!["warn"]![0]!, Is.EqualTo("用法が標準的ではありません。"));
    }
}
