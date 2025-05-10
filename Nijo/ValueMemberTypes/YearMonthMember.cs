using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Models.QueryModelModules;
using Nijo.Parts.CSharp;
using Nijo.SchemaParsing;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.ValueMemberTypes;

/// <summary>
/// 年月型
/// </summary>
internal class YearMonthMember : IValueMemberType {
    string IValueMemberType.TypePhysicalName => "YearMonth";
    string IValueMemberType.SchemaTypeName => "yearmonth";
    string IValueMemberType.CsDomainTypeName => "YearMonth";
    string IValueMemberType.CsPrimitiveTypeName => "int";
    string IValueMemberType.TsTypeName => "number";
    UiConstraint.E_Type IValueMemberType.UiConstraintType => UiConstraint.E_Type.NumberMemberConstraint;
    string IValueMemberType.DisplayName => "年月型";

    void IValueMemberType.Validate(XElement element, SchemaParseContext context, Action<XElement, string> addError) {
        // 年月型の検証
        // 必要に応じて年月の範囲制約などを検証するコードをここに追加できます
    }

    ValueMemberSearchBehavior? IValueMemberType.SearchBehavior => new() {
        FilterCsTypeName = $"{FromTo.CS_CLASS_NAME}<YearMonth?>",
        FilterTsTypeName = "{ from?: number; to?: number }",
        RenderTsNewObjectFunctionValue = () => "{ from: undefined, to: undefined }",
        RenderFiltering = ctx => RangeSearchRenderer.RenderRangeSearchFiltering(ctx),
    };

    void IValueMemberType.RegisterDependencies(IMultiAggregateSourceFileManager ctx) {
        // 特になし
    }

    string IValueMemberType.RenderCreateDummyDataValueBody(CodeRenderingContext ctx) {
        return $$"""
            var now = DateTime.Now;
            return member.IsKey
                ? new YearMonth(now.Year, Math.Max(1, Math.Min(12, (context.GetNextSequence() % 12) + 1)))
                : new YearMonth(now.Year, now.Month);
            """;
    }

