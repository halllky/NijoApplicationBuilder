using MyApp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace MyApp.Test.Tests;


[Category("MessageContainerのテスト")]
internal class MessageContainerのテスト {

    [Test]
    public void ToJsonObjectのテスト() {
        // パスを意味のある値で初期化
        var container = new 医療機器マスタSaveCommandMessages(new List<string> { "医療機器マスタ" });

        // ---------------------
        // Arrange
        // 子オブジェクトの、さらに子配列の要素のプロパティにエラーを追加
        container.機器詳細.付属品[3].付属品名.AddError("付属品名が記載されていません。");
        container.AddError("ルートのエラー");

        // ---------------------
        // Act
        var json = container.ToJsonObject();

        // ---------------------
        // Assert
        Logout(json);

        // ルートレベルのエラーを確認
        Assert.That(json.ContainsKey("error"), Is.True, "ルートのエラーメッセージが存在しません");
        Assert.That((string)json["error"]![0]!, Is.EqualTo("ルートのエラー"));

        // 子オブジェクトのキーが存在することを確認
        Assert.That(json.ContainsKey("機器詳細"), Is.True, "機器詳細キーが存在しません");
        var 機器詳細Json = json["機器詳細"]!.AsObject();

        // 子配列のキーが存在することを確認
        Assert.That(機器詳細Json.ContainsKey("付属品"), Is.True, "付属品キーが存在しません");
        var 付属品Json = 機器詳細Json["付属品"]!.AsObject(); // 配列もJsonObjectとして扱われる

        Assert.That(付属品Json.ContainsKey("3"), Is.True, "付属品[3]が存在しません");
        var 付属品3Json = 付属品Json["3"]!.AsObject();

        Assert.That(付属品3Json.ContainsKey("付属品名"), Is.True, "付属品[3].付属品名が存在しません");
        var 付属品名Json = 付属品3Json["付属品名"]!.AsObject();

        Assert.That(付属品名Json.ContainsKey("error"), Is.True, "付属品[3].付属品名.errorが存在しません");
        var エラー配列 = 付属品名Json["error"]!.AsArray();
        Assert.That(エラー配列.Count, Is.EqualTo(1), "エラーメッセージ数が一致しません");
        Assert.That((string)エラー配列[0]!, Is.EqualTo("付属品名が記載されていません。"));
    }

    [Test]
    public void メッセージがないときのJSONは空になること() {
        // ---------------------
        // Arrange
        var container = new 医療機器マスタSaveCommandMessages(new List<string> { "医療機器マスタ" });
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
        var container = new 医療機器マスタSaveCommandMessages(new List<string> { "医療機器マスタ" });

        // 直接コンテナにエラーを追加
        container.AddError("直接のエラー");

        // 深いネストの子要素にエラーを追加
        container.機器詳細.付属品[3].付属品名.AddError("付属品名が記載されていません。");

        // ---------------------
        // Act & Assert
        Assert.That(container.HasError(), Is.True);
    }

    [Test]
    public void 異なる添字の配列要素にメッセージがある場合() {
        // ---------------------
        // Arrange
        var container = new 医療機器マスタSaveCommandMessages(new List<string> { "医療機器マスタ" });

        // 不連続なインデックスにメッセージを追加
        container.機器詳細.付属品[1].数量.AddError("数量1のエラー");
        container.機器詳細.付属品[5].数量.AddError("数量5のエラー");

        // ---------------------
        // Act
        var json = container.ToJsonObject();
        Logout(json);

        // ---------------------
        // Assert
        // 機器詳細が存在することを確認
        Assert.That(json.ContainsKey("機器詳細"), Is.True, "機器詳細キーが存在しません");
        var 機器詳細Json = json["機器詳細"]!.AsObject();
        // 付属品が存在することを確認
        Assert.That(機器詳細Json.ContainsKey("付属品"), Is.True, "付属品キーが存在しません");
        var 付属品Json = 機器詳細Json["付属品"]!.AsObject();

        // インデックス1が存在することを確認
        Assert.That(付属品Json.ContainsKey("1"), Is.True, "付属品[1]が存在しません");
        var 付属品1Json = 付属品Json["1"]!.AsObject();
        Assert.That(付属品1Json.ContainsKey("数量"), Is.True, "付属品[1].数量が存在しません");
        var 数量1Json = 付属品1Json["数量"]!.AsObject();
        Assert.That(数量1Json.ContainsKey("error"), Is.True, "付属品[1].数量.errorが存在しません");
        Assert.That((string)数量1Json["error"]![0]!, Is.EqualTo("数量1のエラー"));

        // インデックス5が存在することを確認
        Assert.That(付属品Json.ContainsKey("5"), Is.True, "付属品[5]が存在しません");
        var 付属品5Json = 付属品Json["5"]!.AsObject();
        Assert.That(付属品5Json.ContainsKey("数量"), Is.True, "付属品[5].数量が存在しません");
        var 数量5Json = 付属品5Json["数量"]!.AsObject();
        Assert.That(数量5Json.ContainsKey("error"), Is.True, "付属品[5].数量.errorが存在しません");
        Assert.That((string)数量5Json["error"]![0]!, Is.EqualTo("数量5のエラー"));

        // インデックス2は含まれていないことを確認
        Assert.That(付属品Json.ContainsKey("2"), Is.False, "付属品[2]が存在しています");
    }

