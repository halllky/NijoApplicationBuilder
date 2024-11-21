using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts.Utility {
    /// <summary>
    /// 自動生成される側のプロジェクトで定義される日付クラス
    /// </summary>
    internal class RuntimeDateClass {
        internal const string CLASS_NAME = "Date";

        internal static string EFCoreConverterClassFullName => $"{CLASS_NAME}.{EFCORE_CONVERTER}";
        private const string EFCORE_CONVERTER = "EFCoreDateConverter";

        internal static SourceFile RenderDeclaring() => new() {
            FileName = "Date.cs",
            RenderContent = ctx => {
                return $$"""
                    namespace {{ctx.Config.RootNamespace}};

                    /// <summary>
                    /// <see cref="DateTime"/> から時分秒を除いた日付型。
                    /// 通常の <see cref="DateTime"/> を使った場合とで何か挙動が変わるといったことはない。
                    /// 例えば範囲検索で時分秒を除外し忘れるなどといった実装ミスを減らすことを目的としている。
                    /// </summary>
                    public partial class {{CLASS_NAME}} {
                        public Date(int year, int month, int day) {
                            // 年、月、日の範囲チェック
                            if (year < 1 || month < 1 || month > 12 || day < 1 || day > DateTime.DaysInMonth(year, month)) {
                                throw new ArgumentOutOfRangeException($"日付の値が不正です: {year:0000}-{month:00}-{day:00}");
                            }
                            Year = year;
                            Month = month;
                            Day = day;
                        }
                        public Date(DateTime dateTime) {
                            Year = dateTime.Year;
                            Month = dateTime.Month;
                            Day = dateTime.Day;
                        }

                        public int Year { get; }
                        public int Month { get; }
                        public int Day { get; }

                        public DateTime ToDateTime() {
                            return new DateTime(Year, Month, Day);
                        }

                        public override bool Equals(object? obj) {
                            if (obj is Date other) {
                                return Year == other.Year && Month == other.Month && Day == other.Day;
                            }
                            return false;
                        }
                        public override int GetHashCode() {
                            return HashCode.Combine(Year, Month, Day);
                        }
                        public override string ToString() {
                            return $"{Year:0000}-{Month:00}-{Day:00}";
                        }

                        public static bool operator ==(Date? left, Date? right) {
                            return Equals(left, right);
                        }
                        public static bool operator !=(Date? left, Date? right) {
                            return !Equals(left, right);
                        }
                        public static bool operator <(Date? left, Date? right) {
                            if (left == null || right == null)
                                return false;
                            if (left.Year != right.Year)
                                return left.Year < right.Year;
                            if (left.Month != right.Month)
                                return left.Month < right.Month;
                            return left.Day < right.Day;
                        }
                        public static bool operator >(Date? left, Date? right) {
                            if (left == null || right == null)
                                return false;
                            if (left.Year != right.Year)
                                return left.Year > right.Year;
                            if (left.Month != right.Month)
                                return left.Month > right.Month;
                            return left.Day > right.Day;
                        }
                        public static bool operator <=(Date? left, Date? right) {
                            return left < right || left == right;
                        }
                        public static bool operator >=(Date? left, Date? right) {
                            return left > right || left == right;
                        }

                        /// <summary>
                        /// Entity Framework Core 用のDBとC#の型変換定義
                        /// </summary>
                        public class {{EFCORE_CONVERTER}} : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<{{CLASS_NAME}}, DateTime> {
                            public {{EFCORE_CONVERTER}}() : base(
                                date => date.ToDateTime(),
                                datetime => new Date(datetime)) { }
                        }
                    }
                    """;
            }
        };

        internal static UtilityClass.CustomJsonConverter GetCustomJsonConverter() => new() {
            ConverterClassName = $"{Parts.Utility.UtilityClass.CUSTOM_CONVERTER_NAMESPACE}.DateJsonValueConverter",
            ConverterClassDeclaring = $$"""
                class DateJsonValueConverter : JsonConverter<{{CLASS_NAME}}?> {
                    public override {{CLASS_NAME}}? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
                        var strDateTime = reader.GetString();
                        return string.IsNullOrWhiteSpace(strDateTime)
                            ? null
                            : new {{CLASS_NAME}}(DateTime.Parse(strDateTime));
                    }

                    public override void Write(Utf8JsonWriter writer, {{CLASS_NAME}}? value, JsonSerializerOptions options) {
                        if (value == null) {
                            writer.WriteNullValue();
                        } else {
                            writer.WriteStringValue(value.ToString());
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
