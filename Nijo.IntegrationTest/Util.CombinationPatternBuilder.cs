using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.IntegrationTest;

/// <summary>
/// 組み合わせテストのパターンを組み立てるクラス。
/// 例えばテストに関係する因子が3種類あり、それぞれ2パターンの水準をとりうるとき、
/// 機械的に 2 ^ 2 ^ 2 = 8パターンのオブジェクトを組み立てたい、という場合に使います。
/// パターンはC#のクラスとして表現することができ、かつ当該クラスはミュータブルである必要があります。
/// </summary>
/// <typeparam name="T">パターン</typeparam>
public class CombinationPatternBuilder<T> {

    /// <param name="createNewInstance">新しいインスタンスを作成する</param>
    /// <param name="seed">ランダム値のシード</param>
    public CombinationPatternBuilder(Func<Random, T> createNewInstance, int? seed = null) {
        _random = seed.HasValue ? new Random((int)seed) : new Random();
        _createNewInstance = createNewInstance;
    }

    /// <summary>ランダム</summary>
    private readonly Random _random;
    /// <summary>インスタンス作成</summary>
    private readonly Func<Random, T> _createNewInstance;
    /// <summary>組み合わせパターン</summary>
    private readonly List<Action<T, Random>[]> _modifiers = [];
    /// <summary>禁則組み合わせ</summary>
    private readonly List<Func<T, bool>> _forbidden = [];

    /// <summary>組み合わせパターン</summary>
    public CombinationPatternBuilder<T> Pattern(params Action<T, Random>[] modifiers) {
        if (modifiers.Length == 0) throw new ArgumentException("パターンは1つ以上を指定する必要があります。", nameof(modifiers));
        _modifiers.AddRange(modifiers);
        return this;
    }
    /// <summary>組み合わせパターン</summary>
    public CombinationPatternBuilder<T> Pattern(params Action<T>[] modifiers) {
        var actions = modifiers
            .Select(action => new Action<T, Random>((item, random) => action.Invoke(item)))
            .ToArray();
        Pattern(actions);
        return this;
    }

    /// <summary>禁則組み合わせ</summary>
    public CombinationPatternBuilder<T> Forbidden(Func<T, bool> predicate) {
        _forbidden.Add(predicate);
        return this;
    }

    /// <summary>
    /// かけあわされたテストパターンの一覧を返します。
    /// </summary>
    public IEnumerable<T> Build() {
        // テストパターンの数
        var patternCount = _modifiers.Aggregate(1, (result, modifierList) => result * modifierList.Length);

        // 組み合わせ表
        var modifierPatterns = new Action<T, Random>[patternCount, _modifiers.Count];
        for (int i = 0; i < _modifiers.Count; i++) {
            var cycle = _modifiers
                .Skip(i + 1)
                .Aggregate(1, (multiplied, current) => multiplied * current.Length);
            for (int j = 0; j < patternCount; j++) {
                modifierPatterns[j, i] = _modifiers[i][(j / cycle) % _modifiers[i].Length];
            }
        }

        // パターンごとにmodifierを順次適用のうえ、禁則組み合わせに該当しないか確認し、該当しなければ返す
        for (int j = 0; j < patternCount; j++) {
            var item = _createNewInstance(_random);
            for (int i = 0; i < _modifiers.Count; i++) {
                var action = modifierPatterns[j, i];
                action.Invoke(item, _random);
            }
            if (_forbidden.Any(fn => fn.Invoke(item))) {
                continue;
            }
            yield return item;
        }
    }
}
