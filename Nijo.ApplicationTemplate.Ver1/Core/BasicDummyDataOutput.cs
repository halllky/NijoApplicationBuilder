using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApp.Core;

/// <summary>
/// <see cref="IDummyDataOutput"/> のシンプルな実装。
/// 大量のダミーデータをEFCoreのSaveChanges経由でそのまま登録する。
/// パフォーマンスが気になる場合はSaveChangesを使う戦略を廃し、SQL発行でのINSERTに切り替えること。
/// </summary>
public class BasicDummyDataOutput : IDummyDataOutput {
    public BasicDummyDataOutput(MyDbContext dbContext) {
        _dbContext = dbContext;
    }
    private readonly MyDbContext _dbContext;

    public async Task OutputAsync<TEntity>(IEnumerable<TEntity> entities) {
        foreach (var entity in entities) _dbContext.Add(entity!);
        await _dbContext.SaveChangesAsync();
    }
}
