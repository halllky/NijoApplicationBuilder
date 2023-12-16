using static HalApplicationBuilder.Features.TemplateTextHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HalApplicationBuilder.DotnetEx;

namespace HalApplicationBuilder.Features.Util {
    /// <summary>
    /// 作ったは良いものの検索処理のEFCoreメソッドの組み立てが面倒なので保留
    /// </summary>
    internal class CustomValueObject {
        internal required string ClassName { get; init; }
        internal required string CLRTypeName { get; init; }
        internal required string ToDbValueFunction { get; init; }
        internal required string FromDbValueFunction { get; init; }
        internal required Func<CodeRenderingContext, string> RenderSourceCode { get; init; }

        internal static IEnumerable<CustomValueObject> Enumerate() {
            yield return Year;
            yield return YearMonth;
            yield return Date;
            yield return Nendo;
        }

        internal static CustomValueObject Year => new() {
            ClassName = "Year",
            CLRTypeName = "int",
            ToDbValueFunction = "v => v.IntValue",
            FromDbValueFunction = "v => new Year(v)",
            RenderSourceCode = ctx => $$"""
                public partial class Year : ValueObject {
                    public Year(int intValue) {
                        IntValue = intValue;
                    }
                    public Year(DateTime datetime) {
                        IntValue = datetime.Year;
                    }

                    public int IntValue { get; }

                    public DateTime Since => new DateTime(IntValue, 1, 1);
                    public DateTime Until => new DateTime(IntValue, 12, 31).Add(DateTime.MaxValue.TimeOfDay);

                    protected override IEnumerable<object?> ValueObjectIdentifiers() {
                        yield return IntValue;
                    }
                }
                """,
        };
        internal static CustomValueObject YearMonth => new() {
            ClassName = "YearMonth",
            CLRTypeName = "int",
            ToDbValueFunction = "v => (v.Year * 100) + v.Month",
            FromDbValueFunction = "v => new YearMonth(v)",
            RenderSourceCode = ctx => $$"""
                public partial class YearMonth : ValueObject {
                    public YearMonth(DateTime datetime) {
                        Year = datetime.Year;
                        Month = datetime.Month;
                    }
                    public YearMonth(int yyyymm) {
                        Year = yyyymm / 100;
                        Month = yyyymm % 100;
                    }

                    public int Year { get; }
                    public int Month { get; }

                    public DateTime Since => new DateTime(Year, Month, 1);
                    public DateTime Until => Year == 9999 && Month == 12
                        ? DateTime.MaxValue
                        : new DateTime(Year, Month, 1).AddMonths(1).AddDays(-1).Add(DateTime.MaxValue.TimeOfDay);

                    protected override IEnumerable<object?> ValueObjectIdentifiers() {
                        yield return Year;
                        yield return Month;
                    }
                }
                """,
        };
        internal static CustomValueObject Date => new() {
            ClassName = "Date",
            CLRTypeName = "DateTime",
            ToDbValueFunction = "v => v.DateTimeValue",
            FromDbValueFunction = "v => new Date(v)",
            RenderSourceCode = ctx => $$"""
                public partial class Date : ValueObject {
                    public Date(DateTime datetime) {
                        DateTimeValue = datetime.Date;
                    }

                    public DateTime DateTimeValue { get; }

                    protected override IEnumerable<object?> ValueObjectIdentifiers() {
                        yield return DateTimeValue;
                    }
                }
                """,
        };
        internal static CustomValueObject Nendo => new() {
            ClassName = "Nendo",
            CLRTypeName = "int",
            ToDbValueFunction = "v => v.IntValue",
            FromDbValueFunction = "v => new Nendo(v)",
            RenderSourceCode = ctx => $$"""
                public partial class Nendo : ValueObject {
                    public Nendo(int intValue) {
                        IntValue = intValue;
                    }
                    public Nendo(DateTime datetime) {
                        IntValue = datetime.Year;
                    }

                    public int IntValue { get; }

                    public DateTime Since => new DateTime(IntValue, 4, 1);
                    public DateTime Until => new DateTime(IntValue + 1, 3, 31).Add(DateTime.MaxValue.TimeOfDay);

                    protected override IEnumerable<object?> ValueObjectIdentifiers() {
                        yield return IntValue;
                    }
                }
                """,
        };


        internal static SourceFile Render() => new SourceFile {
            FileName = "ValueObjects.cs",
            RenderContent = ctx => {
                return $$"""
                    namespace {{ctx.Config.RootNamespace}} {
                        using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

                        public abstract class ValueObject {
                            protected abstract IEnumerable<object?> ValueObjectIdentifiers();
                            public override bool Equals(object? obj) {
                                if (obj == null) return false;
                                var objType = obj.GetType();
                                var thisType = GetType();
                                if (objType != thisType
                                    && !objType.IsSubclassOf(thisType)
                                    && !thisType.IsSubclassOf(objType)) return false;
                                return ValueObjectIdentifiers().SequenceEqual(((ValueObject)obj).ValueObjectIdentifiers());
                            }
                            public override int GetHashCode() {
                                return ValueObjectIdentifiers()
                                    .Select(x => x != null ? x.GetHashCode() : 0)
                                    .DefaultIfEmpty()
                                    .Aggregate((x, y) => x ^ y);
                            }
                            public static bool operator ==(ValueObject? left, ValueObject? right) {
                                if (left is null ^ right is null) return false;
                                return ReferenceEquals(left, right) || left!.Equals(right);
                            }
                            public static bool operator !=(ValueObject? left, ValueObject? right) {
                                return !(left == right);
                            }
                        }

                        {{Enumerate().SelectTextTemplate(vo => WithIndent(vo.RenderSourceCode(ctx), "    "))}}

                    #region DB入出力時変換
                    {{Enumerate().SelectTextTemplate(vo => $$"""
                        public class {{vo.ClassName}}DbValueConverter : ValueConverter<{{vo.ClassName}}, {{vo.CLRTypeName}}> {
                            public {{vo.ClassName}}DbValueConverter() : base(
                                {{vo.ToDbValueFunction}},
                                {{vo.FromDbValueFunction}}) {
                            }
                        }
                    """)}}
                    #endregion
                    }
                    """;
            }
        };

        internal static string RenderDbContextConversionOption() => $$"""
            protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) {
            {{Enumerate().SelectTextTemplate(vo => $$"""
                configurationBuilder
                    .Properties<{{vo.ClassName}}>()
                    .HaveConversion<{{vo.ClassName}}DbValueConverter>();
            """)}}
            }
            """;
    }
}
