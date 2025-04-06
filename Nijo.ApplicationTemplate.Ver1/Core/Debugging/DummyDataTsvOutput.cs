using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MyApp.Core.Debugging;

/// <summary>
/// ダミーデータをTSV出力する。
/// </summary>
public class DummyDataTsvOutput : IDummyDataOutput {
    public DummyDataTsvOutput(TextWriter textWriter) {
        _textWriter = textWriter;
    }

    private readonly TextWriter _textWriter;

    public async Task OutputAsync<TEntity>(IEnumerable<TEntity> entities) {

        // 型名
        await _textWriter.WriteLineAsync($"★★★ {typeof(TEntity).Name} ★★★");

        // エンティティの型からプロパティ情報を取得
        var properties = typeof(TEntity)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToArray();

        // ヘッダー行の出力
        var headerLine = string.Join("\t", properties.Select(p => p.Name));
        await _textWriter.WriteLineAsync(headerLine);

        // 各エンティティの値を出力
        foreach (var entity in entities) {
            var values = properties
                .Select(p => {
                    var value = p.GetValue(entity);
                    return value?.ToString() ?? string.Empty;
                });

            var dataLine = string.Join("\t", values);
            await _textWriter.WriteLineAsync(dataLine);
        }

        // 空行
        await _textWriter.WriteLineAsync();

        await _textWriter.FlushAsync();
    }
}
