using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts.Utility {
    /// <summary>
    /// 自動生成される側のプロジェクトで定義される年月クラス
    /// </summary>
    internal class RuntimeYearMonthClass {
        internal const string CLASS_NAME = "YearMonth";

        internal static string EFCoreConverterClassFullName => $"{CLASS_NAME}.{EFCORE_CONVERTER}";
        private const string EFCORE_CONVERTER = "EFCoreYearMonthConverter";

        internal static SourceFile RenderDeclaring() => new() {
            FileName = "YearMonth.cs",
            RenderContent = ctx => {
                return $$"""
                    namespace {{ctx.Config.RootNamespace}};

                    /// <summary>
                    /// 年月
                    /// </summary>
                    public class {{CLASS_NAME}} {
                        public YearMonth(int year, int month) {
                            // 年、月の範囲チェック
                            if (year < 1 || month < 1 || month > 12) {
                                throw new ArgumentOutOfRangeException($"年月の値が不正です: {year:0000}-{month:00}");
                            }
                            Year = year;
                            Month = month;
                        }
                        public YearMonth(DateTime dateTime) {
                            Year = dateTime.Year;
                            Month = dateTime.Month;
                        }
                        public YearMonth({{RuntimeDateClass.CLASS_NAME}} date) {
                            Year = date.Year;
                            Month = date.Month;
                        }

                        public int Year { get; }
                        public int Month { get; }

                        public DateTime ToDateTime() {
                            return new DateTime(Year, Month, 1); // 日は1日固定
                        }

                        public bool Contains(DateTime dateTime) {
                            return dateTime.Year == Year && dateTime.Month == Month;
                        }
                        public bool Contains(Date date) {
                            return date.Year == Year && date.Month == Month;
                        }

                        public override bool Equals(object? obj) {
                            if (obj is YearMonth other) {
                                return Year == other.Year && Month == other.Month;
                            }
                            return false;
                        }
                        public override int GetHashCode() {
                            return HashCode.Combine(Year, Month);
                        }
                        public override string ToString() {
                            return $"{Year:0000}-{Month:00}";
                        }

                        public static bool operator ==(YearMonth? left, YearMonth? right) {
                            return Equals(left, right);
                        }
                        public static bool operator !=(YearMonth? left, YearMonth? right) {
                            return !Equals(left, right);
                        }
                        public static bool operator <(YearMonth? left, YearMonth? right) {
                            if (left == null || right == null)
                                return false;
                            if (left.Year != right.Year)
                                return left.Year < right.Year;
                            return left.Month < right.Month;
                        }
                        public static bool operator >(YearMonth? left, YearMonth? right) {
                            if (left == null || right == null)
                                return false;
                            if (left.Year != right.Year)
                                return left.Year > right.Year;
                            return left.Month > right.Month;
                        }
                        public static bool operator <=(YearMonth? left, YearMonth? right) {
                            return left < right || left == right;
                        }
                        public static bool operator >=(YearMonth? left, YearMonth? right) {
                            return left > right || left == right;
                        }

                        /// <summary>
                        /// Entity Framework Core 用のDBとC#の型変換定義
                        /// </summary>
                        public class {{EFCORE_CONVERTER}} : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<{{CLASS_NAME}}, int> {
                            public {{EFCORE_CONVERTER}}() : base(
                                yearMonth => (yearMonth.Year * 100) + yearMonth.Month,
                                yyyymm => new {{CLASS_NAME}}(yyyymm / 100, yyyymm % 100)) { }
                        }
                    }
                    """;
            },
        };

        internal static UtilityClass.CustomJsonConverter GetCustomJsonConverter() => new() {
            ConverterClassName = "YearMonthJsonValueConverter",
            ConverterClassDeclaring = $$"""
                class YearMonthJsonValueConverter : JsonConverter<{{CLASS_NAME}}?> {
                    public override {{CLASS_NAME}}? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
                        if (reader.TokenType == JsonTokenType.Null) {
                            return null;
                        } else {
                            var yyyymm = reader.GetInt32();
                            var year = yyyymm / 100;
                            var month = yyyymm % 100;
                            return new {{CLASS_NAME}}(year, month);
                        }
                    }

                    public override void Write(Utf8JsonWriter writer, {{CLASS_NAME}}? value, JsonSerializerOptions options) {
                        if (value == null) {
                            writer.WriteNullValue();
                        } else {
                            writer.WriteNumberValue((value.Year * 100) + value.Month);
                        }
                    }
                }
                """,
        };

        internal static Func<string, string> RenderEFCoreConversion() {
            return modelBuilder => $$"""
                foreach (var entityType in {{modelBuilder}}.Model.GetEntityTypes()) {
                    foreach (var property in entityType.GetProperties()) {
                        if (property.ClrType == typeof({{CLASS_NAME}})) {
                            property.SetValueConverter(new {{CLASS_NAME}}.{{EFCORE_CONVERTER}}()); // {{CLASS_NAME}}型のDBとC#の間の変換処理を定義
                        }
                    }
                }
                """;
        }
    }
}
