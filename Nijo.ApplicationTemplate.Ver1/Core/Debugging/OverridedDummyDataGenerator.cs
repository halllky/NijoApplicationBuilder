
namespace MyApp.Core;

/// <summary>
/// ダミーデータ生成クラスのオーバーライド。
/// 標準のダミーデータ生成処理をカスタマイズしたい場合はここで適宜オーバーライドする。
/// </summary>
public class OverridedDummyDataGenerator : DummyDataGenerator {
    protected override string? GetRandomWord(DummyDataGenerateContext context, ValueMemberMetadata member) {
        if (member.IsKey) {
            return base.GetRandomWord(context, member);

        } else {
            var value = $"{member.DisplayName}その{context.Random.Next(1, 100)}";
            if (member.MaxLength != null && value.Length > member.MaxLength.Value) {
                value = value.Substring(0, member.MaxLength.Value);
            }
            return value;
        }
    }
}