    [Test]
    public void 複数種類のメッセージをひとつのプロパティに追加できること() {
        // ---------------------
        // Arrange
        var container = new 医療機器マスタSaveCommandMessages(new List<string> { "医療機器マスタ" });

        // エラー・警告・情報の3種類すべてをひとつのプロパティに追加
        container.機器名.AddError("機器名がNULLです。");
        container.機器名.AddWarn("機器名が予約語と競合しています。");
        container.機器名.AddInfo("以前の機器名は「旧機器名」でした。");

        // ---------------------
        // Act
        var json = container.ToJsonObject();
        Logout(json);

        // ---------------------
        // Assert
        Assert.That(json.ContainsKey("機器名"), Is.True, "機器名キーが存在しません");
        var 機器名Json = json["機器名"]!.AsObject();

        // errorメッセージの確認
        Assert.That(機器名Json.ContainsKey("error"), Is.True, "errorメッセージが存在しません");
        Assert.That((string)機器名Json["error"]![0]!, Is.EqualTo("機器名がNULLです。"));

        // warnメッセージの確認
        Assert.That(機器名Json.ContainsKey("warn"), Is.True, "warnメッセージが存在しません");
        Assert.That((string)機器名Json["warn"]![0]!, Is.EqualTo("機器名が予約語と競合しています。"));

        // infoメッセージの確認
        Assert.That(機器名Json.ContainsKey("info"), Is.True, "infoメッセージが存在しません");
        Assert.That((string)機器名Json["info"]![0]!, Is.EqualTo("以前の機器名は「旧機器名」でした。"));
    }

    [Test]
    public void 複数のエラーメッセージを同じプロパティに追加できること() {
        // ---------------------
        // Arrange
        var container = new 医療機器マスタSaveCommandMessages(new List<string> { "医療機器マスタ" });

        // 複数のエラーメッセージを同じプロパティに追加
        container.単価.AddError("単価が未入力です。");
        container.単価.AddError("単価が0以下です。");
        container.単価.AddError("単価の上限を超えています。");

        // ---------------------
        // Act
        var json = container.ToJsonObject();
        Logout(json);

        // ---------------------
        // Assert
        Assert.That(json.ContainsKey("単価"), Is.True, "単価キーが存在しません");
        var 単価Json = json["単価"]!.AsObject();

        // errorメッセージの確認
        Assert.That(単価Json.ContainsKey("error"), Is.True, "errorメッセージが存在しません");
        var エラー配列 = 単価Json["error"]!.AsArray();

        // 3つのエラーメッセージがあることを確認
        Assert.That(エラー配列.Count, Is.EqualTo(3), "エラーメッセージ数が一致しません");
        Assert.That((string)エラー配列[0]!, Is.EqualTo("単価が未入力です。"));
        Assert.That((string)エラー配列[1]!, Is.EqualTo("単価が0以下です。"));
        Assert.That((string)エラー配列[2]!, Is.EqualTo("単価の上限を超えています。"));
    }

    [Test]
    public void 様々な階層のオブジェクトにメッセージを追加できること() {
        // ---------------------
        // Arrange
        var container = new 医療機器マスタSaveCommandMessages(new List<string> { "医療機器マスタ" });

        // ルートオブジェクトにメッセージ追加
        container.AddError("全体的なエラーです。");

        // 直接の子要素にメッセージ追加
        container.機器名.AddError("機器名の値が異常です。");

        // 子の配列要素にメッセージ追加
        container.機器詳細.付属品[2].付属品名.AddError("付属品名が未入力です。");

        // さらに別の添字の配列要素にメッセージ追加
        container.機器詳細.付属品[7].数量.AddWarn("数量が標準的ではありません。");

        // ---------------------
        // Act
        var json = container.ToJsonObject();
        Logout(json);

        // ---------------------
        // Assert
        // ルートレベルのエラー確認
        Assert.That(json.ContainsKey("error"), Is.True, "ルートのエラーメッセージが存在しません");
        Assert.That((string)json["error"]![0]!, Is.EqualTo("全体的なエラーです。"));

        // 直接の子要素のエラー確認
        Assert.That(json.ContainsKey("機器名"), Is.True, "機器名キーが存在しません");
        Assert.That((string)json["機器名"]!["error"]![0]!, Is.EqualTo("機器名の値が異常です。"));

        // 配列要素のエラー確認
        Assert.That(json.ContainsKey("機器詳細"), Is.True, "機器詳細キーが存在しません");
        var 機器詳細Json = json["機器詳細"]!.AsObject();
        Assert.That(機器詳細Json.ContainsKey("付属品"), Is.True, "付属品キーが存在しません");
        var 付属品Json = 機器詳細Json["付属品"]!.AsObject();

        // インデックス2の要素確認
        Assert.That(付属品Json.ContainsKey("2"), Is.True, "付属品[2]が存在しません");
        Assert.That((string)付属品Json["2"]!["付属品名"]!["error"]![0]!, Is.EqualTo("付属品名が未入力です。"));

        // インデックス7の要素確認
        Assert.That(付属品Json.ContainsKey("7"), Is.True, "付属品[7]が存在しません");
        Assert.That((string)付属品Json["7"]!["数量"]!["warn"]![0]!, Is.EqualTo("数量が標準的ではありません。"));
    }

    private static void Logout(JsonObject jsonObject) {
        var options = new JsonSerializerOptions {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true,
        };
        TestContext.Out.WriteLine($"実際の値: {JsonSerializer.Serialize(jsonObject, options)}");
    }
}
