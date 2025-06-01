using Microsoft.EntityFrameworkCore;
using MyApp.Core.Debugging;
using MyApp.WebApi.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApp.Core;

partial class DB接続あり_更新あり {

    /// <summary>
    /// Childの多重度は親テーブルに対し0または1。
    /// Childの追加更新削除が正常に動作するかを確認する。
    /// </summary>
    [Test]
    public async Task Childの追加更新削除が正常に動作するか確認() {
        using var scope = TestUtilImpl.Instance.CreateScope<商品マスタSaveCommandMessages>(
            nameof(Childの追加更新削除が正常に動作するか確認),
            options: new PresentationContextOptions { IgnoreConfirm = true });

        // 外部キー用に全テーブルデータ作成
        var generator = new OverridedDummyDataGenerator();
        var dbDescriptor = new DummyDataDbOutput(scope.App.DbContext);
        await generator.GenerateAsync(dbDescriptor);

        // 不要な部分を削除
        await scope.App.DbContext.アクション結果DbSet.ExecuteDeleteAsync();
        await scope.App.DbContext.在庫調査報告DbSet.ExecuteDeleteAsync();
        await scope.App.DbContext.シフトDbSet.ExecuteDeleteAsync();
        await scope.App.DbContext.注文履歴DbSet.ExecuteDeleteAsync();
        await scope.App.DbContext.店舗マスタDbSet.ExecuteDeleteAsync();
        await scope.App.DbContext.商品マスタDbSet.ExecuteDeleteAsync();

        // --------------------------
        // 商品マスタ、その直下のChild、さらにその直下のChildに登録
        using (var tran = await scope.App.DbContext.Database.BeginTransactionAsync()) {
            var category1 = scope.App.DbContext.カテゴリマスタDbSet.OrderBy(x => x.カテゴリID).First();
            var supplier1 = scope.App.DbContext.仕入先マスタDbSet.OrderBy(x => x.仕入先ID).First();
            var warehouse1 = scope.App.DbContext.倉庫マスタDbSet.OrderBy(x => x.ID).First();
            var employee1 = scope.App.DbContext.従業員マスタDbSet.OrderBy(x => x.従業員ID).First();

            await scope.App.Create商品マスタAsync(new() {
                ID = "PROD001",
                商品名 = "テスト商品1",
                価格 = 1000,
                カテゴリ = new() { カテゴリID = category1.カテゴリID },
                仕入先 = new() { 仕入先ID = supplier1.仕入先ID },
                商品詳細 = new() {
                    説明文 = "テスト商品の説明文",
                    商品仕様 = new() {
                        重量 = 500,
                        サイズ = new() {
                            幅 = 10,
                            高さ = 20,
                            奥行 = 30
                        }
                    },
                    付属品 = new List<付属品CreateCommand> {
                    new() {
                        付属品ID = "ACC001",
                        付属品名 = "取扱説明書",
                        数量 = 1
                    }
                }
                },
                在庫情報 = new List<在庫情報CreateCommand> {
                    new() {
                        倉庫 = new() { ID = warehouse1.ID },
                        在庫数 = 100,
                        棚卸日時 = DateTime.Now,
                        在庫状況履歴 = new List<在庫状況履歴CreateCommand> {
                            new() {
                                履歴ID = "HIST001",
                                変更日時 = DateTime.Now.AddDays(-1),
                                変更前在庫数 = 0,
                                変更後在庫数 = 100,
                                担当者 = new() { 従業員ID = (従業員ID型?)employee1.従業員ID }
                            }
                        }
                    }
                }
            }, scope.PresentationContext.Messages, scope.PresentationContext);
            await tran.CommitAsync();
        }

        Assert.Multiple(() => {
            // エラーが無いことを確認
            Assert.That(scope.PresentationContext.Messages.HasError(), Is.False);

            // 3テーブルともに登録されていることを確認
            Assert.That(scope.App.DbContext.商品マスタDbSet.Count(), Is.EqualTo(1));
            Assert.That(scope.App.DbContext.商品詳細DbSet.Count(), Is.EqualTo(1));
            Assert.That(scope.App.DbContext.商品仕様DbSet.Count(), Is.EqualTo(1));
            Assert.That(scope.App.DbContext.サイズDbSet.Count(), Is.EqualTo(1));
            Assert.That(scope.App.DbContext.付属品DbSet.Count(), Is.EqualTo(1));
            Assert.That(scope.App.DbContext.在庫情報DbSet.Count(), Is.EqualTo(1));
            Assert.That(scope.App.DbContext.在庫状況履歴DbSet.Count(), Is.EqualTo(1));
        });

        // --------------------------
        // 3テーブルすべてUPDATE
        using (var tran = await scope.App.DbContext.Database.BeginTransactionAsync()) {
            var category1 = scope.App.DbContext.カテゴリマスタDbSet.OrderBy(x => x.カテゴリID).First();
            var supplier1 = scope.App.DbContext.仕入先マスタDbSet.OrderBy(x => x.仕入先ID).First();
            var warehouse1 = scope.App.DbContext.倉庫マスタDbSet.OrderBy(x => x.ID).First();
            var employee1 = scope.App.DbContext.従業員マスタDbSet.OrderBy(x => x.従業員ID).First();

            await scope.App.Update商品マスタAsync(new() {
                ID = "PROD001",
                商品名 = "テスト商品1-更新",
                価格 = 1500,
                カテゴリ = new() { カテゴリID = category1.カテゴリID },
                仕入先 = new() { 仕入先ID = supplier1.仕入先ID },
                商品詳細 = new() {
                    説明文 = "テスト商品の説明文-更新",
                    商品仕様 = new() {
                        重量 = 600,
                        サイズ = new() {
                            幅 = 15,
                            高さ = 25,
                            奥行 = 35
                        }
                    },
                    付属品 = new List<付属品UpdateCommand> {
                        new() {
                            付属品ID = "ACC001",
                            付属品名 = "取扱説明書-更新",
                            数量 = 2
                        }
                    }
                },
                在庫情報 = new List<在庫情報UpdateCommand> {
                    new() {
                        倉庫 = new() { ID = warehouse1.ID },
                        在庫数 = 150,
                        棚卸日時 = DateTime.Now,
                        在庫状況履歴 = new List<在庫状況履歴UpdateCommand> {
                            new() {
                                履歴ID = "HIST001",
                                変更日時 = DateTime.Now.AddDays(-1),
                                変更前在庫数 = 0,
                                変更後在庫数 = 100,
                                担当者 = new() { 従業員ID = (従業員ID型?)employee1.従業員ID }
                            },
                            new() {
                                履歴ID = "HIST002",
                                変更日時 = DateTime.Now,
                                変更前在庫数 = 100,
                                変更後在庫数 = 150,
                                担当者 = new() { 従業員ID = (従業員ID型?)employee1.従業員ID }
                            }
                        }
                    }
                },
                Version = 0
            }, scope.PresentationContext.Messages, scope.PresentationContext);
            await tran.CommitAsync();
        }

        Assert.Multiple(() => {
            // エラーが無いことを確認
            Assert.That(scope.PresentationContext.Messages.HasError(), Is.False);

            // 3テーブルともに新しい値に更新されていることを確認
            Assert.That(scope.App.DbContext.商品マスタDbSet.Count(), Is.EqualTo(1));
            var 商品マスタ = scope.App.DbContext.商品マスタDbSet.First();
            Assert.That(商品マスタ.商品名, Is.EqualTo("テスト商品1-更新"));
            Assert.That(商品マスタ.価格, Is.EqualTo(1500));

            var 商品詳細 = scope.App.DbContext.商品詳細DbSet.First();
            Assert.That(商品詳細.説明文, Is.EqualTo("テスト商品の説明文-更新"));

            var 商品仕様 = scope.App.DbContext.商品仕様DbSet.First();
            Assert.That(商品仕様.重量, Is.EqualTo(600));

            var サイズ = scope.App.DbContext.サイズDbSet.First();
            Assert.That(サイズ.幅, Is.EqualTo(15));
            Assert.That(サイズ.高さ, Is.EqualTo(25));
            Assert.That(サイズ.奥行, Is.EqualTo(35));

            Assert.That(scope.App.DbContext.在庫状況履歴DbSet.Count(), Is.EqualTo(2));
        });

        // --------------------------
        // 孫テーブルだけをDELETEし、ルートと子は変更なし
        using (var tran = await scope.App.DbContext.Database.BeginTransactionAsync()) {
            var category1 = scope.App.DbContext.カテゴリマスタDbSet.OrderBy(x => x.カテゴリID).First();
            var supplier1 = scope.App.DbContext.仕入先マスタDbSet.OrderBy(x => x.仕入先ID).First();
            var warehouse1 = scope.App.DbContext.倉庫マスタDbSet.OrderBy(x => x.ID).First();

            await scope.App.Update商品マスタAsync(new() {
                ID = "PROD001",
                商品名 = "テスト商品1-更新",
                価格 = 1500,
                カテゴリ = new() { カテゴリID = category1.カテゴリID },
                仕入先 = new() { 仕入先ID = supplier1.仕入先ID },
                商品詳細 = new() {
                    説明文 = "テスト商品の説明文-更新",
                    商品仕様 = new() {
                        重量 = 600,
                        サイズ = null // サイズを削除
                    },
                    付属品 = new List<付属品UpdateCommand> {
                        new() {
                            付属品ID = "ACC001",
                            付属品名 = "取扱説明書-更新",
                            数量 = 2
                        }
                    }
                },
                在庫情報 = new List<在庫情報UpdateCommand> {
                    new() {
                        倉庫 = new() { ID = warehouse1.ID },
                        在庫数 = 150,
                        棚卸日時 = DateTime.Now,
                        在庫状況履歴 = new List<在庫状況履歴UpdateCommand>() // 履歴を空にして削除
                    }
                },
                Version = 1
            }, scope.PresentationContext.Messages, scope.PresentationContext);
            await tran.CommitAsync();
        }

        Assert.Multiple(() => {
            // エラーが無いことを確認
            Assert.That(scope.PresentationContext.Messages.HasError(), Is.False);

            // ルートと子が影響なし、孫が消えていることを確認
            Assert.That(scope.App.DbContext.商品マスタDbSet.Count(), Is.EqualTo(1));
            Assert.That(scope.App.DbContext.商品詳細DbSet.Count(), Is.EqualTo(1));
            Assert.That(scope.App.DbContext.商品仕様DbSet.Count(), Is.EqualTo(1));
            Assert.That(scope.App.DbContext.サイズDbSet.Count(), Is.EqualTo(0)); // サイズが削除されたことを確認
            Assert.That(scope.App.DbContext.付属品DbSet.Count(), Is.EqualTo(1)); // 付属品は残っていることを確認
            Assert.That(scope.App.DbContext.在庫情報DbSet.Count(), Is.EqualTo(1));
            Assert.That(scope.App.DbContext.在庫状況履歴DbSet.Count(), Is.EqualTo(0)); // 履歴が削除されたことを確認
        });

        // --------------------------
        // 孫テーブルをCREATEし、ルートと子は変更なし
        using (var tran = await scope.App.DbContext.Database.BeginTransactionAsync()) {
            var category1 = scope.App.DbContext.カテゴリマスタDbSet.OrderBy(x => x.カテゴリID).First();
            var supplier1 = scope.App.DbContext.仕入先マスタDbSet.OrderBy(x => x.仕入先ID).First();
            var warehouse1 = scope.App.DbContext.倉庫マスタDbSet.OrderBy(x => x.ID).First();
            var employee1 = scope.App.DbContext.従業員マスタDbSet.OrderBy(x => x.従業員ID).First();

            await scope.App.Update商品マスタAsync(new() {
                ID = "PROD001",
                商品名 = "テスト商品1-更新",
                価格 = 1500,
                カテゴリ = new() { カテゴリID = category1.カテゴリID },
                仕入先 = new() { 仕入先ID = supplier1.仕入先ID },
                商品詳細 = new() {
                    説明文 = "テスト商品の説明文-更新",
                    商品仕様 = new() {
                        重量 = 600,
                        サイズ = new() { // サイズを再追加
                            幅 = 20,
                            高さ = 30,
                            奥行 = 40
                        }
                    },
                    付属品 = new List<付属品UpdateCommand> {
                        new() {
                            付属品ID = "ACC001",
                            付属品名 = "取扱説明書-更新",
                            数量 = 2
                        }
                    }
                },
                在庫情報 = new List<在庫情報UpdateCommand> {
                    new() {
                        倉庫 = new() { ID = warehouse1.ID },
                        在庫数 = 150,
                        棚卸日時 = DateTime.Now,
                        在庫状況履歴 = new List<在庫状況履歴UpdateCommand> { // 履歴を再追加
                            new() {
                                履歴ID = "HIST003",
                                変更日時 = DateTime.Now,
                                変更前在庫数 = 150,
                                変更後在庫数 = 150,
                                担当者 = new() { 従業員ID = (従業員ID型?)employee1.従業員ID }
                            },
                        },
                    },
                },
                Version = 2
            }, scope.PresentationContext.Messages, scope.PresentationContext);
            await tran.CommitAsync();
        }

        Assert.Multiple(() => {
            // エラーが無いことを確認
            Assert.That(scope.PresentationContext.Messages.HasError(), Is.False);

            // 3テーブルすべてデータがあることを確認
            Assert.That(scope.App.DbContext.商品マスタDbSet.Count(), Is.EqualTo(1));
            Assert.That(scope.App.DbContext.商品詳細DbSet.Count(), Is.EqualTo(1));
            Assert.That(scope.App.DbContext.商品仕様DbSet.Count(), Is.EqualTo(1));
            Assert.That(scope.App.DbContext.サイズDbSet.Count(), Is.EqualTo(1)); // サイズが再追加されたことを確認
            Assert.That(scope.App.DbContext.付属品DbSet.Count(), Is.EqualTo(1));
            Assert.That(scope.App.DbContext.在庫情報DbSet.Count(), Is.EqualTo(1));
            Assert.That(scope.App.DbContext.在庫状況履歴DbSet.Count(), Is.EqualTo(1)); // 履歴が再追加されたことを確認

            var サイズ = scope.App.DbContext.サイズDbSet.First();
            Assert.That(サイズ.幅, Is.EqualTo(20));
            Assert.That(サイズ.高さ, Is.EqualTo(30));
            Assert.That(サイズ.奥行, Is.EqualTo(40));
        });

        // --------------------------
        // 子をDELETE
        using (var tran = await scope.App.DbContext.Database.BeginTransactionAsync()) {
            var category1 = scope.App.DbContext.カテゴリマスタDbSet.OrderBy(x => x.カテゴリID).First();
            var supplier1 = scope.App.DbContext.仕入先マスタDbSet.OrderBy(x => x.仕入先ID).First();

            await scope.App.Update商品マスタAsync(new() {
                ID = "PROD001",
                商品名 = "テスト商品1-更新",
                価格 = 1500,
                カテゴリ = new() { カテゴリID = category1.カテゴリID },
                仕入先 = new() { 仕入先ID = supplier1.仕入先ID },
                商品詳細 = null, // 商品詳細を削除
                在庫情報 = new List<在庫情報UpdateCommand>(), // 在庫情報を空にして削除
                Version = 3
            }, scope.PresentationContext.Messages, scope.PresentationContext);
            await tran.CommitAsync();
        }

        Assert.Multiple(() => {
            // エラーが無いことを確認
            Assert.That(scope.PresentationContext.Messages.HasError(), Is.False);

            // ルートだけデータが残っていることを確認
            Assert.That(scope.App.DbContext.商品マスタDbSet.Count(), Is.EqualTo(1));
            Assert.That(scope.App.DbContext.商品詳細DbSet.Count(), Is.EqualTo(0)); // 商品詳細が削除されたことを確認
            Assert.That(scope.App.DbContext.商品仕様DbSet.Count(), Is.EqualTo(0)); // 商品仕様も削除されたことを確認
            Assert.That(scope.App.DbContext.サイズDbSet.Count(), Is.EqualTo(0));
            Assert.That(scope.App.DbContext.付属品DbSet.Count(), Is.EqualTo(0));
            Assert.That(scope.App.DbContext.在庫情報DbSet.Count(), Is.EqualTo(0)); // 在庫情報が削除されたことを確認
            Assert.That(scope.App.DbContext.在庫状況履歴DbSet.Count(), Is.EqualTo(0));
        });
    }

}