    void IValueMemberType.RenderStaticSources(CodeRenderingContext ctx) {
        ctx.CoreLibrary(dir => {
            dir.Generate(new SourceFile {
                FileName = "YearMonth.cs",
                Contents = $$"""
                using System;
                using System.Globalization;
                using System.Text.Json;
                using System.Text.Json.Serialization;

                namespace {{ctx.Config.RootNamespace}};

                /// <summary>
                /// 年月を表す値オブジェクト
                /// </summary>
                public readonly struct YearMonth : IComparable<YearMonth>, IEquatable<YearMonth> {
                    /// <summary>
                    /// 数値表現（YYYYMM形式の6桁整数）
                    /// </summary>
                    private readonly int _value;

                    /// <summary>
                    /// 数値表現（YYYYMM形式の6桁整数）
                    /// </summary>
                    public int Value => _value;

                    /// <summary>
                    /// 年
                    /// </summary>
                    public int Year => _value / 100;

                    /// <summary>
                    /// 月（1〜12）
                    /// </summary>
                    public int Month => _value % 100;

                    /// <summary>
                    /// 指定した年月から値オブジェクトを生成します
                    /// </summary>
                    /// <param name="year">年（4桁）</param>
                    /// <param name="month">月（1〜12）</param>
                    public YearMonth(int year, int month) {
                        if (year < 1 || year > 9999) throw new ArgumentOutOfRangeException(nameof(year), "年は1〜9999の範囲で指定してください");
                        if (month < 1 || month > 12) throw new ArgumentOutOfRangeException(nameof(month), "月は1〜12の範囲で指定してください");
                        _value = year * 100 + month;
                    }

                    /// <summary>
                    /// YYYYMMの数値表現から値オブジェクトを生成します
                    /// </summary>
                    /// <param name="value">YYYYMMの6桁整数</param>
                    public YearMonth(int value) {
                        int year = value / 100;
                        int month = value % 100;

                        if (year < 1 || year > 9999) throw new ArgumentOutOfRangeException(nameof(value), "年は1〜9999の範囲である必要があります");
                        if (month < 1 || month > 12) throw new ArgumentOutOfRangeException(nameof(value), "月は1〜12の範囲である必要があります");

                        _value = value;
                    }

                    /// <summary>
                    /// DateTimeから値オブジェクトを生成します
                    /// </summary>
                    public static YearMonth FromDateTime(DateTime dateTime) {
                        return new YearMonth(dateTime.Year, dateTime.Month);
                    }

                    /// <summary>
                    /// DateOnlyから値オブジェクトを生成します
                    /// </summary>
                    public static YearMonth FromDateOnly(DateOnly dateOnly) {
                        return new YearMonth(dateOnly.Year, dateOnly.Month);
                    }

                    /// <summary>
                    /// 年月をDateTime型に変換します（日付は1日として扱います）
                    /// </summary>
                    public DateTime ToDateTime() {
                        return new DateTime(Year, Month, 1);
                    }

                    /// <summary>
                    /// 現在の年月を取得します
                    /// </summary>
                    public static YearMonth Now => FromDateTime(DateTime.Now);

                    /// <summary>
                    /// UTC現在の年月を取得します
                    /// </summary>
                    public static YearMonth UtcNow => FromDateTime(DateTime.UtcNow);

                    /// <summary>
                    /// 年月をYYYY/MM形式の文字列に変換します
                    /// </summary>
                    public override string ToString() {
                        return $"{Year:0000}/{Month:00}";
                    }

                    /// <summary>
                    /// 2つの年月を比較します
                    /// </summary>
                    public int CompareTo(YearMonth other) {
                        return _value.CompareTo(other._value);
                    }

                    /// <summary>
                    /// 2つの年月が等しいかどうかを判定します
                    /// </summary>
                    public bool Equals(YearMonth other) {
                        return _value == other._value;
                    }

                    /// <summary>
                    /// オブジェクトが等しいかどうかを判定します
                    /// </summary>
                    public override bool Equals(object? obj) {
                        return obj is YearMonth yearMonth && Equals(yearMonth);
                    }

                    /// <summary>
                    /// ハッシュコードを取得します
                    /// </summary>
                    public override int GetHashCode() {
                        return _value.GetHashCode();
                    }

                    /// <summary>
                    /// 等価演算子
                    /// </summary>
                    public static bool operator ==(YearMonth left, YearMonth right) {
                        return left.Equals(right);
                    }

                    /// <summary>
                    /// 非等価演算子
                    /// </summary>
                    public static bool operator !=(YearMonth left, YearMonth right) {
                        return !left.Equals(right);
                    }

                    /// <summary>
                    /// 比較演算子（より大きい）
                    /// </summary>
                    public static bool operator >(YearMonth left, YearMonth right) {
                        return left.CompareTo(right) > 0;
                    }

                    /// <summary>
                    /// 比較演算子（より小さい）
                    /// </summary>
                    public static bool operator <(YearMonth left, YearMonth right) {
                        return left.CompareTo(right) < 0;
                    }

                    /// <summary>
                    /// 比較演算子（以上）
                    /// </summary>
                    public static bool operator >=(YearMonth left, YearMonth right) {
                        return left.CompareTo(right) >= 0;
                    }

                    /// <summary>
                    /// 比較演算子（以下）
                    /// </summary>
                    public static bool operator <=(YearMonth left, YearMonth right) {
                        return left.CompareTo(right) <= 0;
                    }

                    /// <summary>
                    /// 明示的な型変換演算子（int -> YearMonth）
                    /// </summary>
                    public static explicit operator YearMonth(int value) {
                        return new YearMonth(value);
                    }

                    /// <summary>
                    /// 明示的な型変換演算子（YearMonth -> int）
                    /// </summary>
                    public static explicit operator int(YearMonth yearMonth) {
                        return yearMonth._value;
                    }

                    /// <summary>
                    /// JSON変換用のコンバーター
                    /// </summary>
                    [JsonConverter(typeof(YearMonthJsonConverter))]
                    public class YearMonthJsonConverter : JsonConverter<YearMonth> {
                        public override YearMonth Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
                            if (reader.TokenType == JsonTokenType.Number) {
                                return new YearMonth(reader.GetInt32());
                            } else if (reader.TokenType == JsonTokenType.String) {
                                string value = reader.GetString() ?? throw new JsonException();
                                if (int.TryParse(value, out int result)) {
                                    return new YearMonth(result);
                                }

                                // YYYY/MM形式の場合
                                if (value.Length == 7 && value[4] == '/') {
                                    int year = int.Parse(value.Substring(0, 4));
                                    int month = int.Parse(value.Substring(5, 2));
                                    return new YearMonth(year, month);
                                }

                                throw new JsonException($"不正な年月形式です: {value}");
                            }

                            throw new JsonException();
                        }

                        public override void Write(Utf8JsonWriter writer, YearMonth value, JsonSerializerOptions options) {
                            writer.WriteNumberValue(value.Value);
                        }
                    }
                }
                """,
            });
        });
    }
}
