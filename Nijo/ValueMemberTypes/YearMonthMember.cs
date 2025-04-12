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

    void IValueMemberType.Validate(XElement element, SchemaParseContext context, Action<XElement, string> addError) {
        // 年月型の検証
        // 必要に応じて年月の範囲制約などを検証するコードをここに追加できます
    }

    ValueMemberSearchBehavior? IValueMemberType.SearchBehavior => new() {
        FilterCsTypeName = $"{FromTo.CS_CLASS_NAME}<YearMonth?>",
        FilterTsTypeName = "{ from?: number; to?: number }",
        RenderTsNewObjectFunctionValue = () => "{ from: undefined, to: undefined }",
        RenderFiltering = ctx => {
            var fullpath = ctx.Member.GetPathFromEntry().ToArray();
            var pathFromSearchCondition = fullpath.AsSearchConditionFilter(E_CsTs.CSharp).ToArray();
            var whereFullpath = fullpath.AsSearchResult().ToArray();
            var fullpathNullable = $"{ctx.SearchCondition}.{pathFromSearchCondition.Join("?.")}";
            var fullpathNotNull = $"{ctx.SearchCondition}.{pathFromSearchCondition.Join(".")}";
            var isArray = fullpath.Any(node => node is ChildrenAggreagte);

            return $$"""
                if ({{fullpathNullable}}?.From != null && {{fullpathNullable}}?.To != null) {
                    var from = {{this.RenderCastToPrimitiveType(true)}}{{fullpathNotNull}}.From;
                    var to = {{this.RenderCastToPrimitiveType(true)}}{{fullpathNotNull}}.To;
                {{If(isArray, () => $$"""
                    {{ctx.Query}} = {{ctx.Query}}.Where(x => x.{{whereFullpath.SkipLast(1).Join(".")}}.Any(y => y.{{ctx.Member.PhysicalName}} >= from && y.{{ctx.Member.PhysicalName}} <= to));
                """).Else(() => $$"""
                    {{ctx.Query}} = {{ctx.Query}}.Where(x => x.{{whereFullpath.Join(".")}} >= from && x.{{whereFullpath.Join(".")}} <= to);
                """)}}
                } else if ({{fullpathNullable}}?.From != null) {
                    var from = {{this.RenderCastToPrimitiveType(true)}}{{fullpathNotNull}}.From;
                {{If(isArray, () => $$"""
                    {{ctx.Query}} = {{ctx.Query}}.Where(x => x.{{whereFullpath.SkipLast(1).Join(".")}}.Any(y => y.{{ctx.Member.PhysicalName}} >= from));
                """).Else(() => $$"""
                    {{ctx.Query}} = {{ctx.Query}}.Where(x => x.{{whereFullpath.Join(".")}} >= from);
                """)}}
                } else if ({{fullpathNullable}}?.To != null) {
                    var to = {{this.RenderCastToPrimitiveType(true)}}{{fullpathNotNull}}.To;
                {{If(isArray, () => $$"""
                    {{ctx.Query}} = {{ctx.Query}}.Where(x => x.{{whereFullpath.SkipLast(1).Join(".")}}.Any(y => y.{{ctx.Member.PhysicalName}} <= to));
                """).Else(() => $$"""
                    {{ctx.Query}} = {{ctx.Query}}.Where(x => x.{{whereFullpath.Join(".")}} <= to);
                """)}}
                }
                """;
        },
    };

    void IValueMemberType.RegisterDependencies(IMultiAggregateSourceFileManager ctx) {
        // 特になし
    }

    string IValueMemberType.RenderCreateDummyDataValueBody(CodeRenderingContext ctx) {
        return $$"""
            var now = DateTime.Now;
            return (int)new YearMonth(now.Year, now.Month);
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
                    /// 数値から明示的に変換
                    /// </summary>
                    public static explicit operator YearMonth(int value) => new(value);

                    /// <summary>
                    /// 数値へ明示的に変換
                    /// </summary>
                    public static explicit operator int(YearMonth yearMonth) => yearMonth._value;
                }

                /// <summary>
                /// YearMonth型のJsonコンバーター
                /// </summary>
                public class YearMonthJsonConverter : JsonConverter<YearMonth> {
                    public override YearMonth Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
                        if (reader.TokenType == JsonTokenType.Number) {
                            return (YearMonth)reader.GetInt32();
                        }
                        if (reader.TokenType == JsonTokenType.String) {
                            var str = reader.GetString();
                            return int.TryParse(str, out var value)
                                ? (YearMonth)value
                                : throw new JsonException($"YearMonthの形式が不正です: {str}");
                        }
                        throw new JsonException("YearMonthの形式が不正です");
                    }

                    public override void Write(Utf8JsonWriter writer, YearMonth value, JsonSerializerOptions options) {
                        writer.WriteNumberValue((int)value);
                    }
                }
                """
            });
        });
    }
}
